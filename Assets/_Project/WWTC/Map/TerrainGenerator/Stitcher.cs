using UnityEngine;
using System.Collections.Generic;

public static class Stitcher
{
    /// <summary>
    /// 단계적 T-점 보정 + Hole Fill:
    ///  1) 기존 삼각형들의 T-점만 부분 Subdivide
    ///  2) Hole Fill
    ///  3) 새로 생성된 삼각형은 이번에는 건너뛰고, 다음 호출에서 처리
    /// </summary>
    public static void StitchTriangles(
        List<SlopeSubdivider.SubTri> triList,
        Dictionary<SlopeSubdivider.EdgeKey, SlopeSubdivider.EdgeMidpoint> edgeDict,
        System.Func<float, float, float> heightSampler
    )
    {
        // (1) 기존 삼각형 목록과 새로 만들어질 삼각형 목록을 분리
        //     => "oldTris"는 이번에 T-점 보정을 받을 대상
        //     => "newTris"는 이번 호출에서 생성된 삼각형 (검사X)
        var oldTris = new List<SlopeSubdivider.SubTri>();
        var newTris = new List<SlopeSubdivider.SubTri>();

        foreach(var t in triList)
        {
            if(t.removed) continue;
            // triID가 "이번 프레임(또는 이전 단계)에서 생성된 것인지"
            //  구별이 필요하다면, SubTri에 "genFrame"같은 필드를 둬서 표시 가능
            // 여기서는 간단히 "triID< 0"은 이번에 만든 삼각형, 등으로 구분해도 됨
            // 예시로 "triID>=0"인 것은 기존, "triID<0"인 것은 새로 생성" 이런 식.

            // 일단은 "slopeSubdiv이나 partialSubdiv" 쪽에서 triID<0 식으로 할 수도...
            // 여기서는 편의상 triID>=0 => old
            if(t.triID >=0) oldTris.Add(t);
            else            newTris.Add(t);
        }

        // (2) oldTris에 대해서만 Adjacency를 빌드
        var adjacency = BuildAdjacency(oldTris);

        // (3) T-점 보정(Partial Subdivide)
        var outList = new List<SlopeSubdivider.SubTri>(triList.Count*2);
        var visited = new HashSet<int>();

        foreach(var tri in oldTris)
        {
            if(tri.removed) 
            {
                outList.Add(tri);
                continue;
            }
            if(visited.Contains(tri.triID))
            {
                outList.Add(tri);
                continue;
            }

            // T-점 에지 찾기
            var edgesToStitch = FindTjunctionEdges(tri, adjacency, oldTris, edgeDict);
            if(edgesToStitch.Count==0)
            {
                // T-점 없음 => 그대로
                outList.Add(tri);
            }
            else
            {
                // 부분 Subdivide
                tri.removed= true;
                visited.Add(tri.triID);

                var newSubTris = PartialSubdivider.PartialSubdivideTriangle(tri, edgesToStitch, heightSampler, edgeDict);

                // 새로 만들어진 삼각형들의 triID를 <0 등으로 설정 -> "이번에 만든 것" 표시
                foreach(var st in newSubTris)
                {
                    st.triID = -1; // 예: 이번 호출에서 생성
                }
                outList.AddRange(newSubTris);
            }
        }

        // (4) oldTris 처리 끝. 나머지 newTris(이미 있었던 or 앞서 분류된)도 outList에 합침
        //     이들은 이번에는 T-점 검사 안 함
        outList.AddRange(newTris);

        // triList 교체
        triList.Clear();
        triList.AddRange(outList);

        // (5) Hole Fill (단, oldTris에 대해서만) => 새로 생긴 삼각형은 이번엔 제외
        FillHoles(triList, edgeDict, heightSampler);

        // (6) 이제 triList에는 "이번에 만든 삼각형"도 포함되어 있지만,
        //     "이번" 호출 중에는 재검사하지 않음 -> 다음번 StitchTriangles에서 oldTris로 편입
    }

    //-------------------------------------------------------------------------
    // Hole Fill 로직: oldTris만 검사 (새로 생긴 triID<0은 제외)
    //-------------------------------------------------------------------------
    private static void FillHoles(
        List<SlopeSubdivider.SubTri> triList,
        Dictionary<SlopeSubdivider.EdgeKey, SlopeSubdivider.EdgeMidpoint> edgeDict,
        System.Func<float,float,float> heightSampler
    )
    {
        // (A) oldTris만 골라서 edge->tris 매핑
        var oldTris= new List<SlopeSubdivider.SubTri>();
        foreach(var t in triList)
        {
            if(t.removed) continue;
            if(t.triID>=0) oldTris.Add(t);  // triID<0은 이번에 만든 삼각형
        }

        var edge2tris= BuildEdgeToTrianglesMap(oldTris);

        // (B) subdiv된 에지 중 triID가 1개 이하 -> 구멍. 메꿀 삼각형 만들기
        var newTris= new List<SlopeSubdivider.SubTri>();
        foreach(var eKey in edgeDict.Keys)
        {
            if(!edge2tris.TryGetValue(eKey, out var triIDs) || triIDs.Count<=1)
            {
                // => 구멍 or 경계
                //  eKey + eKey midpoint => 새 tri를 하나 생성
                //  ...
                //  아래는 예시 코드 생략 or 간단히 작성
                //  => 만들어진 삼각형 triID=-2 등으로 표시
            }
        }

        // ...
        // triList.AddRange(newTris);
    }

    //--------------------------------------------------------------------------
    // 아래는 기존 로직(Adjacency, AreAdjacent, FindTjunctionEdges 등)
    //--------------------------------------------------------------------------
    private static Dictionary<int, List<int>> BuildAdjacency(List<SlopeSubdivider.SubTri> triList)
    {
        var result= new Dictionary<int, List<int>>(triList.Count);
        // O(N²) 작업이지만, oldTris만 하면 "새 삼각형 증가"로 인한 폭발을 한 번 피할 수 있음
        for(int i=0; i< triList.Count; i++)
        {
            var st= triList[i];
            if(st.removed) continue;
            if(!result.ContainsKey(st.triID))
                result[st.triID]= new List<int>();
        }

        for(int i=0; i< triList.Count; i++)
        {
            var tA= triList[i];
            if(tA.removed) continue;

            for(int j=i+1; j< triList.Count; j++)
            {
                var tB= triList[j];
                if(tB.removed) continue;

                if(AreAdjacent(tA, tB))
                {
                    result[tA.triID].Add(tB.triID);
                    if(!result.ContainsKey(tB.triID))
                        result[tB.triID]= new List<int>();
                    result[tB.triID].Add(tA.triID);
                }
            }
        }
        return result;
    }

    private static bool AreAdjacent(SlopeSubdivider.SubTri A, SlopeSubdivider.SubTri B)
    {
        var Av= new[]{ A.v0, A.v1, A.v2 };
        var Bv= new[]{ B.v0, B.v1, B.v2 };
        int sameCount=0;
        foreach(var a in Av)
        {
            foreach(var b in Bv)
            {
                if((a-b).sqrMagnitude< 1e-10f) sameCount++;
            }
        }
        return (sameCount>=2);
    }

    private static List<SlopeSubdivider.EdgeKey> FindTjunctionEdges(
        SlopeSubdivider.SubTri tri,
        Dictionary<int, List<int>> adjacency,
        List<SlopeSubdivider.SubTri> triList,
        Dictionary<SlopeSubdivider.EdgeKey,SlopeSubdivider.EdgeMidpoint> edgeDict
    )
    {
        var result= new List<SlopeSubdivider.EdgeKey>();
        if(!adjacency.ContainsKey(tri.triID)) return result;

        var neighbors= adjacency[tri.triID];
        var edges= new[]{
            new SlopeSubdivider.EdgeKey(tri.v0, tri.v1),
            new SlopeSubdivider.EdgeKey(tri.v1, tri.v2),
            new SlopeSubdivider.EdgeKey(tri.v2, tri.v0)
        };

        foreach(var nbID in neighbors)
        {
            var nbTri= triList.Find(x => x.triID== nbID);
            if(nbTri==null || nbTri.removed) continue;

            var nbEdges= new[]{
                new SlopeSubdivider.EdgeKey(nbTri.v0, nbTri.v1),
                new SlopeSubdivider.EdgeKey(nbTri.v1, nbTri.v2),
                new SlopeSubdivider.EdgeKey(nbTri.v2, nbTri.v0)
            };

            for(int i=0; i<3; i++)
            {
                var e= edges[i];
                bool triEdgeSubdiv= edgeDict.ContainsKey(e);

                bool foundNb= false;
                bool nbEdgeSubdiv= false;
                for(int j=0; j<3; j++)
                {
                    if(e.Equals(nbEdges[j]))
                    {
                        foundNb= true;
                        nbEdgeSubdiv= edgeDict.ContainsKey(nbEdges[j]);
                        break;
                    }
                }

                // 한쪽만 subdiv => T-점
                if(foundNb && (triEdgeSubdiv != nbEdgeSubdiv))
                {
                    if(!result.Contains(e)) 
                        result.Add(e);
                }
            }
        }
        return result;
    }

    //--------------------------------------------------------------------------
    // Hole Fill에 필요한 edge->tris 매핑
    //--------------------------------------------------------------------------
    private static Dictionary<SlopeSubdivider.EdgeKey, List<int>> 
        BuildEdgeToTrianglesMap(List<SlopeSubdivider.SubTri> triList)
    {
        var dict= new Dictionary<SlopeSubdivider.EdgeKey, List<int>>();
        for(int i=0; i< triList.Count; i++)
        {
            var tri= triList[i];
            if(tri.removed) continue;

            var e0= new SlopeSubdivider.EdgeKey(tri.v0, tri.v1);
            var e1= new SlopeSubdivider.EdgeKey(tri.v1, tri.v2);
            var e2= new SlopeSubdivider.EdgeKey(tri.v2, tri.v0);

            AddEdge(dict, e0, tri.triID);
            AddEdge(dict, e1, tri.triID);
            AddEdge(dict, e2, tri.triID);
        }
        return dict;
    }
    private static void AddEdge(
        Dictionary<SlopeSubdivider.EdgeKey,List<int>> dict,
        SlopeSubdivider.EdgeKey e, int triID
    )
    {
        if(!dict.TryGetValue(e, out var list))
        {
            list= new List<int>();
            dict[e]= list;
        }
        if(!list.Contains(triID)) list.Add(triID);
    }
}
