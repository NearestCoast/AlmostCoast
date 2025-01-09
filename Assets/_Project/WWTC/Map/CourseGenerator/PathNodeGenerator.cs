using UnityEngine;

[ExecuteInEditMode]
public class PathNodeGenerator : MonoBehaviour
{
    [Header("한 방향 당 +/- 몇 개의 i, j를 탐색할지")]
    public int lineCount = 10;

    [Header("삼각 격자의 한 변 길이 (교차점 간의 기준)")]
    public float cellSize = 1f;

    [Header("Inspector에서 설정할, 원하는 z값")]
    public float targetZ = 10f;

    [Header("그리드 색상")]
    public Color gridColor = Color.white;

    // ================================
    // 1) 시작점 (항상 i=0, j=0 이라고 가정)
    // ================================
    private Vector3 startPoint = Vector3.zero;

    // ================================
    // 2) 종단점 (z축 상 교차점 중에서, targetZ와 가장 가까운 곳)
    // ================================
    private Vector3 endPoint = Vector3.zero;

    // ------------------------------------------------
    // 삼각 격자를 그리기 위한 "두 개의 방향 벡터" (XZ 평면)
    // v1 = (1, 0),    v2 = (0.5, sqrt(3)/2)
    // 실제 3D로 치면: v1 -> (1, 0, 0), v2 -> (0.5, 0, sqrt(3)/2)
    // ------------------------------------------------
    private Vector2 v1 = new Vector2(1f, 0f);
    private Vector2 v2 = new Vector2(0.5f, Mathf.Sqrt(3f) / 2f);

    private void OnDrawGizmos()
    {
        // 1) 우선, 삼각 격자를 기즈모로 그려준다.
        Gizmos.color = gridColor;
        DrawTriangularGrid();

        // 2) 시작점은 (i=0, j=0) → 즉 (0,0,0)으로 고정
        startPoint = transform.position; // + Vector3.zero
        //  (만약 '항상 월드 좌표 (0,0,0)'로 하고 싶다면 그냥 Vector3.zero)

        // 3) 종단점 = z축 교차점 중에서 targetZ와 가장 가까운 점
        endPoint   = FindClosestZNode(targetZ);

        // 4) 시작점(빨간색), 종단점(녹색)을 구체로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(startPoint, 0.2f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(endPoint, 0.2f);
    }

    /// <summary>
    /// (i, j)를 정수 범위 내에서 순회하여
    /// 정삼각 격자 라인(교차선)을 Gizmos로 그린다.
    /// </summary>
    private void DrawTriangularGrid()
    {
        // 간단하게, (i, j) 쌍을 -lineCount..lineCount 범위로 순회하면서,
        // 그 점들을 서로 연결하는 방식을 보여줄 수 있습니다.
        //
        // 예) 각 노드 p(i,j)와 p(i+1, j), p(i, j+1) 등을 연결해서 삼각형.
        //
        // 여기서는 "라인으로만" 그려도 되고,
        // 혹은 DrawSphere 등으로 노드만 찍어도 됩니다.

        for (int i = -lineCount; i <= lineCount; i++)
        {
            for (int j = -lineCount; j <= lineCount; j++)
            {
                // 현재 노드 위치
                Vector3 p = To3D(i, j);

                // 격자선 1) p -> p(i+1, j)
                if (i + 1 <= lineCount)
                {
                    Vector3 pRight = To3D(i + 1, j);
                    Gizmos.DrawLine(p, pRight);
                }

                // 격자선 2) p -> p(i, j+1)
                if (j + 1 <= lineCount)
                {
                    Vector3 pUp = To3D(i, j + 1);
                    Gizmos.DrawLine(p, pUp);
                }

                // 격자선 3) p -> p(i+1, j-1)
                //  (정삼각형의 세 번째 변을 위한 연결)
                if ((i + 1 <= lineCount) && (j - 1 >= -lineCount))
                {
                    Vector3 pDiag = To3D(i + 1, j - 1);
                    Gizmos.DrawLine(p, pDiag);
                }
            }
        }
    }

    /// <summary>
    /// 2D (i, j) → XZ 평면상의 3D 좌표로 변환
    /// p(i, j) = i*v1 + j*v2 (여기선 cellSize까지 곱해 줌)
    /// </summary>
    private Vector3 To3D(int i, int j)
    {
        // (x, z) = i*(1,0) + j*(0.5, sqrt(3)/2) = (i + 0.5j, (sqrt(3)/2)j)
        float x = (i + 0.5f * j) * cellSize;
        float z = (Mathf.Sqrt(3f) / 2f) * j * cellSize;

        // 실제 3D world 좌표로 반환
        // (transform.position)만큼 평행이동할 수도 있음
        return transform.position + new Vector3(x, 0f, z);
    }

    /// <summary>
    /// "z축 상의 노드" (x=0) 중, targetZ에 가장 가까운 점을 찾는다.
    /// </summary>
    private Vector3 FindClosestZNode(float targetZValue)
    {
        // z축 위 노드는 (i + 0.5j=0)인 (i, j) 쌍만 가능
        // i + 0.5j = 0 -> i = -0.5j
        //
        // i, j가 모두 정수이어야 하므로, j가 짝수여야 i가 정수가 된다.
        //   j = 2k => i = -k
        //
        // 따라서 k를 -lineCount..lineCount 범위에서 돌면서,
        // 노드 p(-k, 2k)를 조사한다.

        float minDist = float.MaxValue;
        Vector3 bestPos = Vector3.zero;

        for (int k = -lineCount; k <= lineCount; k++)
        {
            int i = -k;
            int j =  2*k;

            // 이 (i, j)가 표현하는 3D 위치
            Vector3 candidate = To3D(i, j);

            // candidate.z와 targetZValue의 차이를 비교
            float dist = Mathf.Abs(candidate.z - targetZValue);

            if (dist < minDist)
            {
                minDist = dist;
                bestPos = candidate;
            }
        }

        return bestPos;
    }
}
