using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 1) BoundingRect를 균일 cellSize로 스캔하여 GridNode 생성.
/// 2) "코스 내부/주변" => isCourseArea=true => courseGridNodes
///    그 외 => isCourseArea=false => hatchGridNodes
/// 3) 동시에 compositeGridNodes(전체)에도 저장
/// 4) OnDrawGizmosSelected 에서 courseGridNodes=빨강, hatchGridNodes=파랑 표시
/// </summary>
public class CompositeGridGenerator : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathDataSO;

    [FoldoutGroup("Settings"), Tooltip("Grid 간격(셀 크기)")]
    public float cellSize = 10f;

    [FoldoutGroup("Settings"), Tooltip("numX * numZ가 이 값을 넘으면 계산 중단")]
    public long maxResolution = 1000000L;

    [FoldoutGroup("Settings"), Tooltip("코스 주변으로 간주할 거리(Margin) 이내 => isCourseArea=true")]
    public float courseMargin = 10f;

    // 내부 캐시
    private List<GridNode> localComposite = new List<GridNode>();
    private List<GridNode> localCourse    = new List<GridNode>();
    private List<GridNode> localHatch     = new List<GridNode>();

    [FoldoutGroup("Actions")]
    [Button("Generate Composite Grid (GridNodes)")]
    public void GenerateCompositeGrid()
    {
        // 0) 검사
        if (pathDataSO == null)
        {
            Debug.LogWarning("[CompositeGridGenerator] pathDataSO가 없음");
            return;
        }
        if (cellSize <= 0f)
        {
            Debug.LogWarning("[CompositeGridGenerator] cellSize <= 0, 중단");
            return;
        }

        Rect r = pathDataSO.BoundingRect;
        if (r.width <= 0f || r.height <= 0f)
        {
            Debug.LogWarning("[CompositeGridGenerator] boundingRect가 유효하지 않음");
            return;
        }

        var cVerts = pathDataSO.CourseVertices;
        var cTris  = pathDataSO.CourseTris;
        if (cVerts==null || cVerts.Count<3 || cTris==null || cTris.Count<3)
        {
            Debug.LogWarning("[CompositeGridGenerator] 코스(mesh) 데이터 부족");
            return;
        }

        // 1) 리스트 초기화
        localComposite.Clear();
        localCourse.Clear();
        localHatch.Clear();

        float xMin = r.xMin, xMax = r.xMax;
        float zMin = r.yMin, zMax = r.yMax;
        float width  = xMax - xMin;
        float height = zMax - zMin;
        if(width<=0f || height<=0f)
        {
            Debug.LogWarning("[CompositeGridGenerator] Rect 크기가 0 이하");
            return;
        }

        int numX = Mathf.CeilToInt(width / cellSize)+1;
        int numZ = Mathf.CeilToInt(height/ cellSize)+1;

        long totalCells = (long)numX * (long)numZ;
        if(totalCells > maxResolution)
        {
            Debug.LogError($"[CompositeGridGenerator] 그리드 해상도 ({numX}x{numZ}={totalCells}) > maxResolution({maxResolution}), 중단");
            return;
        }

        int included=0;
        for(int iz=0; iz<numZ; iz++)
        {
            float zVal= zMin + iz*cellSize;
            if(zVal> zMax) zVal= zMax;

            for(int ix=0; ix<numX; ix++)
            {
                float xVal= xMin + ix*cellSize;
                if(xVal> xMax) xVal= xMax;

                Vector3 p= new Vector3(xVal, 0f, zVal);

                // 코스 내부인가?
                bool inside= IsInsideCourse2D(p, cVerts, cTris);

                // 코스 주변인가?
                bool nearCourse = false;
                if(!inside)
                {
                    nearCourse = IsNearCourse2D(p, cVerts, cTris, courseMargin);
                }
                bool isCourseArea= (inside || nearCourse);

                // y값 = DownhillNodes 참조
                p.y= ComputeHeightByCourse(p);

                // 새 GridNode
                var node= new GridNode(){
                    i= ix,
                    j= iz,
                    position= p,
                    isCourseArea= isCourseArea
                };

                // 전체 (composite)
                localComposite.Add(node);

                // 분류
                if(isCourseArea)
                    localCourse.Add(node);
                else
                    localHatch.Add(node);

                included++;
            }
        }

        Debug.Log($"[CompositeGridGenerator] total={totalCells}, scanned={included}, " +
                  $"composite={localComposite.Count}, course={localCourse.Count}, hatch={localHatch.Count}");

        // 2) PathDataSO에 저장
        pathDataSO.ClearCompositeGridNodes();
        pathDataSO.SetCompositeGridNodes(localComposite);

        pathDataSO.ClearCourseGridNodes();
        pathDataSO.SetCourseGridNodes(localCourse);

        pathDataSO.ClearHatchGridNodes();
        pathDataSO.SetHatchGridNodes(localHatch);

#if UNITY_EDITOR
        EditorUtility.SetDirty(pathDataSO);
#endif

        Debug.Log("[CompositeGridGenerator] compositeGridNodes / courseGridNodes / hatchGridNodes 저장 완료");
    }

    // ---------------------------
    // 내부 함수
    // ---------------------------
    private bool IsInsideCourse2D(Vector3 point, List<Vector3> cVerts, List<int> cTris)
    {
        Vector2 p= new Vector2(point.x, point.z);
        for(int i=0; i<cTris.Count; i+=3)
        {
            Vector2 a= new Vector2(cVerts[cTris[i  ]].x, cVerts[cTris[i  ]].z);
            Vector2 b= new Vector2(cVerts[cTris[i+1]].x, cVerts[cTris[i+1]].z);
            Vector2 c= new Vector2(cVerts[cTris[i+2]].x, cVerts[cTris[i+2]].z);

            if(IsPointInTriangle2D(p,a,b,c))
                return true;
        }
        return false;
    }
    private bool IsPointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0= c - a;
        Vector2 v1= b - a;
        Vector2 v2= p - a;

        float dot00= Vector2.Dot(v0, v0);
        float dot01= Vector2.Dot(v0, v1);
        float dot02= Vector2.Dot(v0, v2);
        float dot11= Vector2.Dot(v1, v1);
        float dot12= Vector2.Dot(v1, v2);

        float invDenom= 1f/(dot00*dot11 - dot01*dot01);
        float u= (dot11*dot02 - dot01*dot12)* invDenom;
        float v= (dot00*dot12 - dot01*dot02)* invDenom;

        return (u>=0f) && (v>=0f) && (u+v<1f);
    }

    private bool IsNearCourse2D(Vector3 point, List<Vector3> cVerts, List<int> cTris, float margin)
    {
        float marginSqr= margin*margin;
        Vector2 p2= new Vector2(point.x, point.z);

        for(int i=0; i<cTris.Count; i+=3)
        {
            Vector2 a= new Vector2(cVerts[cTris[i  ]].x, cVerts[cTris[i  ]].z);
            Vector2 b= new Vector2(cVerts[cTris[i+1]].x, cVerts[cTris[i+1]].z);
            Vector2 c= new Vector2(cVerts[cTris[i+2]].x, cVerts[cTris[i+2]].z);

            float dSqrAB= DistPointToSegmentSqr(p2,a,b);
            float dSqrBC= DistPointToSegmentSqr(p2,b,c);
            float dSqrCA= DistPointToSegmentSqr(p2,c,a);

            float minSqr= Mathf.Min(dSqrAB, Mathf.Min(dSqrBC,dSqrCA));
            if(minSqr< marginSqr)
                return true;
        }
        return false;
    }

    private float DistPointToSegmentSqr(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 AB= b-a;
        Vector2 AP= p-a;
        float magAB= AB.sqrMagnitude;
        if(magAB<1e-8f)
        {
            return AP.sqrMagnitude;
        }
        float t= Vector2.Dot(AP,AB)/ magAB;
        if(t<0f) t=0f;
        else if(t>1f) t=1f;

        Vector2 proj= a+ AB*t;
        return (p-proj).sqrMagnitude;
    }

    private float ComputeHeightByCourse(Vector3 pos)
    {
        // DownhillNodes 중 가장 가까운 node => y
        if(pathDataSO.DownhillNodes==null|| pathDataSO.DownhillNodes.Count==0)
            return 0f;

        float minDist= float.MaxValue;
        float bestY= 0f;
        Vector2 p2= new Vector2(pos.x, pos.z);

        foreach(var nd in pathDataSO.DownhillNodes)
        {
            Vector2 n2= new Vector2(nd.position.x, nd.position.z);
            float dist= (p2- n2).sqrMagnitude;
            if(dist< minDist)
            {
                minDist= dist;
                bestY= nd.position.y;
            }
        }
        return bestY;
    }

    // ================================
    // 기즈모
    // ================================
    private void OnDrawGizmosSelected()
    {
        if(pathDataSO==null) return;

        // 1) courseGridNodes=빨강, hatchGridNodes=파랑
        var courseNodes= pathDataSO.CourseGridNodes;
        if(courseNodes!=null && courseNodes.Count>0)
        {
            Gizmos.color= Color.red;
            foreach(var node in courseNodes)
            {
                Gizmos.DrawSphere(node.position, 0.25f);
            }
        }

        var hatchNodes= pathDataSO.HatchGridNodes;
        if(hatchNodes!=null && hatchNodes.Count>0)
        {
            Gizmos.color= Color.blue;
            foreach(var node in hatchNodes)
            {
                Gizmos.DrawSphere(node.position, 0.25f);
            }
        }

        // 2) compositeGridNodes => 회색 or 노란?
        var compNodes= pathDataSO.CompositeGridNodes;
        if(compNodes!=null && compNodes.Count>0)
        {
            Gizmos.color= Color.gray;
            foreach(var node in compNodes)
            {
                Gizmos.DrawSphere(node.position, 0.1f);
            }
        }
    }
}
