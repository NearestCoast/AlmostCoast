using System;
using _Project.Characters.IngameCharacters.Core;
using UnityEngine;
using Animancer;
using Animancer.FSM;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace _Project.Characters._Core.States.CommonStates
{
    public class CommonState : State
    {
        [Serializable]
        public class StateMachine : StateMachine<CommonState>.WithDefault { }

        [SerializeField, TitleGroup("Animation")] private CommonStateConductor commonStateConductor;
        protected CommonStateConductor CommonStateConductor => commonStateConductor;
        
        protected LockParams LockParams;
        
        protected override void Awake()
        {
            base.Awake();
            gameObject.GetComponentInParentOrChildren(ref commonStateConductor);
            LockParams = GetComponentInParent<LockParams>();
        }
        
        public float StateTime { get; private set; }
        public void ProgressStateTime()
        {
            StateTime += Time.deltaTime;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            StateTime = 0;
        }
    }
}