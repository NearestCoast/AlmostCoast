using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Sirenix.OdinInspector;

//
// Chunk 분할 + LOD + 간단 CrackStitch 예시
// (정규 격자 & Zigzag & SlopeSubdiv & FlatShading 등과는 별도의 응용 가능)
// 
// 실제로는, 아래 'CrackStitcher' 등의 로직을 함께 써서 크랙을 줄임.
//

public class ChunkLODTerrain : MonoBehaviour
{
    public Texture2D heightmap;
    public Transform container;

    [Title("Terrain Size")]
    public float terrainSizeX= 200f;
    public float terrainSizeZ= 200f;
    public float terrainMaxH= 30f;

    [Title("Chunks")]
    public int chunkCount = 4;
    public int baseResolution= 64;  // LOD=0에서의 해상도

    [Title("LOD")]
    public int maxLOD= 2; // 0..maxLOD
    [Tooltip("LOD 결정 방식")] public LODMode lodMode= LODMode.FixedAllSame;
    [ShowIf("lodMode", LODMode.FixedAllSame)]
    public int fixedLOD= 1;

    [Title("CrackStitch")]
    public bool doStitch= true;

    [Title("Material")]
    public Material terrainMat;

    [Title("Debug Info")]
    [ReadOnly] public int totalVerts;

    private GameObject root;
    private ChunkData[,] chunkGrid;

    public enum LODMode { FixedAllSame, Random }

    [Button("Generate Chunks")]
    public void GenerateChunks()
    {
        if (!heightmap)
        {
            Debug.LogWarning("No heightmap!");
            return;
        }
        if (!container)
        {
            Debug.LogWarning("No container!");
            return;
        }

        if (root) DestroyImmediate(root);
        root= new GameObject("ChunkTerrainRoot");
        root.transform.SetParent(container,false);
        root.layer= LayerMask.NameToLayer("Ground");

        chunkGrid= new ChunkData[chunkCount, chunkCount];
        float chunkW= terrainSizeX/chunkCount;
        float chunkH= terrainSizeZ/chunkCount;

        // chunk 생성
        for(int z=0; z< chunkCount; z++)
        {
            for(int x=0; x< chunkCount; x++)
            {
                var c= new ChunkData();
                c.ix= x; c.iz= z;
                c.xStart= x*chunkW; c.xEnd= (x+1)*chunkW;
                c.zStart= z*chunkH; c.zEnd= (z+1)*chunkH;
                c.lod= DecideLOD();
                chunkGrid[x,z]= c;
            }
        }

        // 이웃참조
        for(int z=0; z< chunkCount; z++)
        {
            for(int x=0; x< chunkCount; x++)
            {
                var c= chunkGrid[x,z];
                if (x>0) c.left= chunkGrid[x-1,z];
                if (x<chunkCount-1) c.right= chunkGrid[x+1,z];
                if (z>0) c.back= chunkGrid[x,z-1];
                if (z<chunkCount-1) c.front= chunkGrid[x,z+1];
            }
        }

        // Build
        totalVerts=0;
        for(int z=0; z< chunkCount; z++)
        {
            for(int x=0; x< chunkCount; x++)
            {
                var c= chunkGrid[x,z];
                c.go= new GameObject($"Chunk_{x}_{z}_LOD{c.lod}");
                c.go.transform.SetParent(root.transform, false);
                c.go.layer= LayerMask.NameToLayer("Ground");

                // build
                c.mesh= ChunkBuilder.BuildChunk(c, heightmap, terrainSizeX, terrainSizeZ, terrainMaxH, baseResolution, doStitch);

                var mf= c.go.AddComponent<MeshFilter>();
                mf.sharedMesh= c.mesh;
                var mr= c.go.AddComponent<MeshRenderer>();
                mr.sharedMaterial= terrainMat ? terrainMat: new Material(Shader.Find("Standard"));
                var col= c.go.AddComponent<MeshCollider>();
                col.sharedMesh= c.mesh;

                totalVerts+= c.mesh.vertexCount;
            }
        }

        Debug.Log($"[ChunkLODTerrain] Done. totalVerts={totalVerts}");
    }

    private int DecideLOD()
    {
        switch(lodMode)
        {
            case LODMode.FixedAllSame:
                return Mathf.Clamp(fixedLOD, 0, maxLOD);
            case LODMode.Random:
                return Random.Range(0, maxLOD+1);
            default:
                return 0;
        }
    }
}

/// <summary>
/// Chunk에 대한 정보 데이터
/// </summary>
public class ChunkData
{
    public int ix, iz;
    public float xStart, xEnd, zStart, zEnd;
    public int lod;
    public GameObject go;
    public Mesh mesh;

    // 이웃
    public ChunkData left, right, front, back;
}
