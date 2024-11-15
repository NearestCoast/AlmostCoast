using System;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    public class CharacterControllerEnveloper : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        
#if UNITY_EDITOR

        private void OnValidate()
        {
            characterController ??= GetComponent<UnityEngine.CharacterController>();
        }
#endif

        public float CurrentScale => transform.localScale.x;

        public float SlopeLimit => characterController.slopeLimit;

        public float Height 
        {
            get => characterController.height * CurrentScale;
            set => characterController.height = value / CurrentScale;
        }
        
        public Vector3 Center  
        {
            // center는 스케일에 영향을 받지 않도록 그대로 반환
            get => characterController.center * CurrentScale;
            set => characterController.center = value / CurrentScale;
        }
        
        public float Radius => characterController.radius * CurrentScale;
        
        public float SkinWidth => characterController.skinWidth * CurrentScale;

        public bool IsGrounded => characterController.isGrounded;

        private void Awake()
        {
            characterController ??= GetComponent<UnityEngine.CharacterController>();
            OriginalHeight = characterController.height;
            OriginalCenter = characterController.center;
            OriginalLayer = gameObject.layer;
        }

        public void OnSpawn()
        {
            characterController.enabled = true;
            characterController.height = OriginalHeight;
            characterController.center = OriginalCenter;
        }
            

        public void Move(Vector3 motion)
        {
            if (characterController.enabled) characterController.Move(motion);
        }
        
        private float OriginalHeight { get; set; }
        private Vector3 OriginalCenter { get; set; }
        private int OriginalLayer { get; set; }

        public void OnCrouchStart()
        {
            characterController.height = OriginalHeight / 2;
            characterController.center = OriginalCenter / 2;
        }

        public void OnSlideStart()
        {
            characterController.height = OriginalHeight / 4;
            characterController.center = OriginalCenter / 4;
        }

        public void ResetCharacterController()
        {
            characterController.height = OriginalHeight;
            characterController.center = OriginalCenter;
        }

        public void OnDying()
        {
            gameObject.layer = LayerMask.NameToLayer("UnCollidable");
            // characterController.height = 0;
            // characterController.radius = 0;
        }

        public void OnDead()
        {
            characterController.enabled = false;
            gameObject.layer = OriginalLayer;
            // characterController.height = OriginalHeight / 4;
            // characterController.center = OriginalCenter / 4;
        }
    }
}