using System;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class PrefabsManager : MonoBehaviour
    {
        private void Start()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}