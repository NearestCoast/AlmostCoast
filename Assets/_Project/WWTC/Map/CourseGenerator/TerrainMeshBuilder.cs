using UnityEngine;

/// <summary>
/// (단계3) 지형 생성 담당 (예시)
/// 실제 구현은 Perlin Noise 등으로 Mesh를 만들거나, 
/// Unity TerrainData를 조작하거나, 원하는 알고리즘에 맞게 확장.
/// 여기서는 간단히 인터페이스 성격만 예시.
/// </summary>
public class TerrainMeshBuilder
{
    /// <summary>
    /// 예시: 지형 폭과 높이, 분할 등
    /// </summary>
    public float terrainWidth= 10f;
    public float terrainLength= 20f;
    public int terrainResolution= 50;

    /// <summary>
    /// 실제 구현은 PerlinNoise 등.
    /// 여기선 단순히 Plane Mesh 만드는 식 예시.
    /// </summary>
    public GameObject BuildTerrain(Transform container)
    {
        var terrainObj= new GameObject("ProceduralTerrain");
        terrainObj.transform.SetParent(container, false);

        // 간단히 plane mesh...
        var mf= terrainObj.AddComponent<MeshFilter>();
        var mr= terrainObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial= new Material(Shader.Find("Standard"));

        // 아래는 실제 구현 생략(plane vertices + tri)
        // ...
        // 예시로만 (정말 아무것도 안함)
        // 
        // => Plane mesh or Perlin etc...
        
        Debug.Log("[TerrainMeshBuilder] BuildTerrain - (Placeholder) Done.");
        return terrainObj;
    }
}