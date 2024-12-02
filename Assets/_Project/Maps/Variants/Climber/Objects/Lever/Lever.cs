using _Project._Core;
using _Project.Combat.HitObjects;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class Lever : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private Transform connectedUnlockableT;

        private ILeverUnlockable connectedUnlockable;

        public ILeverUnlockable ConnectedUnlockable
        {
            set
            {
                connectedUnlockable = value;
                connectedUnlockableT = value.Transform;
            }
        }

        public void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            connectedUnlockable = connectedUnlockableT.GetComponent<ILeverUnlockable>();
            connectedUnlockable.Open();
        }
    }
}