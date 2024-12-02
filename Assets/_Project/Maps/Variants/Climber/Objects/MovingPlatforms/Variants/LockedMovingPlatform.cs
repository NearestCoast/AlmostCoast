using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class LockedMovingPlatform : AccMovingPlatform, ILeverUnlockable
    {
        [SerializeField] private string leverID;

        public string LeverID
        {
            get => leverID;
            set => leverID = value;
        }
        
        [SerializeField] private Lever lever;

        public Lever Lever
        {
            get => lever;
            set => lever = value;
        }
        
        public Transform Transform => transform;

        protected override void Awake()
        {
            base.Awake();
            speedRate = 0.1f;
            returnSpeedRate = 0.1f;
            accPow = 0;
            accMaxLength = 0;
        }

        private bool IsOpened { get; set; }
        public void Open()
        {
            IsOpened = true;
        }

        public override void Move()
        {
            if (!IsOpened) return;
            base.Move();
            Acc = Vector3.zero;
        }
    }
}