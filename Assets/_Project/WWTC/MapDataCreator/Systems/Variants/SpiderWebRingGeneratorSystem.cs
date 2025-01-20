using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// "거미줄 트라이앵글" 1단계: 링(Ring) 생성 (거리 기반).
/// - ringSpacing 간격으로 중심→경계 사이에 여러 링을 만든다.
/// - i번째 링은 ringDist = i * ringSpacing
///   각 경계정점 p_j와 중심 c 사이에서
///     ratio_i = min(ringDist / dist_j, 1)
///     ringPoint = c + ratio_i * (p_j - c)
/// - 짧은 에지(mergeThreshold 이하)는 병합 처리(옵션).
///
/// * 볼록(Convex) 폴리곤 전제
/// * 완전히 균일한 형태를 보장하진 않지만, 일정 간격으로 링을 구성
/// </summary>
public class SpiderWebRingGeneratorSystem : MapDataSystem
{
    [FoldoutGroup("Distance-based Ring")]
    [SerializeField, Min(0.01f)]
    [Tooltip("링 간격 (거미줄 링 사이의 거리를 일정하게 유지)\n예: 1.0 => 1m 간격으로 링 생성")]
    private float ringSpacing = 1f;

    [FoldoutGroup("Distance-based Ring")]
    [SerializeField, Min(0f)]
    [Tooltip("이 길이보다 짧은 에지는 병합(Merge)")]
    private float mergeThreshold = 0.1f;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField]
    private Color gizmoColor = Color.magenta;

    public override void Generate()
    {
        base.Generate();
        if (!IsReady) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // 기존 ringPolygonSets 초기화
        mapData.ringPolygonSets.Clear();

        // 모든 arrangedCellPolygons에 대해 처리
        foreach (var poly in mapData.arrangedCellPolygons)
        {
            var boundaryPoints = poly.points;
            if (boundaryPoints == null || boundaryPoints.Count < 3)
                continue;

            // 1) 혹시 폴리곤이 시계(CW)면 반시계(CCW)로 뒤집기
            float signedArea = GetSignedArea(boundaryPoints);
            if (signedArea < 0)
            {
                boundaryPoints.Reverse();
            }

            // 2) 중심(centroid)
            Vector2 center = CalculateCentroid(boundaryPoints);

            // 3) 각 정점까지의 거리 dist_j, 그 중 최댓값 maxDist
            float maxDist = 0f;
            var distList = new float[boundaryPoints.Count];
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                float d = Vector2.Distance(center, boundaryPoints[i]);
                distList[i] = d;
                if (d > maxDist) maxDist = d;
            }

            if (maxDist <= 0f)
            {
                // 모든 점이 거의 같은 위치인 특수 케이스
                continue;
            }

            // 4) 총 링 개수 ringCount = floor(maxDist / ringSpacing)
            int ringCount = Mathf.FloorToInt(maxDist / ringSpacing);

            // (예외) spacing이 너무 커서 ringCount=0이면, 그래도 최소 1개(경계) 링?
            // → i=0~ringCount(포함) 로직에서, i=0 => dist=0 => 모두 center
            //   i=ringCount => ringDist= ringCount* ringSpacing (최대 maxDist 근사)
            // 아래에서 i=ringCount 까지 돌면 "경계"도 커버 가능
            // (단, ringCount * ringSpacing <= maxDist 인 상황)

            // 링들을 담을 리스트
            List<List<Vector2>> ringList = new List<List<Vector2>>(ringCount + 1);

            // 5) 각 i=0..ringCount에 대해 ringDist = i*ringSpacing
            //    i=0이면 dist=0 => 모든 점이 center
            //    i=ringCount => dist ~ (ringCount * ringSpacing <= maxDist)
            for (int i = 0; i <= ringCount; i++)
            {
                float ringDist = i * ringSpacing;
                List<Vector2> ringPoly = new List<Vector2>(boundaryPoints.Count);

                for (int v = 0; v < boundaryPoints.Count; v++)
                {
                    float d = distList[v];
                    Vector2 p = boundaryPoints[v];

                    float ratio = 0f;
                    if (d > 0f)
                    {
                        // ringDist / d  (최대 1)
                        ratio = Mathf.Min(ringDist / d, 1f);
                    }
                    // ringPoint
                    Vector2 ringPoint = center + ratio * (p - center);
                    ringPoly.Add(ringPoint);
                }

                // (옵션) 매우 짧은 에지 병합
                if (mergeThreshold > 0f)
                {
                    ringPoly = MergeShortEdges(ringPoly, mergeThreshold);
                }

                ringList.Add(ringPoly);
            }

            // RingPolygonSet 생성
            RingPolygonSet ringSet = new RingPolygonSet
            {
                cellKey = poly.cellKey,
                rings = ringList
            };

            mapData.ringPolygonSets.Add(ringSet);
        }

        Debug.Log($"[{nameof(SpiderWebRingGeneratorSystem)}] " +
                  $"Distance-based Ring 생성 완료 (ringSpacing={ringSpacing}, mergeThreshold={mergeThreshold}). " +
                  $"(총 {mapData.ringPolygonSets.Count} 셋)");
    }

    /// <summary>
    /// 단일 패스로, 인접 에지가 mergeThreshold 미만이면 중간점으로 병합.
    /// (더 정교하게 하려면 반복 패스 or 다른 로직)
    /// </summary>
    private List<Vector2> MergeShortEdges(List<Vector2> ring, float minLen)
    {
        if (ring.Count < 2) 
            return ring;

        List<Vector2> merged = new List<Vector2>();
        int n = ring.Count;
        int i = 0;
        while (i < n)
        {
            int j = (i + 1) % n;
            Vector2 curr = ring[i];
            Vector2 nxt = ring[j];

            float dist = Vector2.Distance(curr, nxt);
            if (dist < minLen)
            {
                // Merge into midpoint
                Vector2 mid = 0.5f * (curr + nxt);
                merged.Add(mid);

                // skip i+1
                i += 2;
            }
            else
            {
                merged.Add(curr);
                i++;
            }
        }
        return merged;
    }

    /// <summary> 
    /// 폴리곤 signed area (CCW => +, CW => -)
    /// </summary>
    private float GetSignedArea(List<Vector2> points)
    {
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];
            area += (p1.x * p2.y - p2.x * p1.y);
        }
        return area * 0.5f;
    }

    /// <summary>
    /// 볼록 폴리곤의 중심 (단순히 정점 평균)
    /// </summary>
    private Vector2 CalculateCentroid(List<Vector2> polygon)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in polygon)
        {
            sum += p;
        }
        return sum / polygon.Count;
    }

    /// <summary>
    /// 기즈모로 ring들을 표시
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null || mapDataCreator.CurrentMapData == null) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData.ringPolygonSets == null) return;

        Gizmos.color = gizmoColor;

        foreach (var ringSet in mapData.ringPolygonSets)
        {
            foreach (var ring in ringSet.rings)
            {
                if (ring == null || ring.Count < 2) continue;

                for (int i = 0; i < ring.Count; i++)
                {
                    Vector2 p1 = ring[i];
                    Vector2 p2 = ring[(i + 1) % ring.Count];
                    Vector3 v1 = new Vector3(p1.x, 0f, p1.y);
                    Vector3 v2 = new Vector3(p2.x, 0f, p2.y);
                    Gizmos.DrawLine(v1, v2);
                }
            }
        }
    }
}
