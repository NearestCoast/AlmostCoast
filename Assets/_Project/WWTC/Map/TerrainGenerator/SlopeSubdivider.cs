using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// "면적 기준"으로 삼각형을 Subdivide하되, 
/// "한 삼각형을 subdiv하면 인접 삼각형도 에지를 동일하게 subdiv"하여
/// T점(구멍)이 절대 생기지 않도록 하는 구현.
/// 
/// - T-점 보정(Stitcher) 필요 없음
/// - 면적이 큰 삼각형을 찾으면, 그와 연결된 모든 삼각형을 BFS/DFS로 찾아 "동시에" subdiv
/// - subdiv된 에지의 Midpoint는 양쪽 삼각형이 공유 -> 구멍이 생기지 않음.
/// </summary>
public static class SlopeSubdivider
{
    /// <summary>
    /// 삼각형 정보
    /// </summary>
    public class SubTri
    {
        public int triID;      // ID
        public bool removed;   // subdiv 등으로 제거된 경우

        public Vector3 v0, v1, v2;
        public Vector2 uv0, uv1, uv2;

        public float Area()
        {
            Vector3 s1 = v1 - v0;
            Vector3 s2 = v2 - v0;
            return 0.5f * Vector3.Cross(s1, s2).magnitude;
        }
    }

    /// <summary>
    /// 에지 식별용
    /// </summary>
    public struct EdgeKey
    {
        private Vector3 a, b;
        public EdgeKey(Vector3 p1, Vector3 p2)
        {
            // 두 점을 정렬
            if(CompareVec(p1,p2)<=0) { a=p1; b=p2; }
            else { a=p2; b=p1; }
        }

        public override bool Equals(object obj)
        {
            if(obj is EdgeKey o)
                return AlmostEq(a,o.a) && AlmostEq(b,o.b);
            return false;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int h=17;
                h=(h*31)^ a.GetHashCode();
                h=(h*31)^ b.GetHashCode();
                return h;
            }
        }

        static bool AlmostEq(Vector3 v1, Vector3 v2, float eps=1e-8f)
        {
            return (v1 - v2).sqrMagnitude < eps*eps;
        }

        static int CompareVec(Vector3 v1, Vector3 v2)
        {
            if (!Mathf.Approximately(v1.x, v2.x))
                return (v1.x < v2.x)? -1:+1;
            if (!Mathf.Approximately(v1.y, v2.y))
                return (v1.y < v2.y)? -1:+1;
            if (!Mathf.Approximately(v1.z, v2.z))
                return (v1.z < v2.z)? -1:+1;
            return 0;
        }
    }

    /// <summary>
    /// subdiv시 에지 중점
    /// </summary>
    public struct EdgeMidpoint
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    //--------------------------------------------------------------------------
    // PerformSubdivByArea
    //--------------------------------------------------------------------------
    /// <summary>
    /// "면적 기준" Subdivide:
    ///  1) 면적 큰 삼각형을 찾는다
    ///  2) 그 삼각형과 인접(연결)된 삼각형 그룹을 BFS/DFS로 찾음
    ///  3) 그 그룹 전체를 "동시에" subdiv => "한쪽만 subdiv"가 없음 => 구멍x
    ///  (iteration 횟수만큼 반복)
    /// </summary>
    public static Mesh PerformSubdivByArea(
        Mesh baseMesh,
        int iteration,
        float areaThreshold,
        System.Func<float,float,float> heightSampler,
        float randomAmplitude=0f,
        int randomSeed=0
    )
    {
        // (A) Mesh -> SubTri
        var triList = MeshToSubTri(baseMesh);

        // (B) Adjacency 한 번 빌드해놓기
        var adjacency = BuildAdjacency(triList);

        // (B-1) 랜덤 생성기
        System.Random rng = (randomSeed != 0) ? new System.Random(randomSeed) : new System.Random();

        // (C) iteration
        for(int it=0; it< iteration; it++)
        {
            // (C-1) 면적이 큰 Tri만 찾음
            var bigTris = FindLargeTriangles(triList, areaThreshold);

            // (C-2) 이미 subdiv된 Tri나 removed된 Tri는 무시
            var visited = new HashSet<int>();

            // (C-3) bigTris 각각에 대해, 연결된 tri들 "동시에" subdiv
            foreach(var t in bigTris)
            {
                if(t.removed) continue;
                if(visited.Contains(t.triID)) continue;

                // BFS/DFS로 "인접"한 모든 삼각형을 찾음
                var cluster = GatherConnectedTris(t, adjacency, triList);
                // cluster 중 일부는 area가 작을 수도 있지만, "A가 subdiv되면 B도 subdiv" 해야 T점이 생기지 않음
                // => cluster 전체를 subdiv 후보로.

                // EdgeDict
                var edgeDict = new Dictionary<EdgeKey, EdgeMidpoint>();

                // subdiv
                var output = new List<SubTri>(cluster.Count*4);
                foreach(var triC in cluster)
                {
                    triC.removed= true;
                    SubdivideOneTriangle(triC, output, edgeDict, (u,v)=>
                    {
                        float baseH = heightSampler(u,v);
                        if(randomAmplitude>0f)
                        {
                            float rnd = (float)rng.NextDouble()*2f-1f;
                            baseH += rnd*randomAmplitude;
                        }
                        return baseH;
                    });
                    visited.Add(triC.triID);
                }

                // cluster를 output으로 치환
                triList.AddRange(output);
            }

            // (C-4) triList 중 removed된 tri를 제외
            triList.RemoveAll(x=> x.removed);
            
            // (C-5) 다시 adjacency를 완전히 빌드? (삼각형 수 폭증 가능)
            //  - 성능 문제 있지만, 정확성을 위해선 iteration마다 Re-Build해야
            adjacency = BuildAdjacency(triList);
        }

        // (D) SubTri-> Mesh
        return SubTriToMesh(triList);
    }

    //--------------------------------------------------------------------------
    // BFS/DFS로 "연결된 삼각형 그룹" 찾기
    //--------------------------------------------------------------------------
    private static List<SubTri> GatherConnectedTris(
        SubTri start, 
        Dictionary<int, List<int>> adjacency,
        List<SubTri> triList
    )
    {
        var result = new List<SubTri>();
        var queue = new Queue<int>();
        var visited = new HashSet<int>();

        queue.Enqueue(start.triID);
        visited.Add(start.triID);

        while(queue.Count>0)
        {
            int currID = queue.Dequeue();
            var tri = triList.Find(x=> x.triID== currID);
            if(tri==null || tri.removed) continue;
            result.Add(tri);

            // 인접 tri들
            var nbList = adjacency[currID];
            foreach(var nbID in nbList)
            {
                if(!visited.Contains(nbID))
                {
                    visited.Add(nbID);
                    queue.Enqueue(nbID);
                }
            }
        }
        return result;
    }

    //--------------------------------------------------------------------------
    // SubdivideOneTriangle - 풀 subdiv (4개)
    //--------------------------------------------------------------------------
    private static void SubdivideOneTriangle(
        SubTri tri,
        List<SubTri> outList,
        Dictionary<EdgeKey,EdgeMidpoint> edgeDict,
        System.Func<float,float,float> sampler
    )
    {
        // mid
        var (m01,uv01) = GetMidpoint(tri.v0, tri.v1, tri.uv0, tri.uv1, sampler, edgeDict);
        var (m12,uv12) = GetMidpoint(tri.v1, tri.v2, tri.uv1, tri.uv2, sampler, edgeDict);
        var (m20,uv20) = GetMidpoint(tri.v2, tri.v0, tri.uv2, tri.uv0, sampler, edgeDict);

        outList.Add(MakeTri(tri.v0, m01, m20, tri.uv0, uv01, uv20));
        outList.Add(MakeTri(m01, tri.v1, m12, uv01, tri.uv1, uv12));
        outList.Add(MakeTri(m20, m12, tri.v2, uv20, uv12, tri.uv2));
        outList.Add(MakeTri(m01, m12, m20, uv01, uv12, uv20));
    }

    //--------------------------------------------------------------------------
    // FindLargeTriangles
    //--------------------------------------------------------------------------
    private static List<SubTri> FindLargeTriangles(
        List<SubTri> triList, float areaThreshold
    )
    {
        var r = new List<SubTri>();
        foreach(var t in triList)
        {
            if(!t.removed && t.Area()> areaThreshold)
                r.Add(t);
        }
        return r;
    }

    //--------------------------------------------------------------------------
    // Midpoint
    //--------------------------------------------------------------------------
    private static (Vector3, Vector2) GetMidpoint(
        Vector3 p1, Vector3 p2,
        Vector2 uv1, Vector2 uv2,
        System.Func<float,float,float> sampler,
        Dictionary<EdgeKey,EdgeMidpoint> dict
    )
    {
        var ek = new EdgeKey(p1,p2);
        if(dict.TryGetValue(ek, out var mid))
        {
            return (mid.pos, mid.uv);
        }
        else
        {
            Vector3 mp = 0.5f*(p1 + p2);
            Vector2 muv = 0.5f*(uv1 + uv2);
            // sampler로 높이 갱신
            float h = sampler(muv.x, muv.y);
            mp.y = h;

            dict[ek]= new EdgeMidpoint{pos=mp, uv=muv};
            return (mp,muv);
        }
    }

    //--------------------------------------------------------------------------
    // MakeTri (풀 subdiv 시 생성)
    //--------------------------------------------------------------------------
    private static SubTri MakeTri(
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector2 uv0, Vector2 uv1, Vector2 uv2
    )
    {
        return new SubTri{
            triID= -1, // 나중에 할당
            removed= false,
            v0= v0, v1= v1, v2= v2,
            uv0= uv0, uv1= uv1, uv2= uv2
        };
    }

    //--------------------------------------------------------------------------
    // BuildAdjacency (매 iteration마다 다시)
    //--------------------------------------------------------------------------
    private static Dictionary<int, List<int>> BuildAdjacency(List<SubTri> triList)
    {
        var result = new Dictionary<int, List<int>>(triList.Count);
        // 미리 triID가 겹치지 않는다고 가정
        for(int i=0; i<triList.Count; i++)
        {
            var t= triList[i];
            if(t.removed) continue;
            if(!result.ContainsKey(t.triID))
                result[t.triID]= new List<int>();
        }

        // O(N^2)로 모든 쌍 검사
        for(int i=0; i< triList.Count; i++)
        {
            var A= triList[i];
            if(A.removed) continue;

            for(int j=i+1; j< triList.Count; j++)
            {
                var B= triList[j];
                if(B.removed) continue;

                if(AreAdjacent(A,B))
                {
                    result[A.triID].Add(B.triID);
                    result[B.triID].Add(A.triID);
                }
            }
        }
        return result;
    }

    private static bool AreAdjacent(SubTri A, SubTri B)
    {
        // 공유 정점이 2개 이상이면 인접
        var Av= new[]{A.v0, A.v1, A.v2};
        var Bv= new[]{B.v0, B.v1, B.v2};
        int same=0;
        foreach(var a in Av)
        {
            foreach(var b in Bv)
            {
                if((a-b).sqrMagnitude< 1e-10f) same++;
            }
        }
        return (same>=2);
    }

    //--------------------------------------------------------------------------
    // Mesh <-> SubTri
    //--------------------------------------------------------------------------
    private static List<SubTri> MeshToSubTri(Mesh m)
    {
        var vs= m.vertices;
        var us= m.uv;
        var ts= m.triangles;

        var list= new List<SubTri>(ts.Length/3);
        int globalID= 0;
        for(int i=0; i< ts.Length; i+=3)
        {
            int i0= ts[i], i1= ts[i+1], i2= ts[i+2];
            var st= new SubTri{
                triID= globalID++,
                removed= false,
                v0= vs[i0],
                v1= vs[i1],
                v2= vs[i2],
                uv0= us[i0],
                uv1= us[i1],
                uv2= us[i2]
            };
            list.Add(st);
        }
        return list;
    }

    private static Mesh SubTriToMesh(List<SubTri> triList)
    {
        var vs= new List<Vector3>(triList.Count*3);
        var us= new List<Vector2>(triList.Count*3);
        var idx= new List<int>(triList.Count*3);
        int counter= 0;

        foreach(var t in triList)
        {
            if(t.removed) continue;
            vs.Add(t.v0);  us.Add(t.uv0);
            vs.Add(t.v1);  us.Add(t.uv1);
            vs.Add(t.v2);  us.Add(t.uv2);

            idx.Add(counter+0);
            idx.Add(counter+1);
            idx.Add(counter+2);
            counter+=3;
        }

        Mesh mm= new Mesh();
        mm.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        mm.vertices= vs.ToArray();
        mm.uv= us.ToArray();
        mm.triangles= idx.ToArray();

        mm.RecalculateNormals();
        mm.RecalculateBounds();
        return mm;
    }
}
