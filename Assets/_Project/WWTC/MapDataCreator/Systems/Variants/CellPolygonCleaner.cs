using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// MapDataSystem을 상속. 
/// MapDataSO.cellPolygons 에 대해, 연속된 선분 사이의 각이 일정 threshold 이하일 때
/// 그 꼭지점을 삭제(합침)하여 폴리곤을 단순화하는 기능.
/// </summary>
public class CellPolygonCleaner : MapDataSystem
{
    [Title("Cleaner Settings")]
    [SerializeField, Min(0f)]
    private float angleThreshold = 10f; 
    // (단위: 도(degree). 예: 10도 이하라면 "직선에 가깝다"고 보고 합치기)

    public override void Generate()
    {
        if (!IsReady) return;
        var so = mapDataCreator.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[CellPolygonCleaner] MapDataSO가 없습니다.");
            return;
        }

        // cellPolygons 에 대해 반복
        int polygonCount = so.cellPolygons.Count;
        int totalRemovedPoints = 0;

        // 각 폴리곤마다 점을 "합치기(삭제)" 진행
        for (int i = 0; i < polygonCount; i++)
        {
            var poly = so.cellPolygons[i];
            if (poly.points == null || poly.points.Count < 3)
                continue;

            // 반복적으로 점 합치기
            totalRemovedPoints += SimplifyPolygon(poly.points, angleThreshold);

            // 혹시 center, area 등을 다시 구해야 한다면 여기서 재계산 가능
            // poly.center = ...
            // poly.area   = ...
        }

        Debug.Log($"[CellPolygonCleaner] Done. Removed {totalRemovedPoints} vertices in total.");
    }

    /// <summary>
    /// 주어진 points(Convex/Concave 관계없이) 에 대해,
    /// 인접 에지 사이의 각이 angleThreshold 이하인 꼭지점을 반복적으로 합치기(삭제).
    /// 
    /// 반환값: 제거된 꼭지점(점)의 총 개수
    /// </summary>
    private int SimplifyPolygon(List<Vector2> points, float angleThresholdDeg)
    {
        if (points.Count < 3) return 0;

        int removeCount = 0;
        bool changed = true;

        // 라디안으로 변환 (Deg->Rad)
        float angleThresholdRad = angleThresholdDeg * Mathf.Deg2Rad;

        while (changed)
        {
            changed = false;

            // 한 번의 루프에서 "점 하나"를 제거하면 바로 break 후 다시 처음부터 검사
            // (여러 점을 한 번에 제거하면, 인덱스가 꼬이거나 예기치 못한 누락이 생길 수 있음)
            for (int i = 0; i < points.Count; i++)
            {
                int prev = (i - 1 + points.Count) % points.Count;  // 이전 점 인덱스
                int next = (i + 1) % points.Count;                  // 다음 점 인덱스

                Vector2 pA = points[prev];
                Vector2 pB = points[i];
                Vector2 pC = points[next];

                // 에지 벡터
                Vector2 v1 = (pB - pA).normalized; 
                Vector2 v2 = (pC - pB).normalized;

                // 두 벡터 사이의 angle 계산
                // angle = acos( v1·v2 )
                float dot = Vector2.Dot(v1, v2);
                // dot 이 범위를 넘어가는 경우(수치오차) 클램핑
                if (dot > 1f) dot = 1f;
                if (dot < -1f) dot = -1f;
                float angle = Mathf.Acos(dot); // 라디안

                // 만약 angle 이 angleThresholdRad 이하라면 => 점 i 제거
                if (angle <= angleThresholdRad)
                {
                    points.RemoveAt(i);
                    removeCount++;
                    changed = true;
                    break; // 이 루프 중단, 다시 처음부터 검사
                }
            }
        }

        return removeCount;
    }
}
