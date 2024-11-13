using System;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters._Core.Input
{
    public class InputChecker : MonoBehaviour
    {
        [SerializeField] private Character character;

        private void Awake()
        {
            character = GetComponentInParent<Character>();
        }

        private Vector2 direction2;
        private static Camera Cam => Camera.main;
        public Vector3 CamToCharacter3 => (character.transform.position - Cam.transform.position).normalized;

        public Vector2 Direction2
        {
            get => direction2;
            set
            {
                direction2 = value;
                if (direction2 == Vector2.zero) HorizontalDirection3=Vector3.zero;
                else HorizontalDirection3 = CalculateMoveDirection(CamToCharacter3, Direction2).XYZ3toX0Z3();
                HorizontalDirection3VerticalNegative = CalculateMoveDirectionVerticalNegative(-CamToCharacter3, Direction2).XYZ3toX0Z3();


                var dir8 = Direction2.GetEightDirection();
                var right = Quaternion.Euler(0, 90, 0) * character.transform.forward;
                VerticalDirection3 = (character.transform.up * dir8.y + right * dir8.x).normalized;
            }
        }
        
        [ShowInInspector, DisableInPlayMode] public Vector3 HorizontalDirection3 { get; set; }
        public Vector3 HorizontalDirection3VerticalNegative { get; private set; }
        public Vector3 VerticalDirection3 { get; private set; }
        
        private static Vector3 CalculateMoveDirection(Vector3 forward, Vector2 inputDirection)
        {
            var right = Quaternion.Euler(0, 90, 0) * forward;
            forward.y = 0;
            right.y = 0;
            return (forward * inputDirection.y + right * inputDirection.x).normalized;
        }
        
        private static Vector3 CalculateMoveDirectionVerticalNegative(Vector3 forward, Vector2 inputDirection)
        {
            var right = Quaternion.Euler(0, -90, 0) * forward;
            forward.y = 0;  
            right.y = 0;
            return (forward * inputDirection.y + right * inputDirection.x).normalized;
        }
        
        public Vector2 CamDirection2 { get; set; }
    }
}