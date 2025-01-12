using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 코스(PathDataSO)와 그리드(지형 Vertex) 간 Seam(경계) 처리, 
/// 높이 보정
/// </summary>
public class CourseSeamAdjuster : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData; // 코스(DownhillPoints, DownhillNodes 등)

    [FoldoutGroup("Settings"), Tooltip("코스 영향 계수")]
    public float courseInfluence = 5f;

    [FoldoutGroup("Actions")]
    [Button("Adjust Seam by Course", ButtonSizes.Medium)]
    public void AdjustSeamByCourse()
    {
        if (pathData == null)
        {
            Debug.LogWarning("[CourseSeamAdjuster] pathDataSO가 없습니다.");
            return;
        }

        // TODO: gridVertices vs coursePoints -> 가까운 점 Seam, 높이 보정
        Debug.Log("[CourseSeamAdjuster] 코스 Seam 처리 (가정) 완료.");
    }
}