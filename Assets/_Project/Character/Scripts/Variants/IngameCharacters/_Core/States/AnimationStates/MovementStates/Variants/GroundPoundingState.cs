using System;
using System.Collections.Generic;
using _Project.Combat.HitObjects;
using _Project.Effect;
using _Project.Utils;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class GroundPoundingState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.GroundPounding;
        
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

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return !GroundParams.IsGrounded;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                var nextStateType = NextState.Type;
                
                var value = nextStateType switch
                {
                    StateType.Landing => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                return value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            MoveParams.ResetIsGroundPoundingEnded();
        }


        [SerializeField, TitleGroup("Effects")] private VisualEffect collisionVFX;
        [SerializeField, TitleGroup("Effects")] private AudioSource collisionSFX;
        [SerializeField, TitleGroup("Effects")] private HitObject collisionHitObject;
        
        public override void OnExitState()
        {
            base.OnExitState();

            MoveParams.SetIsGroundPoundingEnded();
            DelayOneSecond().Forget();
            collisionVFX.gameObject.SetActive(true);
            collisionVFX.Play();
            collisionSFX.Play();
            collisionHitObject.Invoke();
        }
        
        private async UniTask DelayOneSecond()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(afterTime)); // 딜레이 (밀리초 단위)
            MoveParams.ResetIsGroundPoundingEnded();
        }
    }
 
    public partial class GroundPoundingState : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private float gravityMultiplier = 2;
        [SerializeField, TitleGroup("Specific")] private float afterTime = 1;
        
        protected override Vector3 GetVelocity() 
        {
            MoveParams.GravityTime += Time.deltaTime;   
            MoveParams.Gravity = movementStateValues.GetGravity() * gravityMultiplier;
            if (MoveParams.Gravity.y > 0) Debug.Log("FFFFFFF");
                
            if (GroundParams.IsGrounded && transform.position.y + MoveParams.Gravity.y < GroundParams.GroundPoint.y)
            {
                MoveParams.Gravity = (transform.position - GroundParams.GroundPoint).XYZ3to0Y03();
            }
                
            return MoveParams.Gravity;
        }

        protected override Quaternion GetRotation()
        {
            return transform.rotation;
        }
    }
}