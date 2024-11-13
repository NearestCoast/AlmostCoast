using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.ActionStates.MeleeAttacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Character.Scripts.Enemies.PlatformingEnemies.AttackStates
{
    public class Attack_02_Follower : Attack_02
    {
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 2; 
        [SerializeField, TitleGroup("Velocity")] private float maxTime = 1; 
        public override void OnEnterState()
        {
            base.OnEnterState();
            // Acc = transform.forward * maxLength / maxTime;
        }
    }
}