using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class SlideJumpUp : Collectable
    {
        protected override void Work()
        {
            base.Work();

            playerCharacter.MoveParams.IsSlideJumpPossible = true;
            
            Debug.Log("Player Collected SlideJumpUp");
        }
    }
}