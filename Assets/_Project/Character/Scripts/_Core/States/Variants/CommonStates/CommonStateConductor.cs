using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters._Core.States.CommonStates
{
    public partial class CommonStateConductor : MonoBehaviour
    {
        private void Start()
        {
            BrightnessStateMachine.TrySetDefaultState();
            LockStateMachine.ForceSetDefaultState();
        }
    }

    public partial class CommonStateConductor : MonoBehaviour
    {
        [SerializeField] private CommonState.StateMachine brightnessStateMachine;
        public CommonState.StateMachine BrightnessStateMachine => brightnessStateMachine;
        
        public CommonState CurrentBrightnessState => BrightnessStateMachine.CurrentState;
        
        public void TrySetBrightnessState(CommonState state)
        {
            if (!state.gameObject.activeSelf) return;
            if (state == CurrentBrightnessState) BrightnessStateMachine.TryResetState(state);
            else BrightnessStateMachine.TrySetState(state);
            
            // CommonStateMachine.TrySetState(state);
        }

        public void ForceSetBrightnessState(CommonState state)
        {
            if (!state.gameObject.activeSelf) return;
            BrightnessStateMachine.ForceSetState(state);
        }
    }


    public partial class CommonStateConductor : MonoBehaviour
    {
        [SerializeField] private CommonState.StateMachine lockStateMachine;
        public CommonState.StateMachine LockStateMachine => lockStateMachine;
        
        public CommonState CurrentLockState => LockStateMachine.CurrentState;
        
        public void TrySetLockState(CommonState state)
        {
            if (!state.gameObject.activeSelf) return;
            if (state == CurrentLockState) LockStateMachine.TryResetState(state);
            else LockStateMachine.TrySetState(state);
            
            // CommonStateMachine.TrySetState(state);
        }
        
        public void ForceSetLockState(CommonState state)
        {
            if (!state.gameObject.activeSelf) return;
            LockStateMachine.ForceSetState(state);
        }
    }
    
}