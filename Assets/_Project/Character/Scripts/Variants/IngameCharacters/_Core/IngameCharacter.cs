using System;
using _Project._Core;
using _Project.Cameras;
using _Project.Character.IngameCharacters.Enemies;
using _Project.Characters._Core.States.AnimationStates;
using _Project.Characters._Core.States.CommonStates;
using _Project.Characters._Core.EnvironmentCheckers;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.BrightnessState;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Combat.HitObjects;
using _Project.Maps.Climber;
using _Project.Maps.Climber.Objects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Characters.IngameCharacters.Core
{
    public partial class IngameCharacter : _Project.Characters._Core.Character
    {
        [SerializeField] private bool debug;
        public bool DebugCharacter => debug;
        
        protected IEnvironmentChecker[] environmentCheckers;
        protected CharacterControllerEnveloper characterControllerEnveloper;

        public CharacterControllerEnveloper CharacterControllerEnveloper => characterControllerEnveloper;

        protected AnimationStateConductor animationStateConductor;
        protected CommonStateConductor commonStateConductor;

        protected MovementStateContainer movementStateContainer;
        protected ActionStateContainer actionStateContainer;
        protected BrightnessStateContainer brightnessStateContainer;
        protected LockStateContainer lockStateContainer;

        public MovementStateContainer MovementStateContainer => movementStateContainer;
        public ActionStateContainer ActionStateContainer => actionStateContainer;
        public BrightnessStateContainer BrightnessStateContainer => brightnessStateContainer;
        public LockStateContainer LockStateContainer => lockStateContainer;

        public MovementState CurrentMovementState => animationStateConductor.CurrentMovementState;
        public ActionState CurrentActionState => animationStateConductor.CurrentActionState;

        protected BrightnessState CurrentBrightnessState => commonStateConductor.CurrentBrightnessState as BrightnessState;

        protected LockState CurrentLockState => commonStateConductor.CurrentLockState as LockState;

        protected MovementState PrevMovementState { get; set; }

        public MoveParams MoveParams { get; private set; }
        protected VerticalParams VerticalParams { get; private set; }
        protected GroundParams GroundParams { get; private set; }
        protected LockParams LockParams { get; private set; }

        public SavePoint SavePoint { get; set; }

        protected override void Awake()
        {
            base.Awake();

            environmentCheckers = GetComponentsInChildren<IEnvironmentChecker>();
            characterControllerEnveloper = GetComponent<CharacterControllerEnveloper>();

            animationStateConductor = GetComponent<AnimationStateConductor>();
            commonStateConductor = GetComponent<CommonStateConductor>();

            movementStateContainer = GetComponentInChildren<MovementStateContainer>();
            actionStateContainer = GetComponentInChildren<ActionStateContainer>();
            brightnessStateContainer = GetComponentInChildren<BrightnessStateContainer>();
            lockStateContainer = GetComponentInChildren<LockStateContainer>();

            MoveParams = GetComponent<MoveParams>();
            VerticalParams = GetComponent<VerticalParams>();
            GroundParams = GetComponent<GroundParams>();
            LockParams = GetComponent<LockParams>();

            wallSoundSource = GetComponent<AudioSource>();
        }

        protected override void Start()
        {
            base.Start();
            MoveParams.ResetJumpCount();
            MoveParams.ResetClimbStamina();

            // Application.targetFrameRate = -1;
            // Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

            // animationStateConductor.TrySetActionState(actionStateContainer[ActionState.StateType.AttackIdle]);
            // commonStateConductor.ForceSetLockState(LockStateContainer[LockState.StateType.LockOff]);
        }

        protected bool IsJustStateChanged { get; set; }

        private AudioSource wallSoundSource;
        [SerializeField, TitleGroup("Fx")] private float wallSoundEndTime = 0.5f;

        protected virtual void Update()
        {
            if (IsMovingToSavePoint) return;

            CheckEnvironments();
            CurrentMovementState.ProgressStateTime();
            CurrentBrightnessState?.ProgressStateTime();

            if (!GroundParams.PrevIsGrounded && GroundParams.IsGrounded)
            {
                animationStateConductor.TrySetMovementState(movementStateContainer[MovementState.StateType.Landing]);
            }

            if (GroundParams.IsGroundedOnCharacter && !IsDying && !IsDead)
            {
                MoveParams.SetIsHeadJumping();
                animationStateConductor.ForceSetMovementState(movementStateContainer[MovementState.StateType.Jump]);
            }

            if (!VerticalParams.PrevIsWalled && VerticalParams.IsWalled && !GroundParams.IsGrounded)
            {
                PlayAudioSegment();
            }

            SetAttackMask();
            var velocity = Velocity;
            if (!float.IsNaN(velocity.x) && !float.IsNaN(velocity.y) && !float.IsNaN(velocity.z))
            {
                characterControllerEnveloper.Move(velocity);
            }
            else
            {
                Debug.Log("### Velocity Nan Happened. " + velocity);
                Debug.Log("CurrentMovementState : " + CurrentMovementState);
                Debug.Log("CurrentActionState : " + CurrentActionState);
            }

            if (!IsDying) transform.rotation = CurrentMovementState.Rotation;

            if (MoveParams.IsJustLedgeMoveEnded) MoveParams.ResetIsJustLedgeMoveEnded();

            IsJustStateChanged = PrevMovementState != CurrentMovementState;
            // if (IsJustStateChanged) Debug.Log(CurrentMovementState);

            PrevMovementState = CurrentMovementState;

            CalculateFPS();


            void PlayAudioSegment()
            {
                wallSoundSource.Play();

                // 일정 시간이 지나면 재생 중지
                Invoke(nameof(StopAudio), wallSoundEndTime);
            }

            void SetAttackMask()
            {
                if (CurrentActionState.Type == ActionState.StateType.ActionIdle)
                {
                    animationStateConductor.SetActionMaskNoBody();
                }
                else
                {
                    if (CurrentMovementState.Type == MovementState.StateType.Idle || CurrentMovementState.Type == MovementState.StateType.Landing)
                    {
                        animationStateConductor.SetActionMaskFullBody();
                    }
                    else if (CurrentActionState.Type == ActionState.StateType.SpecialAttack_01)
                    {
                        animationStateConductor.SetActionMaskFullBody();
                    }
                    else animationStateConductor.SetActionMaskUpperBody();
                }
            }
        }

        protected virtual Vector3 Velocity { get; } = Vector3.zero;

        private void StopAudio()
        {
            wallSoundSource.Stop();
        }
    }

    public partial class IngameCharacter
    {
        private void CheckEnvironments()
        {
            foreach (var environmentChecker in environmentCheckers)
            {
                environmentChecker.Check(CurrentMovementState);
            }
        }
    }

    public partial class IngameCharacter
    {
        private MovingPlatform currentMP;

        public MovingPlatform CurrentMovingPlatform
        {
            get => currentMP;
            set
            {
                currentMP = value;
                MoveParams.HasMovingPlatform = value;
            }
        }

        public RollingCube CurrentRollingCube { get; set; }
        public SpotLight CurrentSpotLight { get; set; }
    }

    public partial class IngameCharacter
    {
        public Level CurrentLevel { get; set; }

        public void ForceSetIdle()
        {
            animationStateConductor.ForceSetMovementState(movementStateContainer[MovementState.StateType.Idle]);
        }

        private bool IsMovingToSavePoint { get; set; }

        public virtual void MoveToSavePoint()
        {
            IsMovingToSavePoint = true; // 시작 시 true로 설정
            ResetIsMovingToSavePointAfterDelay(); // 1000ms = 1초 뒤에 false로 설정

            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            ForceSetIdle();

            CurrentRollingCube = null;
            CurrentMovingPlatform = null;

            MoveParams.ResetAcceleration();
            MoveParams.ResetClimbStamina();
            MoveParams.EndClimbing();
            MoveParams.ResetWallJumping();
            MoveParams.ResetHeadJumping();

            MoveParams.ResetJumpCount();
            MoveParams.ResetWallJumpCount();
            MoveParams.ResetKickCount();
            MoveParams.ResetSkillJumpCount();
            
            CharacterControllerEnveloper.OnSpawn();
            
            ResetModelTransform();

            async void ResetIsMovingToSavePointAfterDelay()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                IsMovingToSavePoint = false; // 1초 후 false로 변경
            }

            void ResetModelTransform()
            {
                var modelT = animationStateConductor.Animancer.transform;
                modelT.localPosition = Vector3.zero;
            }
        }

        public HittingInfo HittingInfo { get; private set; }
        
        public override void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            if (IsDying || IsDead) return;
            base.TakeDamage(hittingInfo, damage);
            
            HittingInfo = hittingInfo;

            switch (sideEffect)
            {
                case SideEffect.None:
                {
                    break;
                }
                case SideEffect.Flinch:
                {
                    if (actionStateContainer.ContainsKey(ActionState.StateType.Flinched))
                    {
                        animationStateConductor.TrySetActionState(actionStateContainer[ActionState.StateType.Flinched]);
                        animationStateConductor.TrySetMovementState(movementStateContainer[MovementState.StateType.Idle]);
                    }
                    break;
                }
                case SideEffect.KnockBack:
                {
                    if (actionStateContainer.ContainsKey(ActionState.StateType.KnockBacked))
                    {
                        animationStateConductor.TrySetActionState(actionStateContainer[ActionState.StateType.KnockBacked]);
                        animationStateConductor.TrySetMovementState(movementStateContainer[MovementState.StateType.Idle]);
                    }
                    break;
                }
            }
        }

        protected override void SetDying()
        {
            base.SetDying();
            if (!IsDead)
            {
                IsDying = true;
                animationStateConductor.ForceSetActionState(actionStateContainer[ActionState.StateType.Dying]);
            }
        }

        public override void Die()
        {
            base.Die();
            IsDying = false;
            characterControllerEnveloper.OnDead();
        }
    }

    public partial class IngameCharacter
    {
        public void ToggleLockOnOff(InputAction.CallbackContext ctx)
        {
            if (LockParams.IsLockingOn)
            {
                commonStateConductor.TrySetLockState(LockStateContainer[LockState.StateType.LockOff]);
            }
            else commonStateConductor.TrySetLockState(LockStateContainer[LockState.StateType.LockOn]);
        }

        public void PlayBubbleReady()
        {
            animationStateConductor.TrySetMovementState(movementStateContainer[MovementState.StateType.BubbleReady]);
        }
    }

    public partial class IngameCharacter
    {
        protected GUIStyle guiStyle = new GUIStyle();
        private float elapsedTime = 0f; // 경과 시간 누적
        private int frameCount = 0; // 프레임 수 누적
        protected int FPS { get; set; }

        private void CalculateFPS()
        {
            elapsedTime += Time.deltaTime;
            // 프레임 카운트 증가
            frameCount++;

            // 1초가 지났을 때 평균 FPS 계산
            if (elapsedTime >= 1f)
            {
                // 평균 FPS 계산 (프레임 수 / 경과 시간)
                float averageFPS = frameCount / elapsedTime;

                // FPS를 UI에 표시
                FPS = Mathf.RoundToInt(averageFPS);

                // 경과 시간과 프레임 카운트 초기화
                elapsedTime = 0f;
                frameCount = 0;
            }
        }
    }
}