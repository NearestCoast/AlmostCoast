using _Project.InputSystem;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class SkillJumpDropState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.SkillJumpDrop;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return (MoveParams.IsSkillJumpUpEnded);
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                switch (NextState.Type)
                {
                    case StateType.Idle:
                    {
                        return transform.position.y + 3 < InitialHeightSnap || StateTime > maxHeightTime + fallingTime;
                    }
                    case StateType.Move:
                    {
                        return transform.position.y + 3 < InitialHeightSnap || StateTime > maxHeightTime + fallingTime;
                    }
                    case StateType.Climb:
                    {
                        return (StateTime > 0.2f);
                    }

                    case StateType.AirDash:
                    {
                        return StateTime > 0.2f && MoveParams.KickCount > 0;;
                    }
                }
                
                var value = NextState.Type switch
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

            DirSnap = MoveParams.IsSkillJumpUpInterruptible ? HorizontalDirection3 : Vector3.zero;

            movementStateValues.CurrentHeight = MoveParams.SkillJumpUpHeight + maxJumpHeight;
            IsLeapEnd = false;
            
            InitialHeightSnap = InitialPosition.y;
            
            MoveParams.GravityTime = 0;
        }
    }
    
    public partial class SkillJumpDropState
    {
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 4;
        [SerializeField, TitleGroup("Velocity")] private float maxJumpHeight = 1;
        [SerializeField, TitleGroup("Velocity")] private float maxHeightTime = 1f;
        [SerializeField, TitleGroup("Velocity")] private float fallingTime = 1f;
        [SerializeField, TitleGroup("Velocity")] private float leapSpeed = 10;
        [SerializeField, TitleGroup("Velocity")] private float stayHeightParameter = 1f;
        
        private Vector3 DirSnap { get; set; }
        private bool IsLeapEnd { get; set; }
        private bool JumpEnd { get; set; }
        private float InitialHeightSnap { get; set; }
        

        protected override Vector3 GetVelocity()
        {
            var moveValue = DirSnap * ( maxLength / (maxHeightTime + fallingTime));
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
                        
                        if (yRevision < 0) Debug.Log("FUck");
                    }

                    verticalVelocity = Vector3.up * yRevision;
                }
                else
                {
                    verticalVelocity = Vector3.zero;
                    IsLeapEnd = true;
                }
            }
            else
            { 
                IsLeapEnd = true;
                MoveParams.GravityTime += Time.deltaTime;   
                MoveParams.Gravity = movementStateValues.GetGravity(MoveParams.SkillJumpUpHeight + maxJumpHeight, fallingTime, out var isFinished);
                if (MoveParams.Gravity.y > 0) Debug.Log("FFFFFFF");
                
                JumpEnd = isFinished;
                
                if (GroundParams.IsGrounded && transform.position.y + MoveParams.Gravity.y < GroundParams.GroundPoint.y)
                {
                    JumpEnd = true;
                    MoveParams.Gravity = (transform.position - GroundParams.GroundPoint).XYZ3to0Y03();
                }
                
                verticalVelocity = MoveParams.Gravity;
            }
            
            
            return verticalVelocity + value.XYZ3toX0Z3();
        }
    }
}