using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor; // EditorUtility.SetDirty
#endif

/// <summary>
/// 1) pathDataSO.HeightAppliedPoints (List<GridNode>)에서
///    (i,j) grid 형식으로 삼각형 연결
/// 2) 위치/높이는 그대로 두고,
///    삼각형 인덱스 순서를 flipFaces 옵션에 따라 뒤집어
///    Face 방향(노멀)만 반전
/// 3) 결과 (compositeMeshVerts, compositeMeshTris)를 pathDataSO에 저장
/// 4) OnDrawGizmosSelected()에서 노란색 라인으로 표시
/// </summary>
public class CompositeMeshDataGenerator : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathDataSO;  

    [FoldoutGroup("Face Direction Settings"), Tooltip("면(노멀) 방향을 뒤집을지 여부")]
    public bool flipFaces = true;

    // 내부 캐싱: 메쉬 데이터
    private List<Vector3> localVerts = new List<Vector3>();
    private List<int>     localTris  = new List<int>();

    [FoldoutGroup("Actions")]
    [Button("Generate Composite Mesh from HeightAppliedPoints", ButtonSizes.Medium)]
    public void GenerateCompositeMeshData()
    {
        if (pathDataSO == null)
        {
            Debug.LogWarning("[CompositeMeshDataGenerator] pathDataSO가 없습니다. 중단.");
            return;
        }

        // 1) heightAppliedPoints (List<GridNode>) 읽기
        var gridNodes = pathDataSO.HeightAppliedPoints;
        if (gridNodes == null || gridNodes.Count == 0)
        {
            Debug.LogWarning("[CompositeMeshDataGenerator] heightAppliedPoints가 비어있습니다. 중단.");
            return;
        }

        // 2) (maxI, maxJ) 찾고, nodeDict[(i, j)] = node
        int maxI = 0, maxJ = 0;
        var nodeDict = new Dictionary<(int,int), GridNode>();
        foreach (var node in gridNodes)
        {
            if (node.i > maxI) maxI = node.i;
            if (node.j > maxJ) maxJ = node.j;
            nodeDict[(node.i, node.j)] = node;
        }

        // 3) localVerts, localTris 초기화
        localVerts.Clear();
        localTris.Clear();

        var indexMap = new Dictionary<(int,int), int>();

        // 4) (j=0..maxJ, i=0..maxI) 순으로 vertex 할당
        for (int jj=0; jj<= maxJ; jj++)
        {
            for (int ii=0; ii<= maxI; ii++)
            {
                if (!nodeDict.TryGetValue((ii, jj), out var n))
                {
                    indexMap[(ii, jj)] = -1;
                    continue;
                }
                // 위치/높이 그대로 추가
                int idx = localVerts.Count;
                localVerts.Add(n.position);
                indexMap[(ii, jj)] = idx;
            }
        }

        // 5) 삼각형 연결
        //    flipFaces=true면 인덱스 순서를 (i0,i2,i1) 식으로 바꿔서 면 뒤집음
        for (int jj=0; jj< maxJ; jj++)
        {
            for (int ii=0; ii< maxI; ii++)
            {
                int i0= indexMap[(ii,   jj)];
                int i1= indexMap[(ii+1, jj)];
                int i2= indexMap[(ii,   jj+1)];
                int i3= indexMap[(ii+1, jj+1)];

                if (i0<0 || i1<0 || i2<0 || i3<0)
                    continue;

                if (!flipFaces)
                {
                    // 기존 순서
                    localTris.Add(i0); localTris.Add(i1); localTris.Add(i2);
                    localTris.Add(i2); localTris.Add(i1); localTris.Add(i3);
                }
                else
                {
                    // 뒤집힌 순서(면 방향 반전)
                    // 삼각1: (i0, i2, i1)
                    localTris.Add(i0);
                    localTris.Add(i2);
                    localTris.Add(i1);

                    // 삼각2: (i2, i3, i1)
                    localTris.Add(i2);
                    localTris.Add(i3);
                    localTris.Add(i1);
                }
            }
        }

        // 6) PathDataSO에 저장
        pathDataSO.ClearCompositeMeshData();
        pathDataSO.SetCompositeMeshData(localVerts, localTris);

#if UNITY_EDITOR
        EditorUtility.SetDirty(pathDataSO);
#endif

        Debug.Log($"[CompositeMeshDataGenerator] Generate done. " +
                  $"totalVerts={localVerts.Count}, triCount={localTris.Count/3}, flipFaces={flipFaces}");
    }

    // =========================================================================
    // OnDrawGizmosSelected : compositeMeshVerts + compositeMeshTris => 노란 라인
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        if (pathDataSO == null) 
            return;

        var verts = pathDataSO.CompositeMeshVerts;
        var tris  = pathDataSO.CompositeMeshTris;
        if (verts==null || verts.Count<3) 
            return;
        if (tris==null  || tris.Count<3)  
            return;

        Gizmos.color = Color.yellow;
        for (int i=0; i<tris.Count; i+=3)
        {
            int i0= tris[i];
            int i1= tris[i+1];
            int i2= tris[i+2];

            if (i0<0|| i0>=verts.Count) continue;
            if (i1<0|| i1>=verts.Count) continue;
            if (i2<0|| i2>=verts.Count) continue;

            Vector3 a= verts[i0];
            Vector3 b= verts[i1];
            Vector3 c= verts[i2];
            Gizmos.DrawLine(a,b);
            Gizmos.DrawLine(b,c);
            Gizmos.DrawLine(c,a);
        }
    }
}
