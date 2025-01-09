using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 최소 구현의 NURBS Curve (Uniform Clamped, 모든 weight=1 가정)
/// 실제로는 더 복잡한 NURBS 설정이 가능.
/// </summary>
[System.Serializable]
public class NURBSCurve
{
    public int degree;
    public List<Vector3> controlPoints;
    public List<float> weights;
    public List<float> knots;

    /// <summary>
    /// t in [0..1] → 곡선상의 점 Evaluate
    /// </summary>
    public Vector3 Evaluate(float t)
    {
        if (controlPoints == null || controlPoints.Count == 0)
            return Vector3.zero;

        // Knot 범위
        float minK = knots[degree];
        float maxK = knots[knots.Count - degree - 1];
        float param = Mathf.Lerp(minK, maxK, t);

        Vector3 numerator = Vector3.zero;
        float denominator = 0f;

        for(int i=0; i< controlPoints.Count; i++)
        {
            float N = CoxDeBoor(i, degree, param, knots);
            float w = weights[i];
            numerator += w * N * controlPoints[i];
            denominator += w * N;
        }
        if(Mathf.Abs(denominator) < 1e-8f)
            return controlPoints[0];
        return numerator / denominator;
    }

    /// <summary>
    /// Cox–de Boor 재귀 공식 (N(i,p)(x))
    /// </summary>
    private float CoxDeBoor(int i, int p, float x, List<float> knot)
    {
        // p=0 base case
        if (p == 0)
        {
            if (x >= knot[i] && x < knot[i+1]) return 1f;
            return 0f;
        }

        float left = 0f, right = 0f;

        // Left part
        float denomLeft = (knot[i+p] - knot[i]);
        if(denomLeft != 0f)
        {
            left = ((x - knot[i]) / denomLeft) * CoxDeBoor(i, p-1, x, knot);
        }

        // Right part
        float denomRight = (knot[i+p+1] - knot[i+1]);
        if(denomRight != 0f)
        {
            right = ((knot[i+p+1] - x) / denomRight) * CoxDeBoor(i+1, p-1, x, knot);
        }

        return left + right;
    }
}