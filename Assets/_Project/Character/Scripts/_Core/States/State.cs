using System;
using Animancer;
using Animancer.FSM;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters._Core.States
{
    public class State : StateBehaviour
    { 
        [SerializeField, TitleGroup("StateEvent"), HideLabel, InlineProperty, PropertyOrder(10), PropertySpace(SpaceAfter = 20)]
        private StateEvent stateEvent = new StateEvent();

        private AnimancerEvent.Sequence Events;
        // private AnimancerEvent.Sequence Events { get; set; }
        
        protected AnimancerState AnimancerState { get; set; }
        
        protected virtual void Awake()
        {
            Events = stateEvent.InitializeEvents();
        }

        protected virtual void OnEnable()
        {
            
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            stateEvent.Initialize(gameObject);
        }
#endif

        public override void OnEnterState()
        {
            base.OnEnterState();
            if (AnimancerState is not null)
            {
                if (AnimancerState.Events(this, out var events))
                {
                    events.AddRange(Events);
                }
            }
            else
            {
                
            }
        }
    }
}