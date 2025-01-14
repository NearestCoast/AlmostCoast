using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor; // for EditorUtility.SetDirty
#endif

/// <summary>
/// 1) pathDataSO.HeightAppliedPoints(List<GridNode>) => (maxI,maxJ) 격자
/// 2) 먼저 "코스 내부(isCourseArea==true)"만 간단 스무딩 처리(1회).
/// 3) 각 Quad((i,j)-(i+1,j+1)) 면적 → baseArea 대비 ratio = sqrt(area/baseArea), UV = (0..ratio)
/// 4) flipFaces=true면 삼각형 인덱스 뒤집어 면 방향 반전
/// 5) 최종 (verts, tris, uv) → pathDataSO에 저장
/// 6) OnDrawGizmosSelected()에서 노란 라인 시각화
/// </summary>
public class CompositeMeshDataGenerator : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathDataSO;

    [FoldoutGroup("Face Direction Settings"), Tooltip("면(노멀) 방향을 뒤집을지 여부")]
    public bool flipFaces = true;

    [FoldoutGroup("Smoothing Settings"), Tooltip("Course area를 스무딩할지 여부")]
    public bool doSmoothing = true;

    // 내부 캐싱
    private List<Vector3> localVerts = new List<Vector3>();
    private List<int>     localTris  = new List<int>();
    private List<Vector2> localUV    = new List<Vector2>();

    // "기준 쿼드" 면적 (처음 만난 유효 셀의 면적)
    private float baseArea = -1f;

    [FoldoutGroup("Actions")]
    [Button("Generate Composite Mesh (Course smoothing + AreaProportionalUV)")]
    public void GenerateCompositeMeshData()
    {
        if (pathDataSO == null)
        {
            Debug.LogWarning("[CompositeMeshDataGenerator] pathDataSO가 없음. 중단.");
            return;
        }

        var gridNodes = pathDataSO.HeightAppliedPoints;
        if (gridNodes == null || gridNodes.Count == 0)
        {
            Debug.LogWarning("[CompositeMeshDataGenerator] heightAppliedPoints가 비어있음. 중단.");
            return;
        }

        // -------------------
        // (A) (maxI, maxJ) + nodeDict
        // -------------------
        int maxI=0, maxJ=0;
        var nodeDict = new Dictionary<(int,int), GridNode>();
        foreach(var nd in gridNodes)
        {
            if(nd.i> maxI) maxI= nd.i;
            if(nd.j> maxJ) maxJ= nd.j;
            nodeDict[(nd.i, nd.j)] = nd;
        }

        // -------------------
        // (B) Course 스무딩 (옵션)
        // -------------------
        if(doSmoothing)
        {
            // 간단히 "course area인 노드만" 상하좌우(course area인) 노드와 평균
            // 1회 적용(더 부드럽게 하려면 반복 가능)
            var newPositions = new Vector3[gridNodes.Count];
            // 우선 전부 복사
            for(int idx=0; idx< gridNodes.Count; idx++)
                newPositions[idx] = gridNodes[idx].position;

            // 각 node별 처리
            for(int idx=0; idx< gridNodes.Count; idx++)
            {
                var nd= gridNodes[idx];
                if(!nd.isCourseArea) 
                {
                    // 다운힐은 높이 그대로
                    continue;
                }

                // 코스 노드 => 자기 자신 + 인접 course 노드 평균
                int i= nd.i;
                int j= nd.j;
                Vector3 sumPos= nd.position;
                int count= 1;

                // 상하좌우(4방향) or 8방향 중 선택 (여기선 4방)
                var neighborOffsets= new (int,int)[]{
                    (0,1), (0,-1), (1,0), (-1,0)
                };
                foreach(var off in neighborOffsets)
                {
                    int ni= i+ off.Item1;
                    int nj= j+ off.Item2;
                    if(nodeDict.TryGetValue((ni,nj), out var n2))
                    {
                        if(n2.isCourseArea)
                        {
                            sumPos += n2.position;
                            count++;
                        }
                    }
                }
                var avgPos= sumPos / count;
                newPositions[idx]= avgPos; 
            }

            // 스무딩 결과 반영
            for(int idx=0; idx< gridNodes.Count; idx++)
            {
                if(gridNodes[idx].isCourseArea)
                {
                    gridNodes[idx].position= newPositions[idx];
                }
            }
        }

        // -------------------
        // (C) 이제 기존처럼 Quad 로직
        // -------------------
        localVerts.Clear();
        localTris.Clear();
        localUV.Clear();
        baseArea= -1f;

        for(int j=0; j< maxJ; j++)
        {
            for(int i=0; i< maxI; i++)
            {
                if(!nodeDict.TryGetValue((i,j),     out var n0)) continue;
                if(!nodeDict.TryGetValue((i+1,j),   out var n1)) continue;
                if(!nodeDict.TryGetValue((i,j+1),   out var n2)) continue;
                if(!nodeDict.TryGetValue((i+1,j+1), out var n3)) continue;

                var qPos= new Vector3[]{ n0.position, n1.position, n2.position, n3.position };

                float qArea= EstimateQuadArea(qPos);
                if(baseArea<0f) baseArea= qArea; // 첫 쿼드 면적
                float ratio= 1f;
                if(baseArea> 0f && qArea> 0f)
                {
                    ratio = Mathf.Sqrt(qArea / baseArea);
                }

                // UV
                Vector2[] qUV = {
                    new Vector2(0f, 0f),
                    new Vector2(ratio, 0f),
                    new Vector2(0f, ratio),
                    new Vector2(ratio, ratio)
                };

                AddQuad(qPos, qUV);
            }
        }

        // (D) 저장
        pathDataSO.ClearCompositeMeshData();
        pathDataSO.SetCompositeMeshData(localVerts, localTris);

        pathDataSO.ClearCompositeMeshUV();
        pathDataSO.SetCompositeMeshUV(localUV);

#if UNITY_EDITOR
        EditorUtility.SetDirty(pathDataSO);
#endif

        Debug.Log($"[CompositeMeshDataGenerator] done. smoothing={doSmoothing}, flipFaces={flipFaces}");
    }

    // ============================
    // Quad -> localVerts/localTris/localUV
    // ============================
    private void AddQuad(Vector3[] qPos, Vector2[] qUV)
    {
        int baseIdx= localVerts.Count;
        for(int i=0; i<4; i++)
        {
            localVerts.Add(qPos[i]);
            localUV.Add(qUV[i]);
        }

        int i0= baseIdx;
        int i1= baseIdx+1;
        int i2= baseIdx+2;
        int i3= baseIdx+3;

        if(!flipFaces)
        {
            localTris.Add(i0); localTris.Add(i1); localTris.Add(i2);
            localTris.Add(i2); localTris.Add(i1); localTris.Add(i3);
        }
        else
        {
            // flip winding
            localTris.Add(i0); localTris.Add(i2); localTris.Add(i1);
            localTris.Add(i2); localTris.Add(i3); localTris.Add(i1);
        }
    }

    // ============================
    // Quad 면적
    // ============================
    private float EstimateQuadArea(Vector3[] cPos)
    {
        if(cPos==null || cPos.Length<4) return 0f;

        // tri1: (0,1,2)
        float a1= Vector3.Cross(cPos[1]-cPos[0], cPos[2]-cPos[0]).magnitude *0.5f;
        // tri2: (2,1,3)
        float a2= Vector3.Cross(cPos[1]-cPos[2], cPos[3]-cPos[2]).magnitude *0.5f;
        return a1+a2;
    }

    // ============================
    // Gizmo 시각화
    // ============================
    private void OnDrawGizmosSelected()
    {
        if(pathDataSO==null) return;
        var verts= pathDataSO.CompositeMeshVerts;
        var tris=  pathDataSO.CompositeMeshTris;
        if(verts==null|| verts.Count<3) return;
        if(tris==null || tris.Count<3)  return;

        Gizmos.color= Color.yellow;
        for(int i=0; i< tris.Count; i+=3)
        {
            int i0= tris[i];
            int i1= tris[i+1];
            int i2= tris[i+2];
            if(i0<0|| i0>= verts.Count) continue;
            if(i1<0|| i1>= verts.Count) continue;
            if(i2<0|| i2>= verts.Count) continue;

            Vector3 a= verts[i0];
            Vector3 b= verts[i1];
            Vector3 c= verts[i2];
            Gizmos.DrawLine(a,b);
            Gizmos.DrawLine(b,c);
            Gizmos.DrawLine(c,a);
        }
    }
}
