using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class WallJumpCountUp : Collectable
    {
        protected override void Work()
        {
            base.Work();
            
            playerCharacter.MoveParams.MaxWallJumpCount += 1;
            
            Debug.Log("Player Collected WallJumpCountUp");
        }
    }
}