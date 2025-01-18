using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 맵 경계를 정의한 뒤, FastNoise 를 사용해
/// 노이즈 값별로 좌표를 분류(Dictionary<int, List<Vector2>>) -> 
/// 각 그룹에 대해 Convex Hull -> cellPolygons 생성.
/// 
/// 주파수(frequency) 를 높였을 때, 기존 key quantization( val*10000 ) 로 인한
/// "서로 다른 값인데도 동일 key로 묶이는" 문제를 완화하기 위해,
/// 예: *10000 -> *40000 또는 *10000000 등으로 조정 가능
///
/// 이제 seed는 MapDataSO.noiseSeed를 사용.
/// </summary>
public class BoundaryChopper : MapDataSystem
{
    [Title("Noise Settings")]
    // [SerializeField] private int seed = 1337;  // ← 제거
    [SerializeField] private float frequency = 0.01f;

    [Title("Cell Settings")]
    [SerializeField] private int gridResolution = 50;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color cellEdgeColor = Color.red;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private Color cellCenterColor = Color.yellow;
    [FoldoutGroup("Gizmo Settings")]
    [SerializeField] private float gizmoSphereSize = 0.3f;

    private FastNoise fastNoise;
    private MapDataSO mapData;

    public override void Generate()
    {
        if (!IsReady) return;
        mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // (1) 기존 cellPolygons 초기화
        mapData.cellPolygons.Clear();

        // (2) fastNoise 설정
        // seed를 직접 SerializeField로 받지 않고,
        // mapData.noiseSeed 를 사용
        fastNoise = new FastNoise(mapData.noiseSeed);
        fastNoise.SetFrequency(frequency);
        fastNoise.SetNoiseType(FastNoise.NoiseType.Cellular);
        fastNoise.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);

        var bd = mapData.boundaryData;
        float minX = bd.centerX - bd.width * 0.5f;
        float maxX = bd.centerX + bd.width * 0.5f;
        float minZ = bd.centerZ - bd.length * 0.5f;
        float maxZ = bd.centerZ + bd.length * 0.5f;

        // 폭/길이 비 → aspect
        float aspect = bd.width / bd.length;

        // (3) 노이즈 값별 (키=정수화 노이즈) -> 좌표 목록
        Dictionary<int, List<Vector2>> cellDict = new Dictionary<int, List<Vector2>>();

        for (int i = 0; i < gridResolution; i++)
        {
            float x = Mathf.Lerp(minX, maxX, i / (float)(gridResolution - 1));
            for (int j = 0; j < gridResolution; j++)
            {
                float z = Mathf.Lerp(minZ, maxZ, j / (float)(gridResolution - 1));

                // 스케일링
                float scaledX = (x - minX) / (maxX - minX);
                float scaledZ = (z - minZ) / (maxZ - minZ);
                scaledX *= aspect;

                // FastNoise
                float val = fastNoise.GetNoise(scaledX, scaledZ);

                // 예시) 기존보다 배율 높이기
                // 기존 *10000 → *40000 or *10000000 등
                int key = Mathf.RoundToInt(val * 10000000f);

                if (!cellDict.ContainsKey(key))
                    cellDict[key] = new List<Vector2>();

                cellDict[key].Add(new Vector2(x, z));
            }
        }

        // (4) cellDict에 대해 Convex Hull -> cellPolygons 생성
        foreach (var kv in cellDict)
        {
            var pts = kv.Value;
            if (pts.Count < 3) continue;

            // Convex Hull
            List<Vector2> hull = BuildConvexHull(pts);
            if (hull.Count < 3) continue;

            // 중심(center)
            Vector2 center = Vector2.zero;
            foreach (var p in hull) 
                center += p;
            center /= hull.Count;

            // Shoelace로 면적 계산
            float area = ComputePolygonArea(hull);

            // CellPolygon 생성
            var cellPolygon = new CellPolygon
            {
                cellKey = kv.Key,
                points  = hull,
                center  = center,
                area    = area
            };
            mapData.cellPolygons.Add(cellPolygon);
        }

        Debug.Log($"[BoundaryChopper] {mapData.cellPolygons.Count}개의 셀 생성 완료 (Aspect={aspect:F3}, freq={frequency:F3}, seed={mapData.noiseSeed})");
    }

    /// <summary>
    /// Convex Hull (Monotone chain)
    /// </summary>
    private List<Vector2> BuildConvexHull(List<Vector2> pts)
    {
        if (pts.Count < 3) 
            return new List<Vector2>(pts);

        // 1) 정렬 (x 먼저, y 다음)
        pts.Sort((p1, p2) =>
        {
            if (Mathf.Approximately(p1.x, p2.x))
                return p1.y.CompareTo(p2.y);
            return p1.x.CompareTo(p2.x);
        });

        List<Vector2> hull = new List<Vector2>();

        // 2) lower hull
        for (int i = 0; i < pts.Count; i++)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], pts[i]) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(pts[i]);
        }

        // 3) upper hull
        for (int i = pts.Count - 2, l = hull.Count + 1; i >= 0; i--)
        {
            while (hull.Count >= l && Cross(hull[hull.Count - 2], hull[hull.Count - 1], pts[i]) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(pts[i]);
        }

        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    private float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        // 2D cross: (b-a) x (c-a)
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    /// <summary>
    /// Shoelace formula로 다각형 면적 (signed area)
    /// </summary>
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

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.cellPolygons == null) return;

        // 폴리곤 테두리
        Gizmos.color = cellEdgeColor;
        foreach (var cell in so.cellPolygons)
        {
            var points = cell.points;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % points.Count];
                Gizmos.DrawLine(new Vector3(p1.x, 0f, p1.y), new Vector3(p2.x, 0f, p2.y));
            }
        }

        // 폴리곤 중심
        Gizmos.color = cellCenterColor;
        foreach (var cell in so.cellPolygons)
        {
            Vector2 c = cell.center;
            Gizmos.DrawSphere(new Vector3(c.x, 0f, c.y), gizmoSphereSize);
        }
    }
}
