using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 섬(폴리곤) 경계를 둥글게 처리하기 위해,
/// 각 샘플점과 폴리곤 외곽선 사이 거리(distEdge)를 구해
/// distEdge < fadeRange 구간에서 높이를 페이드(낮춤)시킨다.
///
/// 전제: polygonMeshDataList가 이미 샘플링 + 노이즈 높이까지 들어있는 상태.
/// 순서: PolygonMeshSamplerSystem -> CellularNoiseHeightSystem
///       -> IslandBoundaryFadeSystem -> ...
/// </summary>
public class IslandBoundaryFadeSystem : MapDataSystem
{
    [FoldoutGroup("Fade Settings")]
    [SerializeField] private float fadeRange = 10f;

    // 높이 페이드 방식을 조금 더 제어하기 위해 AnimationCurve 사용
    // (0→경계 근처, 1→fadeRange 이상 떨어짐)
    [FoldoutGroup("Fade Settings"), Tooltip("Fade 커브(ratio 0~1)")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0,0,1,1);

    public override void Generate()
    {
        base.Generate();
        if (!IsReady) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // (samplePoints) + (noiseHeight)가 이미 있어야 함
        var polyMeshList = mapData.polygonMeshDataList;
        if (polyMeshList == null || polyMeshList.Count == 0)
        {
            Debug.LogWarning("[IslandBoundaryFadeSystem] polygonMeshDataList가 비어 있음. " +
                             "먼저 폴리곤 샘플링 + 노이즈 적용을 진행해주세요.");
            return;
        }

        // arrangedCellPolygons 사용
        var polygons = mapData.arrangedCellPolygons;
        if (polygons == null || polygons.Count == 0)
        {
            Debug.LogWarning("[IslandBoundaryFadeSystem] arrangedCellPolygons가 비어 있음. " +
                             "폴리곤 정보가 필요합니다.");
            return;
        }

        // 1) 폴리곤별 외곽 segment 리스트( cellKey -> List<LineSegment2D> ) 구성
        Dictionary<float, List<LineSegment2D>> polygonSegmentsDict = new Dictionary<float, List<LineSegment2D>>();
        foreach (var poly in polygons)
        {
            var segments = new List<LineSegment2D>();
            var pts = poly.points;
            int count = pts.Count;

            for (int i = 0; i < count; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[(i + 1) % count];
                segments.Add(new LineSegment2D 
                { 
                    start = a, 
                    end = b, 
                    startClosed = true, 
                    endClosed = true
                });
            }

            polygonSegmentsDict[poly.cellKey] = segments;
        }

        // 2) 각 폴리곤의 samplePoints에 대해 거리 계산 → 페이드
        foreach (var polyMesh in polyMeshList)
        {
            float cellKey = polyMesh.cellKey;
            if (!polygonSegmentsDict.ContainsKey(cellKey)) 
                continue; // 해당 cellKey 폴리곤이 없으면 스킵

            var segments = polygonSegmentsDict[cellKey];
            if (segments == null || segments.Count == 0) 
                continue;

            var points = polyMesh.samplePoints;
            if (points == null) continue;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];
                float originalHeight = p.y;

                // (x,z)
                Vector2 p2D = new Vector2(p.x, p.z);

                // 경계까지 최소 거리
                float distEdge = GetDistanceToBoundary(p2D, segments);

                // ratio = clamp01(distEdge / fadeRange)
                float ratio = Mathf.Clamp01(distEdge / fadeRange);

                // AnimationCurve로 최종 페이드 비율 계산
                float fadeFactor = fadeCurve.Evaluate(ratio);

                // 최종 높이
                p.y = originalHeight * fadeFactor;
                points[i] = p;
            }
        }

        Debug.Log("[IslandBoundaryFadeSystem] 섬 경계 페이드 처리 완료!");
    }

    /// <summary>
    /// 한 점과 폴리곤 선분들 사이의 최소 거리
    /// </summary>
    private float GetDistanceToBoundary(Vector2 point, List<LineSegment2D> segments)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < segments.Count; i++)
        {
            float dist = DistancePointToSegment(point, segments[i].start, segments[i].end);
            if (dist < minDist) minDist = dist;
        }
        return minDist;
    }

    /// <summary>
    /// 2D 점 p가 선분(a->b)와 갖는 최소 거리
    /// </summary>
    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;

        float abLengthSquared = ab.sqrMagnitude;
        if (abLengthSquared < 1e-8f)
        {
            // a,b가 사실상 같은 점이면
            return Vector2.Distance(p, a);
        }

        float t = Vector2.Dot(ap, ab) / abLengthSquared;
        t = Mathf.Clamp01(t);

        Vector2 closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }
}
