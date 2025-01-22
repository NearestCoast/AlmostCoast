using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class LineDivider : MapDataSystem
{
    [FoldoutGroup("Divide Settings")]
    [SerializeField, Min(0.01f)]
    private float baseSegmentLength = 1f;

    /// <summary>
    /// cornerPoints 중 서로 가까운 점을 병합할 거리 임계값
    /// </summary>
    [FoldoutGroup("Divide Settings")]
    [SerializeField, Min(0f)]
    private float cornerMergeThreshold = 0.5f;

    private static readonly Color[] RAINBOW_COLORS = new Color[]
    {
        Color.red,
        new Color(1f, 0.5f, 0f),
        Color.yellow,
        Color.green,
        Color.blue,
        new Color(0.3f, 0f, 0.5f),
        Color.magenta
    };

    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null)
        {
            Debug.LogError("[LineDivider] MapDataSO가 없습니다.");
            return;
        }

        // (1) 이전 subdiv 정보 초기화
        so.subdivideCellPolygonGroup.Clear();

        // (2) groupedPolygonsByKey 순회
        foreach (var group in so.groupedPolygonsByKey)
        {
            if (group.polygons == null || group.polygons.Count == 0)
                continue;

            CellPolygonGroup newGroup = new CellPolygonGroup
            {
                cellKey = group.cellKey,
                polygons = new List<CellPolygon>()
            };

            // (3) 그룹 내 각 폴리곤 복사 & subdiv 처리
            foreach (var srcPoly in group.polygons)
            {
                var newPoly = new CellPolygon
                {
                    cellKey = srcPoly.cellKey,
                    points  = new List<Vector2>(srcPoly.points),
                    center  = srcPoly.center,
                    area    = srcPoly.area
                };

                // cornerPoints = 원본 points 복제
                newPoly.cornerPoints.AddRange(srcPoly.points);

                // subdivEdges 구성 (아래 메서드 수정)
                SubdividePolygonEdges(newPoly, baseSegmentLength);

                // (4) 가까운 corner point 병합 로직
                if (cornerMergeThreshold > 0f)
                {
                    MergeCloseCorners(newPoly, cornerMergeThreshold);
                }

                newGroup.polygons.Add(newPoly);
            }

            // (5) subdivideCellPolygonGroup에 저장
            so.subdivideCellPolygonGroup.Add(newGroup);
        }

        Debug.Log($"[LineDivider] 완료. 그룹 개수={so.subdivideCellPolygonGroup.Count}, " +
                  $"cornerMergeThreshold={cornerMergeThreshold}");
    }

    /// <summary>
    /// Polygon의 각 에지를 baseSegmentLength로 잘게 나누어 
    /// newPoly.subdivEdges에 SubdivEdge 형태로 저장
    /// </summary>
    private void SubdividePolygonEdges(CellPolygon poly, float baseLen)
    {
        var pts = poly.points;
        if (pts == null || pts.Count < 2)
            return;

        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 start = pts[i];
            Vector2 end   = pts[(i + 1) % n];
            float segLen  = Vector2.Distance(start, end);

            // 새 SubdivEdge 객체를 만들어, subdiv된 점들을 담는다.
            SubdivEdge subdivEdge = new SubdivEdge();

            if (segLen < 1e-9f)
            {
                // 너무 짧다면 별도 분할 없이 바로 추가
                poly.subdivEdges.Add(subdivEdge);
                continue;
            }

            int numDiv = Mathf.RoundToInt(segLen / baseLen);
            if (numDiv < 1) numDiv = 1;

            // start~end 사이를 numDiv로 분할한 점들을 edgePoints에 추가
            for (int d = 1; d < numDiv; d++)
            {
                float t = d / (float)numDiv;
                Vector2 p = Vector2.Lerp(start, end, t);
                subdivEdge.edgePoints.Add(p);
            }

            poly.subdivEdges.Add(subdivEdge);
        }
    }

    /// <summary>
    /// 같은 폴리곤 내부의 cornerPoints 중
    /// 서로 일정 거리 이하로 가까운 두 점을
    /// 중점으로 병합.
    /// </summary>
    private void MergeCloseCorners(CellPolygon poly, float mergeDistance)
    {
        var corners = poly.cornerPoints;
        if (corners.Count < 2) return;

        bool merged = true;
        while (merged)
        {
            merged = false;
            for (int i = 0; i < corners.Count - 1; i++)
            {
                for (int j = i + 1; j < corners.Count; j++)
                {
                    float dist = Vector2.Distance(corners[i], corners[j]);
                    if (dist <= mergeDistance)
                    {
                        Vector2 mid = 0.5f * (corners[i] + corners[j]);
                        corners[i] = mid;
                        corners.RemoveAt(j);

                        merged = true;
                        break;  
                    }
                }
                if (merged) 
                    break;
            }
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // 기즈모 시각화 (cornerPoints & subdivEdges)
    // ───────────────────────────────────────────────────────────────────────
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private bool drawSubdivided = true;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField, Min(0.001f)] private float pointSize = 0.05f;

    private void OnDrawGizmos()
    {
        if (!drawGizmo || !drawSubdivided) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.subdivideCellPolygonGroup == null) return;

        foreach (var group in so.subdivideCellPolygonGroup)
        {
            if (group.polygons == null) continue;

            foreach (var poly in group.polygons)
            {
                // cornerPoints (하얀색)
                Gizmos.color = Color.white;
                foreach (var corner in poly.cornerPoints)
                {
                    Gizmos.DrawSphere(new Vector3(corner.x, 0f, corner.y), pointSize);
                }

                // subdivEdges (무지개색)
                int colorIndex = 0;
                foreach (var subdivEdge in poly.subdivEdges)
                {
                    Gizmos.color = RAINBOW_COLORS[colorIndex % RAINBOW_COLORS.Length];
                    colorIndex++;

                    // subdivEdge의 edgePoints를 하나씩 그려줌
                    foreach (var p in subdivEdge.edgePoints)
                    {
                        Gizmos.DrawSphere(new Vector3(p.x, 0f, p.y), pointSize);
                    }
                }
            }
        }
    }
}
