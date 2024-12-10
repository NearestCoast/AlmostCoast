using System;
using _Project.Managers.Scripts._Core.SaveManager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace _Project.Maps.Climber.Objects
{
    public class FallingMovingPlatform : AccMovingPlatform, ISavable
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

        private void Start()
        {
            if (IsWorked)
            {
                transform.position = Position;
                StartPosition = Position;
                enabled = false;
            }
        }

        private bool IsWorked { get; set; }
        private Vector3 Position { get; set; }
        public override void Move()
        {
            if (rope || IsWorked) return;
            if (!isGoDir)
            {
                IsWorked = true;
                Position = transform.position;
                StartPosition = Position;
                
                // Save(SaveFileData.SelectedSaveFileName);
                
                enabled = false;
            }
            base.Move();
            Acc = Vector3.zero;
        }

        public bool EnrollToSaveManager => true;
        public bool Save(string saveFileName)
        {
            ISavable.EasySave($"{ID}_IsWorked", IsWorked, saveFileName);
            ISavable.EasySave($"{ID}_Position", Position, saveFileName);
            return true;
        }

        public bool Load(string saveFileName)
        {
            IsWorked = ISavable.EasyLoad<bool>($"{ID}_IsWorked", saveFileName);
            Position = ISavable.EasyLoad<Vector3>($"{ID}_Position", saveFileName);
            return true;
        }
    }
}