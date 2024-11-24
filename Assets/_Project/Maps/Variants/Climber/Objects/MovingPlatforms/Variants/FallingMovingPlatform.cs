using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects
{
    public class FallingMovingPlatform : AccMovingPlatform
    {
        [FormerlySerializedAs("ropeID")] [SerializeField] private string ropeBrokableID;
        [SerializeField] private Brokable rope;

        public string RopeBrokableID
        {
            get => ropeBrokableID;
            set => ropeBrokableID = value;
        }

        public Brokable Rope
        {
            get => rope;
            set => rope = value;
        }

        private bool IsWorked { get; set; }
        public override void Move()
        {
            if (rope || IsWorked) return;
            if (!isGoDir)
            {
                IsWorked = true;
                enabled = false;
            }
            base.Move();
            Acc = Vector3.zero;
        }
    }
}