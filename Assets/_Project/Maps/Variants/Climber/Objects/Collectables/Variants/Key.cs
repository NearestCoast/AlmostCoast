using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Key : Collectable
    {
        public enum Type
        {
            Silver,
            Gold
        }

        [SerializeField] private Type type;

        public Type KeyType => type;
        
        protected override void Work()
        {
            base.Work();
            playerCharacter.AddKey(type);
            Debug.Log("Player Collected " + KeyType + " Key");
        }
    }
}