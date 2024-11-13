using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.InputSystem
{
    [CreateAssetMenu(fileName = "InputBindings", menuName = "Project", order = 0)]
    public class InputBindings : ScriptableObject
    {
        public string bindingsJson;
        
        public InputActionAsset actionAsset;
        public InputBindings inputBindings;  // 인스펙터에서 할당

        public void SaveBindings()
        {
            inputBindings.bindingsJson = actionAsset.ToJson();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(inputBindings);  // 변경사항을 표시
            UnityEditor.AssetDatabase.SaveAssets();  // 변경사항을 디스크에 저장
#endif
        }
    }
}