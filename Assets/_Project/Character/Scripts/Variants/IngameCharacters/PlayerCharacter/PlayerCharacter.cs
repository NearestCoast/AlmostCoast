using System;
using System.Linq;
using System.Threading;
using _Project.Cameras;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.BrightnessState;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Managers.Scripts._Core.SaveManager;
using _Project.Maps.Climber;
using _Project.Maps.Climber.Objects;
using _Project.Maps.Climber.Objects.Collectables;
using _Project.UI.InGame;
using Cysharp.Threading.Tasks;
using Renge.PPB;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter
{
    public class PlayerCharacter : IngameCharacter, ISavable
    {
        [SerializeField, TitleGroup("CameraTarget")] private CameraTarget cameraTarget;
        
        private int stateChangeFrameCount;
        private Vector3 PrevCamTargetMoveValue { get; set; }
        public bool IsStealthMove => MoveParams.IsStealthMove;
        private ChargingUI[] chargingUIs;

        protected override void Awake()
        {
            base.Awake();
            chargingUIs = GetComponentsInChildren<ChargingUI>(true);
            
            cts = new CancellationTokenSource();
        }

        protected override void Start()
        {
            base.Start();
            
            CurrentLevel?.StartLevel();
        }

        protected override Vector3 Velocity
        {
            get
            {
                var velocity = CurrentMovementState.Velocity;
#if UNITY_EDITOR
                if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
                {
                    Debug.Log(velocity);
                }
#endif
                
                velocity += CurrentActionState.GetVelocity();
#if UNITY_EDITOR
                if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
                {
                    Debug.Log(velocity);
                }
#endif
                velocity += CurrentActionState.Acc * Time.deltaTime;
#if UNITY_EDITOR
                if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
                {
                    Debug.Log(velocity);
                }
#endif
                
                velocity += CurrentMovingPlatform ? CurrentMovingPlatform.Velocity : Vector3.zero;
#if UNITY_EDITOR
                if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
                {
                    Debug.Log(velocity);
                }
#endif
                velocity += CurrentRollingCube ? CurrentRollingCube.Velocity : Vector3.zero;
#if UNITY_EDITOR
                if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
                {
                    Debug.Log(velocity);
                }
#endif
                velocity += MoveParams.Acceleration * Time.deltaTime;
#if UNITY_EDITOR
                if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z))
                {
                    Debug.Log(velocity);
                }
#endif
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
            
            if (MoveParams.IsClimbable && !MoveParams.IsClimbButtonPressed && !GroundParams.IsGrounded && VerticalParams.IsEdgeOfPlatformFromTop)
            {
                if (movementStateContainer.ContainsKey(MovementState.StateType.Hang))
                {
                    animationStateConductor.TrySetMovementState(movementStateContainer[MovementState.StateType.Hang]);
                }
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
                // if (IsJustStateChanged) Debug.Log(CurrentMovementState);
            }
        }
        
        private CancellationTokenSource cts;
        private bool isApplicationQuitting = false;
        public override async void MoveToSavePoint()
        {
            var curtainUI = FindAnyObjectByType<CurtainUI>();

            try
            {
                // FadeOut 호출 시 CancellationToken 전달
                await curtainUI.FadeOut(cts.Token);

                if (isApplicationQuitting) return;
                CharacterControllerEnveloper.OnSpawn();

                gameObject.SetActive(false);
                transform.position = SavePoint.transform.position;
                gameObject.SetActive(true);

                base.MoveToSavePoint();

                CurrentLevel.ResetLevel();
                commonStateConductor.TrySetLockState(LockStateContainer[LockState.StateType.LockOff]);
                cameraTarget?.ResetRotationAsync(SavePoint.transform.forward);

                // FadeIn 호출 시 CancellationToken 전달
                await curtainUI.FadeIn(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("MoveToSavePoint cancelled due to application quit.");
            }
        }

        [SerializeField] private SavePoint initialSavePoint;
        public SavePoint InitialSavePoint
        {
            get => initialSavePoint;
            set => initialSavePoint = value;
        }

        public override void SetDying()
        {
            base.SetDying();
            
            var saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
            saveLoadManager.SaveGame();
        }

        public override void Die()
        {
            base.Die();
            
            WaitAndRespawn().Forget();
            
            return;
            async UniTaskVoid WaitAndRespawn()
            {
                var curtainUI = FindAnyObjectByType<CurtainUI>();
                await curtainUI.FadeOut(cts.Token);
                
                var currentSceneName = SceneManager.GetActiveScene().name;
                await SceneManager.LoadSceneAsync(currentSceneName);

                return;
                IsDead = false;
                SavePoint = initialSavePoint;
                MoveToSavePoint();
                MoveParams.ResetCrowdControlled();
                characterControllerEnveloper.ResetCharacterController();
                Stat.CurrentHealth = 100;
                
                animationStateConductor.ForceSetActionState(actionStateContainer[ActionState.StateType.ActionIdle]);
                
                // 현재 Scene 이름 가져오기
                

                // 현재 Scene 다시 로드
                // SceneManager.LoadScene(currentSceneName);
                
            }
        }
        
        

        [SerializeField] private int silverKeyAmount;
        [SerializeField] private int goldKeyAmount;

        public int SilverKeyAmount => silverKeyAmount;
        public int GoldKeyAmount => goldKeyAmount;

        public void AddKey(Key.Type keyType)
        {
            switch (keyType)
            {
                case Key.Type.Silver:
                {
                    silverKeyAmount += 1;
                    break;
                }
                case Key.Type.Gold:
                {
                    goldKeyAmount += 1;
                    break;
                }
            }
            
        }

        public void TryUseKey(Key.Type keyType, out bool success)
        {
            success = false;
            switch (keyType)
            {
                case Key.Type.Silver:
                {
                    if (silverKeyAmount > 0)
                    {
                        success = true;
                        silverKeyAmount -= 1;
                    }
                    
                    break;
                }
                case Key.Type.Gold:
                {
                    if (goldKeyAmount > 0)
                    {
                        success = true;
                        goldKeyAmount -= 1;
                    }
                    break;
                }
            }
        }
        
        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel(); // 작업 취소
            }
        }

        private void OnDestroy()
        {
            cts?.Cancel(); // 작업 취소
            cts?.Dispose(); // CancellationTokenSource 정리
        }

        private void OnGUI()
        {
            var i = 0;
            guiStyle.fontSize = (int)(Screen.height * 0.02f);
            guiStyle.normal.textColor = Color.gray;
            
            DrawLabel("");
            DrawLabel("");
            DrawLabel("");
            DrawLabel("");
            DrawLabel("");
            DrawLabel("PlayTime : " + (int)Time.realtimeSinceStartup + "    " + FPS);
            if (CurrentLevel) DrawLabel(CurrentLevel.ID + ", LevelType : " + CurrentLevel.LevelType);
            // DrawLabel(CurrentSpotLight?.ToString());
            // DrawLabel(CurrentBrightnessState.Type + " " + (int)CurrentBrightnessState.StateTime);
            // DrawLabel(CurrentLockState.Type + " " + (int)CurrentLockState.StateTime);
            DrawLabel(CurrentMovementState.Type + ", " + (int)CurrentMovementState.StateTime);
            // DrawLabel("MoveParams.MaxWallJumpCount : " + MoveParams.MaxWallJumpCount);
            // DrawLabel("MoveParams.JumpCount : " + MoveParams.JumpCount);
            DrawLabel("MoveParams.WallJumpCount : " + MoveParams.WallJumpCount + "/" + MoveParams.MaxWallJumpCount);
            // DrawLabel("MoveParams.ClimbJumpCount : " + MoveParams.ClimbJumpCount);
            // DrawLabel("VerticalParams.IsEdgeOfPlatform : " + VerticalParams.IsEdgeOfPlatform);
            DrawLabel("GroundParams.IsGrounded : " + GroundParams.IsGrounded);
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
            // DrawLabel("VerticalParams.IsWalled : " + VerticalParams.IsWalled);
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

        public bool EnrollToSaveManager => true;
        public bool Save(string saveFileName)
        {
            var position = IsDying ? Vector3.zero : transform.position;
            ISavable.EasySave("PlayerPosition", position, saveFileName);
            ISavable.EasySave("SavePoint", SavePoint, saveFileName);
            ISavable.EasySave("CurrentLevel ID", CurrentLevel.ID, saveFileName);
            
            return true;
        }

        public bool Load(string saveFileName)
        {
            gameObject.SetActive(false);
            transform.position = ISavable.EasyLoad<Vector3>("PlayerPosition", saveFileName);
            SavePoint = ISavable.EasyLoad<SavePoint>("SavePoint", saveFileName);
            
            var levelID = ISavable.EasyLoad<string>("CurrentLevel ID", saveFileName);
            var levels = FindObjectsByType<Level>(FindObjectsSortMode.None);
            CurrentLevel = levels.ToList().Find(x => x.ID == levelID); 
            
            gameObject.SetActive(true);
            
            return true;
        }

    }
}