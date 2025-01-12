using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 1) PathDataSO.compositeMeshVerts, .compositeMeshTris를 읽어
/// 2) 각 삼각형마다 정점을 "복제"하여 면 단위로 분리(Flat Shading 스타일)
/// 3) 결과(lowpolyVerts, lowpolyTris)를 PathDataSO에 저장
/// 4) OnDrawGizmosSelected()에서 다른 색으로 표시(예: 하늘색)
/// </summary>
public class LowpolyMeshDataConverter : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathDataSO;

    // 내부 캐싱
    private List<Vector3> lowpolyVerts = new List<Vector3>();
    private List<int>     lowpolyTris  = new List<int>();

    // === OnDrawGizmos에서 시각화할지 여부 ===
    [FoldoutGroup("Gizmo"), Tooltip("Gizmos로 로우폴리 메쉬 표시")]
    public bool showGizmo = true;

    [FoldoutGroup("Gizmo"), ColorPalette]
    public Color lowpolyColor = new Color(0f, 1f, 1f, 1f); // 예: 하늘색

    [FoldoutGroup("Actions")]
    [Button("Convert to LowPoly", ButtonSizes.Medium)]
    public void ConvertToLowPoly()
    {
        if(pathDataSO == null)
        {
            Debug.LogWarning("[LowpolyMeshDataConverter] pathDataSO가 없음. 중단.");
            return;
        }
        var origVerts = pathDataSO.CompositeMeshVerts;
        var origTris  = pathDataSO.CompositeMeshTris;
        if(origVerts == null || origVerts.Count <3 || origTris == null || origTris.Count<3)
        {
            Debug.LogWarning("[LowpolyMeshDataConverter] CompositeMeshVerts/Tris가 유효치 않음.");
            return;
        }

        // 1) 결과 리스트 초기화
        lowpolyVerts.Clear();
        lowpolyTris.Clear();

        // 2) 각 삼각형마다 "새로운 정점 3개" 생성
        //    => 면마다 고유 정점 => flat shading
        for(int i=0; i< origTris.Count; i+=3)
        {
            int i0= origTris[i];
            int i1= origTris[i+1];
            int i2= origTris[i+2];

            // 검증
            if(i0<0 || i0>= origVerts.Count) continue;
            if(i1<0 || i1>= origVerts.Count) continue;
            if(i2<0 || i2>= origVerts.Count) continue;

            Vector3 v0= origVerts[i0];
            Vector3 v1= origVerts[i1];
            Vector3 v2= origVerts[i2];

            // subVerts: v0,v1,v2 => 순서대로 추가
            int baseIdx= lowpolyVerts.Count; // 이번 삼각형의 시작 인덱스
            lowpolyVerts.Add(v0);
            lowpolyVerts.Add(v1);
            lowpolyVerts.Add(v2);

            // subTris: (baseIdx+0, baseIdx+1, baseIdx+2)
            lowpolyTris.Add(baseIdx);
            lowpolyTris.Add(baseIdx+1);
            lowpolyTris.Add(baseIdx+2);
        }

        // 3) PathDataSO에 저장 (새로운 필드가 필요하다면 거기에 저장)
        //    여기서는 "compositeMeshVerts / compositeMeshTris"를 덮어쓰거나,
        //    또는 "lowpolyMeshVerts / lowpolyMeshTris" 같은 필드를 만들 수도 있음.
        //    일단 여기서는 예시로 "compositeMeshVerts/Tris"를 덮어쓰지 않고,
        //    pathDataSO.SetCompositeMeshData(...)하면 "원본"이 사라지므로,
        //    => 별도 메서드가 있다고 가정(혹은 원래 필드를 덮어써도 괜찮다면 그렇게)
        pathDataSO.ClearCompositeMeshData(); // 예: 덮어쓰고 싶다면
        pathDataSO.SetCompositeMeshData(lowpolyVerts, lowpolyTris);

#if UNITY_EDITOR
        EditorUtility.SetDirty(pathDataSO);
#endif

        Debug.Log($"[LowpolyMeshDataConverter] LowPoly 변환 완료. newVerts={lowpolyVerts.Count}, newTris={lowpolyTris.Count/3}");
    }

    // =========================================================================
    // OnDrawGizmosSelected : lowpolyVerts / lowpolyTris 시각화 (option)
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        if(!showGizmo) return;

        if(lowpolyVerts == null || lowpolyVerts.Count<3) return;
        if(lowpolyTris  == null || lowpolyTris.Count<3)  return;

        Gizmos.color= lowpolyColor; // 하늘색

        for(int i=0; i< lowpolyTris.Count; i+=3)
        {
            int i0= lowpolyTris[i];
            int i1= lowpolyTris[i+1];
            int i2= lowpolyTris[i+2];
            if(i0<0|| i0>= lowpolyVerts.Count) continue;
            if(i1<0|| i1>= lowpolyVerts.Count) continue;
            if(i2<0|| i2>= lowpolyVerts.Count) continue;

            Vector3 a= lowpolyVerts[i0];
            Vector3 b= lowpolyVerts[i1];
            Vector3 c= lowpolyVerts[i2];

            Gizmos.DrawLine(a,b);
            Gizmos.DrawLine(b,c);
            Gizmos.DrawLine(c,a);
        }
    }
}
