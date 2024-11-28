using _Project._Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.ActionStates.CombatActionsState
{
    public class DyingState : ActionLayerClipActionState
    {
        public override StateType Type => StateType.Dying;
        public override bool CanExitState => false;

        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.SetCrowdControlled();
            characterControllerEnveloper.OnDying();
        }
        
        public override Vector3 GetVelocity()
        {
            if (AnimNormalizedTime >= 1)
            {
                if (masterCharacter.IsDying) masterCharacter.Die();
            }
            
            if (masterCharacter.HittingInfo.hitObject && masterCharacter.HittingInfo.hitObject.SideEffect == SideEffect.KnockBack)
            {
                var dir = masterCharacter.HittingInfo.GetHitDirectionFromCenter();
                return dir * 10 * Time.deltaTime + MoveParams.Gravity;
            }

            return base.GetVelocity();
        }
    }
}