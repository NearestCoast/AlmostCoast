using _Project.Combat.HitObjects;
using UnityEngine;

namespace _Project._Core
{
    public enum SideEffect
    {
        None,
        Flinch,
        KnockBack,
    }
    
    public interface IReceiver
    {

    }

    public interface IDamageReceiver : IReceiver
    {
        public void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None);
    }

}