using UnityEngine;

/// <summary>
/// Downhill 코스의 각 노드에 대한 상세 정보를 담는 구조체 예시
/// </summary>
[System.Serializable]
public class DownhillNode
{
    public Vector3 position;    // 노드의 3D 좌표
    public float slope;         // 기울기(경사도)
    public float curvature;     // 곡률(또는 코너각)
    public Vector3 direction;   // 진행 방향(탄젠트)
    // 필요시 roll, bankAngle 등도 추가 가능
}

/// <summary>
/// 그리드 구조에 활용할 노드 정보:
///   - i, j 인덱스
///   - position (월드좌표)
///   - isCourseArea (코스 영향권인지 여부)
/// </summary>
[System.Serializable]
public class GridNode
{
    public int i;
    public int j;
    public Vector3 position;
    public bool isCourseArea;
}
