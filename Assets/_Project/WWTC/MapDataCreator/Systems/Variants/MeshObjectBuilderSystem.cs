using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor; // AssetDatabase
#endif

/// <summary>
/// 1) MapDataSO.generatedMeshList를 참조하여 Scene에 Mesh 오브젝트 생성
/// 2) 정점 수가 많을 경우, 여러 Chunk로 분할
/// 3) 생성된 오브젝트는 "containerObject" 하위로 들어감
/// 4) 기존 container 자식은 모두 제거 후 새로 만듦
/// 5) (옵션) 생성된 Mesh를 특정 폴더에 .asset 으로 저장
/// </summary>
public class MeshObjectBuilderSystem : MapDataSystem
{
    [FoldoutGroup("Mesh Object Settings")]
    [SerializeField, Tooltip("Scene에서 직접 지정할 컨테이너 GameObject")]
    private GameObject containerObject;

    [FoldoutGroup("Mesh Object Settings")]
    [SerializeField, Tooltip("MeshRenderer에 적용할 기본 머티리얼")]
    private Material defaultMaterial;

    [FoldoutGroup("Mesh Object Settings")]
    [SerializeField, Tooltip("하나의 Mesh가 가질 수 있는 최대 정점 수(16비트 인덱스). 65535 이하")]
    private int maxVerticesPerMesh = 65000;

    // ────────────────────────────────────────────────────────
    // 추가: Mesh를 Asset으로 저장할지 여부, 폴더 경로
    // ────────────────────────────────────────────────────────
    [FoldoutGroup("Asset Save Settings")]
    [SerializeField, Tooltip("생성된 Mesh를 에셋으로 저장할지 여부")]
    private bool saveMeshesAsAssets = false;

    [FoldoutGroup("Asset Save Settings"), 
     FolderPath(AbsolutePath = false), 
     Tooltip("에셋을 저장할 폴더(예: 'Assets/MyMeshes')")]
    [SerializeField] 
    private string folderPath = "Assets";

    public override void Generate()
    {
        base.Generate();
        if (!IsReady) return; // mapDataCreator != null && CurrentMapData != null

        var mapData = mapDataCreator.CurrentMapData;
        if (mapData == null) return;

        // 1) Container 검사
        if (containerObject == null)
        {
            Debug.LogWarning("[MeshObjectBuilderSystem] Container가 할당되지 않았습니다. " +
                             "Inspector에서 containerObject를 지정해주세요.");
            return;
        }

        // 2) 기존 Container 자식들 제거
        ClearContainerChildren();

        // 3) generatedMeshList 읽기
        var generatedMeshes = mapData.generatedMeshList;
        if (generatedMeshes == null || generatedMeshes.Count == 0)
        {
            Debug.LogWarning("[MeshObjectBuilderSystem] generatedMeshList가 비어있습니다. " +
                             "MeshGeneratorSystem에서 메쉬 데이터를 생성하세요.");
            return;
        }

        // 4) 각 GeneratedMeshData를 청크 단위로 생성
        int createdCount = 0;
        foreach (var gmd in generatedMeshes)
        {
            createdCount += CreateChunkedMeshes(gmd);
        }

        Debug.Log($"[MeshObjectBuilderSystem] 총 {createdCount}개의 Mesh 오브젝트 생성. " +
                  $"(Container='{containerObject.name}')");
    }

    /// <summary>
    /// containerObject의 모든 자식을 제거
    /// </summary>
    private void ClearContainerChildren()
    {
        Transform ct = containerObject.transform;
        for (int i = ct.childCount - 1; i >= 0; i--)
        {
            var child = ct.GetChild(i).gameObject;
            DestroyImmediate(child);
        }
    }

    /// <summary>
    /// 하나의 GeneratedMeshData가 정점 수가 많으면 여러 Mesh로 쪼개서 생성.
    /// 생성된 오브젝트 수를 반환.
    /// </summary>
    private int CreateChunkedMeshes(GeneratedMeshData gmd)
    {
        var verts = gmd.vertices;
        var tris = gmd.triangles;
        var uv = gmd.uv;

        int vertexCount = (verts != null) ? verts.Length : 0;
        if (vertexCount == 0 || tris == null || tris.Length < 3)
        {
            // 유효하지 않은 메쉬 데이터
            return 0;
        }

        if (vertexCount <= maxVerticesPerMesh)
        {
            // 하나의 Mesh로 충분
            CreateSingleMeshObject(
                gmd.cellKey, 
                0, 
                verts, 
                tris, 
                uv
            );
            return 1;
        }
        else
        {
            // 여러 청크로 분할
            int start = 0;
            int chunkId = 0;
            int createdObjCount = 0;

            while (start < vertexCount)
            {
                int chunkVertCount = Mathf.Min(maxVerticesPerMesh, vertexCount - start);

                // 삼각 재매핑
                List<int> chunkTris = new List<int>();
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int i0 = tris[i];
                    int i1 = tris[i + 1];
                    int i2 = tris[i + 2];

                    // 해당 삼각형이 [start ~ start+chunkVertCount) 범위에 속하는가?
                    if (i0 >= start && i0 < start + chunkVertCount &&
                        i1 >= start && i1 < start + chunkVertCount &&
                        i2 >= start && i2 < start + chunkVertCount)
                    {
                        // new index
                        int ni0 = i0 - start;
                        int ni1 = i1 - start;
                        int ni2 = i2 - start;
                        chunkTris.Add(ni0);
                        chunkTris.Add(ni1);
                        chunkTris.Add(ni2);
                    }
                }

                // 정점 / uv 슬라이스
                Vector3[] chunkVerts = new Vector3[chunkVertCount];
                Vector2[] chunkUV = (uv != null && uv.Length == vertexCount)
                                    ? new Vector2[chunkVertCount]
                                    : null;

                for (int c = 0; c < chunkVertCount; c++)
                {
                    chunkVerts[c] = verts[start + c];
                    if (chunkUV != null)
                    {
                        chunkUV[c] = uv[start + c];
                    }
                }

                // Mesh 생성
                CreateSingleMeshObject(
                    gmd.cellKey,
                    chunkId,
                    chunkVerts,
                    chunkTris.ToArray(),
                    chunkUV
                );

                start += chunkVertCount;
                chunkId++;
                createdObjCount++;
            }

            return createdObjCount;
        }
    }

    /// <summary>
    /// 실제로 하나의 Mesh -> 하나의 GameObject를 생성하여
    /// containerObject 하위에 붙인다. + (옵션) 에셋으로 저장
    /// </summary>
    private void CreateSingleMeshObject(float cellKey, int chunkId,
                                        Vector3[] verts, int[] tris, Vector2[] uv)
    {
        // 이름
        string goName = (chunkId == 0)
            ? $"Mesh_CellKey_{cellKey}"
            : $"Mesh_CellKey_{cellKey}_chunk{chunkId}";

        GameObject go = new GameObject(goName);
        go.transform.SetParent(containerObject.transform, false);

        // Layer
        go.layer = LayerMask.NameToLayer("Ground");

        // Mesh
        Mesh mesh = new Mesh
        {
            name = (chunkId == 0)
                ? $"GeneratedMesh_{cellKey}"
                : $"GeneratedMesh_{cellKey}_chunk{chunkId}"
        };
        mesh.vertices = verts;
        mesh.triangles = tris;
        if (uv != null && uv.Length == verts.Length)
        {
            mesh.uv = uv;
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = defaultMaterial;

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        // ─────────────────────────────────────
        // (옵션) Mesh를 에셋 파일로 저장하기
        // ─────────────────────────────────────
#if UNITY_EDITOR
        if (saveMeshesAsAssets)
        {
            // 폴더 경로(유니티 상대경로)
            // 예: "Assets/MyMeshes"
            string folder = folderPath;
            if (string.IsNullOrEmpty(folder))
            {
                folder = "Assets";
            }

            // 파일 이름, 중복 방지 위해 GenerateUniqueAssetPath
            // chunkId가 있으면 파일 이름에 _chunk 추가
            string fileName = mesh.name + ".asset"; 
            string rawPath = System.IO.Path.Combine(folder, fileName);
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(rawPath);

            AssetDatabase.CreateAsset(mesh, uniquePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Mesh saved as asset: {uniquePath}");
        }
#endif
    }
}
