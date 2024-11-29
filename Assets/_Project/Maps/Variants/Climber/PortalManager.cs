using System.Collections.Generic;
using _Project.Maps.Climber.Objects;
using UnityEngine;

namespace _Project.Maps.Climber
{
    public class PortalManager : MonoBehaviour
    {
        private void Awake()
        {
            SetDepartPortals();
        }

        private void SetDepartPortals()
        {
            var chapters = GetComponentsInChildren<Chapter>();
            
            var departDict = new Dictionary<string, Portal>();
            var arrivalDict = new Dictionary<string, Portal>();
            foreach (var chapter in chapters)
            {
                foreach (var portal in chapter.Portals)
                {
                    if (portal.PortalType == Portal.Type.Depart) departDict.Add(portal.ID, portal);
                    else if (portal.PortalType == Portal.Type.Arrival) arrivalDict.Add(portal.ID, portal);
                }
            }
            
            foreach (var portal in departDict.Values)
            {
                portal.ArrivalPortal = arrivalDict[portal.ArrivalID];
            }
        }
    }
}