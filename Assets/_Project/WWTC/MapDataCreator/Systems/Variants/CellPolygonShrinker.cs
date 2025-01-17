using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Convex 폴리곤에 대해, 면적이 일정 threshold 이상이면 shrinkOffset만큼 내부로 offset.
/// 결과는 MapDataSO.shrunkPolygons에 저장.
/// 부모클래스(MapDataSystem)에 drawGizmo, mapDataCreator 등이 있다고 가정.
/// </summary>
public class CellPolygonShrinker : MapDataSystem
{
    [TitleGroup("Shrink Settings")]
    [SerializeField] private float shrinkOffset = 1f;

    [TitleGroup("Shrink Settings")]
    [SerializeField] private float minAreaThreshold = 1f;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color shrinkEdgeColor = Color.magenta;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color shrinkCenterColor = Color.cyan;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private float shrinkGizmoSphereSize = 0.2f;

    public override void Generate()
    {
        // IsReady와 mapDataCreator는 부모 클래스인 MapDataSystem에 있다고 가정
        if (!IsReady) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null)
        {
            Debug.LogWarning("[CellPolygonShrinker] No MapDataSO assigned.");
            return;
        }

        mapData.shrunkPolygons.Clear();

        int count = 0;
        foreach (var cell in mapData.cellPolygons)
        {
            var poly = cell.points;
            if (poly.Count < 3) continue;

            float area = Mathf.Abs(ComputePolygonArea(poly));
            if (area < minAreaThreshold)
                continue; // 면적이 작은 폴리곤은 skip

            // 실제 Convex 폴리곤으로 가정
            List<Vector2> shrinked = InwardOffset(poly, shrinkOffset);
            if (shrinked.Count < 3)
                continue; // 소멸된 경우

            // center 계산
            Vector2 center = Vector2.zero;
            foreach (var p in shrinked)
                center += p;
            center /= shrinked.Count;

            // shrinked 결과 저장
            CellPolygon newPoly = new CellPolygon
            {
                cellKey = cell.cellKey,
                points = shrinked,
                center = center
            };
            mapData.shrunkPolygons.Add(newPoly);
            count++;
        }

        Debug.Log($"[CellPolygonShrinker] {count} polygons shrinked. offset={shrinkOffset}, minArea={minAreaThreshold}");
    }

    private List<Vector2> InwardOffset(List<Vector2> polygon, float offset)
    {
        List<Vector2> result = new List<Vector2>(polygon);
        bool isCW = IsClockwise(polygon);

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Count];

            Vector2 edge = p2 - p1;
            float len = edge.magnitude;
            if (len < 1e-9f) continue;

            edge /= len;
            Vector2 normal = isCW ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x);

            Vector2 linePt = p1 + normal * offset;
            result = ClipByLine(result, linePt, normal, isCW);
            if (result.Count < 3) break;
        }
        return result;
    }

    private List<Vector2> ClipByLine(List<Vector2> subject, Vector2 linePt, Vector2 normal, bool isCW)
    {
        List<Vector2> output = new List<Vector2>();
        if (subject.Count == 0) return output;

        for (int i = 0; i < subject.Count; i++)
        {
            Vector2 curr = subject[i];
            Vector2 prev = subject[(i - 1 + subject.Count) % subject.Count];

            bool currInside = IsInside(curr, linePt, normal, isCW);
            bool prevInside = IsInside(prev, linePt, normal, isCW);

            if (prevInside && currInside)
            {
                output.Add(curr);
            }
            else if (prevInside && !currInside)
            {
                if (Intersect(prev, curr, linePt, normal, isCW, out Vector2 ip))
                    output.Add(ip);
            }
            else if (!prevInside && currInside)
            {
                if (Intersect(prev, curr, linePt, normal, isCW, out Vector2 ip))
                    output.Add(ip);
                output.Add(curr);
            }
        }
        return output;
    }

    private bool IsInside(Vector2 pt, Vector2 linePt, Vector2 normal, bool isCW)
    {
        float d = Vector2.Dot(normal, pt - linePt);
        return isCW ? (d >= 0f) : (d <= 0f);
    }

    private bool Intersect(Vector2 a, Vector2 b, Vector2 linePt, Vector2 normal, bool isCW, out Vector2 ip)
    {
        ip = Vector2.zero;
        Vector2 ab = b - a;
        float denom = Vector2.Dot(normal, ab);
        if (Mathf.Abs(denom) < 1e-9f)
            return false;

        float numer = Vector2.Dot(normal, (linePt - a));
        float t = numer / denom;
        if (t < 0f || t > 1f)
            return false;

        ip = a + ab * t;
        return true;
    }

    private bool IsClockwise(List<Vector2> poly)
    {
        float signedArea = ComputePolygonArea(poly);
        return (signedArea < 0f); // signedArea<0 => CW
    }

    private float ComputePolygonArea(List<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 c1 = poly[i];
            Vector2 c2 = poly[(i + 1) % poly.Count];
            area += (c1.x * c2.y - c2.x * c1.y);
        }
        return 0.5f * area;
    }

    // OnDrawGizmos는 부모에 drawGizmo가 있고, mapDataCreator도 있다고 가정
    private void OnDrawGizmos()
    {
        // 만약 부모클래스의 drawGizmo가 없으면, 아래 코드는 컴파일 에러
        if (!drawGizmo) return;
        if (mapDataCreator == null) return;
        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.shrunkPolygons == null) return;

        // shrunkPolygons 그리기
        Gizmos.color = shrinkEdgeColor;
        foreach (var cell in so.shrunkPolygons)
        {
            var pts = cell.points;
            for (int i=0; i<pts.Count; i++)
            {
                Vector2 p1 = pts[i];
                Vector2 p2 = pts[(i+1)%pts.Count];
                Gizmos.DrawLine(new Vector3(p1.x,0,p1.y), new Vector3(p2.x,0,p2.y));
            }
        }

        // 축소된 폴리곤들의 center
        Gizmos.color = shrinkCenterColor;
        foreach (var cell in so.shrunkPolygons)
        {
            Vector2 c = cell.center;
            Gizmos.DrawSphere(new Vector3(c.x, 0, c.y), shrinkGizmoSphereSize);
        }
    }
}
