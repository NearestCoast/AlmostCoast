using _Project.Combat.HitObjects;
using _Project.Effect;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class HeadKickState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.HeadKick;
        
        
        [SerializeField, TitleGroup("Effects")] private VisualEffect collisionVFX;
        [SerializeField, TitleGroup("Effects")] private AudioSource collisionSFX;
        [SerializeField, TitleGroup("Effects")] private HitObject collisionHitObject;
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            var collisionEffectHolder = GetComponentInChildren<CollisionEffectHolder>(true);
            collisionVFX = collisionEffectHolder.GetComponentInChildren<VisualEffect>(true);
            collisionSFX = collisionEffectHolder.GetComponentInChildren<AudioSource>(true);
            collisionHitObject = collisionEffectHolder.GetComponentInChildren<HitObject>(true);
        }
#endif
        public override bool CanExitState => IsAnimEnded;
        
        public override void OnExitState()
        {
            base.OnExitState();
            
            collisionVFX.gameObject.SetActive(true);
            collisionVFX.Play();
            collisionSFX.Play();
            collisionHitObject.Invoke();
        }

        protected override Vector3 GetVelocity()
        {
            return base.GetVelocity();
        }
    }
}