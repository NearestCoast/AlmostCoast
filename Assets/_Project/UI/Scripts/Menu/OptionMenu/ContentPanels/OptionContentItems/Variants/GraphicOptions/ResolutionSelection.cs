using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.OptionMenu
{
    public class ResolutionSelection : OptionContentItem
    {
        private Resolution[] resolutions;
        private Resolution CurrentResolution => resolutions[currentIndex];
        protected override void Awake()
        {
            base.Awake();
            resolutions = Screen.resolutions.Reverse().ToArray();
            ItemLength = resolutions.Length;
        }

        public override void Treat(MoveDirection direction)
        {
            base.Treat(direction);
            SetDescription();
        }

        public override void Apply()
        {
            base.Apply();
            setResolution();
            FindAnyObjectByType<GameManager>().DelayedOnApply().Forget();;
            

            void setResolution()
            {
                Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, Screen.fullScreen);
            }
        }

        protected override void SetDescription(string value = "")
        {
            value = $"{CurrentResolution.width} x {CurrentResolution.height}";
            base.SetDescription(value);
        }
    }
}