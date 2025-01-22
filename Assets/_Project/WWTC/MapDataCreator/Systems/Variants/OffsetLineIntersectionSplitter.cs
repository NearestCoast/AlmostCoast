using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class OffsetLineIntersectionSplitter : MapDataSystem
{
    [FoldoutGroup("Intersection Split Settings")]
    [SerializeField] private int maxIterations = 10;
    [FoldoutGroup("Intersection Split Settings")]
    [SerializeField] private float epsilon = 1e-4f;
    [FoldoutGroup("Intersection Split Settings")]
    [SerializeField] private int maxRemovalIteration = 5;

    private static readonly Color[] colorPalette = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.cyan,
        Color.magenta,
        Color.yellow,
        new Color(1f, 0.5f, 0f)
    };

    [Button("RemoveSegmentsByCenterMidCheck")]
    private void RemoveSegmentsByCenterMidCheck()
    {
        var so = mapDataCreator?.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[RemoveSegmentsByCenterMidCheck] MapDataSO is null.");
            return;
        }
        if (so.offsetLineGroups == null || so.offsetLineGroups.Count == 0)
        {
            Debug.Log("[RemoveSegmentsByCenterMidCheck] No offsetLineGroups found.");
            return;
        }

        for (int iteration = 0; iteration < maxRemovalIteration; iteration++)
        {
            bool removedAnyThisPass = false;

            foreach (var group in so.offsetLineGroups)
            {
                if (group.lines == null || group.lines.Count < 2) 
                    continue;

                bool removed = CheckAndRemoveSegmentsByMidCenter(group, epsilon);
                if (removed) 
                    removedAnyThisPass = true;
            }

            if (!removedAnyThisPass)
                break;
        }

        Debug.Log("[RemoveSegmentsByCenterMidCheck] Completed new feature (center-mid checking).");
    }

    [Button("Split Intersections Only")]
    private void PerformSplitIntersectionsOnly()
    {
        var so = mapDataCreator?.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[PerformSplitIntersectionsOnly] MapDataSO is null.");
            return;
        }
        if (so.offsetLineGroups == null || so.offsetLineGroups.Count == 0)
        {
            Debug.Log("[PerformSplitIntersectionsOnly] No offsetLineGroups found.");
            return;
        }

        int groupProcessed = 0;

        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null || group.lines.Count < 2)
                continue;

            int iteration = 0;
            while (iteration < maxIterations)
            {
                bool splitted = SplitAllIntersectionsInGroup(group, epsilon);
                if (!splitted) break; 
                iteration++;
            }
            groupProcessed++;
        }

        Debug.Log($"[PerformSplitIntersectionsOnly] Done. Groups processed={groupProcessed}");
    }

    public override void Generate()
    {
        if (!IsReady) return;
        RemoveSegmentsByCenterMidCheck();
        PerformSplitIntersectionsOnly();
    }

    private bool CheckAndRemoveSegmentsByMidCenter(OffsetLineGroup group, float eps)
    {
        if (group.lines == null || group.lines.Count < 1)
            return false;

        var oldLines = group.lines;
        var newLines = new List<LineSegment2D>();
        bool anyRemoved = false;

        foreach (var segA in oldLines)
        {
            Vector2 midA = 0.5f * (segA.start + segA.end);
            Vector2 cPos = group.center;
            var testLine = new LineSegment2D
            {
                start = cPos,
                end   = midA
            };

            bool intersected = false;
            foreach (var segB in oldLines)
            {
                if (segB.Equals(segA)) 
                    continue;

                if (TryGetIntersection(testLine, segB, eps, out var _, out var _))
                {
                    intersected = true;
                    break;
                }
            }

            if (intersected)
            {
                anyRemoved = true;
            }
            else
            {
                newLines.Add(segA);
            }
        }

        group.lines = newLines;
        return anyRemoved;
    }

    private bool SplitAllIntersectionsInGroup(OffsetLineGroup group, float eps)
    {
        var oldLines = group.lines;
        int n = oldLines.Count;
        if (n < 2) return false;

        var intersectionMap = new Dictionary<int, List<IntersectionOnLine>>();
        for (int i = 0; i < n; i++)
            intersectionMap[i] = new List<IntersectionOnLine>();

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var segA = oldLines[i];
                var segB = oldLines[j];
                if (TryGetIntersection(segA, segB, eps, out var interA, out var interB))
                {
                    intersectionMap[i].Add(new IntersectionOnLine
                    {
                        t     = interA.t,
                        point = interA.point
                    });
                    intersectionMap[j].Add(new IntersectionOnLine
                    {
                        t     = interB.t,
                        point = interB.point
                    });
                }
            }
        }

        bool foundAny = false;
        foreach (var kv in intersectionMap)
        {
            if (kv.Value.Count > 0)
            {
                foundAny = true;
                break;
            }
        }
        if (!foundAny) return false;

        var newLines = new List<LineSegment2D>();
        for (int i = 0; i < n; i++)
        {
            var seg = oldLines[i];
            var list = intersectionMap[i];
            if (list.Count == 0)
            {
                newLines.Add(seg);
                continue;
            }

            list.Sort((a, b) => a.t.CompareTo(b.t));

            Vector2 currStart = seg.start;
            for (int k = 0; k < list.Count; k++)
            {
                float tVal = list[k].t;
                Vector2 pt = list[k].point;

                bool nearStart = (tVal <= eps);
                bool nearEnd   = (tVal >= 1f - eps);

                if (!nearStart && !nearEnd)
                {
                    var segPart = new LineSegment2D
                    {
                        start       = currStart,
                        end         = pt,
                        startClosed = false,
                        endClosed   = false
                    };
                    newLines.Add(segPart);
                    currStart = pt;
                }
            }

            var lastPart = new LineSegment2D
            {
                start       = currStart,
                end         = seg.end,
                startClosed = false,
                endClosed   = false
            };
            newLines.Add(lastPart);
        }

        group.lines = newLines;
        return true;
    }

    private bool TryGetIntersection(LineSegment2D segA, LineSegment2D segB, float eps,
                                    out IntersectionData interA, out IntersectionData interB)
    {
        interA = default;
        interB = default;

        var A = segA.start;
        var B = segA.end;
        var C = segB.start;
        var D = segB.end;

        Vector2 AB = B - A;
        Vector2 CD = D - C;
        float denom = AB.x * CD.y - AB.y * CD.x;
        if (Mathf.Abs(denom) < eps)
            return false;

        Vector2 AC = C - A;
        float tA = (AC.x * CD.y - AC.y * CD.x) / denom;
        float tB = (AC.x * AB.y - AC.y * AB.x) / denom;

        if (tA < -eps || tA > 1f + eps) return false;
        if (tB < -eps || tB > 1f + eps) return false;

        Vector2 ip = A + AB * tA;
        interA.t = tA; interA.point = ip;
        interB.t = tB; interB.point = ip;
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator?.CurrentMapData;
        if (so == null || so.offsetLineGroups == null) return;

        int colorIndex = 0;
        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;

            foreach (var seg in group.lines)
            {
                Gizmos.color = colorPalette[colorIndex % colorPalette.Length];
                colorIndex++;

                Gizmos.DrawLine(
                    new Vector3(seg.start.x, 0, seg.start.y),
                    new Vector3(seg.end.x,   0, seg.end.y));
            }
        }
    }
}
