using UnityEngine; // Unity 관련 클래스 사용을 위해 필요

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Strawberry : Collectable
    {
        [SerializeField] private float rotationSpeed = 100f; // 회전 속도(도/초)

        private void Update()
        {
            // Vector3.up 축을 기준으로 rotationSpeed * Time.deltaTime 만큼 회전
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }
}