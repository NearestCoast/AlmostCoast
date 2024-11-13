using UnityEngine;
using UnityEngine.UI;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates.Variants
{
    public class LockOffState : LockState
    {
        public override StateType Type => StateType.LockOff;
        
        public override bool CanEnterState
        {
            get
            {
                // Debug.Log("TryLockOff");
                return LockParams.IsLockingOn;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            LockParams.LockOff();
            lockOnMarker.enabled = false; // LockOn 대상이 없으면 비활성화s
            
            moveCameraTarget.CancelRecenter();
        }
    }
}