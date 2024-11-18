using System;
using _Project.Characters._Core.EnvironmentCheckers;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Utils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core
{
    public class VerticalSurfaceChecker : MonoBehaviour, IEnvironmentChecker
    {
        private VerticalParams VerticalParams;
        [ShowInInspector] private CharacterControllerEnveloper characterControllerEnveloper;
        // private BoxCollider boxCollider;
        private LayerMask surfaceLayers;

        private void Awake()
        {
            characterControllerEnveloper = GetComponentInParent<CharacterControllerEnveloper>();
            VerticalParams = GetComponentInParent<VerticalParams>();
            
            // boxCollider = GetComponent<BoxCollider>();
            // boxCollider.size = new Vector3((1 + characterController.skinWidth * 2), boxCollider.size.y, (1 + characterController.skinWidth * 2)); 
            
            surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("GroundUnlit") | 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Ceiling");
            
            VerticalParams.IsWalled = false;
        }

        [SerializeField] private float sightCheckOffset = 0.2f;

        private readonly Vector3[] directions = new Vector3[]
        {
            Vector3.forward, // 앞
            Vector3.back, // 뒤
            Vector3.left, // 왼쪽
            Vector3.right, // 오른쪽
            (Vector3.forward + Vector3.right).normalized, // 앞오른쪽 대각선
            (Vector3.forward + Vector3.left).normalized, // 앞왼쪽 대각선
            (Vector3.back + Vector3.right).normalized, // 뒤오른쪽 대각선
            (Vector3.back + Vector3.left).normalized // 뒤왼쪽 대각선
        };
        
        private Vector3 HeadOrigin => transform.position + characterControllerEnveloper.Center + Vector3.up * (characterControllerEnveloper.Height / 2 - characterControllerEnveloper.Radius);
        private Vector3 BodyOrigin => transform.position + characterControllerEnveloper.Center;
        
        
        public void Check(MovementState currentState)
        {
            VerticalParams.PrevIsWalled = VerticalParams.IsWalled;
            
            VerticalParams.IsWalled = GetIsWalled();
            VerticalParams.IsSightOpened = GetIsSightOpened();
            VerticalParams.IsRightSightOpened = GetIsRightSightOpened();
            VerticalParams.IsLeftSightOpened = GetIsLeftSightOpened();
            VerticalParams.IsLeftLedgeMovable = GetIsLeftLedgeMovable();
            // if (VerticalParams.IsRightSightOpened) VerticalParams.IsRightLedgeMovable = GetIsRightLedgeMovable();
            // else VerticalParams.IsRightLedgeMovable = false;
            // if (VerticalParams.IsLeftSightOpened) VerticalParams.IsLeftLedgeMovable = GetIsLeftLedgeMovable();
            // else VerticalParams.IsLeftLedgeMovable = false; 
            
            VerticalParams.IsHeadOpen = GetIsHeadOpen();
            VerticalParams.IsEdgeOfPlatform = GetIsEdgeOfPlatform();
        }

        private bool GetIsHeadOpen()
        {
            var rayCenter = new Ray(HeadOrigin, Vector3.up);
            var rayLength = characterControllerEnveloper.Height + characterControllerEnveloper.Radius + characterControllerEnveloper.SkinWidth;
            Debug.DrawRay(rayCenter.origin, rayCenter.direction * rayLength, Color.cyan);
            return VerticalParams.IsHeadOpen = !Physics.Raycast(rayCenter, rayLength, surfaceLayers);
        }
        public bool GetIsWalled()
        {
            foreach (var direction in directions)
            {
                var ray = new Ray(HeadOrigin, direction);
                var rayLength = characterControllerEnveloper.Radius * 3f;
                var hit = Physics.Raycast(ray, out var hitInfo, rayLength, surfaceLayers);
                // Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.blue);
            
                if (!hit) continue;
                // Debug.Log(Vector3.Dot(hitInfo.normal, Vector3.up));
                // Debug.Log(Vector3.Angle(hitInfo.normal, Vector3.up));
                // if (Vector3.Dot(hitInfo.normal, Vector3.up) > 0.1f) continue;
                if (Vector3.Angle(hitInfo.normal, Vector3.up) > 120)
                {
                    VerticalParams.WallNormal = null;
                    VerticalParams.WallPoint = null;
                    continue;
                }
                
                VerticalParams.WallNormal = hitInfo.normal;
                VerticalParams.WallPoint = hitInfo.point;
                
                return true;
            }
            
            VerticalParams.WallNormal = null;
            VerticalParams.WallPoint = null;
            return false;
        }

        private bool HitDetectClimbOverLedge { get; set; }
        private bool HitDetectLeftSight { get; set; }
        private bool HitDetectRightSight { get; set; }
        private bool HitDetectLeftMovable { get; set; }
        private bool HitDetectClimbRightLedge { get; set; }
        
        private Vector3 HeadAboveSightOrigin => HeadOrigin + Vector3.up * (characterControllerEnveloper.Height / 2 + characterControllerEnveloper.Radius);
        private Vector3 LeftSightOrigin => BodyOrigin + (-transform.right) * (characterControllerEnveloper.Radius * 2);
        private Vector3 RightSightOrigin => BodyOrigin + (transform.right) * (characterControllerEnveloper.Radius * 2);
        private Vector3 LeftMovableOrigin => BodyOrigin;
        
        private Ray BoxCastRayClimbOverLedge => new Ray(HeadAboveSightOrigin, transform.forward);
        private Ray BoxCastRayLeftSight => new Ray(LeftSightOrigin, transform.forward);
        private Ray BoxCastRayRightSight => new Ray(RightSightOrigin, transform.forward);
        private Ray BoxCastRayLeftMovable => new Ray(LeftMovableOrigin, transform.right);
        
        private Vector3 HalfExtents => new Vector3(characterControllerEnveloper.Radius, characterControllerEnveloper.Height / 2, characterControllerEnveloper.Radius);
        private float BoxCastRayLength => characterControllerEnveloper.Radius * 2;
        
        

        [SerializeField] private TriggerManager upSightTrigger;
        public bool GetIsSightOpened()
        {
            HitDetectClimbOverLedge = upSightTrigger.IsHit;
            return !HitDetectClimbOverLedge;
        }

        [SerializeField] private TriggerManager rightSightTrigger;
        public bool GetIsRightSightOpened()
        {
            HitDetectRightSight = rightSightTrigger.IsHit;
            return !HitDetectRightSight;
        }

        [SerializeField] private TriggerManager leftSightTrigger;
        public bool GetIsLeftSightOpened()
        {
            HitDetectLeftSight = leftSightTrigger.IsHit;
            return !HitDetectLeftSight;
        }

        public bool GetIsRightLedgeMovable()
        {
            return false;
            // var rayLength = boxCollider.size.z;
            // var rayCenter = new Ray(transform.position + boxCollider.center + (transform.right) * sightCheckOffset + transform.forward * rayLength, -transform.right);
            // var hitCenter = Physics.Raycast(rayCenter, rayLength, surfaceLayers);
            // Debug.DrawRay(rayCenter.origin, rayCenter.direction * rayLength, Color.blue);
            // return hitCenter;
        }

        public bool GetIsLeftLedgeMovable()
        {
            return false;
            // HitDetectLeftMovable = Physics.BoxCast(BoxCastRayLeftMovable.origin, HalfExtents , BoxCastRayLeftMovable.direction, transform.rotation, characterController.skinWidth);
            // return HitDetectLeftMovable;
        }

        [SerializeField] private float edgeCheckOffset = 0.25f;
        public bool GetIsEdgeOfPlatform()
        {
            if (!VerticalParams.IsWalled || !VerticalParams.IsHeadOpen) return false;

            // 캐릭터 최상단에서 Ray를 쏠 시작 위치
            Vector3 topOrigin = transform.position + Vector3.up * (characterControllerEnveloper.Height);

            // 최상단에서 Ray를 쏠 오프셋 위치
            Vector3 offsetOrigin = topOrigin + Vector3.up * edgeCheckOffset;

            // Ray 방향
            Vector3 rayDirection = transform.forward;

            // Ray 길이
            float rayLength = characterControllerEnveloper.Radius * 2;

            // Debug용 Ray 시각화
            Debug.DrawRay(topOrigin, rayDirection * rayLength, Color.red);
            Debug.DrawRay(offsetOrigin, rayDirection * rayLength, Color.blue);

            // 캐릭터 최상단에서 forward 방향 Ray
            bool isTopHit = Physics.Raycast(topOrigin, rayDirection, rayLength, surfaceLayers);

            // 캐릭터 최상단 + offset 위치에서 forward 방향 Ray
            bool isOffsetHit = Physics.Raycast(offsetOrigin, rayDirection, rayLength, surfaceLayers);

            // 조건을 만족할 때 true 반환
            return isTopHit && !isOffsetHit;
        }

        
        private void OnDrawGizmos()
        {
            return;
            if (!Application.isPlaying) return;
            
            DrawBoxCast(BoxCastRayClimbOverLedge, transform.rotation, BoxCastRayLength, HitDetectClimbOverLedge);
            DrawBoxCast(BoxCastRayLeftSight, transform.rotation, BoxCastRayLength, HitDetectLeftSight);
            DrawBoxCast(BoxCastRayRightSight, transform.rotation, BoxCastRayLength, HitDetectRightSight);
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