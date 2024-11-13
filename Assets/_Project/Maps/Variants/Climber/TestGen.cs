using _Project.Characters.IngameCharacters.Core.MovementStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Maps.Climber
{
    public class TestGen : MonoBehaviour
    {
        [SerializeField] private GameObject plane;
         
        [SerializeField, TitleGroup("Jump")] private JumpState jumpState;
        private float HorizontalReach => jumpState.MaxLength;
        private float MinVerticalReach => jumpState.MinJumpHeight;
        private float MaxVerticalReach => jumpState.MaxJumpHeight;

        [Button]
        private void Generate()
        {
            
        }
    }
}