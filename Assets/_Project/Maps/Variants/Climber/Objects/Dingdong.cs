using System;
using _Project.Characters._Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects
{
    public class Dingdong : MonoBehaviour
    {
        [SerializeField] public string masterID;

        public string MasterID
        {
            get => masterID;
            set => masterID = value;
        }

        [SerializeField] private float rotationDuration = 2;
        
        public void Rotate()
        {
            // Z축을 기준으로 회전하는 트윈 설정
            transform.DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)  // 일정한 속도로 회전
                .SetLoops(-1, LoopType.Restart); // 무한 반복
        }

        public bool IsCaptured { get; set; }

        public void BeCaptured()
        {
            IsCaptured = true;
            gameObject.SetActive(false);
        }

        public void Restore()
        {
            IsCaptured = false;
            gameObject.SetActive(true);
        }
        
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (IsCaptured) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var playerCharacter = other.attachedRigidbody.GetComponent<_Project.Characters._Core.Character>();
                BeCaptured();
            }
        }
    }
}