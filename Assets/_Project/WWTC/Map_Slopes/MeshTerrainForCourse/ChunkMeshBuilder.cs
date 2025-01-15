using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor; 
#endif

/// <summary>
/// 1) pathDataSO.CompositeMeshVerts + pathDataSO.CompositeMeshTris, compositeMeshUV
///    를 읽어,
/// 2) 큰 메쉬를 chunkCountX * chunkCountZ 로 분할(삼각형 중심점 기준)하여,
/// 3) 각 Chunk별 SubMesh(GameObject) 생성.
/// 4) enableFlatShading에 따라 Smooth/Flat 셰이딩.
///    - Smooth: 중복 정점 재활용, UV도 동일 인덱스
///    - Flat: 삼각형마다 정점 3개 복제 + 노멀/UV도 복제
/// </summary>
public class ChunkMeshBuilder : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData; // compositeMeshVerts, compositeMeshTris, compositeMeshUV

    [Header("Chunk Container")]
    public Transform container; // Chunk 오브젝트를 담을 부모 (null이면 자기 자신)

    [FoldoutGroup("Settings")]
    [Tooltip("X방향 Chunk 개수")]
    public int chunkCountX = 4;

    [FoldoutGroup("Settings")]
    [Tooltip("Z방향 Chunk 개수")]
    public int chunkCountZ = 4;

    [FoldoutGroup("Settings")]
    [Tooltip("생성된 Chunk Mesh에 배정할 머티리얼(옵션)")]
    public Material chunkMaterial;

    [FoldoutGroup("Settings")]
    [Tooltip("플랫 셰이딩(=로우폴리) 적용 여부")]
    public bool enableFlatShading = false;

    [FoldoutGroup("Actions")]
    [Button("Build Chunks & Create Objects", ButtonSizes.Medium)]
    public void BuildAndCreateChunks()
    {
        if (pathData == null)
        {
            Debug.LogWarning("[ChunkMeshBuilder] PathDataSO가 없음. 중단.");
            return;
        }

        // (1) 메쉬 데이터 (verts, tris, uv)
        List<Vector3> allVerts = pathData.CompositeMeshVerts;
        List<int>     allTris  = pathData.CompositeMeshTris;
        List<Vector2> allUV    = pathData.CompositeMeshUV; 
        if(allVerts==null || allVerts.Count<3 
           || allTris==null  || allTris.Count<3
           || allUV==null    || allUV.Count!= allVerts.Count)
        {
            Debug.LogWarning("[ChunkMeshBuilder] CompositeMeshVerts/Tris/UV가 유효치 않거나 갯수 불일치.");
            return;
        }

        // (2) container 기본값
        if (container==null)
            container= this.transform;

        // (3) boundingRect
        Rect r= pathData.BoundingRect;
        if(r.width<=0f || r.height<=0f)
        {
            Debug.LogWarning("[ChunkMeshBuilder] boundingRect가 유효하지 않음.");
            return;
        }

        float chunkWidth = r.width/ chunkCountX;
        float chunkHeight= r.height/ chunkCountZ;

        int chunkTotal= chunkCountX* chunkCountZ;
        List<List<int>> chunkTrisList= new List<List<int>>(chunkTotal);
        for(int i=0; i< chunkTotal; i++)
            chunkTrisList.Add(new List<int>());

        // (A) 삼각형 분류
        for(int i=0; i< allTris.Count; i+=3)
        {
            int i0= allTris[i];
            int i1= allTris[i+1];
            int i2= allTris[i+2];
            if(i0<0|| i0>= allVerts.Count) continue;
            if(i1<0|| i1>= allVerts.Count) continue;
            if(i2<0|| i2>= allVerts.Count) continue;

            Vector3 v0= allVerts[i0];
            Vector3 v1= allVerts[i1];
            Vector3 v2= allVerts[i2];

            Vector3 center= (v0+ v1+ v2)/3f;

            float dx= center.x - r.xMin;
            float dz= center.z - r.yMin;
            if(dx<0f|| dz<0f|| dx> r.width|| dz> r.height)
                continue;

            int cx= Mathf.FloorToInt(dx/ chunkWidth);
            int cz= Mathf.FloorToInt(dz/ chunkHeight);

            if(cx<0) cx=0; 
            if(cx>= chunkCountX) cx= chunkCountX-1;
            if(cz<0) cz=0;
            if(cz>= chunkCountZ) cz= chunkCountZ-1;

            int cIndex= cz* chunkCountX+ cx;
            chunkTrisList[cIndex].Add(i0);
            chunkTrisList[cIndex].Add(i1);
            chunkTrisList[cIndex].Add(i2);
        }

        // (B) 기존 Chunk_ 오브젝트 제거
        List<Transform> oldChildren= new List<Transform>();
        for(int i=0; i< container.childCount; i++)
        {
            var c= container.GetChild(i);
            if(c.name.StartsWith("Chunk_"))
                oldChildren.Add(c);
        }
        foreach(var c in oldChildren)
        {
#if UNITY_EDITOR
            DestroyImmediate(c.gameObject);
#else
            Destroy(c.gameObject);
#endif
        }

        // (C) 각 Chunk => Build Mesh => GameObject
        for(int cz=0; cz< chunkCountZ; cz++)
        {
            for(int cx=0; cx< chunkCountX; cx++)
            {
                int cIdx= cz* chunkCountX+ cx;
                List<int> subTris= chunkTrisList[cIdx];
                if(subTris.Count<3) 
                    continue;

                Mesh subMesh;
                if(enableFlatShading)
                {
                    subMesh= BuildSubMeshFlat(allVerts, allUV, subTris);
                }
                else
                {
                    subMesh= BuildSubMesh(allVerts, allUV, subTris);
                }

                // GameObject
                GameObject go= new GameObject($"Chunk_{cx}_{cz}");
                go.transform.SetParent(container,false);

                // layer= "Ground"
                go.layer= LayerMask.NameToLayer("Ground");

                MeshFilter mf= go.AddComponent<MeshFilter>();
                MeshRenderer mr= go.AddComponent<MeshRenderer>();
                MeshCollider mc= go.AddComponent<MeshCollider>();

                mf.sharedMesh= subMesh;
                mc.sharedMesh= subMesh;
                mc.convex= false;

                if(chunkMaterial!= null)
                    mr.sharedMaterial= chunkMaterial;
            }
        }

        Debug.Log($"[ChunkMeshBuilder] Chunks 생성 완료. flatShading={enableFlatShading}");
    }

    // ------------------------------------------------------------------------
    // (Smooth) : 중복 정점 재활용 + UV도 재활용
    // ------------------------------------------------------------------------
    private Mesh BuildSubMesh(List<Vector3> allVerts, List<Vector2> allUV, List<int> subTris)
    {
        var vertMap= new Dictionary<int,int>();
        var subVerts= new List<Vector3>();
        var subUV= new List<Vector2>();
        var newTris= new List<int>(subTris.Count);

        for(int i=0; i< subTris.Count; i++)
        {
            int oriIdx= subTris[i];
            if(!vertMap.TryGetValue(oriIdx, out int newIdx))
            {
                newIdx= subVerts.Count;
                subVerts.Add( allVerts[oriIdx] );
                subUV.Add( allUV[oriIdx] ); // ← PathDataSO에 저장된 UV
                vertMap[oriIdx]= newIdx;
            }
            newTris.Add(newIdx);
        }

        Mesh mesh= new Mesh();
        mesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(subVerts);
        mesh.SetTriangles(newTris, 0);
        mesh.SetUVs(0, subUV);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ------------------------------------------------------------------------
    // (Flat) : 삼각형마다 새 정점 3개(면 분리) + UV도 복제
    // ------------------------------------------------------------------------
    private Mesh BuildSubMeshFlat(List<Vector3> allVerts, List<Vector2> allUV, List<int> subTris)
    {
        var subVerts   = new List<Vector3>(subTris.Count);
        var subNormals = new List<Vector3>(subTris.Count);
        var subUVs     = new List<Vector2>(subTris.Count);
        var newTris    = new List<int>(subTris.Count);

        for(int i=0; i< subTris.Count; i+=3)
        {
            int i0= subTris[i];
            int i1= subTris[i+1];
            int i2= subTris[i+2];
            if(i0<0|| i0>= allVerts.Count) continue;
            if(i1<0|| i1>= allVerts.Count) continue;
            if(i2<0|| i2>= allVerts.Count) continue;

            Vector3 v0= allVerts[i0];
            Vector3 v1= allVerts[i1];
            Vector3 v2= allVerts[i2];

            // Face normal
            Vector3 faceN= Vector3.Cross((v1-v0),(v2-v0)).normalized;

            int baseIdx= subVerts.Count;

            // V0
            subVerts.Add(v0);
            subNormals.Add(faceN);
            subUVs.Add(allUV[i0]); // ←원본 UV 복제

            // V1
            subVerts.Add(v1);
            subNormals.Add(faceN);
            subUVs.Add(allUV[i1]);

            // V2
            subVerts.Add(v2);
            subNormals.Add(faceN);
            subUVs.Add(allUV[i2]);

            newTris.Add(baseIdx);
            newTris.Add(baseIdx+1);
            newTris.Add(baseIdx+2);
        }

        Mesh mesh= new Mesh();
        mesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(subVerts);
        mesh.SetNormals(subNormals);
        mesh.SetUVs(0, subUVs);
        mesh.SetTriangles(newTris, 0);

        mesh.RecalculateBounds();
        return mesh;
    }
}
