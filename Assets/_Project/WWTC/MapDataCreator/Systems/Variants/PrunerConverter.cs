using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

public class PrunerConverter : MapDataSystem
{
    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator?.CurrentMapData;
        if (so == null)
        {
            Debug.LogWarning("[PrunerConverter] MapDataSO가 없습니다.");
            return;
        }

        so.groupedPolygonsByKey.Clear();

        var iterationList = so.prunerIterations
            .OrderBy(iter => iter.iterationIndex)
            .ToList();

        var polygonsByKey = new Dictionary<float, List<(int iterationIndex, CellPolygon poly)>>();

        foreach (var iterData in iterationList)
        {
            int iterIdx = iterData.iterationIndex;
            if (iterData.polygons == null) continue;

            foreach (var poly in iterData.polygons)
            {
                float key = poly.cellKey;
                if (!polygonsByKey.ContainsKey(key))
                {
                    polygonsByKey[key] = new List<(int, CellPolygon)>();
                }
                polygonsByKey[key].Add((iterIdx, poly));
            }
        }

        foreach (var kvp in polygonsByKey)
        {
            float cellKey = kvp.Key;
            var tupleList = kvp.Value;

            tupleList.Sort((a, b) => a.iterationIndex.CompareTo(b.iterationIndex));

            List<CellPolygon> sortedPolys = tupleList
                .Select(t => t.poly)
                .ToList();

            var group = new CellPolygonGroup
            {
                cellKey  = cellKey,
                polygons = sortedPolys
            };
            so.groupedPolygonsByKey.Add(group);
        }

        Debug.Log($"[PrunerConverter] prunerIterations -> cellKey 그룹화 완료. " +
                  $"그룹 수={so.groupedPolygonsByKey.Count}");
    }

    private static readonly Color[] RAINBOW_COLORS = new Color[]
    {
        new Color(1f, 0f, 0f),
        new Color(1f, 0.5f, 0f),
        new Color(1f, 1f, 0f),
        new Color(0f, 1f, 0f),
        new Color(0f, 0f, 1f),
        new Color(0.29f, 0f, 0.51f),
        new Color(0.93f, 0.51f, 0.93f)
    };

    [FoldoutGroup("Gizmo Settings")] 
    [SerializeField] private bool drawKeyedPolygons = true;

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;  
        if (!drawKeyedPolygons) return;
        if (mapDataCreator == null) return;

        var so = mapDataCreator.CurrentMapData;
        if (so == null || so.groupedPolygonsByKey == null) return;

        int polygonIndex = 0;

        foreach (var grp in so.groupedPolygonsByKey)
        {
            if (grp.polygons == null) continue;

            foreach (var poly in grp.polygons)
            {
                var c = RAINBOW_COLORS[polygonIndex % RAINBOW_COLORS.Length];
                Gizmos.color = c;
                polygonIndex++;

                var pts = poly.points;
                if (pts == null || pts.Count < 2) continue;

                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 a = pts[i];
                    Vector2 b = pts[(i + 1) % pts.Count];

                    Gizmos.DrawLine(
                        new Vector3(a.x, 0, a.y),
                        new Vector3(b.x, 0, b.y)
                    );
                }
            }
        }
    }
}
