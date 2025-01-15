using UnityEngine;

public static class FlatShadingUtility
{
    public static Mesh MakeFlatShading(Mesh original)
    {
        var vs = original.vertices;
        var us = original.uv;
        var ts = original.triangles;

        int triCount = ts.Length / 3;
        var newVerts = new Vector3[ts.Length];
        var newUVs = new Vector2[ts.Length];
        var newNorms = new Vector3[ts.Length];
        var newTris = new int[ts.Length];

        for (int i = 0; i < triCount; i++)
        {
            int iTri = i * 3;
            int i0 = ts[iTri + 0];
            int i1 = ts[iTri + 1];
            int i2 = ts[iTri + 2];

            newVerts[iTri + 0] = vs[i0];
            newVerts[iTri + 1] = vs[i1];
            newVerts[iTri + 2] = vs[i2];

            newUVs[iTri + 0] = us[i0];
            newUVs[iTri + 1] = us[i1];
            newUVs[iTri + 2] = us[i2];

            var s1 = newVerts[iTri + 1] - newVerts[iTri + 0];
            var s2 = newVerts[iTri + 2] - newVerts[iTri + 0];
            var n = Vector3.Cross(s1, s2).normalized;

            newNorms[iTri + 0] = n;
            newNorms[iTri + 1] = n;
            newNorms[iTri + 2] = n;

            newTris[iTri + 0] = iTri + 0;
            newTris[iTri + 1] = iTri + 1;
            newTris[iTri + 2] = iTri + 2;
        }

        var flat = new Mesh();
        flat.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        flat.vertices = newVerts;
        flat.uv = newUVs;
        flat.normals = newNorms;
        flat.triangles = newTris;
        flat.RecalculateBounds();
        return flat;
    }
}