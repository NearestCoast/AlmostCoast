using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// CourseVertices로부터 xMin,xMax,zMin,zMax + offset => boundingRect
/// PathDataSO에 저장
/// </summary>
public class RectCalculator : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData;

    [FoldoutGroup("Settings"), Tooltip("바운더리 오프셋 (X 방향만 적용)")]
    public float offset = 50f;

    [FoldoutGroup("Result"), ReadOnly]
    public Rect computedRect;

    [FoldoutGroup("Actions")]
    [Button("Compute Rect from CourseData", ButtonSizes.Medium)]
    public void ComputeBoundingRect()
    {
        if (pathData == null)
        {
            Debug.LogWarning("[RectCalculator] PathDataSO가 없음");
            return;
        }

        List<Vector3> cVerts = pathData.CourseVertices;
        if (cVerts == null || cVerts.Count < 3)
        {
            Debug.LogWarning("[RectCalculator] courseVertices가 부족");
            return;
        }

        float xMin = float.MaxValue, xMax = float.MinValue;
        float zMin = float.MaxValue, zMax = float.MinValue;

        // 1) xMin, xMax, zMin, zMax 찾기
        foreach(var v in cVerts)
        {
            if (v.x < xMin) xMin = v.x;
            if (v.x > xMax) xMax = v.x;
            if (v.z < zMin) zMin = v.z;
            if (v.z > zMax) zMax = v.z;
        }

        // 2) X방향에만 offset 적용, Z방향에는 적용하지 않음
        xMin -= offset;
        xMax += offset;

        // zMin, zMax는 그대로
        // zMin -= 0;  // 생략
        // zMax += 0;  // 생략

        // 3) Rect 생성
        float w = xMax - xMin;
        float h = zMax - zMin;
        computedRect = new Rect(xMin, zMin, w, h);

        // 4) PathDataSO에 저장
        pathData.SetBoundingRect(computedRect);

        Debug.Log($"[RectCalculator] computedRect= {computedRect}");
    }

    private void OnDrawGizmos()
    {
        if (computedRect.width <= 0 || computedRect.height <= 0) return;

        Vector3 c1 = new Vector3(computedRect.xMin, 0, computedRect.yMin);
        Vector3 c2 = new Vector3(computedRect.xMax, 0, computedRect.yMin);
        Vector3 c3 = new Vector3(computedRect.xMax, 0, computedRect.yMax);
        Vector3 c4 = new Vector3(computedRect.xMin, 0, computedRect.yMax);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(c1, c2);
        Gizmos.DrawLine(c2, c3);
        Gizmos.DrawLine(c3, c4);
        Gizmos.DrawLine(c4, c1);
    }
}
