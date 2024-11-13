using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    public class LockParams : MonoBehaviour
    {
        [ShowInInspector] public Transform LockOnTarget { get; set; }
        public bool IsLockingOn { get; private set; }
        public float MarkerHeight { get; private set; }

        public void LockOn(Transform target)
        {
            LockOnTarget = target;
            if (LockOnTarget)
            {
                var characterControllerEnveloper = target.GetComponent<IngameCharacter>().CharacterControllerEnveloper;
                if (characterControllerEnveloper) MarkerHeight = characterControllerEnveloper.Center.y + characterControllerEnveloper.Height / 4;
            }
            
            IsLockingOn = true;
        }

        public void LockOff()
        {
            LockOnTarget = null;
            IsLockingOn = false;
        }

        public void Toggle()
        {
            IsLockingOn = !IsLockingOn;
        }
    }
}