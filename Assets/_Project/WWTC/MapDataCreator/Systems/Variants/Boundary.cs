using UnityEngine;
using Sirenix.OdinInspector;

public class Boundary : MapDataSystem
{
    [Title("Boundary Settings")]
    [SerializeField]
    private BoundaryData boundaryDataToSet;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField]
    private Color gizmoColor = Color.green;

    public override void Generate()
    {
        if (!IsReady) return;
        boundaryDataToSet.centerX = boundaryDataToSet.offset.x;
        boundaryDataToSet.centerZ = boundaryDataToSet.offset.z + boundaryDataToSet.length * 0.5f;
        mapDataCreator.CurrentMapData.boundaryData = boundaryDataToSet;
        Debug.Log($"[{name}] BoundaryData가 MapDataSO에 적용되었습니다.");
    }

    private void OnDrawGizmos()
    {
        if (!IsReady) return;
        var data = mapDataCreator.CurrentMapData.boundaryData;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(new Vector3(data.centerX, 0, data.centerZ), new Vector3(data.width, 0, data.length));
    }
}