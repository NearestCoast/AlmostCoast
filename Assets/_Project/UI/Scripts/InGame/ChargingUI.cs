using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using Renge.PPB;
using UnityEngine;

namespace _Project.UI.InGame
{
    public class ChargingUI : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0, 2, 0); // UI 위치 오프셋
        private static Camera mainCamera => Camera.main;
        private ProceduralProgressBar progressBar;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            progressBar = GetComponent<ProceduralProgressBar>();
        }

        public void OnUpdate(Vector3 targetPosition)
        {
            // 월드 좌표에 오프셋을 더해 화면 좌표로 변환
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition) + offset; // 오프셋 적용 후 변환

            // UI가 카메라의 앞쪽에 있는 경우에만 위치 업데이트
            if (screenPosition.z > 0)
            {
                rectTransform.position = screenPosition;
                rectTransform.gameObject.SetActive(true); // UI 활성화
            }
            else
            {
                rectTransform.gameObject.SetActive(false); // 카메라 뒤에 있으면 UI 비활성화
            }
        }

        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private Color orangeColor = new Color(1f, 0.65f, 0f); // 주황색
        [SerializeField] private Color yellowColor = Color.yellow;
        [SerializeField] private Color greenColor = Color.green;

        public void SetColor(float value)
        {
            // 범위에 따라 색상 보간
            Color targetColor;

            if (value < 0.5f)
            {
                // 0 ~ 0.5 구간: 빨강에서 주황으로, 주황에서 노랑으로
                targetColor = Color.Lerp(
                    Color.Lerp(redColor, orangeColor, value * 2),
                    Color.Lerp(orangeColor, yellowColor, value * 2),
                    value * 2
                );
            }
            else
            {
                // 0.5 ~ 1 구간: 노랑에서 초록으로
                float adjustedValue = (value - 0.5f) * 2;
                targetColor = Color.Lerp(
                    Color.Lerp(orangeColor, yellowColor, adjustedValue),
                    Color.Lerp(yellowColor, greenColor, adjustedValue),
                    adjustedValue
                );
            }

            progressBar.InnerColor = targetColor;
        }
    }
}
