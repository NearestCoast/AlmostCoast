using _Project.InputSystem;
using _Project.Utils;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
        
    [InfoBox("오를 수 있는 높이 최대 : (maxSpeed * staminaTime) + 1")]
    public partial class ClimbState : MovementState
    {
        public override StateType Type => StateType.Climb;
        
        [SerializeField, TitleGroup("Animation")] private LinearMixerTransition anims;

        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.BaseLayer.Play(anims);
            AnimancerState.NormalizedTime = animCutStartNormalizedTime;
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                
                return VerticalParams.IsWalled && MoveParams.IsClimbable && connectedInput.IsPressing &&
                       VerticalParams.IsWallPerpendicularToGround;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (Time.time - DebugEndTime < 0.05f)
                {
                    Debug.Log("#### 기다리고 기다리던 버그 등장 ####");
                    Debug.Log("기대한 행동 : Climb -> ClimbOverLedge");
                    Debug.Log("NextState.Type : " + NextState.Type);
                    Debug.Log("MoveParams.IsUnderCrowdControl : " + MoveParams.IsUnderCrowdControl);
                    Debug.Log("MoveParams.IsClimbable : " + MoveParams.IsClimbable);
                    Debug.Log("VerticalParams.IsWalled : " + VerticalParams.IsWalled);
                    Debug.Log("VerticalParams.IsWallPerpendicularToGround : " + VerticalParams.IsWallPerpendicularToGround);
                    Debug.Log("connectedInput.IsPressing : " + connectedInput.IsPressing);
                }
                if (MoveParams.IsUnderCrowdControl) return true;
                if (!MoveParams.IsClimbable) return true;
                if (!VerticalParams.IsWalled) return true;
                if (!VerticalParams.IsWallPerpendicularToGround) return true;
                if (!connectedInput.IsPressing) return true;
                
                var value = NextState.Type switch
                {
                    StateType.Jump => true,
                    StateType.Hang => true,
                    StateType.ClimbOverLedge => VerticalParams.IsSightOpened,
                    StateType.ClimbRightLedge => VerticalParams.IsRightSightOpened,
                    StateType.ClimbLeftLedge => VerticalParams.IsLeftSightOpened,
                    
                    StateType.ClimbLeftTransition => true,
                    StateType.ClimbRightTransition => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                if (Time.time - DebugEndTime < 0.05f)
                {
                    Debug.Log(value);
                }
                
                return value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            
            MoveParams.Gravity = Vector3.zero;
            MoveParams.GravityTime = 0;
            
            MoveParams.StartClimbing();
            MoveParams.ResetAcceleration();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.EndClimbing();
            DebugEndTime = Time.time;
        }
        
        private float DebugEndTime { get; set; }
    }
    public partial class ClimbState : MovementState
    {
        [SerializeField, TitleGroup("Velocity")] private PressingOnlyInput connectedInput;
        [SerializeField, TitleGroup("Velocity")] private float maxSpeed = 6;
        [SerializeField, TitleGroup("Velocity")] private float attachTime = 0.1f;
        
        protected override Vector3 GetVelocity()
        {
            MoveParams.DecreaseClimbStaminaPerFrame();
            
            var inputMagnitudeAmplified = Mathf.Pow(InputDirection.magnitude, 2);

            var depthValue = Vector3.zero;
            if (VerticalParams.WallPoint is not null)
            {
                var dist = Vector3.Distance(VerticalParams.WallPoint.Value, transform.position);
                if (dist > characterControllerEnveloper.Radius) depthValue = transform.forward * (dist - characterControllerEnveloper.Radius - characterControllerEnveloper.SkinWidth) / attachTime;
            }

            var moveValue = Vector3.zero;
            var dotToUp = Vector3.Dot(VerticalDirection3, Vector3.up);
            if (VerticalParams.IsEdgeOfPlatformFromBottom)
            {
                moveValue = VerticalDirection3 * (inputMagnitudeAmplified * maxSpeed);
            }
            else
            {
                if (dotToUp <= 0) moveValue = VerticalDirection3 * (inputMagnitudeAmplified * maxSpeed);
            }
            
            
            // return (depthValue) * Time.deltaTime;
            
            if (InputDirection.y > 0) // Upper  
            {
                // anims.State.Parameter = 3;
                
                if (InputDirection.x > 0) // Right
                {
                    anims.State.Parameter = 4;
                }
                else if (InputDirection.x < 0) // Left
                {
                    anims.State.Parameter = 5;
                }
                else // Stay Center
                {
                    anims.State.Parameter = 3;
                }
            }
            else if (InputDirection.y < 0)// Lower
            {
                // anims.State.Parameter = 6;
                
                if (InputDirection.x > 0) // Right
                {
                    anims.State.Parameter = 7;
                }
                else if (InputDirection.x < 0) // Left
                {
                    anims.State.Parameter = 8;
                }
                else // Stay Center
                {
                    anims.State.Parameter = 6;
                }
            }
            else // Stay Height
            {
                if (InputDirection.x > 0) // Right
                {
                    anims.State.Parameter = 1;
                }
                else if (InputDirection.x < 0) // Left
                {
                    anims.State.Parameter = 2;
                }
                else // Stay Center
                {
                    anims.State.Parameter = 0;
                }
            }

            return (moveValue + depthValue) * Time.deltaTime;
        }

        protected override Quaternion GetRotation()
        {
            if (VerticalParams.WallNormal is null)
            {
                var value = Quaternion.LookRotation(transform.forward, Vector3.up);
                return Quaternion.Slerp(transform.rotation, value, 0.5f);
            }
            else
            {
                // Debug.Log(VerticalParams.WallNormal.Value);
                var value = Quaternion.LookRotation(-VerticalParams.WallNormal.Value, transform.up);
                return value;
            }
        }
    }
}