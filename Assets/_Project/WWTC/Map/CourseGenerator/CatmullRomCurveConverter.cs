using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector; // Odin Inspector 네임스페이스

[ExecuteInEditMode] // 에디터 모드에서 곡선 확인 가능
public class CatmullRomCurveConverter : MonoBehaviour
{
    [Header("PathNodeGenerator 참조")]
    public PathNodeGenerator generator;

    [FoldoutGroup("Catmull-Rom Settings"), Range(2, 200)]
    public int sampleRate = 20;

    [FoldoutGroup("Catmull-Rom Settings")]
    public bool isLoop = false;

    // 곡률(=Tension) 조절 옵션
    // 0이면 Catmull-Rom과 유사, 1이면 거의 직선에 가까워짐
    [FoldoutGroup("Catmull-Rom Settings"), Range(0f, 1f)]
    public float tension = 0f;

    [FoldoutGroup("Catmull-Rom Settings"), ColorPalette]
    public Color curveColor = Color.yellow;

    // 생성된 곡선 포인트들
    [SerializeField] private List<Vector3> curvePoints = new List<Vector3>();
    public List<Vector3> CurvePoints => curvePoints;

    [FoldoutGroup("Catmull-Rom Settings")]
    [Button("Generate Curve", ButtonSizes.Large)]
    public void GenerateCurve()
    {
        curvePoints.Clear();

        if (generator == null)
        {
            Debug.LogWarning("[CatmullRomCurveConverter] generator( PathNodeGenerator )가 할당되지 않았습니다!");
            return;
        }

        List<Vector3> pathNodes = generator.PathNodes;
        if (pathNodes == null || pathNodes.Count < 2)
        {
            Debug.LogWarning("[CatmullRomCurveConverter] pathNodes가 충분하지 않습니다. (2개 미만)");
            return;
        }

        int nodeCount = pathNodes.Count;
        for (int i = 0; i < nodeCount; i++)
        {
            // 루프가 아니면 마지막 구간은 계산 X
            if (!isLoop && i == nodeCount - 1)
                break;

            Vector3 p1 = pathNodes[i];
            Vector3 p2 = pathNodes[(i + 1) % nodeCount];

            Vector3 p0 = pathNodes[(i - 1 + nodeCount) % nodeCount];
            Vector3 p3 = pathNodes[(i + 2) % nodeCount];

            // 루프가 아닐 때 양 끝단은 p0, p3를 클램핑
            if (!isLoop)
            {
                if (i == 0) p0 = p1;
                if (i + 2 >= nodeCount) p3 = p2;
            }

            // 각 구간을 sampleRate만큼 샘플링
            for (int j = 0; j < sampleRate; j++)
            {
                float t = j / (float)sampleRate;
                // Cardinal Spline(= Hermite) 공식
                Vector3 position = GetCardinalPosition(t, p0, p1, p2, p3, tension);
                curvePoints.Add(position);
            }
        }

        Debug.Log($"[CatmullRomCurveConverter] Curve Generated ({curvePoints.Count} points) with Tension={tension:0.00}");
    }

    /// <summary>
    /// Cardinal Spline(또는 Hermite) 공식으로
    /// p0, p1, p2, p3 사이의 t(0~1) 지점 좌표를 반환.
    /// tension으로 곡률 조절 가능.
    /// </summary>
    private Vector3 GetCardinalPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tension)
    {
        // (1 - T) / 2
        float s = (1f - tension) / 2f;

        // 두 탄젠트(기울기) 벡터
        Vector3 m1 = s * (p2 - p0);
        Vector3 m2 = s * (p3 - p1);

        float t2 = t * t;
        float t3 = t2 * t;

        // Hermite basis 함수
        float h1 =  2f * t3 - 3f * t2 + 1f; //  2t^3 - 3t^2 + 1
        float h2 =  t3 - 2f * t2 + t;       //  t^3 - 2t^2 + t
        float h3 = -2f * t3 + 3f * t2;      // -2t^3 + 3t^2
        float h4 =  t3 - t2;               //  t^3 - t^2

        // 최종 위치
        return (h1 * p1) + (h2 * m1) + (h3 * p2) + (h4 * m2);
    }

    private void OnDrawGizmos()
    {
        if (curvePoints == null || curvePoints.Count < 2)
            return;

        Gizmos.color = curveColor;
        for (int i = 0; i < curvePoints.Count - 1; i++)
        {
            Gizmos.DrawLine(curvePoints[i], curvePoints[i + 1]);
        }

        // 루프인 경우 마지막-첫 점 연결
        if (isLoop && curvePoints.Count > 2)
        {
            Gizmos.DrawLine(curvePoints[curvePoints.Count - 1], curvePoints[0]);
        }
    }
}
