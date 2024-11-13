using System.Collections.Generic;
using _Project.Characters._Core;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class IdleState : MovementState
    {
        public override StateType Type => StateType.Idle;
        
        [SerializeField, TitleGroup("Animation")] private List<ClipTransition> anims;

        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.BaseLayer.Play(anims[0]);
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                return inputChecker.Direction2 == Vector2.zero;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (NextState.Type == StateType.AirDash) return false;
                return NextState.Type != StateType.Idle;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.SetStealthMove();
        }

        [SerializeField, TitleGroup("Velocity"),Range(0,1)] private float angleGravityRate = 0.5f;
        [SerializeField, TitleGroup("Velocity")] private float exceptionalMove = 10;

        protected override Vector3 GetVelocity()
        {
            if (GroundParams.IsGrounded)
            {
                MoveParams.GravityTime = 0f;
                
                if (GroundParams.GroundNormal == Vector3.up)
                // if (GroundParams.GroundNormalDotWithGroundPlane > 0.98f)
                {
                    if (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth > 0.00001f)
                    {
                        MoveParams.Gravity = Vector3.down * (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth);
                        if (MoveParams.HasMovingPlatform)
                        {
                            MoveParams.Gravity = Vector3.zero;
                        }
                    }
                    else
                    {
                        MoveParams.Gravity = Vector3.zero;
                    }
                }
                else
                {
                    MoveParams.Gravity = Vector3.ProjectOnPlane(Vector3.down, GroundParams.GroundNormal).normalized * GroundParams.SlopeAngleRad * angleGravityRate;
                    if (!characterControllerEnveloper.IsGrounded)
                    {
                        var sphereCastHit = Physics.SphereCast(transform.position, characterControllerEnveloper.Radius, Vector3.down, out var sphereCastHitInfo, characterControllerEnveloper.Height);
                        if (sphereCastHit)
                        {
                            MoveParams.Gravity += Vector3.down * sphereCastHitInfo.distance * 0.1f;
                            // Debug.Log(hitInfo.distance);
                        }
                    }
                }
            }
            else
            {
                if (GroundParams.GroundNormalDotWithGroundPlane > 0.99f)
                {
                    MoveParams.Gravity = movementStateValues.GetGravity();
                    MoveParams.GravityTime += Time.deltaTime;
                }
                else
                {
                    MoveParams.Gravity = Vector3.ProjectOnPlane(Vector3.down, GroundParams.GroundNormal).normalized * GroundParams.SlopeAngleRad * angleGravityRate + movementStateValues.GetGravity();
                }
            }

            var exceptionalMoveValue = Vector3.zero;
            
            // var rayOrigin = transform.position + Vector3.up * characterControllerEnveloper.Height / 2;
            // var rayDistance = characterControllerEnveloper.Height;
            // var layerMask = LayerMask.GetMask("Player", "Character");
            // if (Physics.Raycast(rayOrigin, Vector3.down, out var hitInfo, rayDistance, layerMask))
            // {
            //     // 레이어가 "Player" 또는 "Character"일 때 실행할 로직
            //     // Debug.Log($"Hit {hitInfo.collider.gameObject.name} on 'Player' or 'Character' layer");
            //
            //     if (hitInfo.collider.attachedRigidbody && 
            //         hitInfo.collider.attachedRigidbody.gameObject != characterControllerEnveloper.gameObject)
            //     {
            //         exceptionalMoveValue = transform.forward * exceptionalMove * Time.deltaTime;
            //     }
            // }
            
            
            return (MoveParams.Gravity + exceptionalMoveValue);
        }

        protected override Quaternion GetRotation()
        {
            return transform.rotation;
        }
    }
}