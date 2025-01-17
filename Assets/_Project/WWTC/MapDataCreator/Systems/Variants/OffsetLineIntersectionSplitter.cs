using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// MapDataSystem을 상속.
/// - 새 기능: 각 그룹의 center ~ 선분(a)의 중점을 연결하는 선분이 다른 선분과 교차하면 선분(a)를 제거
/// - 기존 기능: 선분 쌍 교차점을 찾아 분할
/// - Generate()는 위 순서대로 두 기능을 연달아 호출
/// </summary>
public class OffsetLineIntersectionSplitter : MapDataSystem
{
    [FoldoutGroup("Intersection Split Settings")]
    [SerializeField] private int maxIterations = 10;      // 교차 분할 반복 제한

    [FoldoutGroup("Intersection Split Settings")]
    [SerializeField] private float epsilon = 1e-4f;       // 교차 판단 오차

    [FoldoutGroup("Intersection Split Settings")]
    [SerializeField] private int maxRemovalIteration = 5; // "중점-센터" 교차 검사 후 제거 반복 제한

    // 원하는 색상 팔레트를 지정
    private static readonly Color[] colorPalette = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.cyan,
        Color.magenta,
        Color.yellow,
        new Color(1f, 0.5f, 0f) // 오렌지
    };

    //--------------------------------------------------------------------------
    // (1) 새 기능만 실행하는 버튼
    //--------------------------------------------------------------------------
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

        // 여러 번 반복하여 선분 제거
        for (int iteration = 0; iteration < maxRemovalIteration; iteration++)
        {
            bool removedAnyThisPass = false;

            // 각 offsetLineGroup에 대해 검사
            foreach (var group in so.offsetLineGroups)
            {
                if (group.lines == null || group.lines.Count < 2) 
                    continue;

                bool removed = CheckAndRemoveSegmentsByMidCenter(group, epsilon);
                if (removed) 
                    removedAnyThisPass = true;
            }

            // 한 번도 제거가 일어나지 않았다면 종료
            if (!removedAnyThisPass)
                break;
        }

        Debug.Log("[RemoveSegmentsByCenterMidCheck] Completed new feature (center-mid checking).");
    }

    //--------------------------------------------------------------------------
    // (2) 기존 기능만 실행하는 버튼
    //--------------------------------------------------------------------------
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

    //--------------------------------------------------------------------------
    // Generate() => (1)새 기능 → (2)기존 기능
    //--------------------------------------------------------------------------
    public override void Generate()
    {
        if (!IsReady) return;

        // 1) 중점-센터 교차 검사 후 제거
        RemoveSegmentsByCenterMidCheck();

        // 2) 선분 교차점 분할
        PerformSplitIntersectionsOnly();
    }

    //--------------------------------------------------------------------------
    // (A) 새 기능: 중점-센터 교차 검사 후 제거
    //--------------------------------------------------------------------------
    /// <summary>
    /// 그룹의 center ~ (각 선분의 중점) 을 잇는 선분이
    /// 기존 다른 선분들과 교차하면 해당 선분(자기 자신)을 제거.
    /// </summary>
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
            // center ~ 중점을 이은 임시 선분
            var testLine = new LineSegment2D
            {
                start = cPos,
                end   = midA
            };

            bool intersected = false;
            // 자기 자신 제외
            foreach (var segB in oldLines)
            {
                // 같은 참조(동일 선분)인지 확인할 방법이 없으므로, 
                // 실제 좌표값이 같은지 비교하거나 별도 아이디가 있어야 하지만,
                // 여기서는 "동일 개체"가 아닌 "동일 index"로 구분했다고 가정.
                // => 편의상 "본인" 제외가 필요하다면, oldLines.IndexOf(segA)...등 추가 작업 필요

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
                // 교차 시 제거
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

    //--------------------------------------------------------------------------
    // (B) 기존 기능: 선분 쌍 교차점 분할
    //--------------------------------------------------------------------------
    private bool SplitAllIntersectionsInGroup(OffsetLineGroup group, float eps)
    {
        var oldLines = group.lines;
        int n = oldLines.Count;
        if (n < 2) return false;

        // (1) 각 선분별 교차점 수집
        var intersectionMap = new Dictionary<int, List<IntersectionOnLine>>();
        for (int i = 0; i < n; i++)
            intersectionMap[i] = new List<IntersectionOnLine>();

        // (2) 모든 쌍(i<j)에 대해 교차 검사
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

        // 교차점이 하나도 없다면 false
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

        // (3) 교차점 있는 선분 분할
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

            // t값 정렬
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

            // 마지막 조각
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

    //--------------------------------------------------------------------------
    // 공용 교차 검사
    //--------------------------------------------------------------------------
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
            return false; // 평행

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

    // 교차점 구조체
    private struct IntersectionData
    {
        public float t;
        public Vector2 point;
    }
    private struct IntersectionOnLine
    {
        public float t;
        public Vector2 point;
    }

    //--------------------------------------------------------------------------
    // 기즈모: 선분별 색상 순환
    //--------------------------------------------------------------------------
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
