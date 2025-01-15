using UnityEngine;

/// <summary>
/// 맵의 X, Z 범위를 관리하기 위한 클래스.
/// MapDataSystem을 상속받아, 기즈모와 GenerateSystem을 구현.
/// </summary>
[System.Serializable]
public class Boundary : MapDataSystem
{
    [Header("Center")]
    [SerializeField]
    private float centerX;
    [SerializeField]
    private float centerZ;

    [Header("Dimensions")]
    [SerializeField]
    private float width = 10f;
    [SerializeField]
    private float length = 20f;

    /// <summary>
    /// OnDrawGizmo 오버라이드: 기즈모(바운더리 영역) 그리기
    /// </summary>
    public override void OnDrawGizmo()
    {
        if (!DrawGizmo) return;

        Gizmos.color = Color.green;

        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float minX = centerX - halfW;
        float maxX = centerX + halfW;
        float minZ = centerZ - halfL;
        float maxZ = centerZ + halfL;

        Vector3 p1 = new Vector3(minX, 0, minZ);
        Vector3 p2 = new Vector3(minX, 0, maxZ);
        Vector3 p3 = new Vector3(maxX, 0, maxZ);
        Vector3 p4 = new Vector3(maxX, 0, minZ);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }

    /// <summary>
    /// Generate 버튼 클릭 시 실행되는 실제 로직
    /// </summary>
    protected override void GenerateSystem()
    {
        // MapDataSO가 존재한다면, 여기에 바운더리 정보를 기록하거나 
        // 혹은 다른 로직(예: Boundary 관련 메시지, 계산 등)을 수행할 수 있음.
        Debug.Log($"Boundary GenerateSystem: Using MapData [{mapData.name}]");
        Debug.Log($"Boundary: Center({centerX}, {centerZ}), Width={width}, Length={length}");
    }
}