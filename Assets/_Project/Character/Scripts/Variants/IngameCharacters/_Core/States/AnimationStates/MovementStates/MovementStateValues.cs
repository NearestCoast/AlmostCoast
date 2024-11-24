using System;
using _Project.Maps.Climber.Objects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public static class ActionsStateParams
    {
        public static Bubble CurrentBubble { get; set; }

        public static void SetBubble(Bubble bubble)
        {
            CurrentBubble = bubble;
        }

        public static void ReleaseBubble()
        {
            CurrentBubble = null;
        }
    }
    
    public class MovementStateValues : MonoBehaviour
    {
        [SerializeField, TitleGroup("Velocity")] protected float gravityPow = 2;
        [SerializeField, TitleGroup("Velocity")] protected float accelerationFactor = 2;
        [SerializeField, TitleGroup("Velocity"), Range(0,1)] protected float stayHeightParameter = 1;
        [SerializeField, TitleGroup("Velocity"), Range(0,1)] protected float wallJumpSlowRate = 1;
        
        private IngameCharacter masterCharacter;
        private CharacterControllerEnveloper characterControllerEnveloper;
        private MoveParams MoveParams;
        private GroundParams GroundParams;
        
        private void Awake()
        {
            masterCharacter = GetComponentInParent<IngameCharacter>();
            characterControllerEnveloper = GetComponentInParent<CharacterControllerEnveloper>();
            MoveParams = GetComponentInParent<MoveParams>();
            GroundParams = GetComponentInParent<GroundParams>();
            
            OriginalGravityPow = gravityPow;
            OriginalAccelerationFactor = accelerationFactor;
        }

        private float OriginalGravityPow { get; set; }
        private float GravityPow => (MoveParams.IsWallJumping) ? OriginalGravityPow * wallJumpSlowRate : OriginalGravityPow;  
        public Vector3 GetGravity()
        {
            var yRevision = (Mathf.Pow(MoveParams.GravityTime + stayHeightParameter, GravityPow) * Mathf.Abs(Physics.gravity.y) * Time.deltaTime);
            // var yRevision = (Mathf.Pow(DownTime, 2) * gravitySpeed * pressingRevisio n) * Time.deltaTime;
            return Vector3.down * yRevision;
        }

        public float CurrentHeight { get; set; } // OnEnterState에서만 초기화 되어야함!!!!!!

        private float OriginalAccelerationFactor { get; set; }
        private float AccelerationFactor => (MoveParams.IsWallJumping) ? OriginalAccelerationFactor * wallJumpSlowRate : OriginalAccelerationFactor;
        public Vector3 GetGravity(float maxJumpHeight, float totalTime, out bool isFinished)
        {
            // 경과된 시간을 0에서 1로 정규화
            float t = Mathf.Clamp01(MoveParams.GravityTime / totalTime);
            // 비선형 가속도 적용 (t^2 또는 t^3 등 곡선 가속 적용)
            float nonLinearT = Mathf.Pow(t, AccelerationFactor);
            
            // 현재 높이를 계산 (Y축에서 시작 높이에서 0까지 비선형 가속으로 이동)
            float newHeight = Mathf.Lerp(maxJumpHeight, 0f, nonLinearT);
            

            // 이번 프레임에서 떨어진 거리 반환
            float fallDistance = CurrentHeight - newHeight;
            // Debug.Log(fallDistance);

            // 현재 높이를 업데이트
            CurrentHeight = newHeight;
            isFinished = CurrentHeight == 0;
            return Vector3.down * fallDistance;
        }

        public void SetGravity()
        {
            if (GroundParams.IsGrounded)
            {
                if (GroundParams.GroundNormal == Vector3.up)
                {
                    MoveParams.GravityTime = 0f;
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
                    var normalGravity = GetGravity();
                    MoveParams.GravityTime += Time.deltaTime;
                    var slopeDownSpeed = GroundParams.SlopeAngleDeg / 90;
                    var gravityDir = Vector3.ProjectOnPlane(Vector3.down, GroundParams.GroundNormal).normalized;
                    if (GroundParams.SlopeAngleDeg > characterControllerEnveloper.SlopeLimit)
                    {
                        MoveParams.Gravity = gravityDir * normalGravity.magnitude * slopeDownSpeed;
                    }
                    else
                    {
                        MoveParams.GravityTime = 0f;
                        MoveParams.Gravity = Vector3.zero;
                    }
                    
                    Debug.DrawRay(transform.position, MoveParams.Gravity * 10, Color.green);
                    
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
                if (masterCharacter.CurrentMovingPlatform && masterCharacter.CurrentMovingPlatform.enabled)
                {
                    
                }
                else 
                {
                    MoveParams.Gravity = GetGravity();
                    MoveParams.GravityTime += Time.deltaTime;
                }
            }
        }
    }
}