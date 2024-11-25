using _Project._Core;
using _Project.Combat.HitObjects;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class Lever : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private LeverDoor connectedDoor;

        public LeverDoor ConnectedDoor
        {
            get => connectedDoor;
            set => connectedDoor = value;
        }
        
        public void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            ConnectedDoor.Open();
        }
    }
}