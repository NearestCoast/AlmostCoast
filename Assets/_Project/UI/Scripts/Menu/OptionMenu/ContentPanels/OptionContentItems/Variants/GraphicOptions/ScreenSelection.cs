using UnityEngine;
using UnityEngine.EventSystems;
using Screen = UnityEngine.Device.Screen;

namespace _Project.UI.OptionMenu
{
    public class ScreenSelection : OptionContentItem
    {
        private string Description => (currentIndex == 0) ? "On" : "Off";
        protected override void Awake()
        {
            base.Awake();
            ItemLength = 2;
        }

        public override void Treat(MoveDirection direction)
        {
            base.Treat(direction);
            SetDescription();
        }

        public override void Apply()
        {
            base.Apply();
            if (currentIndex == 0)
            {
                // Screen.fullScreen = true;
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            }
            else
            {
                // Screen.fullScreen = false;
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
            FindObjectOfType<GameManager>().DelayedOnApply().Forget();
        }

        protected override void SetDescription(string value = "")
        {
            value = Description;
            base.SetDescription(value);
        }
    }
}