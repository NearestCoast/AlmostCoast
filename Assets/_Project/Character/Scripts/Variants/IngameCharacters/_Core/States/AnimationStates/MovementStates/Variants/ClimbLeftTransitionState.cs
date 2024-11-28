using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class ClimbLeftTransitionState : MovementState
    {
        public override StateType Type => StateType.ClimbLeftTransition;
        [SerializeField] private VerticalSurfaceChecker checker;
        
        public override bool CanEnterState 
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                if (VerticalParams.WallNormal is not null)
                {
                    var isDirectionCorrect = inputChecker.Direction2.x > 0;
                    var dot = Vector3.Dot(-VerticalParams.WallNormal.Value, transform.forward);
                    var isLookingAtWall = 1 - dot < 0.05f;
                    return isDirectionCorrect &&
                           isLookingAtWall &&
                           checker.GetIsLeftSightOpened() &&
                           MoveParams.IsClimbable;
                }
                else return false;
            }
        }

        // public override bool CanExitState => false;
        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                return IsEnd || VerticalParams.WallNormal is null || value;
            }
        }

        // [SerializeField] private float ledgeOffset = 0.1f;
        public override void OnEnterState()
        {
            base.OnEnterState();

            ForwardDirection = (transform.forward) * (characterControllerEnveloper.Radius * 3);
            ReadyDirection = (-transform.right) * (characterControllerEnveloper.Radius * 2);
            
            IsReadyOver = false;
            IsEnd = false;
            ElapsedTime = 0;
        }
    }
    public partial class ClimbLeftTransitionState : MovementState
    {
        // [SerializeField, TitleGroup("Velocity")] private float ledgeOffset = 0.1f;
        [SerializeField, TitleGroup("Velocity")] private float readyTime = 0.1f;
        [SerializeField, TitleGroup("Velocity")] private float forwardTime = 0.1f;
        private bool IsReadyOver { get; set; }
        private float ElapsedTime { get; set; }

        private Vector3 ReadyDirection { get; set; }
        private Vector3 ForwardDirection { get; set; }
        
        private bool IsEnd { get; set; }
        protected override Vector3 GetVelocity()
        {
            if (!IsReadyOver)
            {
                if (ElapsedTime < readyTime)
                {
                    ElapsedTime += Time.deltaTime;
                    var t = ElapsedTime / readyTime;  // 0에서 1로 변하는 비율
                    var newPosition = Vector3.Lerp(InitialPosition, InitialPosition + ReadyDirection, t);

                    if (ElapsedTime >= readyTime)
                    {
                        ElapsedTime = 0;
                        IsReadyOver = true;
                    }

                    return newPosition - transform.position;
                }
            }
            else
            {
                if (ElapsedTime < forwardTime)
                {
                    ElapsedTime += Time.deltaTime;
                    var t = ElapsedTime / forwardTime;  // 0에서 1로 변하는 비율
                    var newPosition = Vector3.Lerp(InitialPosition + ReadyDirection, InitialPosition + ReadyDirection + ForwardDirection, t);
                    
                    if (ElapsedTime >= forwardTime)
                    {
                        ElapsedTime = forwardTime;
                        IsEnd = true;
                    }

                    return newPosition - transform.position;
                }
            }
            
            return Vector3.zero;
        }


        private Quaternion TargetRotation { get; set; }
        protected override Quaternion GetRotation()
        {
            if (VerticalParams.WallNormal is not null) TargetRotation = Quaternion.LookRotation(-VerticalParams.WallNormal.Value, Vector3.up);
            return Quaternion.Slerp(transform.rotation, TargetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}