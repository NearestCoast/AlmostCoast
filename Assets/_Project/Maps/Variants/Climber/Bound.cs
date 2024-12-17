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

        private LayerMask enemyLayers;

        private void Awake()
        {
            enemyLayers = 1 << LayerMask.NameToLayer("Character");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer.IsInLayerMask(enemyLayers))
            {
                var enemy = other.gameObject.GetComponent<Enemy>();
                if (level.Enemies.Contains(enemy)) return;
                enemy.CurrentLevel = level;
                enemy.Level = Level;
                level.Enemies.Add(enemy);
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var character = other.gameObject.GetComponent<IngameCharacter>();
                if (character.CurrentLevel != level)
                {
                    character.CurrentLevel = level;
                    level.StartLevel();

                    if (!character.SavePoint)
                    {
                        character.SavePoint = level.SavePoints[0];
                    }
                }
            }
        }
    }
}