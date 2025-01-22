using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class BoundaryChopper : MapDataSystem
{
    [Title("Noise Settings")]
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
        fastNoise = new FastNoise(mapData.noiseSeed);
        fastNoise.SetFrequency(frequency);
        fastNoise.SetNoiseType(FastNoise.NoiseType.Cellular);
        fastNoise.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);

        var bd = mapData.boundaryData;
        float minX = bd.centerX - bd.width * 0.5f;
        float maxX = bd.centerX + bd.width * 0.5f;
        float minZ = bd.centerZ - bd.length * 0.5f;
        float maxZ = bd.centerZ + bd.length * 0.5f;

        float aspect = bd.width / bd.length;

        Dictionary<int, List<Vector2>> cellDict = new Dictionary<int, List<Vector2>>();

        for (int i = 0; i < gridResolution; i++)
        {
            float x = Mathf.Lerp(minX, maxX, i / (float)(gridResolution - 1));
            for (int j = 0; j < gridResolution; j++)
            {
                float z = Mathf.Lerp(minZ, maxZ, j / (float)(gridResolution - 1));

                float scaledX = (x - minX) / (maxX - minX);
                float scaledZ = (z - minZ) / (maxZ - minZ);
                scaledX *= aspect;

                float val = fastNoise.GetNoise(scaledX, scaledZ);
                int key = Mathf.RoundToInt(val * 10000000f);

                if (!cellDict.ContainsKey(key))
                    cellDict[key] = new List<Vector2>();

                cellDict[key].Add(new Vector2(x, z));
            }
        }

        foreach (var kv in cellDict)
        {
            var pts = kv.Value;
            if (pts.Count < 3) continue;

            List<Vector2> hull = BuildConvexHull(pts);
            if (hull.Count < 3) continue;

            Vector2 center = Vector2.zero;
            foreach (var p in hull) 
                center += p;
            center /= hull.Count;

            float area = ComputePolygonArea(hull);

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

    private List<Vector2> BuildConvexHull(List<Vector2> pts)
    {
        if (pts.Count < 3) 
            return new List<Vector2>(pts);

        pts.Sort((p1, p2) =>
        {
            if (Mathf.Approximately(p1.x, p2.x))
                return p1.y.CompareTo(p2.y);
            return p1.x.CompareTo(p2.x);
        });

        List<Vector2> hull = new List<Vector2>();

        for (int i = 0; i < pts.Count; i++)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], pts[i]) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(pts[i]);
        }

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
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

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

        Gizmos.color = cellCenterColor;
        foreach (var cell in so.cellPolygons)
        {
            Vector2 c = cell.center;
            Gizmos.DrawSphere(new Vector3(c.x, 0f, c.y), gizmoSphereSize);
        }
    }
}
