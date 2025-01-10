using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PathNodeGenerator : MonoBehaviour
{
    [Header("삼각 격자 범위(-lineCount ~ lineCount)")]
    public int lineCount = 10;

    [Header("삼각 격자의 한 변 길이 (교차점 간 거리)")]
    public float cellSize = 1f;

    [Header("종단점으로 삼고싶은 대략적인 z값")]
    public float targetZ = 10f;

    [Header("최소 노드 수(경로 길이)")]
    public int minNodeCount = 5;

    [Header("바운더리의 x 범위")]
    public float boundaryMinX = -5f;
    public float boundaryMaxX =  5f;

    [Header("바운더리를 그릴 때 사용할 색상")]
    public Color boundaryColor = new Color(1f, 1f, 0f, 0.2f);

    [Header("그리드(삼각 격자) 라인 색상")]
    public Color gridColor = Color.white;

    [Header("경로 라인 색상 (최종 경로)")]
    public Color pathColor = Color.cyan;

    [Header("경로 노드 점 색상 (최종 경로)")]
    public Color pathNodeColor = Color.magenta;

    [Header("랜덤 DFS 탐색 횟수 제한 (무한루프 방지)")]
    public int maxIteration = 10000;

    // -----------------------------
    private Vector3 startPoint;  
    private Vector3 endPoint;    
    private int endI, endJ;      

    [SerializeField] private List<Vector3> pathNodes = new List<Vector3>();

    public List<Vector3> PathNodes => pathNodes;
    
    private int iterationCount = 0;

    private static readonly Vector2 v1 = new Vector2(1f, 0f);
    private static readonly Vector2 v2 = new Vector2(0.5f, Mathf.Sqrt(3f)/2f);

    // =========================================================================
    // OnDrawGizmos
    // =========================================================================
    private void OnDrawGizmos()
    {
        // (1) 시작/종단점
        startPoint = To3D(0, 0);
        endPoint   = FindClosestZNode(targetZ);
        (endI, endJ) = FindIJFromWorld(endPoint);

        // (2) 삼각 격자 라인
        Gizmos.color = gridColor;
        DrawTriangularGrid();

        // (3) 축소된 바운더리 Z 범위 (반 칸)
        float rawMinZ = Mathf.Min(startPoint.z, endPoint.z);
        float rawMaxZ = Mathf.Max(startPoint.z, endPoint.z);
        float actualMinZ, actualMaxZ;
        if (startPoint.z < endPoint.z)
        {
            actualMinZ = rawMinZ + cellSize * 0.5f; 
            actualMaxZ = rawMaxZ - cellSize * 0.5f; 
        }
        else
        {
            actualMinZ = rawMaxZ + cellSize * 0.5f;
            actualMaxZ = rawMinZ - cellSize * 0.5f;
        }

        // (4) 바운더리
        DrawBoundaryRect(boundaryMinX, boundaryMaxX, actualMinZ, actualMaxZ);

        // (5) 시작점(빨간), 종단점(녹색)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(startPoint, 0.2f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(endPoint, 0.2f);

        // (6) 최종 경로
        if (pathNodes.Count > 1)
        {
#if UNITY_EDITOR
            // === (A) Handles로 일정 화면 크기의 구를 그린다 ===
            Handles.color = pathNodeColor;

            foreach (var pos in pathNodes)
            {
                // "handleSize"는 씬 뷰 카메라 위치에 따라 자동 비례
                float handleSize = HandleUtility.GetHandleSize(pos) * 0.07f;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, handleSize, EventType.Repaint);
            }
#else
            // === (B) 런타임 or 에디터가 아닌 환경에선, 그냥 Gizmos.DrawSphere 사용 ===
            Gizmos.color = pathNodeColor;
            foreach (var pos in pathNodes)
            {
                Gizmos.DrawSphere(pos, 0.07f);
            }
#endif

            // 경로 라인
            Gizmos.color = pathColor;
            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                Gizmos.DrawLine(pathNodes[i], pathNodes[i + 1]);
            }
        }
    }

    // =========================================================================
    // [Button] : 경로 생성
    // =========================================================================
    [Button("Compute Path (Random DFS)")]
    public void GeneratePath()
    {
        startPoint = To3D(0, 0);
        endPoint   = FindClosestZNode(targetZ);
        (endI, endJ) = FindIJFromWorld(endPoint);

        float rawMinZ = Mathf.Min(startPoint.z, endPoint.z);
        float rawMaxZ = Mathf.Max(startPoint.z, endPoint.z);

        float actualMinZ, actualMaxZ;
        if (startPoint.z < endPoint.z)
        {
            actualMinZ = rawMinZ + cellSize * 0.5f; 
            actualMaxZ = rawMaxZ - cellSize * 0.5f; 
        }
        else
        {
            actualMinZ = rawMaxZ + cellSize * 0.5f;
            actualMaxZ = rawMinZ - cellSize * 0.5f;
        }

        pathNodes.Clear();
        iterationCount = 0;

        pathNodes = FindRandomDFSPath(minNodeCount, boundaryMinX, boundaryMaxX,
                                      actualMinZ, actualMaxZ);

#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    // =========================================================================
    // 이하 동일 (To3D, DrawTriangularGrid, DrawBoundaryRect, FindClosestZNode, etc.)
    // =========================================================================

    private Vector3 To3D(int i, int j)
    {
        float x = (i + 0.5f*j)*cellSize;
        float z = (Mathf.Sqrt(3f)/2f)*j * cellSize;
        return transform.position + new Vector3(x,0,z);
    }

    private void DrawTriangularGrid()
    {
        for(int i=-lineCount; i<=lineCount;i++)
        {
            for(int j=-lineCount;j<=lineCount;j++)
            {
                Vector3 p=To3D(i,j);
                if(i+1<=lineCount)
                {
                    Gizmos.DrawLine(p, To3D(i+1,j));
                }
                if(j+1<=lineCount)
                {
                    Gizmos.DrawLine(p, To3D(i,j+1));
                }
                if(i+1<=lineCount && j-1>=-lineCount)
                {
                    Gizmos.DrawLine(p, To3D(i+1,j-1));
                }
            }
        }
    }

    private void DrawBoundaryRect(float minX, float maxX, float minZ, float maxZ)
    {
#if UNITY_EDITOR
        Handles.color = boundaryColor;
        Vector3 c1 = transform.position + new Vector3(minX,0,minZ);
        Vector3 c2 = transform.position + new Vector3(maxX,0,minZ);
        Vector3 c3 = transform.position + new Vector3(maxX,0,maxZ);
        Vector3 c4 = transform.position + new Vector3(minX,0,maxZ);

        Handles.DrawLine(c1,c2);
        Handles.DrawLine(c2,c3);
        Handles.DrawLine(c3,c4);
        Handles.DrawLine(c4,c1);
#else
        Gizmos.color = boundaryColor;
        Vector3 c1 = transform.position + new Vector3(minX,0,minZ);
        Vector3 c2 = transform.position + new Vector3(maxX,0,minZ);
        Vector3 c3 = transform.position + new Vector3(maxX,0,maxZ);
        Vector3 c4 = transform.position + new Vector3(minX,0,maxZ);
        Gizmos.DrawLine(c1,c2);
        Gizmos.DrawLine(c2,c3);
        Gizmos.DrawLine(c3,c4);
        Gizmos.DrawLine(c4,c1);
#endif
    }

    private Vector3 FindClosestZNode(float targetZValue)
    {
        float minDist = float.MaxValue;
        Vector3 bestPos = Vector3.zero;
        for(int k=-lineCount; k<=lineCount; k++)
        {
            int i=-k;
            int j=2*k;
            Vector3 candidate=To3D(i,j);
            float dist=Mathf.Abs(candidate.z - targetZValue);
            if(dist<minDist)
            {
                minDist=dist;
                bestPos=candidate;
            }
        }
        return bestPos;
    }

    private List<Vector3> FindRandomDFSPath(int minCount,
                                            float minX,float maxX,
                                            float minZ,float maxZ)
    {
        int si=0, sj=0;
        var visited=new HashSet<(int,int)>();
        visited.Add((si,sj));

        var pathIJ=new List<(int,int)>();
        pathIJ.Add((si,sj));

        iterationCount=0;
        bool success= DFSRandom(si,sj,endI,endJ,
                                visited,pathIJ,
                                minCount,
                                minX,maxX,minZ,maxZ);

        if(!success)
        {
            Debug.LogWarning("랜덤 DFS로 경로를 찾지 못했습니다.");
            return new List<Vector3>();
        }

        var result=new List<Vector3>(pathIJ.Count);
        foreach(var (ii,jj) in pathIJ)
        {
            result.Add(To3D(ii,jj));
        }
        return result;
    }

    private bool DFSRandom(int i,int j, int endI,int endJ,
                           HashSet<(int,int)> visited, List<(int,int)> pathIJ,
                           int minCount,
                           float minX,float maxX,float minZ,float maxZ)
    {
        iterationCount++;
        if(iterationCount>maxIteration) return false;

        if(i==endI && j==endJ && pathIJ.Count>=minCount)
        {
            return true;
        }

        var neighbors= GetNeighbors(i,j);
        Shuffle(neighbors);

        foreach(var (ni,nj) in neighbors)
        {
            if(visited.Contains((ni,nj))) continue;
            if(!IsInBoundary(ni,nj, minX,maxX,minZ,maxZ))
                continue;

            visited.Add((ni,nj));
            pathIJ.Add((ni,nj));

            if(DFSRandom(ni,nj,endI,endJ,
                         visited,pathIJ,
                         minCount,
                         minX,maxX,minZ,maxZ))
            {
                return true;
            }
            pathIJ.RemoveAt(pathIJ.Count-1);
            visited.Remove((ni,nj));
        }
        return false;
    }

    private bool IsInBoundary(int i,int j,
                              float minX,float maxX,
                              float minZ,float maxZ)
    {
        // (1) lineCount 범위
        if(Mathf.Abs(i)>lineCount|| Mathf.Abs(j)>lineCount) 
            return false;

        // (2) z라인
        if(2*i + j==0)
        {
            // 시작점 or 종단점만 허용
            if(!((i==0 && j==0)||(i==endI && j==endJ)))
                return false;
        }

        Vector3 p= To3D(i,j);

        // x 범위
        if(p.x<minX||p.x>maxX)
            return false;

        // 종단점 예외
        if(i==endI && j==endJ)
        {
            // z 범위 무시
        }
        else
        {
            // 일반 노드 => z in (minZ, maxZ)
            if(p.z<=minZ|| p.z>=maxZ)
                return false;
        }

        return true;
    }

    private (int,int) FindIJFromWorld(Vector3 pos)
    {
        Vector3 local= pos-transform.position;
        float x= local.x/cellSize;
        float z= local.z/cellSize;

        float jF= z*2f/Mathf.Sqrt(3f);
        float iF= x-0.5f*jF;

        int i=Mathf.RoundToInt(iF);
        int j=Mathf.RoundToInt(jF);
        return (i,j);
    }

    private static readonly int[,] neighborOffsets=
    {
        {1,0},{-1,0},
        {0,1},{0,-1},
        {1,-1},{-1,1}
    };
    private List<(int,int)> GetNeighbors(int i,int j)
    {
        var list=new List<(int,int)>();
        for(int idx=0;idx<neighborOffsets.GetLength(0);idx++)
        {
            int ni=i+ neighborOffsets[idx,0];
            int nj=j+ neighborOffsets[idx,1];
            list.Add((ni,nj));
        }
        return list;
    }

    private static System.Random rng=new System.Random();
    private static void Shuffle<T>(List<T> list)
    {
        for(int i=list.Count-1;i>0;i--)
        {
            int swapIndex= rng.Next(0,i+1);
            (list[i], list[swapIndex])= (list[swapIndex], list[i]);
        }
    }
}
