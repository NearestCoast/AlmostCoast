using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Chunk 단위로 메쉬를 생성 + (LOD / CrackStitch) 처리
/// </summary>
public static class ChunkBuilder
{
    public static Mesh BuildChunk(
        ChunkData cd,
        Texture2D heightmap,
        float sizeX, float sizeZ, float maxH,
        int baseResolution,
        bool doStitch
    )
    {
        // 1) 해상도 결정: res = baseRes >> lod
        int lod = cd.lod;
        int res = Mathf.Max(2, baseResolution >> lod);

        // 2) 정규 격자 생성
        var verts = new Vector3[res*res];
        var uvs   = new Vector2[res*res];
        var tris  = new List<int>((res-1)*(res-1)*6);

        float stepU= 1f/(res-1);
        float stepV= 1f/(res-1);

        // (샘플 높이)
        float SampleHeightUV(float u, float v)
        {
            Color c= heightmap.GetPixelBilinear(u, v);
            return c.r* maxH;
        }
        // worldX = Mathf.Lerp(cd.xStart, cd.xEnd, u)
        // worldZ = Mathf.Lerp(cd.zStart, cd.zEnd, v)

        for(int z=0; z< res; z++)
        {
            for(int x=0; x< res; x++)
            {
                int i= z*res + x;
                float fu= x*stepU;  // 0..1
                float fv= z*stepV;  // 0..1
                float wx= Mathf.Lerp(cd.xStart, cd.xEnd, fu);
                float wz= Mathf.Lerp(cd.zStart, cd.zEnd, fv);

                // height
                float hh=0f;
                {
                    float gu = wx/sizeX;  // 전역 UV
                    float gv = wz/sizeZ;
                    hh= SampleHeightUV(gu, gv);
                }

                verts[i]= new Vector3(wx, hh, wz);
                uvs[i]= new Vector2(wx/sizeX, wz/sizeZ); 
            }
        }

        for(int z=0; z< res-1; z++)
        {
            for(int x=0; x< res-1; x++)
            {
                int curr= z*res+ x;
                int next= (z+1)*res+ x;
                tris.Add(curr); 
                tris.Add(next); 
                tris.Add(curr+1);
                tris.Add(next);
                tris.Add(next+1);
                tris.Add(curr+1);
            }
        }

        var mesh= new Mesh();
        mesh.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices= verts;
        mesh.uv= uvs;
        mesh.triangles= tris.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 3) (옵션) doStitch
        if (doStitch)
        {
            // CrackStitcher.StitchChunk(cd, mesh, ...);
            // 여기서 실제론 인접 ChunkLOD 차를 확인하여
            // 경계라인 subdiv, or index 재배치
            // 
            // 본 예시에서는 생략(Placeholder)
        }

        return mesh;
    }
}
