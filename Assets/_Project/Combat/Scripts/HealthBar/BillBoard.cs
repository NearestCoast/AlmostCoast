using System;
using UnityEngine;

namespace _Project.Combat.BilLBoards
{
    public class BillBoard : MonoBehaviour
    {
        private void FixedUpdate()
        {
            var rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            transform.rotation = rotation;
        }
    }
}