using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TestShrinker : MapDataSystem
{
    private static int s_nextGroupID = 1;

    [Title("Offset Settings")]
    [SerializeField] private float offset = 1f;

    [Title("Polygon Filter")]
    [SerializeField, Min(0f)]
    private float minAreaThreshold = 1f;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color lineColor = Color.magenta;

    public void SetOffset(float newOffset)
    {
        offset = newOffset;
    }

    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[TestShrinker] No MapDataSO assigned.");
            return;
        }

        so.offsetLineGroups.Clear();

        int totalGroups = 0;
        int totalLines = 0;

        foreach (var poly in so.cellPolygons)
        {
            if (poly.area < minAreaThreshold) 
                continue;

            var pts = poly.points;
            if (pts == null || pts.Count < 3)
                continue;

            bool isCW = IsClockwise(pts);

            OffsetLineGroup group = new OffsetLineGroup
            {
                uniqueID = s_nextGroupID++,
                cellKey  = poly.cellKey,
                center   = poly.center,            
                lines    = new List<LineSegment2D>()
            };

            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = pts[i];
                Vector2 p2 = pts[(i + 1) % n];
                Vector2 edge = p2 - p1;
                float length = edge.magnitude;
                if (length < 1e-9f) continue;
                edge /= length;

                Vector2 normal = isCW 
                    ? new Vector2(edge.y, -edge.x)
                    : new Vector2(-edge.y, edge.x);

                Vector2 shift = normal * offset;
                Vector2 p1Shifted = p1 + shift;
                Vector2 p2Shifted = p2 + shift;
                p1Shifted -= edge * offset;
                p2Shifted += edge * offset;

                LineSegment2D seg = new LineSegment2D
                {
                    start       = p1Shifted,
                    end         = p2Shifted,
                    startClosed = false,
                    endClosed   = false
                };

                group.lines.Add(seg);
            }

            if (group.lines.Count > 0)
            {
                so.offsetLineGroups.Add(group);
                totalGroups++;
                totalLines += group.lines.Count;
            }
        }

        Debug.Log($"[TestShrinker] Created {totalGroups} groups, total {totalLines} lines, offset={offset}, minArea={minAreaThreshold}");
    }

    private bool IsClockwise(List<Vector2> pts)
    {
        float area = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            var c1 = pts[i];
            var c2 = pts[(i + 1) % pts.Count];
            area += (c1.x * c2.y - c2.x * c1.y);
        }
        return (area < 0f);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.offsetLineGroups == null) return;

        Gizmos.color = lineColor;

        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;
            foreach (var seg in group.lines)
            {
                Vector3 a = new Vector3(seg.start.x, 0f, seg.start.y);
                Vector3 b = new Vector3(seg.end.x,   0f, seg.end.y);
                Gizmos.DrawLine(a, b);
            }

            Vector3 centerPos = new Vector3(group.center.x, 0f, group.center.y);
            Gizmos.DrawSphere(centerPos, 0.1f); 
        }
    }
}
