using DG.Tweening;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class LeverDoor : Door, ILeverUnlockable
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
        
        private float Length { get; set; }
        protected override void Awake()
        {
            StartPosition = transform.position;
            Length = Vector3.Distance(StartPosition, TargetPosition);
        }

        public Transform Transform => transform;

        public override void Open()
        {
            base.Open();
            transform.DOMove(TargetPosition, Length / 10);
        }

        protected override void ResetDoor()
        {
            base.ResetDoor();
        }
    }
}