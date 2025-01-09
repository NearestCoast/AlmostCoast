using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CourseGenerator : MonoBehaviour
{
    // ----------------------
    // "NURBSCurveGenerator"를 InlineProperty로 선언
    // => 이 내부 필드들이 Inspector에 노출됨
    // ----------------------
    [InlineProperty] 
    [BoxGroup("Curve Generation Options")]
    public NURBSCurveGenerator curveGen = new NURBSCurveGenerator();

    // ----------------------
    // 컨테이너
    // ----------------------
    [BoxGroup("Output Containers")]
    [LabelText("Curve Container")]
    public Transform curveContainer;

    [BoxGroup("Output Containers")]
    [LabelText("CourseMesh Container")]
    public Transform meshContainer;

    // ----------------------
    // 라인렌더러 & 메쉬 설정
    // ----------------------
    [BoxGroup("Line Renderer")]
    [LabelText("Line Width")]
    public float lineWidth = 0.1f;

    [BoxGroup("Line Renderer")]
    [LabelText("LineRenderer 자동 생성?")]
    public bool autoCreateLineRenderer = true;

    [BoxGroup("Mesh")]
    [LabelText("샘플링 횟수")]
    public int sampleCount = 50;

    [BoxGroup("Mesh")]
    [LabelText("튜브 반지름")]
    public float tubeRadius = 0.5f;

    [BoxGroup("Mesh")]
    [LabelText("튜브 단면 분할수")]
    public int circleResolution = 8;

    [BoxGroup("Mesh")]
    [LabelText("튜브 메쉬 생성?")]
    public bool generateTubeMesh = false;

    // ----------------------
    // NURBS 관련
    // ----------------------
    [BoxGroup("NURBS")]
    [LabelText("Degree")]
    public int degree = 3;

    [BoxGroup("NURBS")]
    [LabelText("Knots 자동 생성?")]
    public bool autoGenerateKnots = true;

    // 내부
    private List<Transform> controlPoints;
    private NURBSCurve curve;

    private GameObject curveObj;
    private GameObject courseMeshObj;

    //=========================================================
    // 1) GenerateAutoPoints - using NURBSCurveGenerator
    //=========================================================
    [Button("1) Generate Auto Points", ButtonHeight = 30)]
    public void GenerateAutoPoints()
    {
        if(controlPoints!= null)
        {
            // 기존 포인트 제거
            foreach(var tf in controlPoints)
            {
                if(tf) DestroyImmediate(tf.gameObject);
            }
        }

        if(!curveContainer) curveContainer= this.transform;

        // NURBSCurveGenerator 사용
        controlPoints= curveGen.GenerateAutoPoints(curveContainer);

        Debug.Log($"[CourseGenerator] Points Generated. total= {controlPoints.Count}");
    }

    //=========================================================
    // 2) BuildCurveAndMesh
    //=========================================================
    [Button("2) Build Curve & CourseMesh", ButtonHeight = 30)]
    public void BuildCurveAndMesh()
    {
        if(controlPoints== null || controlPoints.Count< (degree+ 1))
        {
            Debug.LogWarning("[CourseGenerator] Not enough controlPoints.");
            return;
        }

        // (A) NURBSCurve
        curve= new NURBSCurve
        {
            degree= degree,
            controlPoints= new List<Vector3>(),
            weights= new List<float>(),
            knots= new List<float>()
        };

        // ControlPoints -> curve
        foreach(var tf in controlPoints)
        {
            curve.controlPoints.Add(tf.position);
        }

        for(int i=0; i< curve.controlPoints.Count; i++)
            curve.weights.Add(1f);

        if(autoGenerateKnots)
        {
            int n= curve.controlPoints.Count;
            int knotCount= n+ degree+1;
            for(int i=0; i< knotCount; i++)
            {
                if(i<= degree) curve.knots.Add(0f);
                else if(i>= (knotCount - degree-1))
                    curve.knots.Add(knotCount- 2f* degree -1);
                else
                    curve.knots.Add(i- degree);
            }
        }

        // (B) 기존 obj 제거
        if(curveObj) DestroyImmediate(curveObj);
        if(courseMeshObj) DestroyImmediate(courseMeshObj);

        // (C) CurveObj
        curveObj= new GameObject("CurveLineObj");
        curveObj.transform.SetParent(curveContainer, false);

        var lr= curveObj.AddComponent<LineRenderer>();
        lr.widthMultiplier= lineWidth;
        lr.loop= false;
        lr.material= new Material(Shader.Find("Sprites/Default"));

        // (D) 샘플링
        List<Vector3> samples= new List<Vector3>();
        for(int s=0; s<= sampleCount; s++)
        {
            float t= (float)s/ sampleCount;
            var pt= curve.Evaluate(t);
            samples.Add(pt);
        }

        // 마지막 샘플을 '종단점'으로 강제 세팅
        if(samples.Count>0 && controlPoints.Count>0)
        {
            Vector3 endPos= controlPoints[controlPoints.Count -1].position;
            samples[samples.Count -1]= endPos;
        }

        lr.positionCount= samples.Count;
        lr.SetPositions(samples.ToArray());

        // (E) 튜브 생성
        if(generateTubeMesh)
        {
            if(!meshContainer) meshContainer= this.transform;

            courseMeshObj= new GameObject("CourseMeshObj");
            courseMeshObj.transform.SetParent(meshContainer, false);
            BuildTube(samples, courseMeshObj);
        }

        Debug.Log("[CourseGenerator] BuildCurveAndMesh done.");
    }

    private void BuildTube(List<Vector3> centerLine, GameObject parentObj)
    {
        var mf= parentObj.AddComponent<MeshFilter>();
        var mr= parentObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial= new Material(Shader.Find("Standard"));

        var verts= new List<Vector3>();
        var tris= new List<int>();

        for(int seg=0; seg< centerLine.Count; seg++)
        {
            Vector3 forward= Vector3.forward;
            if(seg< centerLine.Count-1)
                forward= (centerLine[seg+1]- centerLine[seg]).normalized;
            else if(seg>0)
                forward= (centerLine[seg]- centerLine[seg-1]).normalized;

            Vector3 up= Vector3.up;
            if(Vector3.Dot(forward, up)> 0.9f)
                up= Vector3.right;

            Vector3 right= Vector3.Cross(forward, up).normalized;
            up= Vector3.Cross(right, forward).normalized;

            for(int c=0; c< circleResolution; c++)
            {
                float theta= Mathf.PI*2f* c/ circleResolution;
                Vector3 offset= right* Mathf.Cos(theta)+ up* Mathf.Sin(theta);
                offset*= tubeRadius;
                verts.Add(centerLine[seg]+ offset);
            }
        }

        int segCount= centerLine.Count-1;
        for(int seg=0; seg< segCount; seg++)
        {
            int baseIdx= seg* circleResolution;
            int nextBase= (seg+1)* circleResolution;
            for(int c=0; c< circleResolution; c++)
            {
                int i0= baseIdx+ c;
                int i1= baseIdx+ (c+1)% circleResolution;
                int i2= nextBase+ c;
                int i3= nextBase+ (c+1)% circleResolution;

                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }

        Mesh tube= new Mesh();
        tube.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        tube.SetVertices(verts);
        tube.SetTriangles(tris, 0);
        tube.RecalculateNormals();

        mf.sharedMesh= tube;
    }
}
