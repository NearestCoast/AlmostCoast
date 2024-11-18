using System;
using _Project.Character.IngameCharacters.Enemies;
using _Project.Characters.IngameCharacters.Core;
using _Project.Utils;
using UnityEngine;

namespace _Project.Maps.Climber
{
    public class Bound : MonoBehaviour
    {
        [SerializeField] private Level level;

        public Level Level
        {
            get => level;
            set => level = value;
        }

        private LayerMask targetLayers;

        private void Awake()
        {
            targetLayers = 1 << LayerMask.NameToLayer("Character");
        }

        private void OnTriggerEnter(Collider other)
        {
            // Debug.Log(other);
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer.IsInLayerMask(targetLayers))
            {
                var enemy = other.attachedRigidbody.GetComponent<Enemy>();
                if (level.Enemies.Contains(enemy)) return;
                enemy.CurrentLevel = level;
                level.Enemies.Add(enemy);
            }
        }
    }
}