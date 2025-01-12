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