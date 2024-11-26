using System;
using Unity.Cinemachine;
using UnityEngine;

namespace _Project.Cameras
{
    public class CameraDeoccluderController : MonoBehaviour
    {
        [SerializeField] private CinemachineDeoccluder cinemachineDeoccluder;
        [SerializeField] private CinemachineDeoccluder.ObstacleAvoidance dirtTerrainSetting;
        
        private CinemachineDeoccluder.ObstacleAvoidance OriginalSetting { get; set; }
        

        private void Awake()
        {
            OriginalSetting = cinemachineDeoccluder.AvoidObstacles;
        }

        public void SetDirtTerrainSetting()
        {
            cinemachineDeoccluder.AvoidObstacles = dirtTerrainSetting;
        }

        public void ResetToOriginalSetting()
        {
            cinemachineDeoccluder.AvoidObstacles = OriginalSetting;
        }
    }
}