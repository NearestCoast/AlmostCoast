using System;
using _Project.Characters.IngameCharacters.Core;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace _Project.Cameras
{
    public class CameraTarget : MonoBehaviour
    {
        [SerializeField] private CinemachineOrbitalFollow cinemachineOrbitalFollow;
        [SerializeField] private CinemachineInputAxisController inputAxisController;
        [SerializeField] private LockParams lockParams;

        private void Awake()
        {
            // cinemachineOrbitalFollow.RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.TrackingTarget;
        }

        public void Move(Vector3 motion)
        {
            transform.position += motion;
        }

        public async UniTask ResetRotationAsync(Vector3 direction)
        {
            // 회전 리셋 처리
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            
            Recenter();

            // 0.1초 대기
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            ResetRecenter();
        }

        public void RecenterImmediate()
        {
            if (isRecentering) return;
            Recenter();
        }

        public void ProvideLookAtDirection(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void CancelRecenter()
        {
            ResetRecenter();
        }

        public void SetRotation(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private bool isRecentering;
        private void Recenter()
        {
            isRecentering = true;
            cinemachineOrbitalFollow.VerticalAxis.Recentering.Time = 0;
            cinemachineOrbitalFollow.VerticalAxis.Recentering.Wait = 0;
            cinemachineOrbitalFollow.VerticalAxis.Recentering.Enabled = true;
            cinemachineOrbitalFollow.HorizontalAxis.Recentering.Time = 0;
            cinemachineOrbitalFollow.HorizontalAxis.Recentering.Wait = 0;
            cinemachineOrbitalFollow.HorizontalAxis.Recentering.Enabled = true;
            inputAxisController.enabled = false;
        }

        private void ResetRecenter()
        {
            isRecentering = false;
            cinemachineOrbitalFollow.VerticalAxis.Recentering.Time = 0;
            cinemachineOrbitalFollow.VerticalAxis.Recentering.Wait = 0;
            cinemachineOrbitalFollow.VerticalAxis.Recentering.Enabled = false;
            cinemachineOrbitalFollow.HorizontalAxis.Recentering.Time = 0;
            cinemachineOrbitalFollow.HorizontalAxis.Recentering.Wait = 0;
            cinemachineOrbitalFollow.HorizontalAxis.Recentering.Enabled = false;
            inputAxisController.enabled = true;
        }
    }
}