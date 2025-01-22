using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CellPolygonCleaner : MapDataSystem
{
    [Title("Cleaner Settings")]
    [SerializeField, Min(0f)]
    private float angleThreshold = 10f;

    // 일정 길이 이하의 선분을 삭제하기 위한 길이 임계값
    [SerializeField, Min(0f)]
    private float minSegmentLength = 0.01f;

    public override void Generate()
    {
        if (!IsReady) return;
        var so = mapDataCreator.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[CellPolygonCleaner] MapDataSO가 없습니다.");
            return;
        }

        int polygonCount = so.cellPolygons.Count;
        int totalRemovedPoints = 0;

        for (int i = 0; i < polygonCount; i++)
        {
            var poly = so.cellPolygons[i];
            if (poly.points == null || poly.points.Count < 3)
                continue;

            totalRemovedPoints += SimplifyPolygon(poly.points, angleThreshold, minSegmentLength);
        }

        Debug.Log($"[CellPolygonCleaner] Done. Removed {totalRemovedPoints} vertices in total.");
    }

    /// <summary>
    /// 폴리곤의 꼭짓점 목록에 대해
    /// 1) 일정 길이 이하의 선분 제거 (양 옆 선분을 연장하여 교차점으로 연결)
    /// 2) 일정 각도 이하(=angleThresholdDeg)의 꼭짓점 제거
    /// 를 반복 수행하여 단순화한다.
    /// </summary>
    private int SimplifyPolygon(List<Vector2> points, float angleThresholdDeg, float lengthThreshold)
    {
        if (points.Count < 3) return 0;

        int removeCount = 0;
        bool changed = true;
        float angleThresholdRad = angleThresholdDeg * Mathf.Deg2Rad;

        while (changed)
        {
            changed = false;

            // 폴리곤 유효성 체크 (3개 미만이면 종료)
            if (points.Count < 3) break;

            for (int i = 0; i < points.Count; i++)
            {
                // (i+1) % points.Count
                int next = (i + 1) % points.Count;

                // -------------------------------
                // (1) 일정 길이 이하의 선분 제거
                // -------------------------------
                float segmentLength = Vector2.Distance(points[i], points[next]);
                if (segmentLength < lengthThreshold && points.Count > 3)
                {
                    int prev = (i - 1 + points.Count) % points.Count;
                    int nextNext = (next + 1) % points.Count;

                    Vector2 pPrev = points[prev];
                    Vector2 pI = points[i];
                    Vector2 pNext = points[next];
                    Vector2 pNextNext = points[nextNext];

                    // pPrev -> pI, pNext -> pNextNext 두 직선의 연장선 교차점
                    Vector2 intersection;
                    if (GetLineIntersection(pPrev, pI, pNext, pNextNext, out intersection))
                    {
                        // ------------------------------
                        // 안전하게 Remove -> Insert
                        // ------------------------------
                        // i, next 중 더 큰 인덱스를 먼저 제거해야 인덱스가 틀어지지 않음
                        int idxA = i;
                        int idxB = next;
                        if (idxB < idxA)
                        {
                            // swap
                            int temp = idxA;
                            idxA = idxB;
                            idxB = temp;
                        }

                        // idxB가 항상 더 크거나 같음
                        // 먼저 idxB 제거, 그 다음 idxA 제거
                        points.RemoveAt(idxB); // next
                        points.RemoveAt(idxA); // i

                        // 이제 intersection을 idxA 위치에 삽입
                        // (두 점을 제거했으므로 지금 idxA 위치가 “새로운 꼭짓점 위치”가 됨)
                        points.Insert(idxA, intersection);

                        removeCount += 2;  
                        changed = true;
                        break;
                    }
                    else
                    {
                        // 교차점이 없는(평행) 케이스:
                        // -> 여기서는 별도 처리를 하지 않고,
                        //    아래 "각도 검사" 로직을 태우도록 둠.
                    }
                }

                // ----------------------------
                // (2) 일정 각도 이하의 꼭짓점 제거
                // ----------------------------
                {
                    int prev = (i - 1 + points.Count) % points.Count;
                    int nextNext = (i + 1) % points.Count;

                    Vector2 pA = points[prev];
                    Vector2 pB = points[i];
                    Vector2 pC = points[nextNext];

                    Vector2 v1 = (pB - pA).normalized; 
                    Vector2 v2 = (pC - pB).normalized;
                    float dot = Vector2.Dot(v1, v2);
                    dot = Mathf.Clamp(dot, -1f, 1f);
                    float angle = Mathf.Acos(dot);

                    if (angle <= angleThresholdRad)
                    {
                        points.RemoveAt(i);
                        removeCount++;
                        changed = true;
                        break;
                    }
                }
            }
        }

        return removeCount;
    }

    /// <summary>
    /// 두 직선 p1->p2, p3->p4 (무한 연장선)이 교차하는지 판단하여,
    /// 교차점이 존재하면 intersection에 담고 true 반환.
    /// 평행 또는 거의 평행이면 false 반환.
    /// </summary>
    private bool GetLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        // 선형대수로 직선의 교차점을 구하는 공식
        float denom = (p1.x - p2.x) * (p3.y - p4.y) - (p1.y - p2.y) * (p3.x - p4.x);
        if (Mathf.Abs(denom) < 1e-6f)
        {
            return false;
        }

        float xNum = (p1.x * p2.y - p1.y * p2.x) * (p3.x - p4.x)
                   - (p1.x - p2.x) * (p3.x * p4.y - p3.y * p4.x);

        float yNum = (p1.x * p2.y - p1.y * p2.x) * (p3.y - p4.y)
                   - (p1.y - p2.y) * (p3.x * p4.y - p3.y * p4.x);

        float x = xNum / denom;
        float y = yNum / denom;
        intersection = new Vector2(x, y);
        return true;
    }
}
