using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects.Variants
{
    public class DingdongLockRollingCube : RollingCube
    {
        [SerializeField] private List<Dingdong> ringdongs = new List<Dingdong>();

        public List<Dingdong> Ringdongs
        {
            get => ringdongs;
            set => ringdongs = value;
        }

        // 회전을 중단하는 함수
        public override void ResetCube()
        {
            base.ResetCube();
            
            foreach (var dingdong in ringdongs)
            {
                dingdong.Restore();
            }
        }

        public override void Work(Transform playerT)
        {
            foreach (var dingdong in ringdongs)
            {
                if (!dingdong.IsCaptured) return;
            }
            
            base.Work(playerT);
        }
    }
}