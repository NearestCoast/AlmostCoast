using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// MapDataSO.cellPolygons를 대상으로,
/// (면적 >= minAreaThreshold 인 것만) Convex 폴리곤을 'offset'만큼 안쪽으로 평행이동(Inward)한 뒤,
/// 그 결과로 생성된 (Shift + 양쪽 확장) 선분들을 MapDataSO.offsetLineGroups에 저장.
/// OffsetLineGroup에는 cellPolygon의 무게중심(center)도 함께 저장.
///
/// LineSegment2D에 startClosed, endClosed 필드는 있지만, 여기서는 기본값(false)로만 사용.
/// (닫힘 판단 등은 다른 로직에서 별도로 설정 가능)
/// 
/// 변경 요점: 
///   1) 기존과 동일하게 노멀 방향으로 offset만큼 '이동'
///   2) 그 후, 선분 양끝을 선분 진행 방향으로 offset만큼 추가 연장
///      => "양쪽으로 offset만큼 선분 길이를 늘린다."
/// </summary>
public class TestShrinker : MapDataSystem
{
    [Title("Offset Settings")]
    [SerializeField] private float offset = 1f;

    [Title("Polygon Filter")]
    [SerializeField, Min(0f)]
    private float minAreaThreshold = 1f;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color lineColor = Color.magenta;

    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[TestShrinker] No MapDataSO assigned.");
            return;
        }

        // 결과 리스트 초기화
        so.offsetLineGroups.Clear();

        int totalGroups = 0;
        int totalLines = 0;

        // cellPolygons 순회
        foreach (var poly in so.cellPolygons)
        {
            // 면적 필터
            if (poly.area < minAreaThreshold) 
                continue;

            var pts = poly.points;
            if (pts == null || pts.Count < 3)
                continue;

            // CW(CCW) 판별
            bool isCW = IsClockwise(pts);

            // 새로운 그룹 생성
            OffsetLineGroup group = new OffsetLineGroup
            {
                cellKey = poly.cellKey,
                center  = poly.center,            
                lines   = new List<LineSegment2D>()
            };

            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = pts[i];
                Vector2 p2 = pts[(i + 1) % n];

                Vector2 edge = p2 - p1;
                float length = edge.magnitude;
                if (length < 1e-9f)
                    continue;

                edge /= length;  // 에지 방향 단위벡터

                // 1) 노멀 방향 오프셋
                Vector2 normal = isCW 
                    ? new Vector2(edge.y, -edge.x)
                    : new Vector2(-edge.y, edge.x);

                Vector2 shift = normal * offset;

                // 평행 이동된 점
                Vector2 p1Shifted = p1 + shift;
                Vector2 p2Shifted = p2 + shift;

                // 2) 선분 진행 방향으로 (edge 방향) 양 끝을 offset만큼 추가 연장
                //    p1Shifted - (edge*offset), p2Shifted + (edge*offset) 
                p1Shifted -= edge * offset;  // 뒤쪽으로 offset만큼
                p2Shifted += edge * offset;  // 앞쪽으로 offset만큼

                // 새 선분 생성
                LineSegment2D seg = new LineSegment2D
                {
                    start = p1Shifted,
                    end   = p2Shifted,
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

    /// <summary>
    /// Shoelace formula로 폴리곤이 CW인지 CCW인지 판별
    /// area < 0 => CW, area > 0 => CCW
    /// </summary>
    private bool IsClockwise(List<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            var c1 = poly[i];
            var c2 = poly[(i + 1) % poly.Count];
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

        // 저장된 line segments 시각화
        foreach (var group in so.offsetLineGroups)
        {
            foreach (var seg in group.lines)
            {
                Vector3 a = new Vector3(seg.start.x, 0f, seg.start.y);
                Vector3 b = new Vector3(seg.end.x,   0f, seg.end.y);
                Gizmos.DrawLine(a, b);
            }

            // center 표시 
            Vector3 centerPos = new Vector3(group.center.x, 0f, group.center.y);
            Gizmos.DrawSphere(centerPos, 0.1f); 
        }
    }
}
