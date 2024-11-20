using System;
using System.Collections.Generic;
using _Project.Cameras;
using _Project.Characters._Core.Input;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using Animancer;
using Animancer.FSM;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace _Project.Characters._Core.States.AnimationStates
{
    public partial class AnimationState : State
    {
        [Serializable]
        public class StateMachine : StateMachine<AnimationState>.WithDefault
        {
        }

        [SerializeField, TitleGroup("Animation")] private AnimationStateConductor animationStateConductor;
        protected AnimationStateConductor AnimationStateConductor => animationStateConductor;

        protected AnimationState NextAnimationState => AnimationStateConductor.MovementStateMachine.NextState;
        protected AnimationState PrevAnimationState => AnimationStateConductor.MovementStateMachine.PreviousState;

        protected virtual bool IsAnimEnded => (AnimancerState.NormalizedTime >= animCutEndNormalizedTime);

        protected MoveParams MoveParams;
        protected VerticalParams VerticalParams;
        protected GroundParams GroundParams;
        protected LockParams LockParams;
        
        protected InputChecker inputChecker;
        protected IngameCharacter masterCharacter;
        protected MovementStateValues movementStateValues;
        protected CharacterControllerEnveloper characterControllerEnveloper;
        protected CameraTarget moveCameraTarget;
        protected LayerMask surfaceLayers;
        
        protected Vector2 InputDirection => inputChecker.Direction2;
        protected Vector3 HorizontalDirection3 => inputChecker.HorizontalDirection3;
        protected Vector3 HorizontalDirection3VerticalNegative => inputChecker.HorizontalDirection3VerticalNegative;
        protected Vector3 VerticalDirection3 => inputChecker.VerticalDirection3;
        protected Vector3 CamToCharacter3 => inputChecker.CamToCharacter3;
        
        [SerializeField, TitleGroup("Animation")] protected float animCutStartNormalizedTime = 0;
        [SerializeField, TitleGroup("Animation")] protected float animCutEndNormalizedTime = 1;

        protected override void Awake()
        {
            base.Awake();
            animationStateConductor = GetComponentInParent<AnimationStateConductor>();
            MoveParams = GetComponentInParent<MoveParams>();
            VerticalParams = GetComponentInParent<VerticalParams>();
            GroundParams = GetComponentInParent<GroundParams>();
            LockParams = GetComponentInParent<LockParams>();
            
            
            masterCharacter = GetComponentInParent<IngameCharacter>();
            characterControllerEnveloper = GetComponentInParent<CharacterControllerEnveloper>();
            movementStateValues = GetComponentInParent<MovementStateValues>();
            moveCameraTarget = GameObject.Find("PlayerCameraTarget").GetComponent<CameraTarget>();
            surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("GroundUnlit") | 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Player");
            inputChecker = masterCharacter.transform.GetComponentInChildren<InputChecker>();
        }
    }
}