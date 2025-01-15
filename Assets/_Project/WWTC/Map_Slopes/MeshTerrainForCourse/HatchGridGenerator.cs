using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 1) pathDataSO.BoundingRect + courseVertices, courseTris
/// 2) cellSize 간격으로 grid 점 생성 (Rect 내에서만, Clamp 처리)
/// 3) 코스 내부(2D)면 제외
/// 4) 코스 주변 => y값을 DownhillNodes 이용해 반영(ComputeHeightByCourse)
/// 5) gridVertices를 기즈모로 표시
/// 
/// * Rect 밖으로 넘어가지 않도록, 마지막 칸 근처는 cellSize가 남아있지 않아도 Clamp로 일치
/// * grid가 X/Z(가로/세로)로 일정간격으로 깔리되, Rect 경계 넘어서지 않음
/// </summary>
public class HatchGridGenerator : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData;

    [FoldoutGroup("Settings"), Tooltip("Grid 간격(셀 크기)")]
    public float cellSize = 10f;

    [FoldoutGroup("Settings"), Tooltip("numX * numZ가 이 값을 넘으면 계산 불가로 간주")]
    public long maxResolution = 1000000;  // 예: 1,000,000

    [FoldoutGroup("Result"), ReadOnly]
    public List<Vector3> gridVertices = new List<Vector3>();

    [FoldoutGroup("Result"), ReadOnly]
    public int numX, numZ;  // 실제 계산된 해상도(칸 수 + 1)

    [FoldoutGroup("Actions")]
    [Button("Generate HatchGrid", ButtonSizes.Medium)]
    public void GenerateHatchGrid()
    {
        // 0) 기본 검사
        if (pathData == null)
        {
            Debug.LogWarning("[HatchGridGenerator] pathData가 없습니다.");
            return;
        }
        if (cellSize <= 0)
        {
            Debug.LogWarning($"[HatchGridGenerator] cellSize가 0 이하({cellSize})이므로 계산 불가");
            return;
        }

        // 1) boundingRect
        Rect r = pathData.BoundingRect;
        if(r.width <= 0 || r.height <= 0)
        {
            Debug.LogWarning("[HatchGridGenerator] boundingRect가 유효하지 않습니다.");
            return;
        }

        // 2) courseVerts, courseTris
        List<Vector3> cVerts = pathData.CourseVertices;
        List<int> cTris      = pathData.CourseTris;
        if(cVerts == null || cVerts.Count < 3 || cTris == null || cTris.Count < 3)
        {
            Debug.LogWarning("[HatchGridGenerator] 코스 메쉬 데이터가 부족합니다.");
            return;
        }

        // 3) 초기화
        gridVertices.Clear();

        float xMin = r.xMin;
        float xMax = r.xMax;
        float zMin = r.yMin;
        float zMax = r.yMax;

        float width  = xMax - xMin;
        float height = zMax - zMin;
        if(width <= 0 || height <= 0)
        {
            Debug.LogWarning("[HatchGridGenerator] Rect 크기가 0 이하");
            return;
        }

        // 4) numX, numZ 계산
        numX = Mathf.CeilToInt(width  / cellSize) + 1;
        numZ = Mathf.CeilToInt(height / cellSize) + 1;

        // 5) 해상도가 너무 큰지 검사
        long totalCells = (long)numX * (long)numZ;
        if(totalCells > maxResolution)
        {
            Debug.LogError($"[HatchGridGenerator] 그리드 해상도({numX}x{numZ}={totalCells})가 maxResolution({maxResolution}) 초과. 중단합니다.");
            return;
        }

        int included = 0;
        int total    = numX * numZ;

        // 6) (iz, ix) 루프
        for(int iz = 0; iz < numZ; iz++)
        {
            float zVal = zMin + iz*cellSize;
            if(zVal > zMax)
                zVal = zMax; // Clamp

            for(int ix = 0; ix < numX; ix++)
            {
                float xVal = xMin + ix*cellSize;
                if(xVal > xMax)
                    xVal = xMax; // Clamp

                Vector3 candidate = new Vector3(xVal, 0f, zVal);

                // 코스 내부(2D)면 제외
                if(IsInsideCourse2D(candidate, cVerts, cTris))
                {
                    continue;
                }

                // y값 계산
                candidate.y = ComputeHeightByCourse(candidate);

                gridVertices.Add(candidate);
                included++;
            }
        }

        Debug.Log($"[HatchGridGenerator] total={total}, included={included}, excluded={total - included}");
    }

    /// <summary>
    /// 코스 내부 여부 (Point-In-Triangle), 2D 판정
    /// </summary>
    private bool IsInsideCourse2D(Vector3 point, List<Vector3> verts, List<int> tris)
    {
        Vector2 p = new Vector2(point.x, point.z);
        for(int i=0; i < tris.Count; i+=3)
        {
            Vector2 a = new Vector2(verts[tris[i  ]].x, verts[tris[i  ]].z);
            Vector2 b = new Vector2(verts[tris[i+1]].x, verts[tris[i+1]].z);
            Vector2 c = new Vector2(verts[tris[i+2]].x, verts[tris[i+2]].z);

            if(IsPointInTriangle2D(p,a,b,c))
                return true;
        }
        return false;
    }

    private bool IsPointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = c - a;
        Vector2 v1 = b - a;
        Vector2 v2 = p - a;

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float invDenom = 1f/(dot00*dot11 - dot01*dot01);
        float u = (dot11*dot02 - dot01*dot12)* invDenom;
        float v = (dot00*dot12 - dot01*dot02)* invDenom;

        return (u>=0f) && (v>=0f) && (u+v<1f);
    }

    /// <summary>
    /// DownhillNodes => pos.xz와 노드 position.xz 거리 최소 => 그 노드의 y
    /// </summary>
    private float ComputeHeightByCourse(Vector3 pos)
    {
        if(pathData.DownhillNodes==null || pathData.DownhillNodes.Count==0)
            return 0f;

        float minDist = float.MaxValue;
        float bestY   = 0f;

        foreach(var nd in pathData.DownhillNodes)
        {
            Vector2 ndxz= new Vector2(nd.position.x, nd.position.z);
            Vector2 pxz= new Vector2(pos.x, pos.z);
            float dist= (ndxz - pxz).sqrMagnitude;
            if(dist< minDist)
            {
                minDist= dist;
                bestY  = nd.position.y;
            }
        }
        return bestY;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color= Color.cyan;
        foreach(var v in gridVertices)
        {
            Gizmos.DrawSphere(v,0.3f);
        }
    }
}
