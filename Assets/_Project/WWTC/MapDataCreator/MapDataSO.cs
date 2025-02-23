using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Example/MapDataSO", fileName = "NewMapDataSO")]
public class MapDataSO : ScriptableObject
{
    public BoundaryData boundaryData;

    // 새로 추가
    // boundaryChopper가 사용할 seed
    public int noiseSeed = 1337;

    public List<CellPolygon> cellPolygons = new List<CellPolygon>();
    public List<OffsetLineGroup> offsetLineGroups = new List<OffsetLineGroup>();
}


[System.Serializable]
public struct BoundaryData
{
    public Vector3 offset;
    public float centerX;
    public float centerZ;
    public float width;
    public float length;
}

[System.Serializable]
public class CellPolygon
{
    public float cellKey;
    public List<Vector2> points;
    public Vector2 center;
    public float area;
}

[System.Serializable]
public struct LineSegment2D
{
    public Vector2 start;
    public Vector2 end;

    // 새로 추가된 필드: 각 끝점이 닫혀 있는지 여부
    public bool startClosed;
    public bool endClosed;
}

[System.Serializable]
public class OffsetLineGroup
{
    public float cellKey;
    public Vector2 center;
    public List<LineSegment2D> lines;
}



// 교차점 구조체
public struct IntersectionData
{
    public float t;
    public Vector2 point;
}
public struct IntersectionOnLine
{
    public float t;
    public Vector2 point;
}