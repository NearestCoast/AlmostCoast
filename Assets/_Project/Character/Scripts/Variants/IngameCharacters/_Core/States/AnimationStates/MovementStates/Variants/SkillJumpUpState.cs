using _Project.InputSystem;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class SkillJumpUpState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.SkillJumpUp;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return (MoveParams.SkillJumpCount > 0);
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                var nextStateType = NextState.Type;

                switch (nextStateType)
                {
                    case StateType.Climb:
                    {
                        return (StateTime > 0.2f);
                    }
                    case StateType.SkillJumpDrop:
                    {
                        return IsLeapEnd;
                    }
                }

                var value = nextStateType switch
                {
                    StateType.Landing => true,
                    
                    StateType.Die => true,
                    _ => false,
                };

                return value;
                
            }
        }
        
        public override void OnEnterState()
        {
            base.OnEnterState();
            DirSnap = HorizontalDirection3;
            if (!GroundParams.IsGrounded)
            {
                InitialHeightSnap = InitialPosition.y;
            }
            else
            {
                InitialHeightSnap = GroundParams.GroundPoint.y;
            }
            
            AfterTime = 0;
            IsJumpEnd = false;
            IsLeapEnd = false;
            
            movementStateValues.CurrentHeight = maxJumpHeight;

            MoveParams.GravityTime = 0;
            MoveParams.IsSkillJumpUpEnded = false;
            MoveParams.IsSkillJumpUpInterruptible = true;
            MoveParams.SkillJumpUpHeight = maxJumpHeight;
            
            MoveParams.DecreaseSkillJumpCount();
        }   
    }

    public partial class SkillJumpUpState : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 8;
        [SerializeField, TitleGroup("Velocity")] private float maxJumpHeight = 4;
        [SerializeField, TitleGroup("Velocity")] private float maxHeightTime = 1f;
        [SerializeField, TitleGroup("Velocity")] private float fallingTime = 1f;
        [SerializeField, TitleGroup("Velocity")] private float leapSpeed = 10;
        [SerializeField, TitleGroup("Velocity")] private float stayHeightParameter = 1;
        [SerializeField] private UnityEvent onEnd;
        
        private Vector3 DirSnap { get; set; }
        private float AfterTime { get; set; }
        private float InitialHeightSnap { get; set; }
        private bool IsLeapEnd { get; set; }
        private bool IsJumpEnd { get; set; }    

        protected override Vector3 GetVelocity()
        {
            var moveValue = DirSnap * (maxLength / (maxHeightTime));
            var ray = new Ray(characterControllerEnveloper.transform.position, moveValue);
            var value = (moveValue);
            value *= Time.deltaTime;
            
            
            Vector3 verticalVelocity;
            if (!IsLeapEnd && StateTime <= maxHeightTime)
            {
                var rayHead = new Ray(transform.position + Vector3.up * characterControllerEnveloper.Height / 2, Vector3.up);
                var isHeadHit = Physics.Raycast(rayHead.origin , rayHead.direction, characterControllerEnveloper.SkinWidth, surfaceLayers);
                
                if (!isHeadHit)
                {
                    var diff = InitialHeightSnap + maxJumpHeight - transform.position.y;
                    var log = Mathf.Log(diff + 1 + stayHeightParameter);
                    var yRevision = (log * leapSpeed) * Time.deltaTime;
                    if (transform.position.y + yRevision > InitialHeightSnap + maxJumpHeight)
                    {
                        yRevision = maxJumpHeight + InitialHeightSnap - transform.position.y;
                        IsLeapEnd = true;
                        MoveParams.IsSkillJumpUpEnded = true;
                        
                        if (yRevision < 0) Debug.Log("FUck");
                    }

                    verticalVelocity = Vector3.up * yRevision;
                }
                else
                {
                    verticalVelocity = Vector3.zero;
                    IsLeapEnd = true;
                    MoveParams.IsSkillJumpUpEnded = true;
                }
                
                return verticalVelocity + value.XYZ3toX0Z3();
            }
            else
            {
                if (IsJumpEnd) return Vector3.zero;
                AfterTime += Time.deltaTime;
                if (AfterTime > fallingTime)
                {
                    IsJumpEnd = true;
                    onEnd?.Invoke();
                    MoveParams.IsSkillJumpUpInterruptible = false;
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