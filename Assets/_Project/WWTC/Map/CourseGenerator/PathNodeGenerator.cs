using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PathNodeGenerator : MonoBehaviour
{
    [Header("PathDataSO (ScriptableObject)")]
    public PathDataSO pathDataSO;

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

    // 내부 상태
    private Vector3 startPoint;
    private Vector3 endPoint;
    private int endI, endJ;

    private int iterationCount = 0;

    // =========================================================================
    // [Button] : 경로 생성
    // =========================================================================
    [Button("Compute Path (Random DFS)")]
    public void GeneratePath()
    {
        // 1) 시작 / 종단점 계산
        startPoint = To3D(0, 0);
        endPoint   = FindClosestZNode(targetZ);
        (endI, endJ) = FindIJFromWorld(endPoint);

        // 2) 바운더리 z 범위 계산 (반 칸 축소)
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

        iterationCount = 0;

        // 3) DFS 탐색 → pathNodes 생성
        List<Vector3> newPath = FindRandomDFSPath(minNodeCount,
                                                  boundaryMinX, boundaryMaxX,
                                                  actualMinZ, actualMaxZ);
        if (newPath.Count < 2)
        {
            Debug.LogWarning("[PathNodeGenerator] 유효한 경로를 찾지 못했습니다.");
            return;
        }

        // 4) 경로 찾기에 성공하면, 새 start/end 노드 추가
        //    - start: z방향으로 -cellSize
        //    - end  : z방향으로 +cellSize
        Vector3 oldStart = newPath[0];
        Vector3 oldEnd   = newPath[newPath.Count - 1];

        // 새 시작점 (z방향 아래쪽: -cellSize)
        Vector3 newStart = oldStart + new Vector3(0, 0, -cellSize);
        // 새 종단점 (z방향 위쪽: +cellSize)
        Vector3 newEnd   = oldEnd + new Vector3(0, 0,  cellSize);

        // 앞뒤에 삽입
        newPath.Insert(0, newStart);           // 경로 맨 앞에 새 start
        newPath.Add(newEnd);                  // 경로 맨 뒤에 새 end

        // 5) ScriptableObject에 저장
        if (pathDataSO != null)
        {
            pathDataSO.SetPathNodes(newPath);
            Debug.Log($"[PathNodeGenerator] PathNodes generated. Count={newPath.Count}");
        }
        else
        {
            Debug.LogWarning("[PathNodeGenerator] pathDataSO가 설정되지 않았습니다. 결과를 저장할 수 없습니다.");
        }

#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    // =========================================================================
    // OnDrawGizmos : pathNodes 시각화
    // =========================================================================
    private void OnDrawGizmos()
    {
        // (1) 시작/종단점
        startPoint = To3D(0, 0);
        endPoint   = FindClosestZNode(targetZ);
        (endI, endJ) = FindIJFromWorld(endPoint);

        // (2) 삼각 격자
        Gizmos.color = gridColor;
        DrawTriangularGrid();

        // (3) 바운더리
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
        DrawBoundaryRect(boundaryMinX, boundaryMaxX, actualMinZ, actualMaxZ);

        // (4) 시작점(빨간), 종단점(녹색)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(startPoint, 0.2f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(endPoint, 0.2f);

        // (5) pathNodes 시각화
        if (pathDataSO != null)
        {
            List<Vector3> pathNodes = pathDataSO.PathNodes;
            if (pathNodes != null && pathNodes.Count > 1)
            {
#if UNITY_EDITOR
                // Handles로 일정 화면 크기의 구를 그리기
                Handles.color = pathNodeColor;
                foreach (var pos in pathNodes)
                {
                    float handleSize = HandleUtility.GetHandleSize(pos) * 0.07f;
                    Handles.SphereHandleCap(0, pos, Quaternion.identity, handleSize, EventType.Repaint);
                }
#else
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
    }

    // =========================================================================
    // DrawTriangularGrid
    // =========================================================================
    private void DrawTriangularGrid()
    {
        for (int i = -lineCount; i <= lineCount; i++)
        {
            for (int j = -lineCount; j <= lineCount; j++)
            {
                Vector3 p = To3D(i, j);

                // 오른쪽
                if (i + 1 <= lineCount)
                {
                    Gizmos.DrawLine(p, To3D(i + 1, j));
                }

                // 위
                if (j + 1 <= lineCount)
                {
                    Gizmos.DrawLine(p, To3D(i, j + 1));
                }

                // 대각
                if ((i + 1 <= lineCount) && (j - 1 >= -lineCount))
                {
                    Gizmos.DrawLine(p, To3D(i + 1, j - 1));
                }
            }
        }
    }

    // =========================================================================
    // DrawBoundaryRect
    // =========================================================================
    private void DrawBoundaryRect(float minX, float maxX, float minZ, float maxZ)
    {
#if UNITY_EDITOR
        Handles.color = boundaryColor;
        Vector3 c1 = transform.position + new Vector3(minX, 0, minZ);
        Vector3 c2 = transform.position + new Vector3(maxX, 0, minZ);
        Vector3 c3 = transform.position + new Vector3(maxX, 0, maxZ);
        Vector3 c4 = transform.position + new Vector3(minX, 0, maxZ);

        Handles.DrawLine(c1, c2);
        Handles.DrawLine(c2, c3);
        Handles.DrawLine(c3, c4);
        Handles.DrawLine(c4, c1);
#else
        Gizmos.color = boundaryColor;
        Vector3 c1 = transform.position + new Vector3(minX, 0, minZ);
        Vector3 c2 = transform.position + new Vector3(maxX, 0, minZ);
        Vector3 c3 = transform.position + new Vector3(maxX, 0, maxZ);
        Vector3 c4 = transform.position + new Vector3(minX, 0, maxZ);

        Gizmos.DrawLine(c1, c2);
        Gizmos.DrawLine(c2, c3);
        Gizmos.DrawLine(c3, c4);
        Gizmos.DrawLine(c4, c1);
#endif
    }

    // =========================================================================
    // FindClosestZNode, FindRandomDFSPath, etc.
    // =========================================================================
    private Vector3 FindClosestZNode(float targetZValue)
    {
        float minDist = float.MaxValue;
        Vector3 bestPos = Vector3.zero;
        for(int k=-lineCount; k<=lineCount; k++)
        {
            int i=-k;
            int j=2*k;
            Vector3 candidate = To3D(i,j);
            float dist = Mathf.Abs(candidate.z - targetZValue);
            if(dist < minDist)
            {
                minDist = dist;
                bestPos = candidate;
            }
        }
        return bestPos;
    }

    private List<Vector3> FindRandomDFSPath(int minCount, float minX, float maxX,
                                            float minZ, float maxZ)
    {
        int si=0, sj=0;
        var visited = new HashSet<(int,int)>();
        visited.Add((si,sj));

        var pathIJ = new List<(int,int)>();
        pathIJ.Add((si,sj));

        iterationCount=0;
        bool success = DFSRandom(si, sj, endI,endJ, visited, pathIJ,
                                 minCount, minX,maxX, minZ,maxZ);

        if(!success)
        {
            return new List<Vector3>(); // 실패 시 빈
        }

        // (i,j)->(x,z)
        var result = new List<Vector3>(pathIJ.Count);
        foreach(var (ii,jj) in pathIJ)
        {
            result.Add(To3D(ii,jj));
        }
        return result;
    }

    private bool DFSRandom(int i, int j, int endI, int endJ,
                           HashSet<(int,int)> visited, List<(int,int)> pathIJ,
                           int minCount,
                           float minX, float maxX,
                           float minZ, float maxZ)
    {
        iterationCount++;
        if (iterationCount>maxIteration)
            return false;

        if (i==endI && j==endJ && pathIJ.Count>=minCount)
        {
            return true;
        }

        var neighbors = GetNeighbors(i,j);
        Shuffle(neighbors);

        foreach(var (ni,nj) in neighbors)
        {
            if (visited.Contains((ni,nj))) 
                continue;
            if (!IsInBoundary(ni,nj, minX,maxX, minZ,maxZ))
                continue;

            visited.Add((ni,nj));
            pathIJ.Add((ni,nj));

            if (DFSRandom(ni,nj,endI,endJ, visited,pathIJ,minCount,
                          minX,maxX,minZ,maxZ))
            {
                return true;
            }

            pathIJ.RemoveAt(pathIJ.Count -1);
            visited.Remove((ni,nj));
        }
        return false;
    }

    private bool IsInBoundary(int i,int j, float minX,float maxX,
                              float minZ,float maxZ)
    {
        if(Mathf.Abs(i)>lineCount || Mathf.Abs(j)>lineCount)
            return false;

        // z축 라인 => (0,0) or (endI,endJ)만 허용
        if(2*i + j==0)
        {
            if(!((i==0&&j==0)||(i==endI&&j==endJ)))
                return false;
        }

        Vector3 p= To3D(i,j);

        if (p.x<minX|| p.x>maxX)
            return false;

        // 종단점 예외
        if (i==endI&& j==endJ)
        {
            // z 범위 무시
        }
        else
        {
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
        float iF= x- 0.5f*jF;

        int i= Mathf.RoundToInt(iF);
        int j= Mathf.RoundToInt(jF);
        return (i,j);
    }

    private Vector3 To3D(int i,int j)
    {
        float x= (i+ 0.5f*j)*cellSize;
        float z= (Mathf.Sqrt(3f)/2f)* j* cellSize;
        return transform.position + new Vector3(x,0,z);
    }

    private static readonly int[,] neighborOffsets={
        {1,0},{-1,0},{0,1},{0,-1},{1,-1},{-1,1}
    };
    private List<(int,int)> GetNeighbors(int i,int j)
    {
        var list=new List<(int,int)>();
        for(int idx=0; idx<neighborOffsets.GetLength(0); idx++)
        {
            int ni= i+ neighborOffsets[idx,0];
            int nj= j+ neighborOffsets[idx,1];
            list.Add((ni,nj));
        }
        return list;
    }

    private static System.Random rng= new System.Random();
    private static void Shuffle<T>(List<T> list)
    {
        for(int i=list.Count-1; i>0; i--)
        {
            int swapIndex= rng.Next(0,i+1);
            (list[i], list[swapIndex])= (list[swapIndex], list[i]);
        }
    }
}
