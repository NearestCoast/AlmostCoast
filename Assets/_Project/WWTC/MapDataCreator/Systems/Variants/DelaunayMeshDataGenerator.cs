using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

public class DelaunayMeshDataGenerator : MapDataSystem
{
    [Title("Voronoi (Cellular) Settings")]
    [SerializeField]
    private int fastNoiseSeed = 1337;

    [SerializeField, Range(0.001f, 0.1f)]
    private float frequency = 0.01f;
    
    [FoldoutGroup("Gizmo Settings"), SerializeField, Tooltip("centerPoints 기즈모 표시 여부")]
    private bool drawCenterPoints = true;

    [FoldoutGroup("Gizmo Settings"), SerializeField, Tooltip("Voronoi 경계(선분) 기즈모 표시 여부")]
    private bool drawVoronoiEdges = true;
    

    public override void Generate()
    {
        if (!IsReady) return;
        var so = mapDataCreator.CurrentMapData;
        if (so == null) return;

        so.generatedMeshDataList.Clear();

        if (so.subdivideCellPolygonGroup == null || so.subdivideCellPolygonGroup.Count == 0)
        {
            Debug.LogWarning("[DelaunayMeshDataGenerator] subdivideCellPolygonGroup is empty.");
            return;
        }

        // FastNoise 설정
        FastNoise fastNoise = new FastNoise(fastNoiseSeed);
        fastNoise = new FastNoise(so.noiseSeed);
        fastNoise.SetFrequency(frequency);
        fastNoise.SetNoiseType(FastNoise.NoiseType.Cellular);
        fastNoise.SetCellularReturnType(FastNoise.CellularReturnType.CellValue);
    }

}
