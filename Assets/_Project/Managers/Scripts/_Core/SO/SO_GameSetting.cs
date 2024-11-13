using UnityEngine;

namespace _Project.Data
{
    [CreateAssetMenu(fileName = "GameSetting", menuName = "Project/GameSetting", order = 0)]
    public class SO_GameSetting : ScriptableObject
    {
        [SerializeField] private GUIStyle gameStatusStyle;
        public GUIStyle GameStatusStyle => gameStatusStyle;
    }
}