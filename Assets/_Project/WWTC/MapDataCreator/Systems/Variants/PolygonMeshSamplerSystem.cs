using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PolygonMeshSamplerSystem : MapDataSystem
{
    [FoldoutGroup("Sampling Settings")]
    [SerializeField, Min(0.01f)]
    private float sampleGridSize = 1f;   // 격자 간격

    [FoldoutGroup("Sampling Settings")]
    [SerializeField]
    private bool useBoundaryOffset = true;

    // "격자 중심" 샘플링 모드
    [FoldoutGroup("Sampling Settings")]
    [SerializeField, Tooltip("각 (row,col)의 중심점을 사용해 폴리곤 내부를 검사")]
    private bool useCenterSampling = true;

    [FoldoutGroup("Gizmo Settings")]
    [SerializeField]
    private float gizmoSamplePointRadius = 0.1f;

    public override void Generate()
    {
        base.Generate();
        if (!IsReady) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // 이전 샘플 결과 초기화
        mapData.polygonMeshDataList.Clear();

        // boundary offset (x,z)
        Vector2 bdOffset = Vector2.zero;
        if (useBoundaryOffset)
        {
            var bd = mapData.boundaryData;
            bdOffset = new Vector2(bd.offset.x, bd.offset.z);
        }

        foreach (var poly in mapData.arrangedCellPolygons)
        {
            float minX, maxX, minY, maxY;
            GetPolygonBounds(poly.points, bdOffset, out minX, out maxX, out minY, out maxY);

            float width = maxX - minX;
            float height = maxY - minY;
            if (width <= 0f || height <= 0f)
            {
                Debug.LogWarning($"[PolygonMeshSamplerSystem] cellKey={poly.cellKey}, bounding box가 이상.");
                continue;
            }

            // 행, 열 개수
            int colCount = Mathf.FloorToInt(width / sampleGridSize);
            int rowCount = Mathf.FloorToInt(height / sampleGridSize);

            PolygonMeshData polyMeshData = new PolygonMeshData
            {
                cellKey = poly.cellKey,
                rowCount = rowCount,
                colCount = colCount,
                samplePoints = new Vector3[(rowCount + 1) * (colCount + 1)]
            };

            // 실제 샘플링
            for (int row = 0; row <= rowCount; row++)
            {
                for (int col = 0; col <= colCount; col++)
                {
                    // 기본 좌표
                    float x = minX + col * sampleGridSize;
                    float y = minY + row * sampleGridSize;

                    // "격자 중심" 모드면, + halfGrid
                    if (useCenterSampling)
                    {
                        x += sampleGridSize * 0.5f;
                        y += sampleGridSize * 0.5f;
                    }

                    Vector2 pt = new Vector2(x, y);

                    bool inside = IsPointInPolygon(pt, poly.points, bdOffset);

                    int idx = row * (colCount + 1) + col;
                    if (inside)
                    {
                        polyMeshData.samplePoints[idx] = new Vector3(x, 0f, y);
                    }
                    else
                    {
                        polyMeshData.samplePoints[idx] = new Vector3(
                            float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity
                        );
                    }
                }
            }

            mapData.polygonMeshDataList.Add(polyMeshData);
        }

        Debug.Log($"[PolygonMeshSamplerSystem] 샘플링 완료. " +
                  $"arrangedCellPolygons={mapData.arrangedCellPolygons.Count}, " +
                  $"polygonMeshDataList={mapData.polygonMeshDataList.Count}");
    }

    private void GetPolygonBounds(List<Vector2> points, Vector2 offset,
                                  out float minX, out float maxX,
                                  out float minY, out float maxY)
    {
        float px0 = points[0].x + offset.x;
        float py0 = points[0].y + offset.y;
        minX = maxX = px0;
        minY = maxY = py0;

        for (int i = 1; i < points.Count; i++)
        {
            float px = points[i].x + offset.x;
            float py = points[i].y + offset.y;

            if (px < minX) minX = px;
            if (px > maxX) maxX = px;
            if (py < minY) minY = py;
            if (py > maxY) maxY = py;
        }
    }

    private bool IsPointInPolygon(Vector2 pt, List<Vector2> polygon, Vector2 offset)
    {
        int crossingCount = 0;
        int count = polygon.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = polygon[i] + offset;
            Vector2 b = polygon[(i + 1) % count] + offset;

            bool yBetween = (a.y > pt.y) != (b.y > pt.y);
            if (yBetween)
            {
                float xCross = (b.x - a.x) * (pt.y - a.y) / (b.y - a.y) + a.x;
                if (xCross > pt.x)
                {
                    crossingCount++;
                }
            }
        }
        return (crossingCount % 2 == 1);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        if (mapDataCreator == null || mapDataCreator.CurrentMapData == null) return;

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData.polygonMeshDataList == null) return;

        Gizmos.color = Color.cyan;

        foreach (var polyData in mapData.polygonMeshDataList)
        {
            var points = polyData.samplePoints;
            if (points == null) continue;

            for (int i = 0; i < points.Length; i++)
            {
                var pt = points[i];
                if (!float.IsInfinity(pt.x))
                {
                    Gizmos.DrawSphere(pt, gizmoSamplePointRadius);
                }
            }
        }
    }
}
