using System;
using System.Collections.Generic;
using _Project.Character.IngameCharacters.Enemies;
using _Project.Maps.Climber.Objects;
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
    }
}