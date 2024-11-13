using System;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters._Core.EnvironmentCheckers;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    public class GroundSurfaceChecker : MonoBehaviour, IEnvironmentChecker
    {
        [ShowInInspector] private CharacterControllerEnveloper characterControllerEnveloper;
        [SerializeField] private float groundCheckOffset = 0.1f;
        private LayerMask surfaceLayers;
        private GroundParams GroundParams;

        private void Awake()
        {
            characterControllerEnveloper = GetComponentInParent<CharacterControllerEnveloper>();
            GroundParams = GetComponentInParent<GroundParams>();
            var master = GetComponentInParent<IngameCharacter>();
            if (master is PlayerCharacter)
            {
                surfaceLayers = 1 << LayerMask.NameToLayer("Ground") |
                                1 << LayerMask.NameToLayer("GroundUnlit") |
                                1 << LayerMask.NameToLayer("Wall") |
                                1 << LayerMask.NameToLayer("Character");
            }
            else
            {
                surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 
                                1 << LayerMask.NameToLayer("GroundUnlit") | 
                                1 << LayerMask.NameToLayer("Wall") | 
                                1 << LayerMask.NameToLayer("Player");
            }
                

            HalfExtents = new Vector3(characterControllerEnveloper.Radius, groundCheckOffset / 2, characterControllerEnveloper.Radius);
        }
        
        private Ray Ray { get; set; }
        private Vector3 HalfExtents { get; set; }
        private bool Hit { get; set; }

        // [SerializeField] private TriggerManager groundTrigger;
        [SerializeField] private float lengthToGroundPoint = 1; // size of ground groundTrigger height

        public void Check(MovementState currentState)
        {
            GroundParams.IsGroundedOnCharacter = false;
            
            GroundParams.PrevIsGrounded = GroundParams.IsGrounded;
            
            GroundParams.RayLength = characterControllerEnveloper.Height / 2 + characterControllerEnveloper.SkinWidth + groundCheckOffset;
            // GroundParams.RayLength = characterController.height / 2 + characterController.skinWidth;
            GroundParams.GroundNormal = Vector3.up;
            
            var hitLength = float.MaxValue;
            var hit0 = isBottomHit(transform.position + characterControllerEnveloper.Center, Vector3.zero);
            // var hit1 = isBottomHit(transform.position, (-transform.forward) * characterController.radius);
            // var hit2 = isBottomHit(transform.position, (-transform.right) * characterController.radius);
            // var hit3 = isBottomHit(transform.position, (transform.right) * characterController.radius);
            // var hit4 = isBottomHit(transform.position, (transform.forward) * characterController.radius);
            // var hit5 = isBottomHit(transform.position, (-transform.forward-transform.right).normalized * characterController.radius);
            // var hit6 = isBottomHit(transform.position, (-transform.forward+transform.right).normalized * characterController.radius);
            // var hit7 = isBottomHit(transform.position, (transform.forward-transform.right).normalized * characterController.radius);
            // var hit8 = isBottomHit(transform.position, (transform.forward+transform.right).normalized * characterController.radius);
            
            var hit1 = isBottomHit(transform.position + characterControllerEnveloper.Center, (-transform.forward) * characterControllerEnveloper.Radius);
            
            if (GroundParams.IsGrounded)
            {
                var revision = 1f;
                if (currentState is MoveState)
                {
                    revision = 4;
                }
                hit1 = isBottomHit(transform.position + characterControllerEnveloper.Center, (-transform.forward) * (characterControllerEnveloper.Radius * revision));
            }
            var hit2 = isBottomHit(transform.position + characterControllerEnveloper.Center, (-transform.right) * characterControllerEnveloper.Radius);
            var hit3 = isBottomHit(transform.position + characterControllerEnveloper.Center, (transform.right) * characterControllerEnveloper.Radius);
            var hit4 = isBottomHit(transform.position + characterControllerEnveloper.Center, (transform.forward) * characterControllerEnveloper.Radius);
            var hit5 = isBottomHit(transform.position + characterControllerEnveloper.Center, (-transform.forward-transform.right).normalized * characterControllerEnveloper.Radius);
            var hit6 = isBottomHit(transform.position + characterControllerEnveloper.Center, (-transform.forward+transform.right).normalized * characterControllerEnveloper.Radius);
            var hit7 = isBottomHit(transform.position + characterControllerEnveloper.Center, (transform.forward-transform.right).normalized * characterControllerEnveloper.Radius);
            var hit8 = isBottomHit(transform.position + characterControllerEnveloper.Center, (transform.forward+transform.right).normalized * characterControllerEnveloper.Radius);
            
            // GroundParams.IsGrounded = hit0 || hit1 || hit2 || hit3 || hit4 || hit5 || hit6 || hit7 || hit8;
            // GroundParams.IsGrounded = hit0 || hit1 || hit2 || hit3 || hit4 || hit5 || hit6 || hit7 || hit8 || characterController.isGrounded;
            // GroundParams.IsGrounded = hit0 || hit1 || hit2 || hit3 || hit4 || hit5 || hit6 || hit7 || hit8 || characterController.isGrounded || groundTrigger.IsHit;
            var rayHit = (hit0 || hit1 || hit2 || hit3 || hit4 || hit5 || hit6 || hit7 || hit8);
            // if (!rayHit) GroundParams.GroundNormal = Vector3.up;
            // GroundParams.IsGrounded = rayHit && (groundTrigger.IsHit);
            GroundParams.IsGrounded = rayHit;
            // GroundParams.IsGrounded = rayHit || (groundTrigger.IsHit);
            // Debug.Log(rayHit + ", " + groundTrigger.IsHit + ", " + GroundParams.IsGrounded); 
            // GroundParams.IsGrounded = groundTrigger.IsHit;
            
            return;

            bool isBottomHit(Vector3 center, Vector3 offset)
            {
                var ray = new Ray(center + offset * 2, Vector3.down);
                
                var hit =  Physics.Raycast(ray, out var hitInfo, GroundParams.RayLength, surfaceLayers);
                
                if (hit)
                {
                    if (hitInfo.distance < hitLength)
                    {
                        var optionRay = new Ray(hitInfo.point, hitInfo.normal);
                        Debug.DrawRay(optionRay.origin, optionRay.direction * characterControllerEnveloper.Height, Color.magenta);
                        var optionHit = Physics.Raycast(optionRay, out var optionHitInfo, characterControllerEnveloper.Height, surfaceLayers);
                        if (optionHit)
                        {
                            // Debug.Log("COme");
                        }
                        else
                        {
                            hitLength = hitInfo.distance;
                            GroundParams.GroundNormal = hitInfo.normal;
                            GroundParams.SlopeAngleDeg = Vector3.Angle(GroundParams.GroundNormal, Vector3.up);
                        }
                    }
                }
                
                ray = new Ray(center + offset, Vector3.down);
                var hit2 = Physics.Raycast(ray, out hitInfo, lengthToGroundPoint + characterControllerEnveloper.Center.y, surfaceLayers);
                
                // Debug.DrawRay(ray.origin, ray.direction * GroundParams.RayLength, Color.red);
                
                if (hit2)
                {
                    GroundParams.GroundPoint = hitInfo.point;
                    
                    if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Character") || hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        GroundParams.IsGroundedOnCharacter = true;
                        // hit2 = false;
                    }
                        
                    Debug.DrawRay(ray.origin, ray.direction * hitInfo.distance, Color.red);
                }
                else
                {
                    GroundParams.GroundPoint = transform.position;
                    Debug.DrawRay(ray.origin, ray.direction * (lengthToGroundPoint + characterControllerEnveloper.Center.y), Color.blue);
                }

                return hit2;
            }
        }
        
        private void OnDrawGizmos()
        {
            return;
            if (!Application.isPlaying) return;
            
            DrawBoxCast(Ray, transform.rotation, groundCheckOffset, Hit);
            // DrawBoxCast(BoxCastRayLeftSight, transform.rotation, BoxCastRayLength, HitDetectLeftSight);
            // DrawBoxCast(BoxCastRayRightSight, transform.rotation, BoxCastRayLength, HitDetectRightSight);
            // DrawBoxCast(BoxCastRayLeftMovable, transform.rotation, characterController.skinWidth, HitDetectLeftMovable);

            void DrawBoxCast(Ray ray, Quaternion orientation, float rayLength, bool isHit)
            {
                // 시야 선(레이캐스트)의 시작점
                Vector3 origin = ray.origin;
                var direction = ray.direction;

                // BoxCast 시작점과 끝점을 계산
                Vector3 endPoint = origin + direction.normalized * rayLength;

                // Gizmo 색상 설정
                Gizmos.color = isHit ? Color.red: Color.green;

                // BoxCast의 시작점 그리기 (회전 반영)
                Gizmos.matrix = Matrix4x4.TRS(origin, orientation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, HalfExtents * 2);

                // BoxCast의 끝점 그리기 (회전 반영)
                Gizmos.matrix = Matrix4x4.TRS(endPoint, orientation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, HalfExtents * 2);

                // BoxCast 경로를 선으로 그리기
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawLine(origin, endPoint);
            }
        }
    }
}