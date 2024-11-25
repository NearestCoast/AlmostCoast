using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class DingdongDoor : MonoBehaviour
    {
        [SerializeField] private Level level;
        [SerializeField] private List<Dingdong> dingdongs = new List<Dingdong>();

        public Level Level
        {
            get => level;
            set => level = value;
        }

        public List<Dingdong> Dingdongs
        {
            get => dingdongs;
            set => dingdongs = value;
        }

        [SerializeField] private string id;
        [SerializeField] private Vector3 targetPosition;

        public string ID
        {
            get => id;
            set => id = value;
        }

        public Vector3 TargetPosition
        {
            get => targetPosition;
            set => targetPosition = value;
        }

        private Vector3 StartPosition { get; set; }
        private float Length { get; set; }
        private void Awake()
        {
            StartPosition = transform.position;
            Length = Vector3.Distance(StartPosition, TargetPosition);
        }

        private bool IsAllDingdongCaptured
        {
            get
            {
                var captureCount = 0;
                foreach (var dingdong in dingdongs)
                {
                    if (dingdong.IsCaptured) captureCount++;
                }

                return captureCount == dingdongs.Count;
            }
        }

        private bool IsMoveStarted { get; set; }
        private void Update()
        {
            if (!IsAllDingdongCaptured) return;
            if (IsMoveStarted) return;
            transform.DOMove(TargetPosition, 1);
            IsMoveStarted = true;
        }

        public void ResetDingdongDoor()
        {
            foreach (var dingdong in dingdongs)
            {
                dingdong.Restore();
            }

            IsMoveStarted = false;
            gameObject.SetActive(false);
            transform.position = StartPosition;
            gameObject.SetActive(true);
        }
    }
}