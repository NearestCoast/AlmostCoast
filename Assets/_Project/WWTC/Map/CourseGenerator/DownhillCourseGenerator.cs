using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 1) PathDataSO.CurvePoints를 기반으로
/// 2) cornerWindow 범위에서 코너 각(cornerAngle) 계산
/// 3) cornerAngle >= noSlopeAngle이면 경사=0, 아니면 minSlope~maxSlope 적용
/// 4) DownhillNodes(DownhillNode 리스트)를 만들어서 PathDataSO에 저장
///    + DownhillPoints(기존 Vector3 리스트)도 필요하다면 같이 생성
/// 
/// * Gizmos가 항상 보이도록 OnDrawGizmos()에서 라인을 그림.
/// </summary>
[ExecuteInEditMode]
public class DownhillCourseGenerator : MonoBehaviour
{
    [Header("PathDataSO (ScriptableObject)")]
    public PathDataSO pathDataSO;  

    // ==========================================================
    // Odin SuffixLabel을 사용하여 기울기를 각도로 표시
    // slope → angle(°) = atan(slope)*Rad2Deg
    // ==========================================================
    [FoldoutGroup("Downhill Settings"), Min(0f)]
    [SuffixLabel("@(Mathf.Atan(minSlope) * Mathf.Rad2Deg).ToString(\"F2\") + \"°\"", Overlay = true)]
    public float minSlope = 0.01f; 

    [FoldoutGroup("Downhill Settings"), Min(0f)]
    [SuffixLabel("@(Mathf.Atan(maxSlope) * Mathf.Rad2Deg).ToString(\"F2\") + \"°\"", Overlay = true)]
    public float maxSlope = 0.10f; 

    [FoldoutGroup("Downhill Settings"), Tooltip("코너각(각도). 예:30~40도")]
    public float noSlopeAngle = 30f;

    [FoldoutGroup("Downhill Settings")]
    public bool randomSlope = true;

    [FoldoutGroup("Downhill Settings")]
    public float startHeightOffset = 0f;

    [FoldoutGroup("Downhill Settings"), Tooltip("Downhill 결과를 SO에 저장할지 여부")]
    public bool saveDownhillToSO = true;

    [FoldoutGroup("Downhill Settings"), Tooltip("코너 각 계산 시, i±cornerWindow 범위를 참고")]
    [MinValue(1), MaxValue(10)]
    public int cornerWindow = 2;

    // ======================================
    // 기존에 Vector3로만 보관하던 리스트 (디버그용)
    [SerializeField] private List<Vector3> localDownhillPoints = new List<Vector3>();
    public List<Vector3> DownhillPoints => localDownhillPoints;

    // 새롭게 DownhillNode 구조체를 저장 (디버그용)
    [SerializeField] private List<DownhillNode> localDownhillNodes = new List<DownhillNode>();
    public List<DownhillNode> DownhillNodes => localDownhillNodes;

    // ======================================
    // 버튼: Convert to Downhill
    // ======================================
    [FoldoutGroup("Downhill Settings")]
    [Button("Convert to Downhill", ButtonSizes.Large)]
    public void ConvertToDownhill()
    {
        localDownhillPoints.Clear();
        localDownhillNodes.Clear();

        if (pathDataSO == null)
        {
            Debug.LogWarning("[DownhillCourseGenerator] pathDataSO가 비어있습니다!");
            return;
        }

        // 1) ScriptableObject에서 곡선(CurvePoints) 가져오기
        List<Vector3> curve = pathDataSO.CurvePoints;  
        if (curve == null || curve.Count < 2)
        {
            Debug.LogWarning("[DownhillCourseGenerator] CurvePoints가 부족합니다.");
            return;
        }

        // 2) 첫 지점
        Vector3 firstPos = curve[0];
        firstPos.y += startHeightOffset;

        // 첫 DownhillNode
        DownhillNode firstNode = new DownhillNode();
        firstNode.position  = firstPos;
        firstNode.slope     = 0f;      // 첫 구간 slope=0 가정
        firstNode.curvature = 0f;      // 첫 지점 curvature=0 가정
        firstNode.direction = (curve.Count > 1)
                              ? (curve[1] - curve[0]).normalized
                              : Vector3.forward; // 임의
        localDownhillPoints.Add(firstPos);
        localDownhillNodes.Add(firstNode);

        // 3) 이후 포인트들 - 코너 각도 & 경사 적용
        for (int i = 1; i < curve.Count; i++)
        {
            Vector3 currentPos     = curve[i];
            Vector3 prevDownPos    = localDownhillPoints[i - 1];
            DownhillNode prevNode  = localDownhillNodes[i - 1];

            // cornerAngle 계산
            float cornerAngle = ComputeCornerAngle(curve, i, cornerWindow);

            // slope 결정
            float slopeValue;
            if (cornerAngle >= noSlopeAngle) 
            {
                // 충분히 큰 코너각 → 경사 0
                slopeValue = 0f;
            }
            else
            {
                // 좁은 각 → minSlope ~ maxSlope
                if (randomSlope) 
                     slopeValue = Random.Range(minSlope, maxSlope);
                else 
                     slopeValue = 0.5f * (minSlope + maxSlope);
            }

            // XZ 거리
            float distXZ = Vector2.Distance(
                new Vector2(prevDownPos.x, prevDownPos.z),
                new Vector2(currentPos.x,   currentPos.z)
            );
            float deltaY = distXZ * slopeValue;

            // 새 위치
            Vector3 newPos = currentPos;
            newPos.y = prevDownPos.y - deltaY;

            // direction
            Vector3 dir = (i < curve.Count - 1)
                          ? (curve[i+1] - curve[i]).normalized 
                          : (curve[i]   - curve[i-1]).normalized;

            // curvature
            float curvatureVal = 0f;
            if (i > 0 && i < curve.Count - 1)
            {
                Vector3 dirA = (curve[i]   - curve[i-1]).normalized;
                Vector3 dirB = (curve[i+1] - curve[i]).normalized;
                float angle = Vector3.Angle(dirA, dirB); 
                curvatureVal = angle; // 간단히 각도(°)
            }

            // 새 DownhillNode
            DownhillNode newNode = new DownhillNode();
            newNode.position  = newPos;
            newNode.slope     = slopeValue;
            newNode.curvature = curvatureVal; 
            newNode.direction = dir;

            localDownhillPoints.Add(newPos);
            localDownhillNodes.Add(newNode);
        }

        // 4) ScriptableObject에 저장
        if (saveDownhillToSO)
        {
            // (A) Vector3 리스트(DownhillPoints)
            pathDataSO.SetDownhillPoints(localDownhillPoints);

            // (B) DownhillNode 리스트(downhillNodes)
            pathDataSO.ClearDownhillNodes();
            pathDataSO.SetDownhillNodes(localDownhillNodes);

            Debug.Log($"[DownhillCourseGenerator] Downhill 변환 & SO 저장. count={localDownhillPoints.Count}");
        }
        else
        {
            Debug.Log($"[DownhillCourseGenerator] Downhill 변환만 완료 (SO 미저장). count={localDownhillPoints.Count}");
        }
    }

    /// <summary>
    /// cornerWindow 범위를 이용해, i 지점의 '코너 각도'를 계산
    /// </summary>
    private float ComputeCornerAngle(List<Vector3> curve, int i, int window)
    {
        int count = curve.Count;
        if (i >= 1)
        {
            Vector3 dirCenter = (curve[i] - curve[i - 1]).normalized;

            int leftIdx = i - window;
            if (leftIdx < 0) leftIdx = 0;
            int rightIdx= i + window;
            if (rightIdx >= count) rightIdx = count - 1;

            Vector3 dirLeft  = (curve[i] - curve[leftIdx]).normalized;
            Vector3 dirRight = (curve[rightIdx] - curve[i]).normalized;

            float angleL = Vector3.Angle(dirLeft,  dirCenter);
            float angleR = Vector3.Angle(dirCenter, dirRight);
            float cornerAngle = (angleL + angleR)*0.5f;
            return cornerAngle;
        }
        return 0f;
    }

    // OnDrawGizmos: Downhill 코스 시각화
    [SerializeField] private bool showGizmo;
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        // Downhill 코스 시각화 (Vector3 리스트 기준)
        List<Vector3> drawList;
        if (saveDownhillToSO && pathDataSO != null && pathDataSO.DownhillPoints != null)
        {
            drawList = pathDataSO.DownhillPoints;
        }
        else
        {
            drawList = localDownhillPoints;
        }

        if (drawList == null || drawList.Count < 2) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < drawList.Count - 1; i++)
        {
            Gizmos.DrawLine(drawList[i], drawList[i+1]);
        }

        // (선택) DownhillNode 정보 시각화 등...
    }
}
