using _Project.Characters.IngameCharacters.Core;
using UnityEngine;
using _Project.Characters.IngameCharacters.Core.MovementStates;

namespace _Project.Characters._Core.EnvironmentCheckers
{
    public interface IEnvironmentChecker
    {
        public void Check(MovementState currentState);
    }
}