using System;
using _Project.Cameras;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.BrightnessState;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Combat.HitObjects;
using _Project.InputSystem;
using _Project.Maps.Climber;
using _Project.UI.InGame;
using Cysharp.Threading.Tasks;
using Renge.PPB;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter
{
    public class PlayerCharacter : IngameCharacter
    {
        [SerializeField, TitleGroup("CameraTarget")] private CameraTarget cameraTarget;
        private ProceduralProgressBar progressBar;
        private int stateChangeFrameCount;
        private Vector3 PrevCamTargetMoveValue { get; set; }
        public bool IsStealthMove => MoveParams.IsStealthMove;
        private ChargingUI[] chargingUIs;
        protected override void Awake()
        {
            base.Awake();
            chargingUIs = GetComponentsInChildren<ChargingUI>(true);
            progressBar = GetComponentInChildren<ProceduralProgressBar>();
            
            Stat.OnHealthChange?.AddListener((value)=> progressBar.Value = value);
        }

        protected override Vector3 Velocity
        {
            get
            {
                var velocity = CurrentMovementState.Velocity;
                velocity += CurrentActionState.GetVelocity();
                velocity += CurrentActionState.Acc * Time.deltaTime;
                
                velocity += CurrentMovingPlatform ? CurrentMovingPlatform.Velocity : Vector3.zero;
                velocity += CurrentRollingCube ? CurrentRollingCube.Velocity : Vector3.zero;
                velocity += MoveParams.Acceleration * Time.deltaTime;
                return velocity;
            }
        }

        protected override void Update()
        {
            if (!IsDying && !IsDead)
            {
                foreach (var chargingUI in chargingUIs)
                {
                    if (chargingUI.gameObject.activeSelf) chargingUI.OnUpdate(transform.position);
                }
            }
            
            if (CurrentLevel)
            {
            
                switch (CurrentLevel.LevelType)
                {
                    case Level.Type.Normal:
                    {
                        commonStateConductor.TrySetBrightnessState(BrightnessStateContainer[BrightnessState.StateType.Normal]);
                        break;
                    }
                    case Level.Type.Dark:
                    {
                        if (CurrentSpotLight) commonStateConductor.TrySetBrightnessState(BrightnessStateContainer[BrightnessState.StateType.Normal]);
                        else commonStateConductor.TrySetBrightnessState(BrightnessStateContainer[BrightnessState.StateType.Dark]);
                        break;
                    }
                }
                
                foreach (var currentLevelMovingPlatform in CurrentLevel.MovingPlatforms)
                {
                    currentLevelMovingPlatform.Move();
                }

                foreach (var rollingCube in CurrentLevel.RollingCubes)
                {
                    rollingCube.Work(transform);
                }
            }
            
            if (MoveParams.IsClimbable && !MoveParams.IsClimbButtonPressed && !GroundParams.IsGrounded && VerticalParams.IsEdgeOfPlatform)
            {
                animationStateConductor.TrySetMovementState(movementStateContainer[MovementState.StateType.Hang]);
            }
            
            base.Update();
            
            CurrentLockState.UpdateLockOnMarker();
            
            ReviseCameraTarget();

            if (CurrentBrightnessState is DarkState darkState && darkState.IsEliminationPhase)
            {
                MoveToSavePoint();
            }
            
            
            return;
            void ReviseCameraTarget()
            {
                if (CurrentLockState.Type == LockState.StateType.LockOn)
                {
                    var camMoveValue = CurrentLockState.CameraTargetUpdate();
                    cameraTarget?.Move(camMoveValue);
                    return;
                }
                if (IsJustStateChanged)
                {
                    // Debug.Log(CurrentActionState);
                    stateChangeFrameCount = 1;
                }
            
                var currentMoveValue = CurrentMovementState.CameraTargetUpdate();
            
                if (0 < stateChangeFrameCount && stateChangeFrameCount < 3)
                {
                    if (Mathf.Abs(PrevCamTargetMoveValue.magnitude - currentMoveValue.magnitude) > 0.2f)
                    {
                        currentMoveValue = currentMoveValue.normalized * Mathf.Clamp(currentMoveValue.magnitude, 0, PrevCamTargetMoveValue.magnitude);
                        // currentMoveValue = currentMoveValue.normalized * Mathf.Clamp(currentMoveValue.magnitude, 0, 0.2f);
                    }
                
                    stateChangeFrameCount++;
                }
                else
                {
                    stateChangeFrameCount = 0;
                }

                cameraTarget?.Move(currentMoveValue);
            
                PrevCamTargetMoveValue = currentMoveValue;
            }
        }

        public override void MoveToSavePoint()
        {
            gameObject.SetActive(false);
            transform.position = SavePoint.transform.position;
            gameObject.SetActive(true);
            
            base.MoveToSavePoint();
            
            CurrentLevel.ResetLevel();
            commonStateConductor.TrySetLockState(LockStateContainer[LockState.StateType.LockOff]);
            cameraTarget?.ResetRotationAsync(SavePoint.transform.forward);
        }

        public override void Die()
        {
            base.Die();
            WaitAndRespawn().Forget();
            
            return;
            async UniTaskVoid WaitAndRespawn()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(4));
                IsDead = false;
                MoveToSavePoint();
                MoveParams.ResetCrowdControlled();
                characterControllerEnveloper.ResetCharacterController();
                Stat.CurrentHealth = 100;
                
                animationStateConductor.ForceSetActionState(actionStateContainer[ActionState.StateType.ActionIdle]);
            }
        }

        private void OnGUI()
        {
            var i = 0;
            guiStyle.fontSize = (int)(Screen.height * 0.02f);
            guiStyle.normal.textColor = Color.gray;
            
            DrawLabel("PlayTime : " + (int)Time.realtimeSinceStartup + "    " + FPS);
            // DrawLabel(CurrentLevel + ", LevelType : " + CurrentLevel.LevelType);
            // DrawLabel(CurrentSpotLight?.ToString());
            // DrawLabel(CurrentBrightnessState.Type + " " + (int)CurrentBrightnessState.StateTime);
            // DrawLabel(CurrentLockState.Type + " " + (int)CurrentLockState.StateTime);
            DrawLabel(CurrentMovementState.Type + ", " + (int)CurrentMovementState.StateTime);
            // DrawLabel("VerticalParams.IsEdgeOfPlatform : " + VerticalParams.IsEdgeOfPlatform);
            // DrawLabel("GroundParams.IsGrounded : " + GroundParams.IsGrounded);
            // DrawLabel("GroundParams.IsGroundedOnCharacter : " + GroundParams.IsGroundedOnCharacter);
            // DrawLabel("MoveParams.HasMovingPlatform : " + MoveParams.HasMovingPlatform);
            // DrawLabel("CurrentLevel : " + CurrentLevel);
            // DrawLabel("CurrentMovingPlatform : " + CurrentMovingPlatform);
            // DrawLabel("CurrentRollingCube : " + CurrentRollingCube);
            // DrawLabel("characterController.isGrounded : " + characterController.isGrounded);
            // DrawLabel("GroundParams.IsGrounded : " + GroundParams.IsGrounded);
            // DrawLabel("GroundParams.PrevIsGrounded : " + GroundParams.PrevIsGrounded);
            // DrawLabel("GroundParams.GroundNormal : " + GroundParams.GroundNormal);
            // DrawLabel("GroundParams.GroundPoint : " + GroundParams.GroundPoint);
            // DrawLabel("GroundParams.SlopeAngleDeg : " + GroundParams.SlopeAngleDeg);
            // DrawLabel("GroundParams.SlopeAngleRad : " + GroundParams.SlopeAngleRad);
            // DrawLabel("");
            DrawLabel("VerticalParams.IsWalled : " + VerticalParams.IsWalled);
            // DrawLabel("VerticalParams.IsWallPerpendicularToGround : " + VerticalParams.IsWallPerpendicularToGround);
            // DrawLabel("VerticalParams.WallNormal : " + VerticalParams.WallNormal);
            // DrawLabel("VerticalParams.WallPoint : " + VerticalParams.WallPoint);
            // DrawLabel("VerticalParams.IsSightOpened : " + VerticalParams.IsSightOpened);
            // DrawLabel("VerticalParams.IsRightSightOpened : " + VerticalParams.IsRightSightOpened);   
            // DrawLabel("VerticalParams.IsRightLedgeMovable : " + VerticalParams.IsRightLedgeMovable);
            // DrawLabel("VerticalParams.IsLeftSightOpened : " + VerticalParams.IsLeftSightOpened);
            // DrawLabel("VerticalParams.IsLeftLedgeMovable : " + VerticalParams.IsLeftLedgeMovable);
            // DrawLabel("");
            DrawLabel("MoveParams.Gravity : " + MoveParams.Gravity.magnitude);
            DrawLabel("MoveParams.GravityTime : " + MoveParams.GravityTime);
            // DrawLabel("MoveParams.IsClimbing : " + MoveParams.IsClimbing);
            // DrawLabel("MoveParams.IsClimbable : " + MoveParams.IsClimbable);
            // DrawLabel("MoveParams.ClimbStamina : " + MoveParams.ClimbStaminaTime);
            // DrawLabel("");
            // DrawLabel("MoveParams.JumpCount : " + MoveParams.JumpCount);
            // DrawLabel("MoveParams.WallJumpCount : " + MoveParams.WallJumpCount);
            // DrawLabel("MoveParams.KickCount : " + MoveParams.KickCount);
            // DrawLabel("MoveParams.IsWallJumpable : " + MoveParams.IsWallJumpable);
            // DrawLabel("MoveParams.Acceleration : " + MoveParams.Acceleration);
            // DrawLabel("");
            
        
            void DrawLabel(string msg)
            {
                GUI.Label(new Rect(0, i++ * 20, 500, guiStyle.fontSize * 1.1f), msg, guiStyle);
            }
        } 
    }
}