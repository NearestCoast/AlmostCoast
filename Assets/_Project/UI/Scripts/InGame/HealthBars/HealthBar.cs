using System;
using Renge.PPB;
using UnityEngine;

namespace _Project.UI.InGame
{
    public class HealthBar : MonoBehaviour
    {
        private ProceduralProgressBar proceduralProgressBar;
        public ProceduralProgressBar ProceduralProgressBar => proceduralProgressBar;
        
        protected virtual void Awake()
        {
            proceduralProgressBar = GetComponent<ProceduralProgressBar>();
        }
    }
}