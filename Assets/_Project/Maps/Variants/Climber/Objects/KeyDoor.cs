using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class KeyDoor : MonoBehaviour
    {
        [SerializeField] private Level level;

        public Level Level
        {
            get => level;
            set => level = value;
        }
    }
}