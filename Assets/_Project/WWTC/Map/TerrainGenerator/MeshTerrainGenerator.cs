using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

//
// MeshTerrainGenerator
//   - (정규 격자 + ZigZag) -> SlopeSubdivider(EdgeDict+인접Subdiv + (옵션)랜덤오프셋) -> FlatShading
//   - 최종 Mesh를 GameObject로 생성
//

public class MeshTerrainGenerator : MonoBehaviour
{
    [Title("Heightmap (in Assets)")]
    public Texture2D heightmap;

    [Title("Output Container")]
    public Transform container;

    [Title("Terrain Size")]
    public float terrainSizeX = 100f;
    public float terrainSizeZ = 100f;
    public float terrainMaxHeight = 20f;

    [Title("Resolution")]
    [Tooltip("정규 격자 해상도 (예: 128, 256)")]
    public int resolutionX = 128;
    public int resolutionZ = 128;

    [Title("Zigzag")]
    public bool useZigzag = false;
    [ShowIf("useZigzag")] public float zigzagAmplitude = 1f;
    [ShowIf("useZigzag")] public float zigzagFrequency = 0.3f;
    [ShowIf("useZigzag")] public float zigzagRandomness = 0.5f;
    [ShowIf("useZigzag")] public int  zigzagRandomSeed = 0;

    [Title("Slope Subdivision")]
    public bool useSlopeSubdiv = false;
    [ShowIf("useSlopeSubdiv")] public int   slopeSubdivIteration = 2;
    [ShowIf("useSlopeSubdiv")] public float slopeAreaThreshold   = 10f;

    // -------------------------
    // [추가] subdiv 시 면을 깨트리는 랜덤 오프셋
    // -------------------------
    [ShowIf("useSlopeSubdiv"), LabelText("Use Random Offset")]
    public bool useRandomOffset = false;

    [ShowIf("@this.useSlopeSubdiv && this.useRandomOffset"), LabelText("Random Amplitude")]
    public float randomAmplitude = 0.3f;

    [ShowIf("@this.useSlopeSubdiv && this.useRandomOffset"), LabelText("Random Seed")]
    public int randomSeed = 0;

    [Title("Flat Shading")]
    public bool useFlatShading = true;

    [Title("Output Material")]
    public Material terrainMaterial;

    [Title("Debug Info")]
    [ReadOnly] public int vertexCount;

    [SerializeField] private GameObject terrainObject; // 최종 생성되는 오브젝트

#if UNITY_EDITOR
    [Button("Generate Mesh (Single)")]
    public void GenerateSingleMesh()
    {
        if (!heightmap)
        {
            Debug.LogWarning("[MeshTerrainGenerator] No heightmap assigned!");
            return;
        }
        if (!container)
        {
            Debug.LogWarning("[MeshTerrainGenerator] No container assigned!");
            return;
        }

        // 1) HeightSampler
        IHeightSampler heightSampler = new HeightmapSampler(heightmap, terrainSizeX, terrainSizeZ, terrainMaxHeight);

        // 2) 정규 격자 생성
        Mesh baseMesh = GenerateRegularGrid(heightSampler);

        // 2-1) Zigzag (옵션)
        if (useZigzag)
        {
            ZigZagModifier.ApplyZigZag(
                baseMesh,
                resolutionX, resolutionZ,
                zigzagAmplitude, 
                zigzagFrequency, 
                zigzagRandomness, 
                zigzagRandomSeed
            );
        }

        // 3) SlopeSubdivider(EdgeDict+인접)
        Mesh subdivMesh = baseMesh;
        if (useSlopeSubdiv)
        {
            // --- 새 기능: randomAmplitude, randomSeed 적용 ---
            // (SlopeSubdivider.PerformSubdivByArea 오버로드를 사용/수정했다고 가정)
            // randomAmplitude= 0 이면 기존과 동일(오프셋x)
            float actualAmplitude = useRandomOffset ? randomAmplitude : 0f;
            int   actualSeed      = useRandomOffset ? randomSeed : 0;

            subdivMesh = SlopeSubdivider.PerformSubdivByArea(
                baseMesh,
                slopeSubdivIteration,
                slopeAreaThreshold,
                (u,v) => heightSampler.SampleHeightUV(u,v), // 0..1
                actualAmplitude,
                actualSeed
            );
        }

        // 4) FlatShading
        Mesh finalMesh = subdivMesh;
        if (useFlatShading)
        {
            finalMesh = FlatShadingUtility.MakeFlatShading(finalMesh);
        }

        // 5) GameObject
        if (terrainObject) DestroyImmediate(terrainObject);
        terrainObject = new GameObject("MeshTerrain");
        terrainObject.transform.SetParent(container, false);
        terrainObject.layer = LayerMask.NameToLayer("Ground");

        var mf = terrainObject.AddComponent<MeshFilter>();
        mf.sharedMesh = finalMesh;

        var mr = terrainObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = terrainMaterial 
            ? terrainMaterial
            : new Material(Shader.Find("Standard"));

        var col = terrainObject.AddComponent<MeshCollider>();
        col.sharedMesh = finalMesh;

        vertexCount = finalMesh.vertexCount;
        Debug.Log($"[MeshTerrainGenerator] Done. vertexCount={vertexCount}");
    }
#endif

    /// <summary>
    /// 정규 격자를 생성 (ZigZag 전의 기본 단계)
    /// </summary>
    private Mesh GenerateRegularGrid(IHeightSampler sampler)
    {
        int vCount = resolutionX * resolutionZ;
        Vector3[] verts = new Vector3[vCount];
        Vector2[] uvs   = new Vector2[vCount];
        int[] tris      = new int[(resolutionX -1)*(resolutionZ -1)*6];

        float stepX = 1f/(resolutionX -1);
        float stepZ = 1f/(resolutionZ -1);

        // 정점
        for (int z = 0; z < resolutionZ; z++)
        {
            for (int x = 0; x < resolutionX; x++)
            {
                int i = z*resolutionX + x;
                float u = x * stepX; // 0..1
                float v = z * stepZ; // 0..1

                float wx = u * terrainSizeX;
                float wz = v * terrainSizeZ;
                float h  = sampler.SampleHeight(wx, wz);

                verts[i] = new Vector3(wx, h, wz);
                uvs[i]   = new Vector2(u, v);
            }
        }

        // 삼각형
        int triIdx = 0;
        for(int z=0; z<resolutionZ-1; z++)
        {
            for(int x=0; x<resolutionX-1; x++)
            {
                int curr = z*resolutionX + x;
                int next = (z+1)*resolutionX + x;

                // 첫 삼각형
                tris[triIdx++] = curr;
                tris[triIdx++] = next;
                tris[triIdx++] = curr + 1;

                // 둘째 삼각형
                tris[triIdx++] = next;
                tris[triIdx++] = next + 1;
                tris[triIdx++] = curr + 1;
            }
        }

        // Mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices    = verts;
        mesh.uv          = uvs;
        mesh.triangles   = tris;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
