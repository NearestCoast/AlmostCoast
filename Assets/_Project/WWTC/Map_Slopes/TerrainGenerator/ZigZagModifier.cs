using UnityEngine;

public static class ZigZagModifier
{
    /// <summary>
    /// 정규 격자(verts.Length = resX*resZ)를 대상으로,
    /// x좌표에 (사인 + 난수) 변형을 적용
    /// </summary>
    public static void ApplyZigZag(
        Mesh mesh,
        int resolutionX,
        int resolutionZ,
        float amplitude,
        float frequency,
        float randomness,
        int randomSeed
    )
    {
        if (!mesh) return;
        var verts = mesh.vertices;
        if (verts.Length != resolutionX*resolutionZ)
        {
            Debug.LogWarning("[ZigZagModifier] Vertex count mismatch");
            return;
        }

        Random.InitState(randomSeed);

        for(int z=0; z< resolutionZ; z++)
        {
            for(int x=0; x< resolutionX; x++)
            {
                int i = z*resolutionX + x;
                Vector3 v = verts[i];

                float wave = Mathf.Sin(z*frequency)*amplitude;
                float rnd  = Random.Range(-randomness, randomness);
                v.x += wave + rnd;  // x좌표만 변형

                verts[i] = v;
            }
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}