using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Project.UI.InGame
{
    public class ItemUI : MonoBehaviour
    {
        [SerializeField] private Image imageCompo;
        [SerializeField] private Sprite nullImage;
        [SerializeField] private Sprite sprite;

        public void SetImage(Sprite sprite)
        {
            this.sprite = sprite;
        }

        public void Activate()
        {
            imageCompo.sprite = sprite;
        }

        public void Deactivate()
        {
            imageCompo.sprite = nullImage;
        }
    }
}