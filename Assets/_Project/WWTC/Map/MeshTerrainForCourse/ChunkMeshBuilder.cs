using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor; 
#endif

/// <summary>
/// 1) pathDataSO.CompositeMeshVerts + pathDataSO.CompositeMeshTris 로부터
///    큰 메쉬를 chunkCountX*chunkCountZ 로 나눠 각각 별도 Mesh 생성
/// 2) 만들어진 Mesh를 GameObject로 만들어 container(Transform) 아래에 배치
/// 
/// * 부분 교차는 삼각형 중점(center)이 어느 Chunk인가로 분류(간단화).
/// * 더 정교한 분할을 원하면 삼각형 단위 재클리핑 로직 구현 필요.
/// </summary>
public class ChunkMeshBuilder : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData; // CompositeMeshVerts, CompositeMeshTris를 보유

    [Header("Chunk Container")]
    [Tooltip("생성된 Chunk 오브젝트들을 담을 부모 Transform (null이면 자기 자신)")]
    public Transform container;

    [FoldoutGroup("Settings"), Tooltip("X방향 Chunk 개수")]
    public int chunkCountX = 4;

    [FoldoutGroup("Settings"), Tooltip("Z방향 Chunk 개수")]
    public int chunkCountZ = 4;

    [FoldoutGroup("Settings"), Tooltip("Chunk Mesh에 할당할 머티리얼 (옵션)")]
    public Material chunkMaterial;

    // 내부 캐싱: 메쉬 데이터
    private List<Vector3> localVerts = new List<Vector3>();
    private List<int>     localTris  = new List<int>();

    [FoldoutGroup("Actions")]
    [Button("Build Chunks & Create Objects", ButtonSizes.Medium)]
    public void BuildAndCreateChunks()
    {
        if (pathData == null)
        {
            Debug.LogWarning("[ChunkMeshBuilder] pathDataSO가 없습니다. 중단.");
            return;
        }

        // (1) 큰 메쉬 데이터
        List<Vector3> verts = pathData.CompositeMeshVerts;
        List<int>     tris  = pathData.CompositeMeshTris;
        if(verts==null || verts.Count<3 || tris==null || tris.Count<3)
        {
            Debug.LogWarning("[ChunkMeshBuilder] CompositeMeshVerts or CompositeMeshTris가 유효치 않음.");
            return;
        }

        // (2) container 기본값
        if (container == null) 
            container = this.transform;

        // (3) boundingRect
        Rect r = pathData.BoundingRect;
        if (r.width<=0f || r.height<=0f)
        {
            Debug.LogWarning("[ChunkMeshBuilder] boundingRect가 유효하지 않음.");
            return;
        }

        // (4) chunk 크기
        float chunkWidth  = r.width / chunkCountX;
        float chunkHeight = r.height/ chunkCountZ;

        // 각 Chunk별 -> tris 목록
        int chunkTotal = chunkCountX * chunkCountZ;
        List<List<int>> chunkTrisList = new List<List<int>>(chunkTotal);
        for(int i=0; i< chunkTotal; i++)
        {
            chunkTrisList.Add(new List<int>());
        }

        // (5) 삼각형을 돌며 "중심점"이 어느 chunk에 속하는지 찾음 (간단화)
        for(int i=0; i< tris.Count; i+=3)
        {
            int i0= tris[i];
            int i1= tris[i+1];
            int i2= tris[i+2];
            if(i0<0||i0>=verts.Count) continue;
            if(i1<0||i1>=verts.Count) continue;
            if(i2<0||i2>=verts.Count) continue;

            Vector3 v0= verts[i0];
            Vector3 v1= verts[i1];
            Vector3 v2= verts[i2];
            Vector3 center= (v0 + v1 + v2)/3f;

            // boundingRect.xMin,yMin => 0,0 기준
            float dx= center.x - r.xMin;
            float dz= center.z - r.yMin;
            if(dx<0f || dz<0f || dx> r.width || dz> r.height)
            {
                // boundingRect 밖 => skip
                continue;
            }

            int chunkX= Mathf.FloorToInt(dx / chunkWidth);
            int chunkZ= Mathf.FloorToInt(dz / chunkHeight);

            // clamp
            if(chunkX<0) chunkX=0; 
            if(chunkX>= chunkCountX) chunkX= chunkCountX-1;
            if(chunkZ<0) chunkZ=0;
            if(chunkZ>= chunkCountZ) chunkZ= chunkCountZ-1;

            int chunkIndex= chunkZ*chunkCountX + chunkX;

            // 해당 chunkTrisList에 (i0,i1,i2) 추가
            chunkTrisList[chunkIndex].Add(i0);
            chunkTrisList[chunkIndex].Add(i1);
            chunkTrisList[chunkIndex].Add(i2);
        }

        // (6) 기존 Chunk 오브젝트 제거
        List<Transform> oldList= new List<Transform>();
        for(int i=0; i< container.childCount; i++)
        {
            var c= container.GetChild(i);
            if(c.name.StartsWith("Chunk_"))
                oldList.Add(c);
        }
        foreach(var c in oldList)
        {
#if UNITY_EDITOR
            DestroyImmediate(c.gameObject);
#else
            Destroy(c.gameObject);
#endif
        }

        // (7) 각 chunk에 대해 Mesh 생성 => GameObject => container 자식
        for(int cz=0; cz< chunkCountZ; cz++)
        {
            for(int cx=0; cx< chunkCountX; cx++)
            {
                int chunkIdx= cz* chunkCountX + cx;
                List<int> subTris= chunkTrisList[chunkIdx];
                if(subTris.Count<3)
                {
                    // 삼각형이 없음 => skip
                    continue;
                }
                // mesh
                Mesh subMesh= BuildSubMesh(verts, subTris);

                // GameObject
                GameObject go= new GameObject($"Chunk_{cx}_{cz}");
                go.transform.SetParent(container, false);

                MeshFilter mf= go.AddComponent<MeshFilter>();
                MeshRenderer mr= go.AddComponent<MeshRenderer>();
                mf.sharedMesh= subMesh;

                // 머티리얼 설정 (옵션)
                if (chunkMaterial != null)
                {
                    mr.sharedMaterial = chunkMaterial;
                }
            }
        }

        Debug.Log("[ChunkMeshBuilder] Chunks 생성 완료");
    }

    /// <summary>
    /// 부분 삼각형 인덱스(subTris)를 모아, subMesh를 만든다.
    /// vertices는 전체 공유
    ///   1) subVerts / subIndices 로 변환 (빈도 낮은 경우) or
    ///   2) 그냥 same vertex array (vertexCount== pathDataSO.CompositeMeshVerts.Count)
    ///        => But subTris는 bounding. 
    /// 여기서는 1)번 방식을 시연
    /// </summary>
    private Mesh BuildSubMesh(List<Vector3> allVerts, List<int> subTris)
    {
        // (A) subVerts, subIndex
        Dictionary<int,int> vertMap= new Dictionary<int,int>();
        List<Vector3> subVerts= new List<Vector3>();
        List<int>     newTris= new List<int>(subTris.Count);

        for(int i=0; i< subTris.Count; i++)
        {
            int oriIdx= subTris[i];
            if(!vertMap.TryGetValue(oriIdx, out int newIdx))
            {
                newIdx= subVerts.Count;
                subVerts.Add( allVerts[oriIdx] );
                vertMap[oriIdx]= newIdx;
            }
            newTris.Add(newIdx);
        }

        // (B) Mesh 생성
        Mesh mesh= new Mesh();
        mesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32; // 큰 경우 대비
        mesh.SetVertices(subVerts);
        mesh.SetTriangles(newTris, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
