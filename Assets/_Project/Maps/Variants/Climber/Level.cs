using System;
using System.Collections.Generic;
using _Project.Character.IngameCharacters.Enemies;
using _Project.Maps.Climber.Objects;
using _Project.Maps.Climber.Objects.Collectables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber
{
    public class Level : MonoBehaviour
    {
        public enum Type
        {
            Normal,
            Dark,
        }

        [SerializeField] private Type type = Type.Normal;
        [SerializeField, DisableInEditorMode] private string id;
        [SerializeField] private List<SavePoint> savePoints = new List<SavePoint>();
        [SerializeField] private List<Hazard> hazards = new List<Hazard>();
        [SerializeField] private List<MovingPlatform> movingPlatforms = new List<MovingPlatform>();
        [SerializeField] private List<DingdongDoor> dingdongDoors = new List<DingdongDoor>();
        [SerializeField] private List<RollingCube> rollingCubes = new List<RollingCube>();
        [SerializeField] private List<Enemy> enemies = new List<Enemy>();
        [SerializeField] private SavePoint savePoint;

        public Type LevelType
        {
            get => type;
            set => type = value;
        }

        public string ID
        {
            get => id;
            set => id = value;
        }

        public List<SavePoint> SavePoints => savePoints;
        public List<Hazard> Hazards => hazards;
        public List<MovingPlatform> MovingPlatforms => movingPlatforms;
        public List<DingdongDoor> DingdongDoors => dingdongDoors;
        public List<RollingCube> RollingCubes => rollingCubes;
        public List<Enemy> Enemies => enemies;

        public void StartLevel()
        {
            Debug.Log("StartLevel");
            foreach (var movingPlatform in movingPlatforms)
            {
                movingPlatform.MoveStart();
            }

            foreach (var dingdongDoor in dingdongDoors)
            {
                foreach (var dingdongDoorDingdong in dingdongDoor.Dingdongs)
                {
                    dingdongDoorDingdong.Rotate();
                }
            }
            
            foreach (var rollingCube in rollingCubes)
            {
                rollingCube.StartWorking();
            }
            
            foreach (var enemy in enemies)
            {
                enemy.StartBehaviour();
            }
        }
        
        public void ResetLevel()
        {
            foreach (var movingPlatform in movingPlatforms)
            {
                movingPlatform.ResetMovingPlatform();
            }
            
            foreach (var dingdongDoor in dingdongDoors)
            {
                dingdongDoor.ResetDingdongDoor();
            }
            
            foreach (var rollingCube in rollingCubes)
            {
                rollingCube.ResetCube();
            }
        }

        private int aliveMonsterCount;
        [SerializeField] private List<GameObject> rewardPrefabs = new List<GameObject>();
        [SerializeField] private List<Vector3> rewardObjectPositions = new List<Vector3>();
        public List<GameObject> RewardPrefabs => rewardPrefabs;
        public List<Vector3> RewardObjectPositions => rewardObjectPositions;
        
        public void UpdateAliveMonsterCount()
        {
            Debug.Log("UpdateAliveMonsterCount");
            aliveMonsterCount = 0;
            foreach (var enemy in Enemies)
            {
                if (!enemy.IsDead) aliveMonsterCount++;
            }

            Debug.Log(aliveMonsterCount);
            if (aliveMonsterCount == 0)
            {
                Debug.Log("Instantiate");
                for (var i = 0; i < rewardPrefabs.Count; i++)
                {
                    var rewardPrefab = rewardPrefabs[i];
                    var position = rewardObjectPositions[i];
                    var collectable = Instantiate(rewardPrefab, transform).GetComponent<Collectable>();
                    collectable.transform.position = position;
                    collectable.ID = $"Collectable_{id}_Reward_{i}";
                    collectable.gameObject.SetActive(true);
                }
            }
        }
    }
}