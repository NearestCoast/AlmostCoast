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
/// 
/// + Flat Shading( enableFlatShading ) 옵션 추가.
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

    [FoldoutGroup("Settings"), Tooltip("플랫 셰이딩(=로우폴리) 적용 여부")]
    public bool enableFlatShading = false;

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

                // mesh 생성 (Flat vs Smooth)
                Mesh subMesh;
                if (enableFlatShading)
                {
                    // flat shading
                    subMesh= BuildSubMeshFlat(verts, subTris);
                }
                else
                {
                    // 기존 방식
                    subMesh= BuildSubMesh(verts, subTris);
                }

                // GameObject
                GameObject go= new GameObject($"Chunk_{cx}_{cz}");
                go.transform.SetParent(container, false);

                // layer= "Ground"
                go.layer = LayerMask.NameToLayer("Ground");

                // 컴포넌트
                MeshFilter mf= go.AddComponent<MeshFilter>();
                MeshRenderer mr= go.AddComponent<MeshRenderer>();
                MeshCollider mc= go.AddComponent<MeshCollider>();

                // mesh 할당
                mf.sharedMesh= subMesh;
                mc.sharedMesh= subMesh;
                mc.convex= false; // 필요 시 설정

                // 머티리얼 설정 (옵션)
                if (chunkMaterial != null)
                {
                    mr.sharedMaterial = chunkMaterial;
                }
            }
        }

        Debug.Log($"[ChunkMeshBuilder] Chunks 생성 완료 (flatShading={enableFlatShading}).");
    }

    /// <summary>
    /// 부분 삼각형 인덱스(subTris)를 모아, subMesh를 만든다 (기존 스무스 셰이딩 방식).
    /// vertices는 전체 공유.
    /// </summary>
    private Mesh BuildSubMesh(List<Vector3> allVerts, List<int> subTris)
    {
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

        Mesh mesh= new Mesh();
        mesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(subVerts);
        mesh.SetTriangles(newTris, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 부분 삼각형 인덱스(subTris)를 모아, subMesh를 만든다 (Flat Shading).
    /// 각 삼각형마다 정점을 "복제"하여, 면 단위 노멀.
    /// => Low-Poly 느낌 구현.
    /// </summary>
    private Mesh BuildSubMeshFlat(List<Vector3> allVerts, List<int> subTris)
    {
        // 예: subTris = [ i0,i1,i2, i3,i4,i5, ... ]
        // 삼각형 하나마다 정점 3개씩 독립 생성 => total newTris.length/3 * 3 정점
        List<Vector3> subVerts = new List<Vector3>(subTris.Count);
        List<Vector3> subNormals = new List<Vector3>(subTris.Count);
        List<int>     newTris = new List<int>(subTris.Count);

        for (int i=0; i< subTris.Count; i+=3)
        {
            int i0= subTris[i];
            int i1= subTris[i+1];
            int i2= subTris[i+2];

            Vector3 v0= allVerts[i0];
            Vector3 v1= allVerts[i1];
            Vector3 v2= allVerts[i2];

            // face normal
            Vector3 faceNormal= Vector3.Cross((v1-v0), (v2-v0)).normalized;

            // 이 삼각형을 위해 독립된 정점 3개 추가
            int baseIdx= subVerts.Count;

            subVerts.Add(v0);
            subVerts.Add(v1);
            subVerts.Add(v2);

            // 노멀 동일
            subNormals.Add(faceNormal);
            subNormals.Add(faceNormal);
            subNormals.Add(faceNormal);

            // 삼각형 인덱스
            newTris.Add(baseIdx);
            newTris.Add(baseIdx+1);
            newTris.Add(baseIdx+2);
        }

        Mesh mesh= new Mesh();
        mesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(subVerts);
        mesh.SetNormals(subNormals);
        mesh.SetTriangles(newTris, 0);

        // flat shading => 이미 노멀을 직접 설정. RecalculateNormals 생략 or 필요시 Bounds만
        mesh.RecalculateBounds();

        return mesh;
    }
}
