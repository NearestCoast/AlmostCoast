using UnityEngine;
using Sirenix.OdinInspector;

public class MapDataSystem : MonoBehaviour
{
    protected MapDataCreator mapDataCreator;

    private void OnValidate()
    {
        mapDataCreator = GetComponent<MapDataCreator>();
    }

    protected bool IsReady
    {
        get
        {
            if (mapDataCreator == null)
            {
                Debug.LogError($"[{name}] MapDataCreator를 찾을 수 없습니다. 같은 GameObject에 MapDataCreator가 있는지 확인하세요.");
                return false;
            }

            if (mapDataCreator.CurrentMapData == null)
            {
                Debug.LogError($"[{name}] MapDataCreator에 MapDataSO가 할당되지 않았습니다.");
                return false;
            }

            return true;
        }
    }

    [Button("Generate")]
    [GUIColor(0, 1, 0)] // 초록색
    public virtual void Generate()
    {
    }

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] protected bool drawGizmo;
}