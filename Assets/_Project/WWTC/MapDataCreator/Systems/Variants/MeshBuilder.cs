using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// MapDataSystem을 상속받아, MapDataSO 내의 generatedMeshDataList로부터
/// 실제 UnityEngine.Mesh를 만들어주는 시스템.
/// 버텍스 수가 많으면 Chunk로 분할해서 생성.
/// </summary>
public class MeshBuilder : MapDataSystem
{
    [Title("Mesh Builder Settings")]
    [Tooltip("생성된 Mesh들을 담을 부모(컨테이너) Transform")]
    [SerializeField] private Transform container;

    [Tooltip("MeshRenderer에 할당할 기본 머티리얼")]
    [SerializeField] private Material meshMaterial;

    [Tooltip("한 Mesh가 가질 수 있는 최대 버텍스 수 (초과 시 여러 Chunk로 분할)")]
    [SerializeField] private int maxVerticesPerChunk = 65000;

    /// <summary>
    /// Generate()가 호출되면, MapDataSO의 generatedMeshDataList를 읽어 Mesh를 생성하고,
    /// 필요하면 Chunk 분할을 진행한다.
    /// </summary>
    public override void Generate()
    {
        // 기본 전처리(에러 로그, mapDataCreator 세팅 등)
        base.Generate();
        if (!IsReady) return; 

        // 이전에 생성된 오브젝트 정리
        ClearContainerChildren();

        // 실제 데이터(GeneratedMeshData 리스트)를 순회하며 메쉬 생성
        var meshDataList = mapDataCreator.CurrentMapData.generatedMeshDataList;
        if (meshDataList == null || meshDataList.Count == 0)
        {
            Debug.LogWarning("[MeshBuilder] generatedMeshDataList가 비어있습니다.");
            return;
        }

        for (int i = 0; i < meshDataList.Count; i++)
        {
            var meshData = meshDataList[i];
            CreateMeshObjects(meshData, i);
        }

        Debug.Log($"[MeshBuilder] Mesh 생성 완료. 총 {meshDataList.Count}개의 MeshData를 처리했습니다.");
    }

    /// <summary>
    /// Container에 자식으로 있는 기존 오브젝트들을 모두 제거한다.
    /// (테스트 시 여러 번 Generate할 때 계속 누적되는 것 방지)
    /// </summary>
    private void ClearContainerChildren()
    {
        if (container == null)
        {
            Debug.LogWarning("[MeshBuilder] Container가 설정되어 있지 않습니다.");
            return;
        }

        // Editor/런타임 구분하여 삭제
        var childCount = container.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);

#if UNITY_EDITOR
            // 에디터 환경에서 즉시 삭제
            DestroyImmediate(child.gameObject);
#else
            // 런타임 환경에서 삭제
            Destroy(child.gameObject);
#endif
        }
    }

    /// <summary>
    /// 하나의 GeneratedMeshData에 대해, maxVerticesPerChunk 초과 여부에 따라
    /// 바로 Mesh를 생성하거나, 여러 Chunk로 분할하여 생성한다.
    /// </summary>
    private void CreateMeshObjects(GeneratedMeshData meshData, int dataIndex)
    {
        // Chunk 분할 결과 가져오기
        var chunkList = ChunkMesh(meshData);

        // chunkList를 순회하면서 GameObject + MeshFilter + MeshRenderer 구성
        for (int i = 0; i < chunkList.Count; i++)
        {
            var chunk = chunkList[i];

            // 새 GameObject 생성
            GameObject go = new GameObject($"Mesh_{meshData.cellKey}_Data{dataIndex}_Chunk{i}");
            if (container != null)
            {
                go.transform.SetParent(container, false);
            }

            // MeshFilter, MeshRenderer 추가
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            if (meshMaterial != null)
            {
                mr.sharedMaterial = meshMaterial;
            }

            // 실제 Mesh 생성
            Mesh unityMesh = new Mesh();
            // Unity의 16bit 인덱스 제한이 65535이므로, Vertex가 그 이상이면 자동으로 32bit 인덱스가 사용되어야 함
            // (단, Unity 버전에 따라 지원 여부가 다르니 주의)
            if (chunk.vertices.Length > 65535)
            {
                unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            unityMesh.vertices  = chunk.vertices;
            unityMesh.triangles = chunk.triangles;
            unityMesh.uv        = chunk.uv;

            unityMesh.RecalculateNormals();
            unityMesh.RecalculateBounds();

            mf.sharedMesh = unityMesh;
        }
    }

    /// <summary>
    /// 하나의 GeneratedMeshData를 받아서, maxVerticesPerChunk 기준으로
    /// 여러 청크로 분할한 결과(버텍스/트라이/UV)를 List로 만들어 반환한다.
    /// </summary>
    private List<ChunkedMeshData> ChunkMesh(GeneratedMeshData meshData)
    {
        List<ChunkedMeshData> result = new List<ChunkedMeshData>();

        Vector3[] originalVerts = meshData.vertices;
        int[] originalTris      = meshData.triangles;
        Vector2[] originalUVs   = meshData.uv;

        int totalVerts = originalVerts.Length;
        if (totalVerts == 0 || originalTris == null || originalTris.Length == 0)
        {
            // 버텍스가 없거나 트라이가 없으면 바로 빈 리스트 반환
            return result;
        }

        // 만약 한번에 가능한 경우(버텍스 수가 한도를 넘지 않는 경우)는 그대로 추가
        if (totalVerts <= maxVerticesPerChunk)
        {
            ChunkedMeshData singleChunk = new ChunkedMeshData
            {
                vertices  = originalVerts,
                triangles = originalTris,
                uv        = null
            };
            result.Add(singleChunk);
            return result;
        }

        // ─────────────────────────────────────────────────────────
        // *** 버텍스가 많은 경우(Chunk로 분할) ***
        // 1) 버텍스 배열을 chunkSize 단위로 나눈다.
        // 2) 삼각형 인덱스 중 해당 chunk 범위에 속하는 인덱스만 포함한다.
        // 3) chunk 범위의 시작점 기준으로 인덱스를 다시 0부터 재정렬한다.
        // ─────────────────────────────────────────────────────────
        int chunkSize = maxVerticesPerChunk;
        int totalTris = originalTris.Length / 3;

        for (int start = 0; start < totalVerts; start += chunkSize)
        {
            int end = Mathf.Min(start + chunkSize, totalVerts);
            int chunkVertCount = end - start;

            // 해당 Chunk의 버텍스/UV 복사
            Vector3[] chunkVerts = new Vector3[chunkVertCount];
            Vector2[] chunkUVs   = new Vector2[chunkVertCount];
            System.Array.Copy(originalVerts, start, chunkVerts, 0, chunkVertCount);
            System.Array.Copy(originalUVs,   start, chunkUVs,   0, chunkVertCount);

            // 해당 Chunk 내에 포함되는 삼각형만 선별
            List<int> chunkTriList = new List<int>();
            for (int t = 0; t < totalTris; t++)
            {
                int i0 = originalTris[t * 3 + 0];
                int i1 = originalTris[t * 3 + 1];
                int i2 = originalTris[t * 3 + 2];

                // 모든 인덱스가 chunk 범위에 속해야 해당 삼각형을 채택
                if (i0 >= start && i0 < end &&
                    i1 >= start && i1 < end &&
                    i2 >= start && i2 < end)
                {
                    // chunkVerts[0]을 기준으로 오프셋을 뺀 인덱스가 된다.
                    chunkTriList.Add(i0 - start);
                    chunkTriList.Add(i1 - start);
                    chunkTriList.Add(i2 - start);
                }
            }

            ChunkedMeshData cmd = new ChunkedMeshData
            {
                vertices  = chunkVerts,
                triangles = chunkTriList.ToArray(),
                uv        = chunkUVs
            };

            result.Add(cmd);
        }

        return result;
    }

    /// <summary>
    /// ChunkMesh에서 사용될 임시 구조체
    /// </summary>
    private struct ChunkedMeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uv;
    }
}
