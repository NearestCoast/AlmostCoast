using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class ClimbRightLedgeState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.ClimbRightLedge;
        [SerializeField] private VerticalSurfaceChecker checker;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                if (VerticalParams.WallNormal is not null)
                {
                    var isDirectionCorrect = inputChecker.Direction2.x > 0;
                    var angle = Vector3.Angle(-VerticalParams.WallNormal.Value, transform.forward);
                    var isLookingAtWall = angle < 0.001f;
                    return isDirectionCorrect &&
                           isLookingAtWall &&
                           checker.GetIsRightSightOpened() &&
                           MoveParams.IsClimbable;
                }
                else return false;
            }
        }

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


        public override void OnEnterState()
        {
            base.OnEnterState();ForwardDirection = (transform.forward) * (characterControllerEnveloper.Radius * 3);
            ReadyDirection = (transform.right) * (characterControllerEnveloper.Radius * 2);
            TargetRotation = Quaternion.LookRotation(-transform.right, Vector3.up);
            
            IsReadyOver = false;
            IsEnd = false;
            ElapsedTime = 0;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.SetLedgeMove();
            MoveParams.DecreaseClimbStaminaAmount(readyTime + forwardTime);
        }
    }

    
    public partial class ClimbRightLedgeState : BaseLayerClipMovementState
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

            if (IsReadyOver) return Quaternion.Slerp(transform.rotation, TargetRotation, rotationSpeed * Time.deltaTime);
            else return transform.rotation;
        }
    }
}