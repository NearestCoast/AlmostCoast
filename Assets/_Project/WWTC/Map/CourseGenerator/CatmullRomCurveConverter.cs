using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CatmullRomCurveConverter : MonoBehaviour
{
    [Header("PathDataSO (ScriptableObject)")]
    public PathDataSO pathDataSO;  // ← PathDataSO 참조

    [FoldoutGroup("Catmull-Rom Settings"), Range(2, 200)]
    public int sampleRate = 20;

    [FoldoutGroup("Catmull-Rom Settings")]
    public bool isLoop = false;

    [FoldoutGroup("Catmull-Rom Settings"), Range(0f, 1f)]
    public float tension = 0f;

    [FoldoutGroup("Catmull-Rom Settings"), ColorPalette]
    public Color curveColor = Color.yellow;

    [FoldoutGroup("Catmull-Rom Settings")]
    [Button("Generate Curve", ButtonSizes.Large)]
    public void GenerateCurve()
    {
        if (pathDataSO == null)
        {
            Debug.LogWarning("[CatmullRomCurveConverter] PathDataSO가 할당되지 않았습니다!");
            return;
        }

        // 1) ScriptableObject에서 "ConvertedPathNodes" 가져오기
        List<Vector3> corneredNodes = pathDataSO.ConvertedPathNodes;
        if (corneredNodes == null || corneredNodes.Count < 2)
        {
            Debug.LogWarning("[CatmullRomCurveConverter] ConvertedPathNodes가 충분하지 않습니다. (2개 미만)");
            return;
        }

        // 2) Catmull-Rom 곡선 샘플링
        List<Vector3> newCurve = new List<Vector3>();
        int nodeCount = corneredNodes.Count;

        for (int i = 0; i < nodeCount; i++)
        {
            // 루프가 아니면 마지막 구간은 계산 X
            if (!isLoop && i == nodeCount - 1)
                break;

            Vector3 p1 = corneredNodes[i];
            Vector3 p2 = corneredNodes[(i + 1) % nodeCount];

            Vector3 p0 = corneredNodes[(i - 1 + nodeCount) % nodeCount];
            Vector3 p3 = corneredNodes[(i + 2) % nodeCount];

            if (!isLoop)
            {
                if (i == 0) p0 = p1;
                if (i + 2 >= nodeCount) p3 = p2;
            }

            for (int j = 0; j < sampleRate; j++)
            {
                float t = j / (float)sampleRate;
                Vector3 position = GetCardinalPosition(t, p0, p1, p2, p3, tension);
                newCurve.Add(position);
            }
        }

        // 3) ScriptableObject에 curvePoints 저장
        pathDataSO.SetCurvePoints(newCurve);
        Debug.Log($"[CatmullRomCurveConverter] Curve Generated with {newCurve.Count} points (from corneredNodes={corneredNodes.Count}). Tension={tension:F2}");
    }

    private Vector3 GetCardinalPosition(float t, Vector3 p0, Vector3 p1,
                                        Vector3 p2, Vector3 p3, float tension)
    {
        float s = (1f - tension) / 2f;

        Vector3 m1 = s * (p2 - p0);
        Vector3 m2 = s * (p3 - p1);

        float t2 = t * t;
        float t3 = t2 * t;

        float h1 =  2f*t3 - 3f*t2 + 1f;
        float h2 =  t3 - 2f*t2 + t;
        float h3 = -2f*t3 + 3f*t2;
        float h4 =  t3 - t2;

        return h1*p1 + h2*m1 + h3*p2 + h4*m2;
    }

    private void OnDrawGizmos()
    {
        if (pathDataSO == null) return;

        List<Vector3> curvePoints = pathDataSO.CurvePoints;
        if (curvePoints == null || curvePoints.Count < 2)
            return;

        Gizmos.color = curveColor;
        for (int i = 0; i < curvePoints.Count - 1; i++)
        {
            Gizmos.DrawLine(curvePoints[i], curvePoints[i + 1]);
        }

        if (isLoop && curvePoints.Count > 2)
        {
            Gizmos.DrawLine(curvePoints[curvePoints.Count - 1], curvePoints[0]);
        }
    }
}
