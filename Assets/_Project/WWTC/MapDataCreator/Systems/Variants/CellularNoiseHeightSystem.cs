using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// FastNoise를 이용하여
/// Cellular (Voronoi) 스타일의 노이즈를 
/// MapDataSO에 저장된 samplePoints에 적용하는 System.
///
/// 전제: polygonMeshDataList가 이미 생성되어 있어야 함
///       (PolygonMeshSamplerSystem 등으로 폴리곤 내부 샘플링 완료).
/// 순서: PolygonMeshSamplerSystem -> CellularNoiseHeightSystem -> ...
/// </summary>
public class CellularNoiseHeightSystem : MapDataSystem
{
    [FoldoutGroup("Noise Settings")]
    [SerializeField] private int seed = 1337;

    [FoldoutGroup("Noise Settings")]
    [SerializeField] private float frequency = 0.01f;

    [FoldoutGroup("Noise Settings")]
    [SerializeField] private float cellularJitter = 0.45f;

    [FoldoutGroup("Noise Settings")]
    [SerializeField]
    private FastNoise.CellularDistanceFunction distanceFunc = FastNoise.CellularDistanceFunction.Euclidean;

    [FoldoutGroup("Noise Settings")]
    [SerializeField]
    private FastNoise.CellularReturnType returnType = FastNoise.CellularReturnType.Distance;

    /// <summary>
    /// 노이즈 결과값에 곱할 배율
    /// 예: 5 -> 최대 높이 5m 등
    /// </summary>
    [FoldoutGroup("Noise Settings")]
    [SerializeField]
    private float amplitude = 5f;

    public override void Generate()
    {
        base.Generate();
        if (!IsReady) return; 
        // IsReady: mapDataCreator != null && mapDataCreator.CurrentMapData != null

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // 1) FastNoise 설정
        FastNoise noise = new FastNoise(seed);
        noise.SetNoiseType(FastNoise.NoiseType.Cellular);
        noise.SetCellularDistanceFunction(distanceFunc);
        noise.SetCellularReturnType(returnType);
        noise.SetCellularJitter(cellularJitter);
        noise.SetFrequency(frequency);

        // 2) polygonMeshDataList 순회
        var polyMeshList = mapData.polygonMeshDataList;
        if (polyMeshList == null || polyMeshList.Count == 0)
        {
            Debug.LogWarning("[CellularNoiseHeightSystem] polygonMeshDataList가 비어있습니다. " +
                             "먼저 폴리곤 내부 샘플링을 진행해주세요.");
            return;
        }

        foreach (var polyData in polyMeshList)
        {
            // samplePoints: List<Vector3>
            if (polyData.samplePoints == null) continue;

            for (int i = 0; i < polyData.samplePoints.Length; i++)
            {
                Vector3 pt = polyData.samplePoints[i];
                // (pt.x, pt.z)를 노이즈 인풋으로 사용
                float rawVal = noise.GetCellular(pt.x, pt.z);

                // 노이즈 값 * amplitude
                float height = rawVal * amplitude;

                // y값 갱신
                pt.y = height;
                polyData.samplePoints[i] = pt;
            }
        }

        Debug.Log("[CellularNoiseHeightSystem] Cellular Voronoi 높이 계산 완료! " +
                  $"(polygonMeshDataList 수: {polyMeshList.Count})");
    }
}
