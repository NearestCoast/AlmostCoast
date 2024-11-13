using System;
using UnityEngine;

namespace _Project.UI
{
    [DefaultExecutionOrder(10)]
    public class UI : MonoBehaviour
    {
        private Canvas[] canvases;

        private void Awake()
        {
            canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                canvas.gameObject.SetActive(true);
            }
        }
    }
}