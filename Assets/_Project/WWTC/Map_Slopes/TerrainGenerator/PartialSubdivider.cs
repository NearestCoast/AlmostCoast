using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// "부분 Subdivide" 로직. 
/// - 한 삼각형에 대해 "지정된 에지"만 subdiv하여 2~4개의 SubTri 생성
/// </summary>
public static class PartialSubdivider
{
    public static List<SlopeSubdivider.SubTri> PartialSubdivideTriangle(
        SlopeSubdivider.SubTri tri,
        List<SlopeSubdivider.EdgeKey> edgesToStitch,   // T-점 대상 or "전부"
        System.Func<float, float, float> heightSampler,
        Dictionary<SlopeSubdivider.EdgeKey, SlopeSubdivider.EdgeMidpoint> edgeDict
    )
    {
        var newTris = new List<SlopeSubdivider.SubTri>(4);

        // 1) 삼각형 정보
        Vector3 v0= tri.v0, v1= tri.v1, v2= tri.v2;
        Vector2 u0= tri.uv0, u1= tri.uv1, u2= tri.uv2;

        // 2) 3개 에지
        var e0 = new SlopeSubdivider.EdgeKey(v0, v1);
        var e1 = new SlopeSubdivider.EdgeKey(v1, v2);
        var e2 = new SlopeSubdivider.EdgeKey(v2, v0);

        bool s0 = edgesToStitch.Contains(e0);
        bool s1 = edgesToStitch.Contains(e1);
        bool s2 = edgesToStitch.Contains(e2);

        // midpoint
        var (m0pos,m0uv) = GetOrCreateMidpoint(e0, v0, v1, u0, u1, heightSampler, edgeDict);
        var (m1pos,m1uv) = GetOrCreateMidpoint(e1, v1, v2, u1, u2, heightSampler, edgeDict);
        var (m2pos,m2uv) = GetOrCreateMidpoint(e2, v2, v0, u2, u0, heightSampler, edgeDict);

        int count = (s0?1:0) + (s1?1:0) + (s2?1:0);

        switch(count)
        {
            case 0:
                // subdiv 없음 => 그대로
                newTris.Add(tri);
                break;
            case 1:
                newTris.AddRange( Subdivide_OneEdge(tri, s0,s1,s2, m0pos,m0uv, m1pos,m1uv, m2pos,m2uv) );
                break;
            case 2:
                newTris.AddRange( Subdivide_TwoEdges(tri, s0,s1,s2, m0pos,m0uv, m1pos,m1uv, m2pos,m2uv) );
                break;
            case 3:
                newTris.AddRange( Subdivide_ThreeEdges(tri, m0pos,m0uv, m1pos,m1uv, m2pos,m2uv) );
                break;
        }

        return newTris;
    }

    // 한 개 에지만 subdiv => Tri 2개
    private static List<SlopeSubdivider.SubTri> Subdivide_OneEdge(
        SlopeSubdivider.SubTri tri,
        bool s0, bool s1, bool s2,
        Vector3 m0pos, Vector2 m0uv,
        Vector3 m1pos, Vector2 m1uv,
        Vector3 m2pos, Vector2 m2uv
    )
    {
        var list = new List<SlopeSubdivider.SubTri>(2);
        var v0=tri.v0; var v1=tri.v1; var v2=tri.v2;
        var u0=tri.uv0; var u1=tri.uv1; var u2=tri.uv2;

        if(s0)
        {
            // e0=(v0->v1)
            list.Add( MakeTri(v0,   m0pos, v2,  u0,   m0uv, u2) );
            list.Add( MakeTri(m0pos,v1,    v2,  m0uv, u1,   u2) );
        }
        else if(s1)
        {
            // e1=(v1->v2)
            list.Add( MakeTri(v1,   m1pos, v0,  u1,   m1uv, u0) );
            list.Add( MakeTri(m1pos,v2,    v0,  m1uv, u2,   u0) );
        }
        else if(s2)
        {
            // e2=(v2->v0)
            list.Add( MakeTri(v2,   m2pos, v1,  u2,   m2uv, u1) );
            list.Add( MakeTri(m2pos,v0,    v1,  m2uv, u0,   u1) );
        }
        return list;
    }

    // 두 개 에지 => Tri 3개
    private static List<SlopeSubdivider.SubTri> Subdivide_TwoEdges(
        SlopeSubdivider.SubTri tri,
        bool s0, bool s1, bool s2,
        Vector3 m0pos, Vector2 m0uv,
        Vector3 m1pos, Vector2 m1uv,
        Vector3 m2pos, Vector2 m2uv
    )
    {
        var list = new List<SlopeSubdivider.SubTri>(3);
        var v0=tri.v0; var v1=tri.v1; var v2=tri.v2;
        var u0=tri.uv0; var u1=tri.uv1; var u2=tri.uv2;

        if(s0 && s1 && !s2)
        {
            // e0,e1
            list.Add( MakeTri(v0, m0pos, m1pos,   u0, m0uv, m1uv) );
            list.Add( MakeTri(m0pos, v1, m1pos,   m0uv, u1, m1uv) );
            list.Add( MakeTri(v2, v0, m1pos,      u2, u0,  m1uv) );
        }
        else if(s1 && s2 && !s0)
        {
            // e1,e2
            list.Add( MakeTri(v1, m1pos, m2pos,   u1, m1uv, m2uv) );
            list.Add( MakeTri(m1pos, v2, m2pos,   m1uv, u2, m2uv) );
            list.Add( MakeTri(v0, v1, m2pos,      u0, u1,  m2uv) );
        }
        else if(s2 && s0 && !s1)
        {
            // e2,e0
            list.Add( MakeTri(v2, m2pos, m0pos,   u2, m2uv, m0uv) );
            list.Add( MakeTri(m2pos, v0, m0pos,   m2uv, u0, m0uv) );
            list.Add( MakeTri(v1, v2, m0pos,      u1, u2, m0uv) );
        }
        return list;
    }

    // 세 개 에지 => Tri 4개
    private static List<SlopeSubdivider.SubTri> Subdivide_ThreeEdges(
        SlopeSubdivider.SubTri tri,
        Vector3 m0pos, Vector2 m0uv,
        Vector3 m1pos, Vector2 m1uv,
        Vector3 m2pos, Vector2 m2uv
    )
    {
        var list = new List<SlopeSubdivider.SubTri>(4);
        var v0=tri.v0; var v1=tri.v1; var v2=tri.v2;
        var u0=tri.uv0; var u1=tri.uv1; var u2=tri.uv2;

        // 4개 삼각형
        list.Add( MakeTri(v0,   m0pos, m2pos,   u0,   m0uv,  m2uv) );
        list.Add( MakeTri(m0pos,v1,    m1pos,   m0uv, u1,    m1uv) );
        list.Add( MakeTri(m2pos,m1pos, v2,      m2uv, m1uv,  u2) );
        list.Add( MakeTri(m0pos,m1pos, m2pos,   m0uv, m1uv,  m2uv) );
        return list;
    }

    private static (Vector3, Vector2) GetOrCreateMidpoint(
        SlopeSubdivider.EdgeKey eKey,
        Vector3 a, Vector3 b,
        Vector2 ua, Vector2 ub,
        System.Func<float, float, float> heightSampler,
        Dictionary<SlopeSubdivider.EdgeKey, SlopeSubdivider.EdgeMidpoint> edgeDict
    )
    {
        if (edgeDict.TryGetValue(eKey, out var mid))
        {
            return (mid.pos, mid.uv);
        }
        else
        {
            Vector3 mp = 0.5f*(a+b);
            Vector2 muv= 0.5f*(ua+ub);

            float h = heightSampler(muv.x, muv.y);
            mp.y = h;

            edgeDict[eKey] = new SlopeSubdivider.EdgeMidpoint{ pos=mp, uv=muv };
            return (mp,muv);
        }
    }

    /// <summary>
    /// 새 삼각형에 triID 할당 (TriIDProvider 사용)
    /// </summary>
    private static SlopeSubdivider.SubTri MakeTri(
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector2 uv0, Vector2 uv1, Vector2 uv2
    )
    {
        return new SlopeSubdivider.SubTri
        {
            triID = TriIDProvider.GetNewID(),
            removed = false,
            v0 = v0, v1 = v1, v2 = v2,
            uv0 = uv0, uv1 = uv1, uv2 = uv2
        };
    }
}
