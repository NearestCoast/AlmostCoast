using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// (단계2) NURBS(또는 임의) 곡선 샘플링 + LineRenderer, 튜브 Mesh 등
///  "코스"를 시각적으로 만들기.
/// </summary>
public class CourseMeshBuilder
{
    /// <summary>
    /// 라인렌더러로 표시할 때의 폭
    /// </summary>
    public float lineWidth = 0.1f;
    /// <summary>
    /// 튜브 메쉬 반지름
    /// </summary>
    public float tubeRadius = 0.5f;
    /// <summary>
    /// 튜브 단면 분할수
    /// </summary>
    public int circleResolution = 8;
    /// <summary>
    /// 샘플링 횟수 (NURBS 곡선을 몇 등분)
    /// </summary>
    public int sampleCount = 50;

    /// <summary>
    /// NURBSCurve를 샘플링해서, LineRenderer + 튜브Mesh를 만들어주는 함수
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="container"></param>
    /// <param name="generateTube"></param>
    /// <returns>LineRenderer (튜브 Mesh는 리턴이 아닌, container 아래에 생성)</returns>
    public LineRenderer BuildCourse(NURBSCurve curve, Transform container, bool generateTube)
    {
        if(curve == null)
        {
            Debug.LogWarning("[CourseMeshBuilder] curve is null!");
            return null;
        }
        if(container == null)
        {
            Debug.LogWarning("[CourseMeshBuilder] container is null!");
            return null;
        }

        // 1) CourseOutput 오브젝트
        var courseObj = new GameObject("CourseOutput");
        courseObj.transform.SetParent(container, false);

        // 2) 샘플링
        var samples = new List<Vector3>();
        for(int s=0; s<= sampleCount; s++)
        {
            float t= (float)s/sampleCount;
            Vector3 pt = curve.Evaluate(t);
            samples.Add(pt);
        }

        // 3) LineRenderer
        var lr = courseObj.AddComponent<LineRenderer>();
        lr.widthMultiplier = lineWidth;
        lr.loop = false;
        lr.positionCount = samples.Count;
        lr.SetPositions(samples.ToArray());
        lr.material = new Material(Shader.Find("Sprites/Default"));

        // 4) 튜브 Mesh (선택)
        if(generateTube)
        {
            GenerateTubeMesh(samples, courseObj);
        }

        return lr;
    }

    /// <summary>
    /// 간단 튜브 메쉬 생성
    /// </summary>
    private void GenerateTubeMesh(List<Vector3> centerLine, GameObject parentObj)
    {
        var mf= parentObj.AddComponent<MeshFilter>();
        var mr= parentObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Standard"));

        var vertices= new List<Vector3>();
        var triangles= new List<int>();

        for(int seg=0; seg< centerLine.Count; seg++)
        {
            Vector3 forward= Vector3.forward;
            if(seg< centerLine.Count-1)
            {
                forward= (centerLine[seg+1]- centerLine[seg]).normalized;
            }
            else if(seg>0)
            {
                forward= (centerLine[seg]- centerLine[seg-1]).normalized;
            }
            // up
            Vector3 up= Vector3.up;
            if(Vector3.Dot(forward, up)>0.9f)
                up= Vector3.right;

            Vector3 right= Vector3.Cross(forward, up).normalized;
            up= Vector3.Cross(right, forward).normalized;

            for(int c=0; c< circleResolution; c++)
            {
                float theta= Mathf.PI*2f * c/ circleResolution;
                Vector3 offset= right*Mathf.Cos(theta) + up*Mathf.Sin(theta);
                offset*= tubeRadius;
                vertices.Add(centerLine[seg]+ offset);
            }
        }

        int segmentCount= centerLine.Count-1;
        for(int seg=0; seg< segmentCount; seg++)
        {
            int baseIdx= seg* circleResolution;
            int nextBase= (seg+1)* circleResolution;
            for(int c=0; c< circleResolution; c++)
            {
                int i0= baseIdx+ c;
                int i1= baseIdx+ (c+1)%circleResolution;
                int i2= nextBase+ c;
                int i3= nextBase+ (c+1)%circleResolution;

                triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
            }
        }

        Mesh tubeMesh= new Mesh();
        tubeMesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        tubeMesh.SetVertices(vertices);
        tubeMesh.SetTriangles(triangles, 0);
        tubeMesh.RecalculateNormals();

        mf.sharedMesh= tubeMesh;
    }
}
