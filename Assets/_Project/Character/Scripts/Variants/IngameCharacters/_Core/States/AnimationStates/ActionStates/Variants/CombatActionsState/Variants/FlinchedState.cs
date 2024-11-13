using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters.IngameCharacters.Core.ActionStates.CombatActionsState
{
    public class FlinchedState : ActionLayerClipActionState
    {
        public override StateType Type => StateType.Flinched;
        
        public override bool CanEnterState => PrevState.StateTime < 0.3f;

        public override bool CanExitState
        {
            get
            {
                if (NextState.Type == StateType.Flinched) return true;
                return IsAnimEnded;
            }
        }

        [SerializeField] private UnityEvent onExitState;
        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.SetCrowdControlled();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.ResetCrowdControlled();
            onExitState?.Invoke();
        }
    }
}