using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// polygonMeshDataList(그리드 기반)에 담긴 점들로부터
/// 유효한 정점만 압축하여(=Infinity 제거) 삼각화.
/// generatedMeshList에 최종 메쉬 데이터(정점/인덱스/uv) 저장.
///
/// 이로써 "Failed extracting collision mesh" / "Invalid AABB" 방지.
/// </summary>
public class MeshGeneratorSystem : MapDataSystem
{
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private bool showInvalidPoints = false; 
    // 기즈모로 외부(무효) 점도 그릴지 여부

    public override void Generate()
    {
        base.Generate();
        if (!IsReady) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // 이전 메쉬 데이터 초기화
        mapData.generatedMeshList.Clear();

        var polyMeshList = mapData.polygonMeshDataList;
        if (polyMeshList == null || polyMeshList.Count == 0)
        {
            Debug.LogWarning("[MeshGeneratorSystem] polygonMeshDataList가 비어있습니다. " +
                             "먼저 폴리곤 샘플링을 진행하세요.");
            return;
        }

        // 폴리곤별로 처리
        foreach (var polyData in polyMeshList)
        {
            if (polyData.samplePoints == null || polyData.samplePoints.Length == 0)
                continue;

            int rowCount = polyData.rowCount;
            int colCount = polyData.colCount;
            Vector3[] oldPoints = polyData.samplePoints;
            int totalCount = oldPoints.Length;

            // ──────────────────────────────────────────
            // 1) 유효 정점(=Infinity가 아닌)만 압축
            //    oldIndex -> newIndex를 기록하기 위해 indexMap 사용
            // ──────────────────────────────────────────
            List<Vector3> validVerts = new List<Vector3>(totalCount);
            List<Vector2> validUV = new List<Vector2>(totalCount);

            int[] indexMap = new int[totalCount];
            for (int i = 0; i < totalCount; i++)
            {
                var v = oldPoints[i];
                if (IsValid(v))
                {
                    int newIdx = validVerts.Count;
                    validVerts.Add(v);

                    // UV도 row,col 기반으로 계산
                    (int row, int col) = ConvertIndexToRowCol(i, rowCount, colCount);
                    float u = (colCount > 0) ? (float)col / colCount : 0;
                    float w = (rowCount > 0) ? (float)row / rowCount : 0;
                    validUV.Add(new Vector2(u, w));

                    indexMap[i] = newIdx;
                }
                else
                {
                    indexMap[i] = -1;
                }
            }

            // validVerts, validUV가 최종 사용할 정점/UV
            if (validVerts.Count < 3)
            {
                // 유효 정점이 너무 적으면 메쉬 생성 불가
                continue;
            }

            // ──────────────────────────────────────────
            // 2) 삼각형 인덱스 생성
            //    rowCount x colCount 개의 사각형 -> 2개 삼각형
            //    4개 점 모두 유효한 경우에만 삼각 생성
            // ──────────────────────────────────────────
            List<int> triList = new List<int>();
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    int i0 = row * (colCount + 1) + col;
                    int i1 = i0 + 1;
                    int i2 = (row + 1) * (colCount + 1) + col;
                    int i3 = i2 + 1;

                    int newI0 = indexMap[i0];
                    int newI1 = indexMap[i1];
                    int newI2 = indexMap[i2];
                    int newI3 = indexMap[i3];

                    // 하나라도 -1이면(무효 정점) → 삼각 생성 X
                    if (newI0 < 0 || newI1 < 0 || newI2 < 0 || newI3 < 0)
                        continue;

                    // 삼각1: (i0, i2, i1)
                    triList.Add(newI0);
                    triList.Add(newI2);
                    triList.Add(newI1);

                    // 삼각2: (i1, i2, i3)
                    triList.Add(newI1);
                    triList.Add(newI2);
                    triList.Add(newI3);
                }
            }

            if (triList.Count < 3)
            {
                // 삼각형이 없으면 스킵
                continue;
            }

            // ──────────────────────────────────────────
            // 3) GeneratedMeshData 구성
            // ──────────────────────────────────────────
            GeneratedMeshData gmd = new GeneratedMeshData();
            gmd.cellKey = polyData.cellKey;
            gmd.vertices = validVerts.ToArray();
            gmd.triangles = triList.ToArray();
            gmd.uv = validUV.ToArray();

            // mapData에 추가
            mapData.generatedMeshList.Add(gmd);
        }

        Debug.Log($"[MeshGeneratorSystem] 메쉬 생성 완료! " +
                  $"(generatedMeshList.Count={mapData.generatedMeshList.Count}).");
    }

    /// <summary>
    /// oldPoints[i]가 Infinity가 아닌지 검사
    /// </summary>
    private bool IsValid(Vector3 v)
    {
        return !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    /// <summary>
    /// 1D 인덱스를 row,col로 변환
    /// row = i / (colCount+1), col = i % (colCount+1)
    /// </summary>
    private (int, int) ConvertIndexToRowCol(int i, int rowCount, int colCount)
    {
        int totalCol = colCount + 1;
        int row = i / totalCol;
        int col = i % totalCol;
        return (row, col);
    }

    // ──────────────────────────────────────────
    // 기즈모: generatedMeshList의 와이어프레임 표시
    // + (옵션) polygonMeshDataList의 무효점 표시
    // ──────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;
        if (mapData.generatedMeshList == null) return;

        // 1) 와이어프레임
        Gizmos.color = gizmoColor;
        foreach (var gmd in mapData.generatedMeshList)
        {
            var verts = gmd.vertices;
            var tris = gmd.triangles;
            if (verts == null || tris == null) continue;

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i + 1];
                int i2 = tris[i + 2];
                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];

                Gizmos.DrawLine(v0, v1);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v0);
            }
        }

        // 2) 무효점 표시 (옵션)
        if (showInvalidPoints && mapData.polygonMeshDataList != null)
        {
            Gizmos.color = Color.red;
            foreach (var pmd in mapData.polygonMeshDataList)
            {
                if (pmd.samplePoints == null) continue;
                for (int i = 0; i < pmd.samplePoints.Length; i++)
                {
                    var v = pmd.samplePoints[i];
                    if (!IsValid(v))
                    {
                        Gizmos.DrawSphere(v, 0.1f);
                    }
                }
            }
        }
    }
}
