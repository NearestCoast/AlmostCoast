using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// DownhillNodes -> (vertices, triangles) 코스 지오메트리 생성 -> PathDataSO에 저장
/// + cornerWindow 문제(초반/후반부) 해결 + 좌/우 flipping 보간
/// + 시작/끝 노드에서 z값 고정
/// 기즈모로 선분 시각화.
/// </summary>
public class CourseGenerator : MonoBehaviour
{
    [Header("PathDataSO (ScriptableObject)")]
    public PathDataSO pathDataSO;

    [FoldoutGroup("Course Settings"), Tooltip("루프 코스 여부")]
    public bool isLoop = false;

    [FoldoutGroup("Course Settings"), Tooltip("최대 폭")]
    public float maxWidth = 1f;

    [FoldoutGroup("Course Settings"), Tooltip("곡률(curvature)가 maxCurv이면 Inside=0")]
    public float maxCurvature = 120f;

    [FoldoutGroup("Course Settings"), Tooltip("경사(slope)=1 일 때 폭 감소 비율(0~1)")]
    [Range(0f, 1f)]
    public float slopeInfluence = 0.3f;

    [FoldoutGroup("Course Settings"), MinValue(1), MaxValue(10)]
    public int cornerWindow = 2;

    [FoldoutGroup("Actions")]
    [Button("Generate Course Data (Fixed)", ButtonSizes.Large)]
    public void GenerateCourseData()
    {
        if (pathDataSO == null)
        {
            Debug.LogWarning("[CourseGenerator] pathDataSO가 없습니다!");
            return;
        }

        // DownhillNodes
        List<DownhillNode> nodes = pathDataSO.DownhillNodes;
        if (nodes == null || nodes.Count < 2)
        {
            Debug.LogWarning("[CourseGenerator] DownhillNodes가 부족(2개 미만)!");
            return;
        }

        int count = nodes.Count;

        // (A) cornerWindow 범위 곡률
        float[] maxCurvInRange = ComputeMaxCurvInRange(nodes, count);

        // (B) 1차 계산: (leftHW[i], rightHW[i]) 보관 후, 2차 pass로 보간
        float[] leftHW  = new float[count];
        float[] rightHW = new float[count];

        for (int i = 0; i < count; i++)
        {
            DownhillNode node = nodes[i];
            float slope = node.slope;
            float curve= maxCurvInRange[i];

            // slope -> 전체 폭 감소
            float totalWidth = maxWidth * (1f - slopeInfluence * Mathf.Clamp01(slope));

            // 곡률 -> insideHalf
            float ratio = 1f - Mathf.Clamp01(curve / maxCurvature);
            float insideHalf  = ratio * (totalWidth * 0.5f);
            float outsideHalf = (totalWidth * 0.5f) - insideHalf;

            bool leftCorner = IsLeftTurn(nodes, i);
            if (leftCorner)
            {
                leftHW[i]  = insideHalf;
                rightHW[i] = outsideHalf;
            }
            else
            {
                leftHW[i]  = outsideHalf;
                rightHW[i] = insideHalf;
            }
        }

        // (C) 2차 pass: 인접 노드간 보간 => 급격 flipping 완화
        SmoothInsideOutside(ref leftHW, ref rightHW);

        // (D) vertex, tri 리스트
        List<Vector3> cVerts = new List<Vector3>(count*2);
        List<int>     cTris  = new List<int>((count -1)*6);

        for (int i=0; i<count; i++)
        {
            DownhillNode node = nodes[i];
            Vector3 pos   = node.position;
            Vector3 dir   = node.direction.normalized;

            Vector3 binormal= Vector3.Cross(dir, Vector3.up).normalized;
            Vector3 leftPos = pos - binormal * leftHW[i];
            Vector3 rightPos= pos + binormal * rightHW[i];

            // ++++++ 여기가 핵심 수정 ++++++
            // 시작점(i=0)은 z를 node[0].position.z로 고정
            if (i == 0)
            {
                leftPos.z  = nodes[0].position.z;
                rightPos.z = nodes[0].position.z;
            }
            // 끝점(i=count-1)은 z를 node[count-1].position.z로 고정
            else if (i == count - 1)
            {
                leftPos.z  = nodes[count-1].position.z;
                rightPos.z = nodes[count-1].position.z;
            }
            // +++++++++++++++++++++++++++++++++

            cVerts.Add(leftPos);
            cVerts.Add(rightPos);
        }

        int segCount= (isLoop)? count : (count-1);
        for (int i=0; i<segCount; i++)
        {
            int iNext= (i+1)%count;
            int iLeft  = i*2;
            int iRight = i*2+1;
            int nLeft  = iNext*2;
            int nRight = iNext*2+1;

            // tri1
            cTris.Add(iLeft);   cTris.Add(iRight); cTris.Add(nLeft);
            // tri2
            cTris.Add(nLeft);   cTris.Add(iRight); cTris.Add(nRight);
        }

        // (E) PathDataSO 저장
        pathDataSO.SetCourseData(cVerts, cTris);

        Debug.Log($"[CourseGenerator] GenerateCourseData => Verts={cVerts.Count}, Tris={cTris.Count/3}");
    }

    // --------------------------------------------------------
    // 곡률 계산( cornerWindow 보완 )
    // --------------------------------------------------------
    private float[] ComputeMaxCurvInRange(List<DownhillNode> nodes, int count)
    {
        float[] maxCurv= new float[count];
        for(int i=0; i<count; i++)
        {
            int leftRange  = Mathf.Min(cornerWindow, i);
            int rightRange = Mathf.Min(cornerWindow, count-1 - i);

            int leftIdx = i - leftRange;
            int rightIdx= i + rightRange;

            float localMax=0f;
            for (int j=leftIdx; j<= rightIdx; j++)
            {
                float c= nodes[j].curvature;
                if (c> localMax) localMax=c;
            }
            maxCurv[i]= localMax;
        }
        return maxCurv;
    }

    // --------------------------------------------------------
    // 인접 노드간 inside/outside 폭 보간
    // --------------------------------------------------------
    private void SmoothInsideOutside(ref float[] leftHW, ref float[] rightHW)
    {
        int count = leftHW.Length;
        // forward pass
        for (int i=1; i<count; i++)
        {
            float oldL = leftHW[i];
            float oldR = rightHW[i];
            float prevL= leftHW[i-1];
            float prevR= rightHW[i-1];

            leftHW[i]  = 0.5f*(oldL + prevL);
            rightHW[i] = 0.5f*(oldR+ prevR);
        }

        // backward pass
        for (int i= count-2; i>=0; i--)
        {
            float oldL = leftHW[i];
            float oldR = rightHW[i];
            float nextL= leftHW[i+1];
            float nextR= rightHW[i+1];

            leftHW[i]  = 0.5f*(oldL + nextL);
            rightHW[i] = 0.5f*(oldR + nextR);
        }
    }

    // --------------------------------------------------------
    // 왼/오른 코너 판단
    // --------------------------------------------------------
    private bool IsLeftTurn(List<DownhillNode> nodes, int i)
    {
        int count = nodes.Count;
        int iNext = (i+1< count)? (i+1) : i;

        Vector3 dA = nodes[i].direction;
        Vector3 dB = nodes[iNext].direction;
        Vector3 cross = Vector3.Cross(dA, dB);
        return (cross.y> 0f);
    }

    // --------------------------------------------------------
    // 기즈모
    // --------------------------------------------------------
    [SerializeField] private bool showGizmo;
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        if (pathDataSO==null) return;
        var cv= pathDataSO.CourseVertices;
        var ct= pathDataSO.CourseTris;
        if (cv==null|| cv.Count<3) return;
        if (ct==null|| ct.Count<3) return;

        Gizmos.color= Color.yellow;
        for (int i=0; i< ct.Count; i+=3)
        {
            Vector3 a= cv[ ct[i]   ];
            Vector3 b= cv[ ct[i+1] ];
            Vector3 c= cv[ ct[i+2] ];

            Gizmos.DrawLine(a,b);
            Gizmos.DrawLine(b,c);
            Gizmos.DrawLine(c,a);
        }
    }
}
