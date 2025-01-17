using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Convex 폴리곤이라는 전제 하에,
/// 1) 면적이 일정 threshold 이상인 폴리곤만 Inward Offset(단순 정점 이동)
/// 2) 그리고 최종적으로 Convex Hull을 적용해 교차/뒤집힘을 제거
/// 3) 결과를 MapDataSO.shrunkPolygons에 저장
/// </summary>
public class TestShrinker : MapDataSystem
{
    [Title("Shrink Settings")]
    [SerializeField] private float shrinkOffset = 1f;

    [Title("Shrink Settings")]
    [SerializeField] private float minAreaThreshold = 1f; // 이 값 미만인 폴리곤은 제외

    // Gizmo (부모 클래스 MapDataSystem에 protected bool drawGizmo 존재 가정)
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color shrinkEdgeColor = Color.magenta;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color shrinkCenterColor = Color.cyan;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private float shrinkGizmoSphereSize = 0.2f;

    public override void Generate()
    {
        if (!IsReady) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null)
        {
            Debug.LogWarning("[TestShrinker] No MapDataSO assigned.");
            return;
        }

        // 결과 보관 리스트 초기화
        mapData.shrunkPolygons.Clear();

        int polygonCount = 0;
        foreach (var cell in mapData.cellPolygons)
        {
            var original = cell.points;
            if (original.Count < 3) 
                continue;

            // 먼저 폴리곤 면적 계산
            float area = Mathf.Abs(SignedArea(original));
            if (area < minAreaThreshold)
            {
                // 면적이 너무 작은 폴리곤은 스킵
                continue;
            }

            // 1) 단순 Inward Offset
            List<Vector2> offsetPoly = InwardOffsetWithoutTrim(original, shrinkOffset);
            if (offsetPoly.Count < 3) continue;

            // 2) Convex Hull로 “교차된 선분” 정리
            List<Vector2> finalPoly = BuildConvexHull(offsetPoly);
            if (finalPoly.Count < 3) continue;

            // 3) 중심점 계산
            Vector2 center = Vector2.zero;
            foreach (var p in finalPoly) center += p;
            center /= finalPoly.Count;

            // 4) 결과 저장
            CellPolygon newCell = new CellPolygon
            {
                cellKey = cell.cellKey, 
                points  = finalPoly,
                center  = center
            };
            mapData.shrunkPolygons.Add(newCell);

            polygonCount++;
        }

        Debug.Log($"[TestShrinker] {polygonCount} polygons offset by {shrinkOffset}, hull-applied; minArea={minAreaThreshold}");
    }

    /// <summary>
    /// Convex 폴리곤에서 각 정점을 '인접 두 에지'의 Inward 방향으로 offset.
    /// 별도 교차/Trim 로직 없이 정점만 이동하므로, offset이 클 때는 교차 가능성 있음.
    /// </summary>
    private List<Vector2> InwardOffsetWithoutTrim(List<Vector2> polygon, float offset)
    {
        bool isCW = IsClockwise(polygon);
        var newPoly = new List<Vector2>(polygon.Count);
        int n = polygon.Count;

        for (int i = 0; i < n; i++)
        {
            Vector2 curr = polygon[i];
            Vector2 prev = polygon[(i + n - 1) % n];
            Vector2 next = polygon[(i + 1) % n];

            Vector2 edgeA = (curr - prev).normalized;
            Vector2 edgeB = (next - curr).normalized;

            // Inward Normal: CW->오른쪽, CCW->왼쪽
            Vector2 normalA = isCW 
                ? new Vector2(edgeA.y, -edgeA.x) 
                : new Vector2(-edgeA.y, edgeA.x);
            Vector2 normalB = isCW
                ? new Vector2(edgeB.y, -edgeB.x)
                : new Vector2(-edgeB.y, edgeB.x);

            // 두 노멀 평균
            Vector2 inwardDir = (normalA + normalB).normalized;
            Vector2 newPos = curr + inwardDir * offset;
            newPoly.Add(newPos);
        }

        return newPoly;
    }

    /// <summary>
    /// Convex Hull (Monotone chain) 
    /// 교차/뒤집힘을 어느정도 제거하기 위해 최종적으로 적용.
    /// </summary>
    private List<Vector2> BuildConvexHull(List<Vector2> pts)
    {
        if (pts.Count < 3) return new List<Vector2>(pts);

        // X좌표, Y좌표 순으로 정렬
        pts.Sort((p1, p2) =>
        {
            if (Mathf.Approximately(p1.x, p2.x))
                return p1.y.CompareTo(p2.y);
            return p1.x.CompareTo(p2.x);
        });

        var hull = new List<Vector2>();

        // lower hull
        for (int i = 0; i < pts.Count; i++)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], pts[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(pts[i]);
        }
        // upper hull
        for (int i = pts.Count - 2, l = hull.Count + 1; i >= 0; i--)
        {
            while (hull.Count >= l && Cross(hull[hull.Count - 2], hull[hull.Count - 1], pts[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(pts[i]);
        }
        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    /// <summary>
    /// Cross(a,b,c): (b - a) x (c - a)
    /// 값 > 0 => a->b->c가 CCW, 값 < 0 => CW
    /// </summary>
    private float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x)*(c.y - a.y) - (b.y - a.y)*(c.x - a.x);
    }

    /// <summary>
    /// 폴리곤의 싸인(area) 계산
    /// area < 0 => CW, area > 0 => CCW
    /// </summary>
    private float SignedArea(List<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            var c1 = poly[i];
            var c2 = poly[(i + 1) % poly.Count];
            area += (c1.x * c2.y - c2.x * c1.y);
        }
        return 0.5f * area;
    }

    private bool IsClockwise(List<Vector2> poly)
    {
        return (SignedArea(poly) < 0f);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return; 
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.shrunkPolygons == null) return;

        // shrunkPolygons 시각화
        Gizmos.color = shrinkEdgeColor;
        foreach (var poly in so.shrunkPolygons)
        {
            var pts = poly.points;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 p1 = pts[i];
                Vector2 p2 = pts[(i+1) % pts.Count];
                Gizmos.DrawLine(new Vector3(p1.x, 0f, p1.y),
                                new Vector3(p2.x, 0f, p2.y));
            }
        }

        // center 표시
        Gizmos.color = shrinkCenterColor;
        foreach (var poly in so.shrunkPolygons)
        {
            Vector2 c = poly.center;
            Gizmos.DrawSphere(new Vector3(c.x, 0f, c.y), shrinkGizmoSphereSize);
        }
    }
}
