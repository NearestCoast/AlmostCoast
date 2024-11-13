using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using Animancer;
using UnityEngine;

namespace _Project.Characters._Core.States.AnimationStates
{
    [DefaultExecutionOrder(-10000)]
    public partial class AnimationStateConductor : MonoBehaviour
    {
        private AnimancerComponent animancer;
        public AnimancerComponent Animancer => animancer;
        
        [SerializeField] private AvatarMask fullBodyMask;
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField] private AvatarMask noBodyMask;
        
        public AnimancerLayer BaseLayer { get; private set; }
        public AnimancerLayer AttackLayer { get; private set; }

        private void Awake()
        {
            animancer = GetComponentInChildren<AnimancerComponent>();
            // ActionStateMachine.InitializeAfterDeserialize();
            // AttackStateMachine.InitializeAfterDeserialize();
            
            BaseLayer = Animancer.Layers[0];
            AttackLayer = Animancer.Layers[1];
            
            SetActionMaskNoBody();
            AttackLayer.SetDebugName("Attack Layer");
        }

        private void Start()
        {
            MovementStateMachine.TrySetDefaultState();
            ActionStateMachine.TrySetDefaultState();    
        }

        public void SetActionMaskNoBody()
        {
            // AttackLayer.Mask = (noBodyMask); // 마스크 제거하여 아무 부분도 영향을 받지 않게 설정
            AttackLayer.Weight = 0; // Attack Layer 비활성화
        }

        public void SetActionMaskUpperBody()
        {
            AttackLayer.Mask = (upperBodyMask);
            AttackLayer.Weight = 1; // Attack Layer 활성화
        }

        public void SetActionMaskFullBody()
        {
            AttackLayer.Mask = (fullBodyMask);
            AttackLayer.Weight = 1; // Attack Layer 활성화
        }   
    }

    public partial class AnimationStateConductor : MonoBehaviour
    {
        [SerializeField] private AnimationState.StateMachine movementStateMachine;
        public AnimationState.StateMachine MovementStateMachine => movementStateMachine;

        public MovementState CurrentMovementState => MovementStateMachine.CurrentState as MovementState;
        
        public void TrySetMovementState(MovementState animationState)
        {
            if (!animationState.gameObject.activeSelf) return;
            if (animationState == CurrentMovementState) MovementStateMachine.TryResetState(animationState);
            else MovementStateMachine.TrySetState(animationState);
        }

        public void ForceSetMovementState(MovementState animationState)
        {
            if (!animationState.gameObject.activeSelf) return;
            MovementStateMachine.ForceSetState(animationState);
        }
    }

    public partial class AnimationStateConductor : MonoBehaviour
    {
        [SerializeField] private AnimationState.StateMachine actionStateMachine;
        public AnimationState.StateMachine ActionStateMachine => actionStateMachine;

        public ActionState CurrentActionState => ActionStateMachine.CurrentState as ActionState;

        public void TrySetActionState(ActionState animationState)
        {
            if (!animationState.gameObject.activeSelf) return;
            if (animationState == CurrentActionState) ActionStateMachine.TryResetState(animationState);
            else ActionStateMachine.TrySetState(animationState);
        }

        public void ForceSetActionState(ActionState animationState)
        {
            if (!animationState.gameObject.activeSelf) return;
            ActionStateMachine.ForceSetState(animationState);
        }
    }
}