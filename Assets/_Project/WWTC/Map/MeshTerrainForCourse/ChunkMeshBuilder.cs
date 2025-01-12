using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// (i, j) 정점 배열( or 리스트 )를 Chunk별로 Mesh 생성 후,
/// 최종 GameObject로 만든다.
/// </summary>
public class ChunkMeshBuilder : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData; // 지형 Vertex 정보가 저장된다고 가정

    [FoldoutGroup("Settings")]
    public int chunkCountX = 4;
    public int chunkCountZ = 4;

    [FoldoutGroup("Actions")]
    [Button("Build Chunks & Create Objects", ButtonSizes.Medium)]
    public void BuildAndCreateChunks()
    {
        if (pathData == null)
        {
            Debug.LogWarning("[ChunkMeshBuilder] pathDataSO가 없습니다.");
            return;
        }

        // TODO: chunkCountX, chunkCountZ를 기반으로
        //       grid vertex -> Mesh -> GameObject
        Debug.Log("[ChunkMeshBuilder] Chunk Mesh 생성 & 오브젝트 배치 (가정) 완료.");
    }
}