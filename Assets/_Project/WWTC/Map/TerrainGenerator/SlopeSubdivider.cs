using UnityEngine;
using System.Collections.Generic;

public static class SlopeSubdivider
{
    private class SubTri
    {
        public int triID;
        public Vector3 v0, v1, v2;
        public Vector2 uv0, uv1, uv2;
        public bool removed;

        public float Area()
        {
            Vector3 s1 = v1 - v0;
            Vector3 s2 = v2 - v0;
            return 0.5f * Vector3.Cross(s1, s2).magnitude;
        }

        public void Subdivide(
            List<SubTri> outList,
            System.Func<float,float,float> heightSampler,
            Dictionary<EdgeKey, EdgeMidpoint> edgeDict,
            System.Random rng,
            float randomAmplitude
        )
        {
            removed = true; 
            var (m01, uv01) = GetOrCreateMidpoint(v0,v1, uv0,uv1, heightSampler, edgeDict, rng, randomAmplitude);
            var (m12, uv12) = GetOrCreateMidpoint(v1,v2, uv1,uv2, heightSampler, edgeDict, rng, randomAmplitude);
            var (m20, uv20) = GetOrCreateMidpoint(v2,v0, uv2,uv0, heightSampler, edgeDict, rng, randomAmplitude);

            outList.Add( MakeTri(v0,   m01,  m20,   uv0,  uv01, uv20) );
            outList.Add( MakeTri(m01,  v1,   m12,   uv01, uv1,  uv12) );
            outList.Add( MakeTri(m20,  m12,  v2,    uv20, uv12, uv2 ) );
            outList.Add( MakeTri(m01,  m12,  m20,   uv01, uv12, uv20) );
        }

        private (Vector3, Vector2) GetOrCreateMidpoint(
            Vector3 p1, Vector3 p2,
            Vector2 u1, Vector2 u2,
            System.Func<float,float,float> sampler,
            Dictionary<EdgeKey, EdgeMidpoint> edgeDict,
            System.Random rng,
            float randomAmplitude
        )
        {
            var ek = new EdgeKey(p1, p2);
            if (edgeDict.TryGetValue(ek, out var mid))
            {
                return (mid.pos, mid.uv);
            }
            else
            {
                Vector3 mp = 0.5f*(p1 + p2);
                Vector2 muv= 0.5f*(u1 + u2);

                float baseH = sampler(muv.x, muv.y);
                mp.y = baseH;

                if (randomAmplitude > 0f)
                {
                    float r = (float)rng.NextDouble()*2f - 1f; // -1..+1
                    mp.y += r * randomAmplitude;
                }

                var e = new EdgeMidpoint{ pos=mp, uv=muv };
                edgeDict[ek] = e;
                return (mp, muv);
            }
        }

        private SubTri MakeTri(Vector3 a, Vector3 b, Vector3 c,
                               Vector2 ua, Vector2 ub, Vector2 uc)
        {
            return new SubTri {
                v0=a, v1=b, v2=c,
                uv0=ua, uv1=ub, uv2=uc
            };
        }
    }

    private struct EdgeKey
    {
        private Vector3 a,b;
        public EdgeKey(Vector3 p1, Vector3 p2)
        {
            if (CompareVec(p1,p2)<=0){ a=p1; b=p2; }
            else { a=p2; b=p1; }
        }
        public override bool Equals(object obj)
        {
            if (obj is EdgeKey o) 
                return AlmostEq(a,o.a)&&AlmostEq(b,o.b);
            return false;
        }
        public override int GetHashCode()
        {
            unchecked{
                int h=17;
                h=(h*31)^ a.GetHashCode();
                h=(h*31)^ b.GetHashCode();
                return h;
            }
        }
        static bool AlmostEq(Vector3 v1, Vector3 v2, float eps=1e-8f)
        {
            return (v1 - v2).sqrMagnitude< eps*eps;
        }
        static int CompareVec(Vector3 v1, Vector3 v2)
        {
            if(!Mathf.Approximately(v1.x,v2.x)) return (v1.x<v2.x)? -1:+1;
            if(!Mathf.Approximately(v1.y,v2.y)) return (v1.y<v2.y)? -1:+1;
            if(!Mathf.Approximately(v1.z,v2.z)) return (v1.z<v2.z)? -1:+1;
            return 0;
        }
    }

    private struct EdgeMidpoint
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    public static Mesh PerformSubdivByArea(
        Mesh baseMesh,
        int maxIter,
        float areaThreshold,
        System.Func<float,float,float> heightSampler,
        float randomAmplitude=0f,
        int randomSeed=0
    )
    {
        var triList= MeshToSubTri(baseMesh);
        var edgeDict= new Dictionary<EdgeKey, EdgeMidpoint>(65536);
        var edge2tris= new Dictionary<EdgeKey,List<SubTri>>(65536);

        BuildAdjacency(triList, edge2tris);

        // 랜덤
        System.Random rng= new System.Random(randomSeed);

        for(int iter=0; iter< maxIter; iter++)
        {
            var subdivSet= MarkSubdivTriangles(triList, areaThreshold, edge2tris);
            if(subdivSet.Count==0) break;

            var nextList= new List<SubTri>(triList.Count*2);
            foreach(var st in triList)
            {
                if(st.removed) continue;

                if(subdivSet.Contains(st.triID))
                {
                    // subdiv
                    st.Subdivide(nextList, heightSampler, edgeDict, rng, randomAmplitude);
                }
                else
                {
                    // 유지
                    nextList.Add(st);
                }
            }

            triList= nextList;
            edge2tris.Clear();
            BuildAdjacency(triList, edge2tris);
        }

        return SubTriToMesh(triList);
    }

    //-------------------------------------------------------------------------
    private static List<SubTri> MeshToSubTri(Mesh m)
    {
        var vs= m.vertices;
        var us= m.uv;
        var ts= m.triangles;

        var list= new List<SubTri>(ts.Length/3);
        int globalID=0;
        for(int i=0; i< ts.Length; i+=3)
        {
            int i0= ts[i], i1= ts[i+1], i2= ts[i+2];
            var st= new SubTri {
                triID= globalID++,
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
        var ts= new List<int>(triList.Count*3);

        foreach(var st in triList)
        {
            if(st.removed) continue;
            int start= vs.Count;
            vs.Add(st.v0); us.Add(st.uv0);
            vs.Add(st.v1); us.Add(st.uv1);
            vs.Add(st.v2); us.Add(st.uv2);

            ts.Add(start+0);
            ts.Add(start+1);
            ts.Add(start+2);
        }

        Mesh mm= new Mesh();
        mm.indexFormat= UnityEngine.Rendering.IndexFormat.UInt32;
        mm.vertices= vs.ToArray();
        mm.uv= us.ToArray();
        mm.triangles= ts.ToArray();

        mm.RecalculateNormals();
        mm.RecalculateBounds();
        return mm;
    }

    private static void BuildAdjacency(List<SubTri> triList, Dictionary<EdgeKey,List<SubTri>> edge2tris)
    {
        foreach(var t in triList)
        {
            if(t.removed) continue;

            var e1= new EdgeKey(t.v0, t.v1);
            var e2= new EdgeKey(t.v1, t.v2);
            var e3= new EdgeKey(t.v2, t.v0);

            InsertEdge(edge2tris, e1, t);
            InsertEdge(edge2tris, e2, t);
            InsertEdge(edge2tris, e3, t);
        }
    }
    private static void InsertEdge(Dictionary<EdgeKey,List<SubTri>> dict, EdgeKey ek, SubTri tri)
    {
        if(!dict.TryGetValue(ek, out var lst))
        {
            lst= new List<SubTri>(2);
            dict[ek] = lst;
        }
        lst.Add(tri);
    }

    private static HashSet<int> MarkSubdivTriangles(
        List<SubTri> triList,
        float areaThreshold,
        Dictionary<EdgeKey,List<SubTri>> edge2tris
    )
    {
        var subdivSet= new HashSet<int>();
        var queue= new Queue<SubTri>();

        foreach(var t in triList)
        {
            if(t.removed) continue;
            if(t.Area() > areaThreshold)
            {
                subdivSet.Add(t.triID);
                queue.Enqueue(t);
            }
        }

        while(queue.Count>0)
        {
            var cur= queue.Dequeue();
            if(cur.removed) continue;

            var e1= new EdgeKey(cur.v0, cur.v1);
            var e2= new EdgeKey(cur.v1, cur.v2);
            var e3= new EdgeKey(cur.v2, cur.v0);

            MarkEdgeNeighbor(e1, cur, subdivSet, queue, edge2tris);
            MarkEdgeNeighbor(e2, cur, subdivSet, queue, edge2tris);
            MarkEdgeNeighbor(e3, cur, subdivSet, queue, edge2tris);
        }

        return subdivSet;
    }
    private static void MarkEdgeNeighbor(
        EdgeKey ek,
        SubTri current,
        HashSet<int> subdivSet,
        Queue<SubTri> queue,
        Dictionary<EdgeKey,List<SubTri>> edge2tris
    )
    {
        if(edge2tris.TryGetValue(ek, out var triList))
        {
            foreach(var nb in triList)
            {
                if(nb.removed) continue;
                if(!subdivSet.Contains(nb.triID))
                {
                    subdivSet.Add(nb.triID);
                    queue.Enqueue(nb);
                }
            }
        }
    }
}
