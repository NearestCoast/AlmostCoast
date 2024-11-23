using System;
using Unity.Cinemachine;
using UnityEngine;

namespace _Project.Cameras
{
    public class CustomCinemachineDeoccluder : CinemachineDeoccluder
    {
        protected override void Awake()
        {
            base.Awake();
            MinimumDistanceFromTarget = 0;
        }

        private void Start()
        {
            Debug.Log(MinimumDistanceFromTarget);
        }
    }
}