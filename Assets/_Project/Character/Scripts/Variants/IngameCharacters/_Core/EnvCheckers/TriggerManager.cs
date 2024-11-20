using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    public class TriggerManager : MonoBehaviour
    {
        [SerializeField] private SpecificTrigger[] triggers;

        [ShowInInspector] public bool IsHit
        {
            get
            {
                var result = false;
                
                foreach (var specificTrigger in triggers)
                {
                    if (!specificTrigger.IsHit) continue;
                    
                    result = true;
                    break;
                }

                Debug.Log(result);
                return result;
            }
        }

        private void Awake()
        {
            triggers = GetComponentsInChildren<SpecificTrigger>();
        }
    }
}