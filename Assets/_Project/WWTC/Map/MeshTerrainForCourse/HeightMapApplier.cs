using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// Texture2D(HeightMap)으로 그리드 정점들에 높이를 적용
/// </summary>
public class HeightMapApplier : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData; // grid vertex 목록을 SO에 저장할 수도, 또는 임시 저장할 수도.

    [FoldoutGroup("Settings")]
    public Texture2D heightMap;
    public float heightScale = 10f;

    [FoldoutGroup("Actions")]
    [Button("Apply HeightMap", ButtonSizes.Medium)]
    public void ApplyHeightMapToGrid()
    {
        if (pathData == null)
        {
            Debug.LogWarning("[HeightMapApplier] pathDataSO가 없습니다.");
            return;
        }
        if (heightMap == null)
        {
            Debug.LogWarning("[HeightMapApplier] HeightMap 텍스처가 없습니다.");
            return;
        }

        // TODO: gridVertices를 어디에 저장했는지( PathDataSO에? ) 찾아,
        //       각 (x,z)에 대해 heightMap 샘플링 -> y값 할당
        Debug.Log("[HeightMapApplier] HeightMap 적용 (가정) 완료.");
    }
}