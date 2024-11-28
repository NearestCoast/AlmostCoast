using System;
using UnityEngine;

namespace _Project.UI.InGame
{
    public class EnemyWorldHealthBar : HealthBar
    {
        private RectTransform uiElement;
        private UIElementAspectRatio aspectRatio;

        private LayerMask obstacleLayers; // 장애물 레이어

        private Camera mainCamera => Camera.main;

        protected override void Awake()
        {
            base.Awake();
            uiElement = GetComponent<RectTransform>();
            aspectRatio = GetComponent<UIElementAspectRatio>();
            obstacleLayers = 1 << LayerMask.NameToLayer("Ground") |
                             1 << LayerMask.NameToLayer("GroundUnlit") |
                             1 << LayerMask.NameToLayer("Wall");
        }

        public void UpdateHealthBarPosition(Vector3 worldPosition, bool show)
        {
            if (IsClosed) return;
            // 월드 좌표를 화면 좌표로 변환
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // UI 요소 위치 업데이트
            if (show && screenPosition.z > 0 && screenPosition.z < 60) // 카메라 앞에 있는 경우만 업데이트
            { 
                // 카메라와의 거리 계산
                float distance = Vector3.Distance(mainCamera.transform.position, worldPosition);

                // 벽에 가려진 경우 확인
                if (IsObstructed(worldPosition))
                {
                    uiElement.gameObject.SetActive(false); // 벽에 가려진 경우 비활성화
                    return;
                }
                
                if (!uiElement.gameObject.activeSelf) uiElement.gameObject.SetActive(true);
                
                // 거리 기반으로 HeightRatio와 WidthRatio 설정
                float widthSizeMultiplier = 1 / distance; // 거리 반비례로 크기 감소
                widthSizeMultiplier = Mathf.Clamp(widthSizeMultiplier, 0.01f, 0.1f); // 최소/최대 값 설정
                
                aspectRatio.CurrentAspectRatio.widthRatio = widthSizeMultiplier;
                aspectRatio.CurrentAspectRatio.heightRatio = widthSizeMultiplier * 0.1f;

                // 동적으로 크기 재조정
                aspectRatio.SetAspect();
                
                uiElement.position = screenPosition;
            }
            else
            {
                uiElement.gameObject.SetActive(false); // 카메라 뒤에 있으면 UI 비활성화
            }
            
            return;
            bool IsObstructed(Vector3 targetPosition)
            {
                Ray ray = new Ray(mainCamera.transform.position, targetPosition - mainCamera.transform.position);
                float distance = Vector3.Distance(mainCamera.transform.position, targetPosition);
            
                if (Physics.Raycast(ray, out RaycastHit hit, distance, obstacleLayers))
                {
                    // Raycast가 몬스터가 아닌 다른 객체와 충돌하면 가려진 상태로 판단
                    return hit.collider.gameObject != gameObject;
                }
                return false; // Raycast가 장애물에 막히지 않은 경우 가려지지 않음
            }
        }

        private bool IsClosed { get; set; }
        public void Close()
        {
            IsClosed = true;
            uiElement.gameObject.SetActive(false);
        } 
    }
}
