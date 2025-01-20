using UnityEngine;

public static class MeshDataUtility
{
    public static Mesh CreateMeshFromData(GeneratedMeshData data)
    {
        Mesh mesh = new Mesh();
        mesh.name = $"Mesh_{data.cellKey}";
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        if (data.uv != null && data.uv.Length == data.vertices.Length)
        {
            mesh.uv = data.uv;
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

// 사용 예시:
// var go = new GameObject("IslandMesh_"+gmd.cellKey);
// var mf = go.AddComponent<MeshFilter>();
// var mr = go.AddComponent<MeshRenderer>();
// mf.mesh = MeshDataUtility.CreateMeshFromData(gmd);
// mr.material = someMaterial;