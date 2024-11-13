
using System;
using _Project._Core;
using _Project.Characters._Core;
using _Project.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Combat.HitObjects
{
    public struct HittingInfo
    {
        public HitObject hitObject;
        public Vector3 hitInvokePosition;
        public Vector3 hitPoint;

        public Vector3 GetHitDirectionFromCenter()
        {
            var dir = (hitPoint - hitInvokePosition).XYZ3toX0Z3().normalized;
            return dir;
        }

        public HittingInfo(HitObject hitObject, Vector3 hitPoint)
        {
            this.hitObject = hitObject;
            this.hitPoint = hitPoint;
            this.hitInvokePosition = hitObject.transform.position;
        }
    }
    
    public class HitObject : MonoBehaviour
    {
        [SerializeField] protected SideEffect sideEffect;
        public SideEffect SideEffect => sideEffect;
        public virtual void Invoke()
        {
            
        }
        
        public virtual void Shutdown(GameObject hitInstance)
        {
            
        }
    }
}