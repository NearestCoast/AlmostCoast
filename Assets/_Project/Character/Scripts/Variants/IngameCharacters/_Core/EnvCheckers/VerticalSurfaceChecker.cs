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

            surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("GroundUnlit") |
                            1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Ceiling");

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

        

        private Vector3 HeadOrigin => transform.position + characterControllerEnveloper.Center +
                                      Vector3.up * (characterControllerEnveloper.Height / 2
                                                    - characterControllerEnveloper.Radius);

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

        [SerializeField] private float headOffset = 0.25f;
        private bool GetIsHeadOpen()
        {
            var rayCenter = new Ray(HeadOrigin, Vector3.up);
            var rayLength = characterControllerEnveloper.Height + characterControllerEnveloper.Radius + characterControllerEnveloper.SkinWidth;
            // Debug.DrawRay(rayCenter.origin, rayCenter.direction * rayLength, Color.cyan);
            return VerticalParams.IsHeadOpen = !Physics.Raycast(rayCenter, rayLength, surfaceLayers);
        }

        public bool GetIsWalled()
        {
            foreach (var direction in directions)
            {
                var ray = new Ray(HeadOrigin - Vector3.up * headOffset, direction);
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

        private Vector3 HeadAboveSightOrigin => HeadOrigin +
                                                Vector3.up * (characterControllerEnveloper.Height / 2 +
                                                              characterControllerEnveloper.Radius);

        private Vector3 LeftSightOrigin => BodyOrigin + (-transform.right) * (characterControllerEnveloper.Radius * 2);
        private Vector3 RightSightOrigin => BodyOrigin + (transform.right) * (characterControllerEnveloper.Radius * 2);
        private Vector3 LeftMovableOrigin => BodyOrigin;

        private Ray BoxCastRayClimbOverLedge => new Ray(HeadAboveSightOrigin, transform.forward);
        private Ray BoxCastRayLeftSight => new Ray(LeftSightOrigin, transform.forward);
        private Ray BoxCastRayRightSight => new Ray(RightSightOrigin, transform.forward);
        private Ray BoxCastRayLeftMovable => new Ray(LeftMovableOrigin, transform.right);

        private Vector3 HalfExtents => new Vector3(characterControllerEnveloper.Radius,
            characterControllerEnveloper.Height / 2, characterControllerEnveloper.Radius);

        private float BoxCastRayLength => characterControllerEnveloper.Radius * 2;

        public bool GetIsSightOpened()
        {
            // 위쪽 및 앞쪽의 공간을 검사
            Vector3 upOrigin = HeadOrigin;
            Vector3 upDirection = Vector3.up;
            float upRayLength = characterControllerEnveloper.Height;

            Vector3 forwardBottomOrigin = HeadOrigin;
            Vector3 forwardMiddleRightOrigin = HeadOrigin + Vector3.up * characterControllerEnveloper.Height / 2 + transform.right * characterControllerEnveloper.Radius;
            Vector3 forwardMiddleLeftOrigin = HeadOrigin + Vector3.up * characterControllerEnveloper.Height / 2 - transform.right * characterControllerEnveloper.Radius;
            Vector3 forwardUpOrigin = HeadOrigin + Vector3.up * characterControllerEnveloper.Height;
            
            float forwardRayLength = characterControllerEnveloper.Radius * 3f;

            // 위쪽 공간이 열려있는지 검사
            bool isUpOpen = !Physics.Raycast(upOrigin, upDirection, upRayLength, surfaceLayers);

            bool isForwardBottomOpen = !Physics.Raycast(forwardBottomOrigin, transform.forward, forwardRayLength, surfaceLayers);
            bool isForwardUpOpen = !Physics.Raycast(forwardUpOrigin, transform.forward, forwardRayLength, surfaceLayers);

            bool isForwardMidRightOpen = !Physics.Raycast(forwardMiddleRightOrigin, transform.forward, forwardRayLength, surfaceLayers);
            bool isForwardMidLeftOpen = !Physics.Raycast(forwardMiddleLeftOrigin, transform.forward, forwardRayLength, surfaceLayers);

            // Debug용 Ray 시각화
            Debug.DrawRay(upOrigin, upDirection * upRayLength, isUpOpen ? Color.green : Color.red);
            Debug.DrawRay(forwardBottomOrigin, transform.forward * forwardRayLength, isForwardBottomOpen ? Color.green : Color.red);
            Debug.DrawRay(forwardUpOrigin, transform.forward * forwardRayLength, isForwardUpOpen ? Color.green : Color.red);
            Debug.DrawRay(forwardMiddleRightOrigin, transform.forward * forwardRayLength, isForwardMidRightOpen ? Color.green : Color.red);
            Debug.DrawRay(forwardMiddleLeftOrigin, transform.forward * forwardRayLength, isForwardMidLeftOpen ? Color.green : Color.red);

            // 위쪽과 앞쪽의 두 공간이 모두 열려있는 경우에만 true 반환
            return isUpOpen && isForwardUpOpen && isForwardBottomOpen && isForwardMidRightOpen && isForwardMidLeftOpen;
        }

        public bool GetIsLeftSightOpened()
        {
            float rayLength = characterControllerEnveloper.Radius * 3;
            
            Vector3 origin1 = BodyOrigin + (-transform.right) * characterControllerEnveloper.Radius;
            Vector3 origin2 = origin1 + (-transform.right) * (characterControllerEnveloper.Radius * 2);
            Vector3 origin3 = origin1 + Vector3.up * characterControllerEnveloper.Height / 2 + (-transform.right) * (characterControllerEnveloper.Radius);
            Vector3 origin4 = origin1 + Vector3.down * characterControllerEnveloper.Height / 2 + (-transform.right) * (characterControllerEnveloper.Radius);
            
            bool isLeftOpen = !Physics.Raycast(origin1, -transform.right, rayLength, surfaceLayers);
            bool isOrigin1ForwardOpen = !Physics.Raycast(origin1, transform.forward, rayLength, surfaceLayers);
            bool isOrigin2ForwardOpen = !Physics.Raycast(origin2, transform.forward, rayLength, surfaceLayers);
            bool isOrigin3ForwardOpen = !Physics.Raycast(origin3, transform.forward, rayLength, surfaceLayers);
            bool isOrigin4ForwardOpen = !Physics.Raycast(origin4, transform.forward, rayLength, surfaceLayers);

            // Debug용 Ray 시각화
            Debug.DrawRay(origin1, -transform.right * rayLength, isLeftOpen ? Color.green : Color.red);
            Debug.DrawRay(origin1, transform.forward * rayLength, isOrigin1ForwardOpen ? Color.green : Color.red);
            Debug.DrawRay(origin2, transform.forward * rayLength, isOrigin2ForwardOpen ? Color.green : Color.red);
            Debug.DrawRay(origin3, transform.forward * rayLength, isOrigin3ForwardOpen ? Color.green : Color.red);
            Debug.DrawRay(origin4, transform.forward * rayLength, isOrigin4ForwardOpen ? Color.green : Color.red);

            // 왼쪽과 왼쪽 앞 두 공간이 모두 열려있는 경우에만 true 반환
            return isLeftOpen && isOrigin1ForwardOpen && isOrigin2ForwardOpen && isOrigin3ForwardOpen && isOrigin4ForwardOpen;
        }

        public bool GetIsRightSightOpened()
        {
            float rayLength = characterControllerEnveloper.Radius * 3;
            
            Vector3 origin1 = BodyOrigin + (transform.right) * characterControllerEnveloper.Radius;
            Vector3 origin2 = origin1 + (transform.right) * (characterControllerEnveloper.Radius * 2);
            Vector3 origin3 = origin1 + Vector3.up * characterControllerEnveloper.Height / 2 + (transform.right) * (characterControllerEnveloper.Radius);
            Vector3 origin4 = origin1 + Vector3.down * characterControllerEnveloper.Height / 2 + (transform.right) * (characterControllerEnveloper.Radius);
            
            bool isRightOpen = !Physics.Raycast(origin1, transform.right, rayLength, surfaceLayers);
            bool isOrigin1ForwardOpen = !Physics.Raycast(origin1, transform.forward, rayLength, surfaceLayers);
            bool isOrigin2ForwardOpen = !Physics.Raycast(origin2, transform.forward, rayLength, surfaceLayers);
            bool isOrigin3ForwardOpen = !Physics.Raycast(origin3, transform.forward, rayLength, surfaceLayers);
            bool isOrigin4ForwardOpen = !Physics.Raycast(origin4, transform.forward, rayLength, surfaceLayers);

            // Debug용 Ray 시각화
            Debug.DrawRay(origin1, transform.right * rayLength, isRightOpen ? Color.green : Color.red);
            Debug.DrawRay(origin1, transform.forward * rayLength, isOrigin1ForwardOpen ? Color.green : Color.red);
            Debug.DrawRay(origin2, transform.forward * rayLength, isOrigin2ForwardOpen ? Color.green : Color.red);
            Debug.DrawRay(origin3, transform.forward * rayLength, isOrigin3ForwardOpen ? Color.green : Color.red);
            Debug.DrawRay(origin4, transform.forward * rayLength, isOrigin4ForwardOpen ? Color.green : Color.red);

            // 왼쪽과 왼쪽 앞 두 공간이 모두 열려있는 경우에만 true 반환
            return isRightOpen && isOrigin1ForwardOpen && isOrigin2ForwardOpen && isOrigin3ForwardOpen && isOrigin4ForwardOpen;
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

        [SerializeField] private float topOriginOffset = 1;
        [SerializeField] private float edgeCheckOffset = 0.25f;

        public bool GetIsEdgeOfPlatform()
        {
            // Edge까지의 거리를 초기화 (-1은 충돌이 없음을 나타냄)
            var edgeDistance = -1f;

            if (!VerticalParams.IsWalled || !VerticalParams.IsHeadOpen)
                return false;

            // 캐릭터 최상단에서 Ray를 쏠 시작 위치
            Vector3 topOrigin =
                transform.position + Vector3.up * (characterControllerEnveloper.Height - topOriginOffset);

            // 최상단에서 Ray를 쏠 오프셋 위치
            Vector3 offsetOrigin = topOrigin + Vector3.up * edgeCheckOffset;

            // Ray 방향
            Vector3 rayDirection = transform.forward;

            // Ray 길이
            float rayLength = characterControllerEnveloper.Radius * 2;

            // Debug용 Ray 시각화
            // Debug.DrawRay(topOrigin, rayDirection * rayLength, Color.red);
            // Debug.DrawRay(offsetOrigin, rayDirection * rayLength, Color.blue);

            // Raycast hit 정보를 저장할 변수
            RaycastHit topHitInfo;
            RaycastHit offsetHitInfo;

            // 캐릭터 최상단에서 forward 방향 Ray
            bool isTopHit = Physics.Raycast(topOrigin, rayDirection, out topHitInfo, rayLength, surfaceLayers);

            // 캐릭터 최상단 + offset 위치에서 forward 방향 Ray
            bool isOffsetHit = Physics.Raycast(offsetOrigin, rayDirection, out offsetHitInfo, rayLength, surfaceLayers);

            // Edge 판정 조건을 만족하는 경우
            if (isTopHit && !isOffsetHit)
            {
                // 위에서 아래로 다시 Ray를 쏘아 정확한 Edge point를 구함
                Vector3 downRayOrigin = topHitInfo.point + Vector3.up; // Ray 시작 위치를 충돌 지점 위로 약간 올림
                Vector3 downRayDirection = Vector3.down;
                float downRayLength = 2; // 충분한 길이를 설정
                RaycastHit downHitInfo;

                // Debug.DrawRay(downRayOrigin, downRayDirection * downRayLength, Color.green);

                if (Physics.Raycast(downRayOrigin, downRayDirection, out downHitInfo, downRayLength, surfaceLayers))
                {
                    // 정확한 Edge point를 구한 경우
                    VerticalParams.EdgeHeight = downHitInfo.point.y; // 아래 Ray의 충돌 지점 y 값을 저장
                    VerticalParams.DistanceToTopEdge = downHitInfo.point.y - transform.position.y;
                }
                else
                {
                    // 아래로 Ray를 쏘아도 충돌이 없으면 EdgeHeight를 초기화
                    VerticalParams.EdgeHeight = float.NaN;
                }
            }
            else
            {
                // Edge를 찾지 못한 경우 값 초기화
                VerticalParams.DistanceToTopEdge = -1f;
                VerticalParams.EdgeHeight = float.NaN; // Edge가 없음을 나타냄
            }

            // 기존 조건 반환
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
                Gizmos.color = isHit ? Color.red : Color.green;

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