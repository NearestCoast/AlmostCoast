using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public class PathCornerConverter : MonoBehaviour
{
    [Header("PathDataSO (ScriptableObject)")]
    public PathDataSO pathDataSO;

    [FoldoutGroup("Corner Settings"), Tooltip("이 각도 이하인 코너는 제거(혹은 중간 노드로 치환)")]
    public float cornerAngleThreshold = 45f;

    [FoldoutGroup("Corner Settings"), Tooltip("생성될 중간 노드가 (i-1->i+1) 구간에서 어느 지점에 위치할지 랜덤 여부")]
    public bool useRandomFraction = true;

    [FoldoutGroup("Corner Settings"), Tooltip("기본 분할 위치 (0.3 ~ 0.7 등)")]
    [Range(0f,1f)]
    public float defaultFraction = 0.5f;

    [FoldoutGroup("Gizmo Settings"), Tooltip("변환된 노드를 항상 Gizmo로 표시할 색상")]
    public Color convertedColor = Color.cyan;

    [FoldoutGroup("Corner Settings")]
    [Button("Convert Corners", ButtonSizes.Large)]
    public void ConvertCorners()
    {
        if (pathDataSO == null)
        {
            Debug.LogWarning("[PathCornerConverter] pathDataSO가 비어있습니다!");
            return;
        }

        List<Vector3> original = pathDataSO.PathNodes;
        if (original == null || original.Count < 3)
        {
            Debug.LogWarning("[PathCornerConverter] PathNodes가 충분하지 않습니다. (3개 미만)");
            return;
        }

        // 새 리스트(코너 변환 후)
        List<Vector3> converted = new List<Vector3>();

        int count = original.Count;

        // 간단히 "loop = false" 가정, 필요 시 루프 처리는 모듈로 연산 등 추가
        for (int i = 0; i < count; i++)
        {
            if (i == 0 || i == count - 1)
            {
                // 맨 앞/뒤 노드는 그냥 추가
                converted.Add(original[i]);
                continue;
            }

            // i-1 / i / i+1
            int iPrev = i - 1;
            int iNext = i + 1;

            Vector3 dirA = (original[i] - original[iPrev]).normalized;
            Vector3 dirB = (original[iNext] - original[i]).normalized;

            float angle = Vector3.Angle(dirA, dirB);

            if (angle < cornerAngleThreshold)
            {
                // "너무 좁은 각" → i 노드 제거 + i-1 ~ i+1 사이에 새 노드 하나 
                float frac = (useRandomFraction) ? Random.Range(0.2f, 0.8f) : defaultFraction;

                // i-1 ~ i+1 구간 중간(lerp)
                Vector3 midPos = Vector3.Lerp(original[iPrev], original[iNext], frac);

                // 기존 i 노드는 skip, 대신 midPos
                converted.Add(midPos);

                Debug.Log($"[PathCornerConverter] Node#{i} angle={angle:F1}° -> replaced by 1 midNode.");
            }
            else
            {
                // 코너가 넓으면 그대로
                converted.Add(original[i]);
            }
        }

        // 결과 저장
        pathDataSO.SetConvertedPathNodes(converted);

        Debug.Log($"[PathCornerConverter] ConvertCorners 완료. origin={original.Count} -> converted={converted.Count}");
    }

    /// <summary>
    /// 항상 Gizmo로 ConvertedPathNodes를 표시
    /// </summary>
    private void OnDrawGizmos()
    {
        if (pathDataSO == null) 
            return;

        List<Vector3> converted = pathDataSO.ConvertedPathNodes;
        if (converted == null || converted.Count < 2)
            return;

        Gizmos.color = convertedColor;

        // 라인
        for (int i = 0; i < converted.Count - 1; i++)
        {
            Gizmos.DrawLine(converted[i], converted[i+1]);
        }

        // 각 노드 위치 표시
        foreach (var pos in converted)
        {
            Gizmos.DrawSphere(pos, 0.06f);
        }
    }
}
