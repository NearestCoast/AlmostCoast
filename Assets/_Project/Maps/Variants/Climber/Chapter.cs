using System.Collections.Generic;
using _Project.Maps.Climber.Objects;
using UnityEngine;

namespace _Project.Maps.Climber
{
    public class Chapter : MonoBehaviour
    {
        [SerializeField] private List<Level> levels = new List<Level>();
        public List<Level> Levels
        {
            get => levels;
            set => levels = value;
        }

        [SerializeField] private List<Portal> portals = new List<Portal>();
        public List<Portal> Portals
        {
            get => portals;
            set => portals = value;
        }
    }
}