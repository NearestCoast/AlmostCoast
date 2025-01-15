using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Odin Inspector를 사용하여 MapDataSO를 생성/할당하는 에디터용 MonoBehaviour.
/// 각 시스템 클래스(Boundary, Noise)는 이곳에서 참조되고,
/// 공통 Generate 버튼은 각 시스템 내부(MapDataSystem)에서 동작.
/// </summary>
public class MapDataCreator : MonoBehaviour
{
    [Title("Map Data Creator")]

    [FolderPath(AbsolutePath = false)]
    [SerializeField]
    private string folderPath = "Assets/";

    [SerializeField]
    private string fileName = "NewMapDataSO";

    [SerializeField, InlineEditor]
    private MapDataSO currentMapData;

    // 시스템들 (예: Boundary, Noise)
    [SerializeField]
    private Boundary boundarySettings;

    [SerializeField]
    private Noise noiseSettings;

    [Button("Create & Assign MapDataSO")]
    public void CreateNewMapDataSO()
    {
#if UNITY_EDITOR
        MapDataSO newMapData = ScriptableObject.CreateInstance<MapDataSO>();
        string rawPath = System.IO.Path.Combine(folderPath, fileName + ".asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(rawPath);

        AssetDatabase.CreateAsset(newMapData, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        currentMapData = newMapData;

        // 시스템들에 mapData 주입
        boundarySettings?.SetMapData(currentMapData);
        noiseSettings?.SetMapData(currentMapData);

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newMapData;
#else
        Debug.LogError("에셋 생성은 Unity 에디터 환경에서만 가능합니다.");
#endif
    }

    // 인스펙터에서 currentMapData 직접 바꿀 수도 있으므로, OnValidate에서 시스템에 주입
    private void OnValidate()
    {
        boundarySettings?.SetMapData(currentMapData);
        noiseSettings?.SetMapData(currentMapData);
    }

    // 씬 뷰에서 기즈모 그리기
    private void OnDrawGizmos()
    {
        boundarySettings?.OnDrawGizmo();
        noiseSettings?.OnDrawGizmo();
    }
}