using _Project._Core;
using _Project.Combat.HitObjects;
using UnityEngine;

namespace _Project.Characters._Core
{
    [RequireComponent(typeof(Statistics))]
    public class Character : MonoBehaviour, IDamageReceiver
    {
        protected Statistics Stat { get; private set; }
        
        public bool IsDying { get; protected set; }
        public bool IsDead { get; private set; }
        
        protected virtual void Awake()
        {
            Stat = GetComponent<Statistics>();
        }

        protected virtual void Start()
        {
            Stat.OnDie.AddListener(SetDying);
        }

        protected virtual void SetDying()
        {
            
        }

        public virtual void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            Stat.TakeDamage(damage);
        }

        public virtual void Die()
        {
            IsDead = true;
        }
    }
}