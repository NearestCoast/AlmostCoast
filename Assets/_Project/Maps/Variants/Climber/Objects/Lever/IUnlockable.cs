using System;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public interface ILeverUnlockable
    {
        public string LeverID { get; set; }
        public Lever Lever { get; set; }
        
        public Transform Transform { get; }
        public void Open();
    }
}