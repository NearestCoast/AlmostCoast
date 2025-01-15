using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor; // for EditorUtility.SetDirty
#endif

/// <summary>
/// 1) pathDataSO.HeightAppliedPoints(List<GridNode>) => (maxI,maxJ) 격자
/// 2) 각 (i,j)-(i+1,j+1) Quad에 대해:
///    - 면적(첫 셀의 baseArea 대비 1.5배↑) or 높이 차(heightDiffThreshold↑)이면 재귀 subdiv
/// 3) flipFaces=true면 삼각형 인덱스 순서를 뒤집어 면 방향만 반전
/// 4) 최종 (verts, tris, uv) => pathDataSO에 저장
///
/// 주의:
///   - 인접 셀과 경계가 어긋날 수 있음(간단 예시).
///   - 구멍 없이 완벽히 연결하려면 인접 Patch Re-Triangulation 등 필요.
/// </summary>
public class SubdividedMeshDataGenerator : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathDataSO;

    [FoldoutGroup("Subdivision Settings"), Tooltip("높이 차 threshold")]
    public float heightDiffThreshold = 2f;

    [FoldoutGroup("Subdivision Settings"), Tooltip("높이 차 검사 사용 여부")]
    public bool checkHeightDiff = true;

    [FoldoutGroup("Face Direction Settings"), Tooltip("면(노멀) 방향 뒤집기 여부")]
    public bool flipFaces = true;

    // ----------------------
    // 내부 보관
    // ----------------------
    private List<Vector3> localVerts = new List<Vector3>();
    private List<int>     localTris  = new List<int>();
    private List<Vector2> localUV    = new List<Vector2>();

    // "기준 셀" 면적 (baseArea): 첫 번째 유효 셀을 만났을 때 계산
    private float baseArea = -1f;  // 아직 미정

    [FoldoutGroup("Actions")]
    [Button("Generate Subdiv Mesh (Adaptive Quad)")]
    public void GenerateCompositeMeshData()
    {
        // 0) 체크
        if (pathDataSO == null)
        {
            Debug.LogWarning("[SubdividedMeshDataGenerator] pathDataSO가 없음. 중단.");
            return;
        }
        var gridNodes = pathDataSO.HeightAppliedPoints;
        if (gridNodes == null || gridNodes.Count==0)
        {
            Debug.LogWarning("[SubdividedMeshDataGenerator] heightAppliedPoints가 비어있음.");
            return;
        }

        // (A) (maxI, maxJ) + nodeDict
        int maxI=0, maxJ=0;
        var nodeDict= new Dictionary<(int,int), GridNode>();
        foreach(var nd in gridNodes)
        {
            if(nd.i> maxI) maxI= nd.i;
            if(nd.j> maxJ) maxJ= nd.j;
            nodeDict[(nd.i, nd.j)] = nd;
        }

        // (B) 초기화
        localVerts.Clear();
        localTris.Clear();
        localUV.Clear();
        baseArea= -1f; // 아직 측정 안 됨

        // (C) 각 (i,j)~(i+1,j+1) Quad => Adaptive Subdiv
        for(int j=0; j< maxJ; j++)
        {
            for(int i=0; i< maxI; i++)
            {
                // 4 corners
                if(!nodeDict.TryGetValue((i,j),   out var n0)) continue;
                if(!nodeDict.TryGetValue((i+1,j), out var n1)) continue;
                if(!nodeDict.TryGetValue((i,j+1), out var n2)) continue;
                if(!nodeDict.TryGetValue((i+1,j+1), out var n3)) continue;

                Vector3[] cornersPos= new Vector3[4] { n0.position, n1.position, n2.position, n3.position };
                Vector2[] cornersUV = new Vector2[4] {
                    new Vector2(0,0),
                    new Vector2(1,0),
                    new Vector2(0,1),
                    new Vector2(1,1)
                };

                SubdivQuad(cornersPos, cornersUV);
            }
        }

        // (D) 결과 -> PathDataSO
        pathDataSO.ClearCompositeMeshData();
        pathDataSO.SetCompositeMeshData(localVerts, localTris);

        pathDataSO.ClearCompositeMeshUV();
        pathDataSO.SetCompositeMeshUV(localUV);

#if UNITY_EDITOR
        EditorUtility.SetDirty(pathDataSO);
#endif

        Debug.Log($"[SubdividedMeshDataGenerator] done. " +
                  $"verts={localVerts.Count}, triCount={localTris.Count/3}, flip={flipFaces}");
    }

    /// <summary>
    /// Quad -> subdiv
    /// if area> (1.5×baseArea) or (heightDiff> heightDiffThreshold) => subdiv
    /// else => 2 triangles
    /// </summary>
    private void SubdivQuad(Vector3[] cPos, Vector2[] cUV)
    {
        // 면적
        float area= EstimateQuadArea(cPos);

        // 아직 baseArea가 미정(-1f)이면, 처음 만난 유효 셀로 설정
        if(baseArea< 0f)
        {
            baseArea= area;
        }

        bool doSubdiv= false;

        // (1) area check
        if(area> 1.5f* baseArea)
        {
            doSubdiv= true;
        }

        // (2) height diff check
        if(checkHeightDiff)
        {
            float minY= float.MaxValue;
            float maxY= float.MinValue;
            for(int i=0; i<4; i++)
            {
                float yy= cPos[i].y;
                if(yy< minY) minY=yy;
                if(yy> maxY) maxY=yy;
            }
            float diff= maxY- minY;
            if(diff> heightDiffThreshold)
                doSubdiv= true;
        }

        if(!doSubdiv)
        {
            // subdiv 불필요 => AddQuad
            AddQuad(cPos, cUV);
        }
        else
        {
            // 4-subQuad
            Vector3 centerPos= (cPos[0]+ cPos[1]+ cPos[2]+ cPos[3]) * 0.25f;
            Vector2 centerUV=  (cUV[0]+ cUV[1]+ cUV[2]+ cUV[3]) * 0.25f;

            // mid horizontal
            Vector3 midTop   = (cPos[0]+ cPos[1])*0.5f;
            Vector3 midBot   = (cPos[2]+ cPos[3])*0.5f;
            Vector2 midTopUV = (cUV[0] + cUV[1])*0.5f;
            Vector2 midBotUV = (cUV[2] + cUV[3])*0.5f;

            // mid vertical
            Vector3 midLeft  = (cPos[0]+ cPos[2])*0.5f;
            Vector3 midRight = (cPos[1]+ cPos[3])*0.5f;
            Vector2 midLeftUV= (cUV[0] + cUV[2])*0.5f;
            Vector2 midRightUV= (cUV[1] + cUV[3])*0.5f;

            // subQuad #1
            SubdivQuad(
                new Vector3[]{ cPos[0], midTop, midLeft, centerPos },
                new Vector2[]{ cUV[0], midTopUV, midLeftUV, centerUV}
            );
            // subQuad #2
            SubdivQuad(
                new Vector3[]{ midTop, cPos[1], centerPos, midRight },
                new Vector2[]{ midTopUV, cUV[1], centerUV, midRightUV}
            );
            // subQuad #3
            SubdivQuad(
                new Vector3[]{ midLeft, centerPos, cPos[2], midBot },
                new Vector2[]{ midLeftUV, centerUV, cUV[2], midBotUV}
            );
            // subQuad #4
            SubdivQuad(
                new Vector3[]{ centerPos, midRight, midBot, cPos[3] },
                new Vector2[]{ centerUV, midRightUV, midBotUV, cUV[3]}
            );
        }
    }

    /// <summary>
    /// Quad => 2 Triangles => localVerts/localTris/localUV
    /// flipFaces => winding 반전
    /// </summary>
    private void AddQuad(Vector3[] cPos, Vector2[] cUV)
    {
        int baseIdx= localVerts.Count;
        for(int i=0; i<4; i++)
        {
            localVerts.Add(cPos[i]);
            localUV.Add(cUV[i]);
        }
        int i0= baseIdx;
        int i1= baseIdx+1;
        int i2= baseIdx+2;
        int i3= baseIdx+3;

        if(!flipFaces)
        {
            // tri1: (i0,i1,i2), tri2: (i2,i1,i3)
            localTris.Add(i0); localTris.Add(i1); localTris.Add(i2);
            localTris.Add(i2); localTris.Add(i1); localTris.Add(i3);
        }
        else
        {
            // flip
            localTris.Add(i0); localTris.Add(i2); localTris.Add(i1);
            localTris.Add(i2); localTris.Add(i3); localTris.Add(i1);
        }
    }

    // Quad 면적
    private float EstimateQuadArea(Vector3[] cPos)
    {
        float a1= Vector3.Cross(cPos[1]-cPos[0], cPos[2]-cPos[0]).magnitude*0.5f;
        float a2= Vector3.Cross(cPos[1]-cPos[2], cPos[3]-cPos[2]).magnitude*0.5f;
        return a1+a2;
    }

    // ========================================================================
    // OnDrawGizmosSelected
    // ========================================================================
    private void OnDrawGizmosSelected()
    {
        if(pathDataSO==null) return;

        var verts= pathDataSO.CompositeMeshVerts;
        var tris=  pathDataSO.CompositeMeshTris;
        if(verts==null || verts.Count<3) return;
        if(tris==null  || tris.Count<3)  return;

        Gizmos.color= Color.yellow;
        for(int i=0; i< tris.Count; i+=3)
        {
            int i0= tris[i];
            int i1= tris[i+1];
            int i2= tris[i+2];
            if(i0<0|| i0>=verts.Count) continue;
            if(i1<0|| i1>=verts.Count) continue;
            if(i2<0|| i2>=verts.Count) continue;

            Vector3 a= verts[i0];
            Vector3 b= verts[i1];
            Vector3 c= verts[i2];
            Gizmos.DrawLine(a,b);
            Gizmos.DrawLine(b,c);
            Gizmos.DrawLine(c,a);
        }
    }
}
