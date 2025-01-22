using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Example/MapDataSO", fileName = "NewMapDataSO")]
public class MapDataSO : ScriptableObject
{
    public float shrinkStartOffset = 0.1f;
    public float shrinkOffset = 0.1f;

    // 경계/노이즈/폴리곤 등 기존 필드들
    public BoundaryData boundaryData;
    public int noiseSeed = 1337;
    public List<CellPolygon> cellPolygons = new List<CellPolygon>();
    public List<OffsetLineGroup> offsetLineGroups = new List<OffsetLineGroup>();

    // Pruner 결과 폴리곤 저장
    public List<PrunerIterationData> prunerIterations = new List<PrunerIterationData>();

    /// <summary>
    /// PrunerConverter에서 cellKey별로 묶어둔 CellPolygonGroup 리스트
    /// </summary>
    public List<CellPolygonGroup> groupedPolygonsByKey = new List<CellPolygonGroup>();

    /// <summary>
    /// LineDivider에서 subdivision된 정보를 저장할 CellPolygonGroup 리스트.
    /// groupedPolygonsByKey와 구조는 동일하지만,  
    /// 각 CellPolygon 내부에 subdiv 정보가 채워져 있음.
    /// </summary>
    public List<CellPolygonGroup> subdivideCellPolygonGroup = new List<CellPolygonGroup>();

    /// <summary>
    /// MeshDataGenerator 등에서 사용할 메쉬 정보
    /// </summary>
    public List<GeneratedMeshData> generatedMeshDataList = new List<GeneratedMeshData>();
}

/// <summary>
/// cellKey별로 여러 CellPolygon을 묶은 구조체
/// </summary>
[System.Serializable]
public class CellPolygonGroup
{
    public float cellKey;
    public List<CellPolygon> polygons;
}

/// <summary>
/// 맵 경계 관련 데이터
/// </summary>
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
    public List<Vector2> cornerPoints = new List<Vector2>();

    // *** 기존: public List<List<Vector2>> subdivEdges;
    // 새로 만든 SubdivEdge 클래스를 사용:
    public List<SubdivEdge> subdivEdges = new List<SubdivEdge>();
}

[System.Serializable]
public class SubdivEdge
{
    // subdivEdges 내부에서 사용하던 Vector2 List를
    // "edgePoints" 등 원하는 필드명으로 옮겨둡니다.
    public List<Vector2> edgePoints = new List<Vector2>();
}

[System.Serializable]
public struct LineSegment2D
{
    public Vector2 start;
    public Vector2 end;
    public bool startClosed;
    public bool endClosed;
}

[System.Serializable]
public class OffsetLineGroup
{
    public int uniqueID;
    public float cellKey;
    public Vector2 center;
    public List<LineSegment2D> lines;
}

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

[System.Serializable]
public class PrunerIterationData
{
    public int iterationIndex;
    public float offsetValue;
    public List<CellPolygon> polygons = new List<CellPolygon>();
}

[System.Serializable]
public class GeneratedMeshData
{
    public float cellKey;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uv;
}
