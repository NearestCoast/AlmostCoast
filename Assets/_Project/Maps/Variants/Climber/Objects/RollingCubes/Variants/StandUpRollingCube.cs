using System;
using UnityEngine;

namespace _Project.Maps.Climber.Objects.Variants
{
    public class StandUpRollingCube : RollingCube
    {
        public override void Work(Transform playerT)
        {
            if (!IsPlayerOnPlatform)
            {
                Velocity = Vector3.zero;
                return;
            }

            base.Work(playerT);
        }
    }
}