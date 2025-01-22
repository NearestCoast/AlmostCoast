using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// EarClippingMeshDataGenerator:
///  - "cellPolygons" (or groupedPolygonsByKey 등)에서
///    하나씩 꺼내어 EarClipping 알고리즘으로 삼각 분할
///  - 그 결과를 MapDataSO.generatedMeshList에 저장
///
/// ※ 선분의 subdivPoints, 순서 등과 무관하게
///    최종 폴리곤(윤곽)만 있으면 됨.
/// </summary>
public class MeshDataGenerator : MapDataSystem
{
    [FoldoutGroup("Mesh Settings")]
    [SerializeField] private bool clearOldMesh = true;

    // 필요하면 holes(안쪽 폴리곤)를 지원하도록 확장 가능
    // 여기서는 '단일 윤곽'만 처리 예시

    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null)
        {
            Debug.LogError("[EarClippingMeshDataGenerator] MapDataSO is null");
            return;
        }

        if (clearOldMesh)
            so.generatedMeshDataList.Clear();

        // 예시: so.cellPolygons 를 바로 Triangulate
        // (만약 groupedPolygonsByKey 등이 필요하다면 변경)
        var polygons = so.cellPolygons;
        if (polygons == null || polygons.Count == 0)
        {
            Debug.LogWarning("[EarClippingMeshDataGenerator] No polygons to process.");
            return;
        }

        int totalMeshCount = 0;

        foreach (var poly in polygons)
        {
            var pts = poly.points;
            if (pts == null || pts.Count < 3) continue;

            // (1) CCW 정렬 (EarClipping 전제: 외곽은 CCW)
            if (IsClockwise(pts))
            {
                pts.Reverse();
            }

            // (2) EarClipping => 삼각인덱스
            var triangles = EarClippingTriangulate(pts);
            if (triangles.Count < 3) continue; // 실패 or degenerate

            // (3) vertices[] = pts를 Vector3로 바꾸되, Z축에 y를 할당(2D->3D)
            // 단순히 polygon 정점 그대로가 vertices가 된다.
            Vector3[] vertices3D = new Vector3[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var v2 = pts[i];
                vertices3D[i] = new Vector3(v2.x, 0f, v2.y);
            }

            // (4) GeneratedMeshData
            // triangles 배열은 3개씩 한 삼각형
            // => int[] 길이는 triangles.Count
            var meshData = new GeneratedMeshData
            {
                cellKey   = poly.cellKey,
                vertices  = vertices3D,
                triangles = triangles.ToArray(), // 3개씩 => EarClipping 결과
                uv        = new Vector2[vertices3D.Length] // 전부 (0,0) 예시
            };
            so.generatedMeshDataList.Add(meshData);
            totalMeshCount++;
        }

        Debug.Log($"[EarClippingMeshDataGenerator] Created {totalMeshCount} Mesh(es).");
    }

    //────────────────────────────────────────────────────────
    // (A) 폴리곤이 CW인지 체크
    //────────────────────────────────────────────────────────
    private bool IsClockwise(List<Vector2> polygon)
    {
        float area = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i+1) % polygon.Count];
            area += (a.x * b.y - b.x * a.y);
        }
        return (area < 0f); 
    }

    //────────────────────────────────────────────────────────
    // (B) EarClipping Triangulate
    //   - polygon(외곽 CCW) → 삼각인덱스 세트
    //   - int[]: 3씩 끊어 (v0, v1, v2) => 하나의 삼각형
    //────────────────────────────────────────────────────────
    private List<int> EarClippingTriangulate(List<Vector2> polygon)
    {
        var result = new List<int>();
        int n = polygon.Count;
        if (n < 3) return result;

        // (1) 정점 인덱스 배열
        var indexList = new List<int>();
        for (int i = 0; i < n; i++)
        {
            indexList.Add(i);
        }

        // (2) Ear Clipping
        int safeCount = 0; // 무한루프 방지
        while (indexList.Count >= 3 && safeCount < 9999)
        {
            bool earFound = false;
            for (int i = 0; i < indexList.Count; i++)
            {
                // 삼각형 후보: (i-1, i, i+1)
                int iPrev = (i - 1 + indexList.Count) % indexList.Count;
                int iNext = (i + 1) % indexList.Count;

                int idxA = indexList[iPrev];
                int idxB = indexList[i];
                int idxC = indexList[iNext];

                Vector2 A = polygon[idxA];
                Vector2 B = polygon[idxB];
                Vector2 C = polygon[idxC];

                if (IsEar(A, B, C, polygon, indexList))
                {
                    // 삼각화 => (idxA, idxB, idxC)
                    result.Add(idxA);
                    result.Add(idxB);
                    result.Add(idxC);

                    // 가운데 점( i )을 제거 => ear 잘라냄
                    indexList.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound) 
                break;

            safeCount++;
        }

        return result;
    }

    /// <summary>
    /// Ear 판단:
    /// 1) ABC가 볼록(내각<180)인지
    /// 2) 다른 모든 점이 삼각형 내부에 있지 않은지 (CCW 폴리곤)
    /// </summary>
    private bool IsEar(Vector2 A, Vector2 B, Vector2 C,
                       List<Vector2> polygon,
                       List<int> indexList)
    {
        // (1) 각도 판단( CCW 폴리곤 기준 => cross>0 이면 ABC가 볼록 )
        float cross = Cross(B - A, C - B);
        if (cross <= 0) return false; // 오목 or 평행

        // (2) 모든 나머지 점이 삼각형 내부에 있는지 체크
        //     있으면 ear아님
        for (int i = 0; i < indexList.Count; i++)
        {
            Vector2 P = polygon[indexList[i]];
            // A,B,C 자체는 패스
            if (P == A || P == B || P == C) 
                continue;

            if (PointInTriangle(P, A, B, C))
                return false;
        }

        return true;
    }

    private float Cross(Vector2 v1, Vector2 v2)
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    /// <summary> 2D 삼각형 내부 체크 (Barycentric 등 간단판정) </summary>
    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float crossAB = Cross(b - a, p - a);
        float crossBC = Cross(c - b, p - b);
        float crossCA = Cross(a - c, p - c);

        bool sameSignAB_BC = (crossAB >= 0f && crossBC >= 0f) || (crossAB <= 0f && crossBC <= 0f);
        bool sameSignBC_CA = (crossBC >= 0f && crossCA >= 0f) || (crossBC <= 0f && crossCA <= 0f);
        return (sameSignAB_BC && sameSignBC_CA);
    }

    //────────────────────────────────────────────────────────
    // OnDrawGizmos : 삼각형 와이어 표시
    //────────────────────────────────────────────────────────
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private bool drawWireGizmo = true;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color wireColor = Color.green;

    private void OnDrawGizmos()
    {
        if (!drawGizmo || !drawWireGizmo) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.generatedMeshDataList == null) return;

        Gizmos.color = wireColor;

        foreach (var meshData in so.generatedMeshDataList)
        {
            var verts = meshData.vertices;
            var tris  = meshData.triangles;
            if (verts == null || tris == null) continue;
            for (int i = 0; i < tris.Length; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i+1];
                int i2 = tris[i+2];
                if (i0 < 0 || i0 >= verts.Length) continue;
                if (i1 < 0 || i1 >= verts.Length) continue;
                if (i2 < 0 || i2 >= verts.Length) continue;

                Vector3 p0 = verts[i0];
                Vector3 p1 = verts[i1];
                Vector3 p2 = verts[i2];

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p0);
            }
        }
    }
}
