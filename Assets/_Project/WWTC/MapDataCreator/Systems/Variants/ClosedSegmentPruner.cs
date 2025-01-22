using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ClosedSegmentPruner : MapDataSystem
{
    [FoldoutGroup("Prune Settings")]
    [SerializeField] private int maxIterations = 5;   
    [FoldoutGroup("Prune Settings")]
    [SerializeField] private float epsilon = 1e-5f;   
    [FoldoutGroup("Prune Settings")]
    [SerializeField] private int maxRemovalIteration = 5;

    private static readonly Color[] iterationColors = new Color[]
    {
        Color.yellow,
        Color.cyan,
        new Color(1f, 0.5f, 0f),
        Color.blue,
        Color.magenta,
        Color.white
    };

    [Button("Check Open/Close")]
    private void CheckOpenClose()
    {
        var so = mapDataCreator?.CurrentMapData;
        if (so == null || so.offsetLineGroups == null)
        {
            Debug.LogWarning("[CheckOpenClose] MapDataSO or offsetLineGroups is null.");
            return;
        }

        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;
            for (int i = 0; i < group.lines.Count; i++)
            {
                var seg = group.lines[i];
                seg.startClosed = false;
                seg.endClosed   = false;
                group.lines[i] = seg;
            }
        }

        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;
            MarkEndpointsIfConnectedOnePass(group.lines, epsilon);
        }

        int idx = 0;
        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;
            int closedCount = 0, openedCount = 0;
            foreach (var seg in group.lines)
            {
                if (seg.startClosed && seg.endClosed) closedCount++;
                else openedCount++;
            }
            Debug.Log($"[CheckOpen/Close] Group[{idx}] => closed={closedCount}, opened={openedCount}");
            idx++;
        }
    }

    [Button("Remove Opened Segments")]
    private void RemoveOpenedSegments()
    {
        var so = mapDataCreator?.CurrentMapData;
        if (so == null || so.offsetLineGroups == null)
        {
            Debug.LogWarning("[RemoveOpenedSegments] No offsetLineGroups to process.");
            return;
        }

        int iteration = 0;
        bool removedSomething = true;

        while (removedSomething && iteration < maxIterations)
        {
            removedSomething = false;
            iteration++;

            int totalRemovedThisPass = 0;
            foreach (var group in so.offsetLineGroups)
            {
                if (group.lines == null) continue;

                var pruned = new List<LineSegment2D>();
                int removedCount = 0;

                foreach (var seg in group.lines)
                {
                    if (seg.startClosed && seg.endClosed)
                    {
                        pruned.Add(seg);
                    }
                    else
                    {
                        removedCount++;
                    }
                }

                if (removedCount > 0) removedSomething = true;
                totalRemovedThisPass += removedCount;
                group.lines = pruned;
            }

            Debug.Log($"[RemoveOpenedSegments] Iteration={iteration}, Removed={totalRemovedThisPass}");
        }

        Debug.Log($"[RemoveOpenedSegments] Finished after {iteration} iteration(s).");
    }

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
            Debug.Log("[RemoveSegmentsByCenterMidCheck] offsetLineGroups is empty.");
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
                if (removed) removedAnyThisPass = true;
            }
            if (!removedAnyThisPass) break;
        }

        Debug.Log("[RemoveSegmentsByCenterMidCheck] Completed center-mid checking.");
    }

    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator?.CurrentMapData;
        if (so == null) return;

        for (int i = 0; i < maxIterations; i++)
        {
            CheckOpenClose();
            RemoveOpenedSegments();
            RemoveSegmentsByCenterMidCheck();
        }

        Debug.Log("[ClosedSegmentPruner] Generate complete (checked open/closed, pruned, etc.)");
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null) return;

        if (so.offsetLineGroups != null)
        {
            foreach (var group in so.offsetLineGroups)
            {
                if (group.lines == null) continue;

                foreach (var seg in group.lines)
                {
                    bool closed = (seg.startClosed && seg.endClosed);
                    Gizmos.color = closed ? Color.green : Color.red;

                    Vector3 a = new Vector3(seg.start.x, 0, seg.start.y);
                    Vector3 b = new Vector3(seg.end.x, 0, seg.end.y);
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        if (so.prunerIterations != null)
        {
            for (int i = 0; i < so.prunerIterations.Count; i++)
            {
                var data = so.prunerIterations[i];
                Color c = iterationColors[i % iterationColors.Length];

                foreach (var poly in data.polygons)
                {
                    var pts = poly.points;
                    if (pts == null || pts.Count < 2) continue;

                    for (int p = 0; p < pts.Count; p++)
                    {
                        Vector2 pA = pts[p];
                        Vector2 pB = pts[(p + 1) % pts.Count];
                        Gizmos.color = c;
                        Gizmos.DrawLine(
                            new Vector3(pA.x, 0, pA.y),
                            new Vector3(pB.x, 0, pB.y));
                    }
                }
            }
        }
    }

    private bool MarkEndpointsIfConnectedOnePass(List<LineSegment2D> lines, float eps)
    {
        if (lines == null) return false;
        bool changed = false;
        int count = lines.Count;

        for (int i = 0; i < count; i++)
        {
            var si = lines[i];
            Vector2 sI = si.start;
            Vector2 eI = si.end;
            bool sIc = si.startClosed;
            bool eIc = si.endClosed;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                var sj = lines[j];
                Vector2 sJ = sj.start;
                Vector2 eJ = sj.end;
                bool sJc = sj.startClosed;
                bool eJc = sj.endClosed;

                if ((sI - sJ).sqrMagnitude < eps*eps)
                {
                    if (!sIc) { sIc = true; changed = true; }
                    if (!sJc) { sJc = true; changed = true; }
                }
                if ((sI - eJ).sqrMagnitude < eps*eps)
                {
                    if (!sIc) { sIc = true; changed = true; }
                    if (!eJc) { eJc = true; changed = true; }
                }
                if ((eI - sJ).sqrMagnitude < eps*eps)
                {
                    if (!eIc) { eIc = true; changed = true; }
                    if (!sJc) { sJc = true; changed = true; }
                }
                if ((eI - eJ).sqrMagnitude < eps*eps)
                {
                    if (!eIc) { eIc = true; changed = true; }
                    if (!eJc) { eJc = true; changed = true; }
                }

                sj.startClosed = sJc;
                sj.endClosed   = eJc;
                lines[j] = sj;
            }

            si.startClosed = sIc;
            si.endClosed   = eIc;
            lines[i] = si;
        }

        return changed;
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
            var testLine = new LineSegment2D { start = cPos, end = midA };

            bool intersected = false;
            foreach (var segB in oldLines)
            {
                if (segB.Equals(segA)) continue;

                if (TryGetIntersection(testLine, segB, eps, out _, out _))
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

    private bool TryGetIntersection(LineSegment2D segA, LineSegment2D segB, float eps,
        out IntersectionData interA, out IntersectionData interB)
    {
        interA = default;
        interB = default;

        Vector2 A = segA.start;
        Vector2 B = segA.end;
        Vector2 C = segB.start;
        Vector2 D = segB.end;

        Vector2 AB = B - A;
        Vector2 CD = D - C;
        float denom = AB.x * CD.y - AB.y * CD.x;
        if (Mathf.Abs(denom) < eps) return false;

        Vector2 AC = C - A;
        float tA = (AC.x * CD.y - AC.y * CD.x) / denom;
        float tB = (AC.x * AB.y - AC.y * AB.x) / denom;

        if (tA < -eps || tA > 1f+eps) return false;
        if (tB < -eps || tB > 1f+eps) return false;

        Vector2 ip = A + AB * tA;
        interA = new IntersectionData { t = tA, point = ip };
        interB = new IntersectionData { t = tB, point = ip };
        return true;
    }
}
