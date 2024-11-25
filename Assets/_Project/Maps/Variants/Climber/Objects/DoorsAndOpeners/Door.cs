using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private Level level;
        public Level Level
        {
            get => level;
            set => level = value;
        }

        [SerializeField] private string id;
        public string ID
        {
            get => id;
            set => id = value;
        }
        
        [SerializeField] private Vector3 targetPosition;
        public Vector3 TargetPosition
        {
            get => targetPosition;
            set => targetPosition = value;
        }

        protected Vector3 StartPosition { get; set; }

        protected virtual void Awake()
        {
            throw new NotImplementedException();
        }

        public virtual void Open()
        {
            
        }

        protected virtual void ResetDoor()
        {
            
        }
    }
}