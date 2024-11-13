using System;
using _Project.Characters.IngameCharacters.Core;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Cameras
{
    public class CameraTarget : MonoBehaviour
    {
        [SerializeField] private CinemachineFreeLook cinemachineFreeLook;
        [SerializeField] private LockParams lockParams;

        private void Awake()
        {
            cinemachineFreeLook.m_RecenterToTargetHeading = new AxisState.Recentering(false, 0, 0); 
        }

        public void Move(Vector3 motion)
        {
            transform.position += motion;
        }

        public async UniTask ResetRotationAsync(Vector3 direction)
        {
            // 회전 리셋 처리
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            cinemachineFreeLook.m_RecenterToTargetHeading = new AxisState.Recentering(true, 0, 0);

            // 0.1초 대기
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            // 리센터링 끄기
            cinemachineFreeLook.m_RecenterToTargetHeading = new AxisState.Recentering(false, 0, 0);
        }

        public void RecenterImmediate()
        {
            cinemachineFreeLook.m_RecenterToTargetHeading = new AxisState.Recentering(true, 0, 0);
        }

        public void ProvideLookAtDirection(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void CancelRecenter()
        {
            cinemachineFreeLook.m_RecenterToTargetHeading = new AxisState.Recentering(false, 0, 0);
        }

        public void SetRotation(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}