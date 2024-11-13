using System;
using _Project.Cameras;
using _Project.Characters._Core.States.CommonStates;
using _Project.Characters._Core.Input;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates
{
    public class LockState: CommonState
    {
        public enum StateType
        {
            LockOff,
            LockOn,
        }

        [ShowInInspector] public virtual StateType Type { get; }
        protected LockState NextState => CommonStateConductor.BrightnessStateMachine.NextState as LockState;
        protected LockState PrevState => CommonStateConductor.BrightnessStateMachine.PreviousState as LockState;
        
        
        protected _Project.Characters._Core.Character masterCharacter;
        protected InputChecker inputChecker;
        protected CameraTarget moveCameraTarget;
        
        [SerializeField] protected Image lockOnMarker; // LockOn 표시용 UI 이미지
        [SerializeField] protected bool isPlayer;

        public bool IsPlayer
        {
            get => isPlayer;
            set => isPlayer = value;
        }

        protected override void Awake()
        {
            base.Awake();
            masterCharacter = GetComponentInParent<_Project.Characters._Core.Character>();
            inputChecker = masterCharacter.transform.GetComponentInChildren<InputChecker>();
            moveCameraTarget = GameObject.Find("PlayerCameraTarget").GetComponent<CameraTarget>();
        }

        public virtual void SwitchTarget()
        {
            
        }

        public virtual Vector3 CameraTargetUpdate()
        {
            var dir = (transform.position + Vector3.up * 1.5f) - moveCameraTarget.transform.position;
            return dir * (10 * Time.deltaTime);
        }

        public virtual void UpdateLockOnMarker()
        {
            
        }
    }
}