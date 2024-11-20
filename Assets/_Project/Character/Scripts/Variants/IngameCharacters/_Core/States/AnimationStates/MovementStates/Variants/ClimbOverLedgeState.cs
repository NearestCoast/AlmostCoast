using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class ClimbOverLedgeState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.ClimbOverLedge;
        [SerializeField] private VerticalSurfaceChecker checker;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                if (VerticalParams.WallNormal is null)
                {
                    if (PrevState.Type == StateType.Climb) Debug.Log("WallNormal Is Null.");
                }
                if (VerticalParams.WallNormal is not null)
                {
                    var isDirectionCorrect = inputChecker.Direction2.y > 0;
                    var dot = Vector3.Dot(-VerticalParams.WallNormal.Value, transform.forward);
                    var isLookingAtWall = 1 - dot < 0.05f;
                    return isDirectionCorrect &&
                           isLookingAtWall &&
                           checker.GetIsSightOpened() &&
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
                
                return IsEnd || value;
            }
        }

        public override void OnEnterState()
        {
            Debug.Log("Enter ClimbOverLedge");
            base.OnEnterState();
            ForwardDirection = (transform.forward) * (characterControllerEnveloper.Radius * 3);
            
            IsReadyOver = false;
            IsEnd = false;
            ElapsedTime = 0;
            
            MoveParams.ResetJumpCount();
            MoveParams.ResetClimbStamina();
            MoveParams.ResetKickCount();
            MoveParams.ResetWallJumpCount();
            
            MoveParams.ResetClimbingButtonPressed();
        }

        public override void OnExitState()
        {
            Debug.Log("Exit ClimbOverLedge");
            base.OnExitState();
            MoveParams.SetLedgeMove();
            MoveParams.DecreaseClimbStaminaAmount(readyTime + forwardTime);
        }
    }

    
    public partial class ClimbOverLedgeState : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private float ledgeOffset = 0.1f;
        [SerializeField, TitleGroup("Velocity")] private float readyTime = 0.1f;
        [SerializeField, TitleGroup("Velocity")] private float forwardTime = 0.1f;
        private bool IsReadyOver { get; set; }
        private float ElapsedTime { get; set; }

        private Vector3 ReadyDirection => Vector3.up * (characterControllerEnveloper.Height + characterControllerEnveloper.SkinWidth + ledgeOffset);
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

        protected override Quaternion GetRotation()
        {
            return transform.rotation;
        }
    }
}