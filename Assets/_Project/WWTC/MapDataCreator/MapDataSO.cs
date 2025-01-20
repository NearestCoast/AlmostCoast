using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Example/MapDataSO", fileName = "NewMapDataSO")]
public class MapDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────
    // 맵 전체 경계/오프셋 정보
    // ─────────────────────────────────────────────────
    public BoundaryData boundaryData;

    // ─────────────────────────────────────────────────
    // 맵 생성이나 노이즈, 랜덤 관련
    // ─────────────────────────────────────────────────
    public int noiseSeed = 1337;

    // ─────────────────────────────────────────────────
    // 실제 폴리곤(셀이 여러 개인 경우) 정보
    // ─────────────────────────────────────────────────
    public List<CellPolygon> cellPolygons = new List<CellPolygon>();

    // ─────────────────────────────────────────────────
    // OffsetLineGroup: 경계선 편집/분할 등에 사용
    // ─────────────────────────────────────────────────
    public List<OffsetLineGroup> offsetLineGroups = new List<OffsetLineGroup>();

    // ─────────────────────────────────────────────────
    // Arrange한 폴리곤(결과) 정보를 담는 리스트
    // ─────────────────────────────────────────────────
    public List<CellPolygon> arrangedCellPolygons = new List<CellPolygon>();

    // ─────────────────────────────────────────────────
    // 샘플링(PolygonMeshSamplerSystem 등) 결과
    // ─────────────────────────────────────────────────
    public List<PolygonMeshData> polygonMeshDataList = new List<PolygonMeshData>();

    // ─────────────────────────────────────────────────
    // 최종 생성된 메쉬 데이터 (Triangulation 등)
    // ─────────────────────────────────────────────────
    public List<GeneratedMeshData> generatedMeshList = new List<GeneratedMeshData>();

    // ─────────────────────────────────────────────────
    // (옵션) 거미줄 Triangulation 같은 단계에서
    // Ring 정보를 저장하고 싶다면, 다음과 같은 구조를 추가:
    // ─────────────────────────────────────────────────
    public List<RingPolygonSet> ringPolygonSets = new List<RingPolygonSet>();
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

/// <summary>
/// 셀(폴리곤) 하나에 대한 정보
/// </summary>
[System.Serializable]
public class CellPolygon
{
    public float cellKey;
    public List<Vector2> points; // 폴리곤 정점들 (2D)
    public Vector2 center;       // 폴리곤 중심
    public float area;           // 폴리곤 면적
}

/// <summary>
/// 2D 선분 구조체
/// </summary>
[System.Serializable]
public struct LineSegment2D
{
    public Vector2 start;
    public Vector2 end;
    public bool startClosed;
    public bool endClosed;
}

/// <summary>
/// OffsetLineGroup: 
/// 다수의 선분을 한 그룹(셀)로 묶은 구조.
/// </summary>
[System.Serializable]
public class OffsetLineGroup
{
    public float cellKey;
    public Vector2 center;
    public List<LineSegment2D> lines;
}

/// <summary>
/// Polygon 내부 샘플링(그리드 등) 결과를 저장하는 구조체
/// </summary>
[System.Serializable]
public class PolygonMeshData
{
    public float cellKey;    // 어떤 폴리곤인지 식별

    public int rowCount;     // 그리드 행
    public int colCount;     // 그리드 열

    // (rowCount+1)*(colCount+1) 크기의 샘플 포인트 배열
    // row-major 순서
    public Vector3[] samplePoints;

    // 행/열 -> 포인트 반환 헬퍼
    public Vector3 GetPoint(int row, int col)
    {
        if (row < 0 || row > rowCount) return Vector3.positiveInfinity;
        if (col < 0 || col > colCount) return Vector3.positiveInfinity;

        int index = row * (colCount + 1) + col;
        return samplePoints[index];
    }
}

/// <summary>
/// 최종적으로 생성된 메쉬(삼각형) 데이터를 저장하는 구조체
/// </summary>
[System.Serializable]
public class GeneratedMeshData
{
    public float cellKey;       // 어느 폴리곤(cellKey)인지
    public Vector3[] vertices;  // 메쉬 정점
    public int[] triangles;     // 메쉬 삼각 인덱스
    public Vector2[] uv;        // (옵션) UV 좌표
    // normals, colors 등 필요한 경우 추가
}

/// <summary>
/// (옵션) 거미줄 Ring 정보를 담고 싶다면:
/// 하나의 셀에 대해, 여러 Ring(각각 List<Vector2>)를 저장
/// </summary>
[System.Serializable]
public class RingPolygonSet
{
    public float cellKey;
    public List<List<Vector2>> rings = new List<List<Vector2>>();
}

// 교차점 구조체들
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
