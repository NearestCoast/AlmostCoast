using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// (단계1) NURBS 곡선 제어점(Transform + 작은 sphere 시각화) 자동 생성 담당
/// - 시작점, 중간점, 종점 등을 생성
/// - 이 클래스를 통째로 CourseGenerator에 'InlineProperty'로 두면,
///   내부 필드들도 Inspector에 함께 노출됨.
/// </summary>
[System.Serializable]
public class NURBSCurveGenerator
{
    [LabelText("코스 길이 (Z방향)")]
    public float scaleZ = 1.0f;

    [LabelText("X방향 스케일(배율)")]
    public float scaleX = 1.0f;

    [LabelText("노트수 (중간 포인트 개수)")]
    public int midPointCount = 2;

    [LabelText("X축 기본 폭 (±)")]
    public float xRange = 0.5f;

    [LabelText("중간 Y값은 모두 0으로?")]
    public bool useFlatY = true;

    [LabelText("랜덤 Seed (0이면 임의)")]
    public int randomSeed = 0;

    [LabelText("Z축 랜덤 오프셋 (±)")]
    public float zJitter = 0.5f;

    /// <summary>
    /// 자동으로 제어점(Transform + 작은 sphere)을 컨테이너 하위에 생성
    /// </summary>
    public List<Transform> GenerateAutoPoints(Transform container)
    {
        var result = new List<Transform>();

        if (!container)
        {
            Debug.LogWarning("[NURBSCurveGenerator] Container가 null!");
            return result;
        }

        System.Random rng = (randomSeed != 0)
            ? new System.Random(randomSeed)
            : new System.Random();

        // 1) Start (0,0,0)
        var startTF = CreatePoint("StartPt", Vector3.zero, container);
        result.Add(startTF);

        // 2) 중간 포인트들
        for(int i=0; i< midPointCount; i++)
        {
            float frac = (float)(i+1)/(midPointCount+1);  
            float zBase = frac * scaleZ;
            float zRand = (float)(rng.NextDouble()*2 -1)* zJitter;
            float zVal  = zBase + zRand;

            float xRand = (float)(rng.NextDouble()*2 -1)* xRange;
            float xVal  = xRand * scaleX;

            float yVal  = 0f;
            if(!useFlatY)
            {
                yVal = (float)(rng.NextDouble()* 0.2f);
            }

            Vector3 pos = new Vector3(xVal, yVal, zVal);
            var midTF = CreatePoint($"MidPt_{i}", pos, container);
            result.Add(midTF);
        }

        // 3) End (0,0, scaleZ)
        var endPos = new Vector3(0,0,scaleZ);
        var endTF  = CreatePoint("EndPt", endPos, container);
        result.Add(endTF);

        return result;
    }

    /// <summary>
    /// 하나의 point + 작은 sphere 시각화
    /// </summary>
    private Transform CreatePoint(string name, Vector3 localPos, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VisualSphere";
        sphere.transform.SetParent(go.transform, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one*0.1f;

        var col = sphere.GetComponent<Collider>();
        if(col) Object.DestroyImmediate(col);

        var mr = sphere.GetComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Standard"));
        mr.sharedMaterial.color = Color.yellow;

        return go.transform;
    }
}
