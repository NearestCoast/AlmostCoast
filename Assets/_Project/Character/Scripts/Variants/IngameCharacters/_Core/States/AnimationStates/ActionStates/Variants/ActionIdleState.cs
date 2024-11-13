using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.ActionStates
{
    public class ActionIdleState : ActionState
    {
        public override StateType Type => StateType.ActionIdle;
        
        [SerializeField, TitleGroup("Animation")] private ClipTransition anim;

        protected override void OnEnable()
        {
            base.OnEnable();
            AnimancerState = AnimationStateConductor.AttackLayer.Play(anim);
        }
        
        public override bool CanExitState => true;
    }
}