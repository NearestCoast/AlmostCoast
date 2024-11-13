using _Project.Combat.HitObjects;

namespace _Project.Characters.IngameCharacters.Core.ActionStates.RangeAttacks
{
    public class RangeAttack : AttackState
    {
        public override StateType Type => StateType.None;
        private HitObjectProjectile projectile;

        protected override void Awake()
        {
            base.Awake();
            projectile = GetComponentInChildren<HitObjectProjectile>(true);
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            if (LockParams.LockOnTarget)
            {
                projectile.Initialize(transform.position + characterControllerEnveloper.Center, LockParams.LockOnTarget.GetComponent<IngameCharacter>());
                projectile.Invoke();
            }
            else
            {
                if (ClosestEnemy)
                {
                    projectile.Initialize(transform.position + characterControllerEnveloper.Center, ClosestEnemy);
                    projectile.Invoke();
                }
                else
                {
                    projectile.Initialize(transform.position + characterControllerEnveloper.Center, transform.position + transform.forward * ActionRange);
                    projectile.Invoke();
                }
            }
            
        }
    }
}