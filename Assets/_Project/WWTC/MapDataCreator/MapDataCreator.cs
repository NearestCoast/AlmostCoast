using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Odin Inspector를 사용하여 MapDataSO를 생성/할당하는 에디터용 MonoBehaviour.
/// 여러 MapDataSystem(Boundary, BoundaryChopper 등)을 한 GameObject에 연달아 넣고,
/// 이 클래스에서 만든 버튼을 통해 각 시스템의 Generate 함수를 순차적으로 호출합니다.
/// </summary>
public class MapDataCreator : MonoBehaviour
{
    [Title("Map Data Creator")]

    [FolderPath(AbsolutePath = false)]
    [SerializeField]
    private string folderPath = "Assets/";

    [SerializeField]
    private string fileName = "NewMapDataSO";

    // MapDataSystem들이 개별 Generate 시에도 참조할 수 있도록
    // 여기서는 public 프로퍼티로 제공
    [SerializeField]
    public MapDataSO CurrentMapData;

    // 같은 GameObject에 달려있는 MapDataSystem들을 순서대로 담을 리스트
    [SerializeField]
    private List<MapDataSystem> mapDataSystems = new List<MapDataSystem>();

    private void OnValidate()
    {
        // 현재 GameObject에 달려 있는 MapDataSystem 컴포넌트를
        // Inspector 순서(추가된 순서)대로 가져와 리스트에 담아둡니다.
        mapDataSystems = new List<MapDataSystem>(GetComponents<MapDataSystem>());
    }

    [HorizontalGroup("ButtonRow", 0.5f)]
    [Button("Create & Assign MapDataSO")]
    public void CreateNewMapDataSO()
    {
#if UNITY_EDITOR
        // 새 MapDataSO 생성
        MapDataSO newMapData = ScriptableObject.CreateInstance<MapDataSO>();
        string rawPath = System.IO.Path.Combine(folderPath, fileName + ".asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(rawPath);

        // 에셋 생성 및 저장
        AssetDatabase.CreateAsset(newMapData, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // CurrentMapData 갱신
        CurrentMapData = newMapData;

        // 에디터 포커스
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newMapData;
#else
        Debug.LogError("에셋 생성은 Unity 에디터 환경에서만 가능합니다.");
#endif
    }

    [HorizontalGroup("ButtonRow", 0.5f)]
    [GUIColor(0, 1, 0)] // 초록색
    [Button("Generate All Systems")]
    public void GenerateAllSystems()
    {
        if (CurrentMapData == null)
        {
            Debug.LogError("MapDataSO가 할당되지 않았습니다. Create & Assign 버튼을 눌러 새로 생성하거나, " +
                           "기존 에셋을 할당해주세요.");
            return;
        }

        // mapDataSystems 리스트에 담겨있는 순서대로 Generate 실행
        foreach (var system in mapDataSystems)
        {
            if (system == null)
            {
                Debug.LogWarning("MapDataSystems 리스트에 null이 포함되어 있습니다. 스킵합니다.");
                continue;
            }
            system.Generate();
        }
    }
}
