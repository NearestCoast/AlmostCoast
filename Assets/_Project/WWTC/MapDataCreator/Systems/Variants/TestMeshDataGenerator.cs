#if UNITY_EDITOR
using UnityEditor;  // Handles.Label
#endif

using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class TestMeshDataGenerator : MapDataSystem
{
    [FoldoutGroup("Triangle Settings")] [SerializeField]
    private float triangleSide = 1f;

    [SerializeField] private float cornerMultiplier = 0.9f;
    [SerializeField] private float subdivMultiplier = 0.75f;

    [SerializeField] private int maxIteration = 5;

    [FoldoutGroup("Gizmo Settings")] [SerializeField]
    private bool drawGizmoMesh = true;

    [FoldoutGroup("Gizmo Settings")] [SerializeField]
    private bool drawLabel = true;

    [FoldoutGroup("Gizmo Settings")] [SerializeField]
    private float apexRadius = 0.1f;

    // ─────────────────────────────────────────────────────────────────
    // 디버그용 점 목록
    // ─────────────────────────────────────────────────────────────────
    [FoldoutGroup("Debug Points"), SerializeField]
    private List<DebugPoint> debugPoints = new List<DebugPoint>();

    // ─────────────────────────────────────────────────────────────────
    // *** apexDataGroups 필드를 유지 (삭제하지 않음) ***
    // ─────────────────────────────────────────────────────────────────
    [FoldoutGroup("Debug Apex"), SerializeField]
    private List<ApexDataGroup> apexDataGroups = new List<ApexDataGroup>();

    // Corner 관련
    private HashSet<Vector2> cornerSet;
    private Dictionary<Vector2, (Vector2 left, Vector2 right)> cornerNeighbors;

    // ringSubdiv에서 corner 인덱스 목록
    private List<int> cornerIndices = new List<int>();
    private int[] segmentOfRingIndex;
    private int segmentCount;

    // segment별 subdiv, subdiv apex 카운터
    private int[] subdivCountPerSegment;
    private int[] subdivApexCountPerSegment;

    // ─────────────────────────────────────────────────────────────────
    // Apex 관련
    // ─────────────────────────────────────────────────────────────────
    private enum ApexType
    {
        Subdiv,
        Corner
    }

    private struct ApexRecord
    {
        public Vector2 position;
        public ApexType apexType;
        public int meshVertexIndex;
        public int ringIndex;
    }

    [System.Serializable]
    public class ApexDataGroup
    {
        public string groupName;
        public float groupKey;
        public List<Vector2> apexPositions = new List<Vector2>();
    }

    [System.Serializable]
    public class DebugPoint
    {
        public string pointName;
        public Vector3 position;
    }

    // ─────────────────────────────────────────────────────────────────
    // Generate()
    // ─────────────────────────────────────────────────────────────────
    public override void Generate()
    {
        if (!IsReady) return;

        var so = mapDataCreator.CurrentMapData;

        // 기존 기능: 매번 새로 메시를 만든다면, 예전 데이터 정리
        so.generatedMeshDataList.Clear();

        // *** apexDataGroups 필드 다시 활용: 여기서 Clear 후 재사용 ***
        apexDataGroups.Clear();
        debugPoints.Clear();

        if (so.subdivideCellPolygonGroup == null || so.subdivideCellPolygonGroup.Count == 0)
        {
            Debug.LogWarning("[TestMeshDataGenerator] subdivideCellPolygonGroup is empty.");
            return;
        }

        // 그룹마다 수행
        for (int gIndex = 0; gIndex < so.subdivideCellPolygonGroup.Count; gIndex++)
        {
            var group = so.subdivideCellPolygonGroup[gIndex];
            if (group.polygons == null || group.polygons.Count == 0)
                continue;

            // (1) 가장 면적 큰 폴리곤
            CellPolygon biggestPoly = FindLargestPolygon(group.polygons);
            if (biggestPoly.points == null || biggestPoly.points.Count < 3)
                continue;


            // 반복적으로 작업 수행
            ProcessPolygonRecursively(biggestPoly, so, gIndex);
        }
    }

    private void ProcessPolygonRecursively(CellPolygon initialPoly, MapDataSO so, int groupIndex)
    {
        CellPolygon currentPoly = initialPoly;
        int iteration = 0;

        while (currentPoly != null && iteration < maxIteration)
        {
            // (1) ring + subdiv
            List<Vector2> ring = BuildOrderedRing(currentPoly);

            List<Vector2> ringSubdiv = (iteration == 0)
                ? SubdivideEdgesByTriangleSide(ring, triangleSide)
                : SubdivideEdgesByCurrentPoly(currentPoly);

            if (ringSubdiv.Count < 3)
                break;

            // (2) Corner 준비, segment 매핑 (기존 로직)
            cornerSet = new HashSet<Vector2>(currentPoly.points);
            cornerNeighbors = new Dictionary<Vector2, (Vector2, Vector2)>();
            PrepareCornerNeighbors(ringSubdiv, currentPoly);

            cornerIndices.Clear();
            for (int i = 0; i < ringSubdiv.Count; i++)
            {
                if (cornerSet.Contains(ringSubdiv[i]))
                    cornerIndices.Add(i);
            }

            cornerIndices.Sort();
            segmentCount = cornerIndices.Count;
            if (segmentCount < 2) break;

            segmentOfRingIndex = new int[ringSubdiv.Count];
            subdivCountPerSegment = new int[segmentCount];
            subdivApexCountPerSegment = new int[segmentCount];
            BuildSegmentMap(ringSubdiv.Count);

            LabelRingDebugPoints(ringSubdiv);

            // (3) 메시 생성
            Mesh mesh = GenerateMixedTriangles_OnePass(
                ringSubdiv,
                currentPoly,
                out List<ApexRecord> apexRecords,
                out int[] apexPerRingVertex,
                out Dictionary<Vector2, int> ringDict
            );

            if (mesh == null || mesh.vertexCount == 0)
                break;

            // (4) Apex 보정, 메시 보강
            AdjustSubdivApexPositions(apexRecords, apexPerRingVertex, ringSubdiv);
            UpdateMeshVertices(mesh, apexRecords);
            AddAdditionalTriangles(mesh, ringSubdiv, apexPerRingVertex, ringDict);
            AddCornerLeftSubdivTriangle(mesh, ringSubdiv, apexPerRingVertex, ringDict);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // (5) iteration 결과 저장
            var gmd = new GeneratedMeshData
            {
                cellKey = currentPoly.cellKey,
                vertices = mesh.vertices,
                triangles = mesh.triangles,
                uv = mesh.uv
            };
            so.generatedMeshDataList.Add(gmd);

            var apexDataGroup = new ApexDataGroup
            {
                groupName = $"Group #{groupIndex}_Iter{iteration}",
                groupKey = currentPoly.cellKey,
                apexPositions = apexRecords.Select(ar => ar.position).ToList()
            };
            apexDataGroups.Add(apexDataGroup);

            // ───────────────────────────────────────────────────────────────────
            // (6) Corner Apex & Subdiv Apex 거리 체크. 하나라도 임계값 이하이면 중단
            // ───────────────────────────────────────────────────────────────────

            bool cornerTooClose = CheckCornerApexDistanceBelowThreshold(apexRecords, triangleSide, cornerMultiplier);
            bool subdivTooClose = CheckSubdivApexDistanceBelowThreshold(apexRecords, triangleSide, subdivMultiplier);

            if (cornerTooClose || subdivTooClose)
            {
                Debug.Log(
                    $"[TestMeshDataGenerator] CornerTooClose={cornerTooClose}, SubdivTooClose={subdivTooClose}. Stop iteration here.");
                break;
            }

            // (7) 다음 폴리곤으로
            currentPoly = DefineNextPolygon(apexRecords);
            iteration++;
        }
    }


    /// <summary>
    /// Corner Apex들 끼리의 거리를 검사.
    /// 하나라도 (triangleSide * thresholdMultiplier) 이하이면 true
    /// </summary>
    private bool CheckCornerApexDistanceBelowThreshold(
        List<ApexRecord> apexRecords,
        float triangleSide,
        float thresholdMultiplier
    )
    {
        var cornerApex = apexRecords
            .Where(ar => ar.apexType == ApexType.Corner)
            .ToList();

        if (cornerApex.Count < 2) return false;

        float threshold = triangleSide * thresholdMultiplier;
        for (int i = 0; i < cornerApex.Count; i++)
        {
            for (int j = i + 1; j < cornerApex.Count; j++)
            {
                float dist = Vector2.Distance(cornerApex[i].position, cornerApex[j].position);
                if (dist < threshold)
                    return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Subdiv Apex들 끼리의 거리를 검사.
    /// 하나라도 (triangleSide * thresholdMultiplier) 이하이면 true
    /// </summary>
    private bool CheckSubdivApexDistanceBelowThreshold(
        List<ApexRecord> apexRecords,
        float triangleSide,
        float thresholdMultiplier
    )
    {
        var subdivApex = apexRecords
            .Where(ar => ar.apexType == ApexType.Subdiv)
            .ToList();

        if (subdivApex.Count < 2) return false;

        float threshold = triangleSide * thresholdMultiplier;
        for (int i = 0; i < subdivApex.Count; i++)
        {
            for (int j = i + 1; j < subdivApex.Count; j++)
            {
                float dist = Vector2.Distance(subdivApex[i].position, subdivApex[j].position);
                if (dist < threshold)
                    return true;
            }
        }

        return false;
    }







    /// <summary>
    /// 이미 subdiv된 currentPoly(코너 + subdivEdges)로부터 ringSubdiv를 구성하여 반환.
    /// iteration==0(첫 번째) 이후의 반복에서, 추가로 subdiv할 필요가 없을 때 사용.
    /// </summary>
    private List<Vector2> SubdivideEdgesByCurrentPoly(CellPolygon poly)
    {
        var ringSubdiv = new List<Vector2>();

        if (poly.points == null || poly.points.Count < 3)
            return ringSubdiv; // 코너가 3개 미만이면 유효하지 않으므로 빈 리스트 반환

        int cornerCount = poly.points.Count;
        // poly.subdivEdges.Count가 cornerCount와 동일하거나, cornerCount와 같거나 작다고 가정
        // (각 코너 ~ 다음 코너 사이의 subdiv 정보를 담고 있기 때문)

        for (int i = 0; i < cornerCount; i++)
        {
            // 먼저 코너(Corner)를 추가
            ringSubdiv.Add(poly.points[i]);

            // 해당 코너와 다음 코너 사이의 subdiv apex 리스트를 이어 붙인다.
            // 예: i번째 subdivEdges => 코너 i ~ 코너 (i+1)
            if (i < poly.subdivEdges.Count)
            {
                ringSubdiv.AddRange(poly.subdivEdges[i].edgePoints);
            }
        }

        // ringSubdiv는 코너 + subdiv apex가 순서대로 이어진 "닫힌 폴리곤" 형태
        return ringSubdiv;
    }

    private CellPolygon DefineNextPolygon(List<ApexRecord> apexRecords)
    {
        if (apexRecords == null || apexRecords.Count < 3)
            return null;

        // 1) Corner / Subdiv 그룹으로 분리
        var cornerRecords = apexRecords
            .Where(ar => ar.apexType == ApexType.Corner)
            .OrderBy(ar => ar.ringIndex) // ringIndex 기준 정렬
            .ToList();

        var subdivRecords = apexRecords
            .Where(ar => ar.apexType == ApexType.Subdiv)
            .OrderBy(ar => ar.ringIndex) // ringIndex 기준 정렬
            .ToList();

        // Corner가 3개 미만이면 폴리곤 불가
        if (cornerRecords.Count < 3)
            return null;

        // 2) 코너들만으로 폴리곤의 points & cornerPoints 구성
        var cornerPositions = cornerRecords.Select(cr => cr.position).ToList();

        // 3) CellPolygon 생성
        var nextPolygon = new CellPolygon
        {
            points = cornerPositions, // 코너만
            cornerPoints = cornerPositions, // cornerPoints도 동일
            subdivEdges = new List<SubdivEdge>()
        };

        // 4) SubdivEdges 구성
        //    Corner i 와 Corner (i+1) 사이에 속하는 Subdiv Apex를 찾아 SubdivEdge를 만든다.
        int cornerCount = cornerRecords.Count;
        for (int i = 0; i < cornerCount; i++)
        {
            // 현재 코너와 다음 코너
            var cornerA = cornerRecords[i];
            var cornerB = cornerRecords[(i + 1) % cornerCount]; // wrap-around
            int ringA = cornerA.ringIndex;
            int ringB = cornerB.ringIndex;

            // ringIndex가 A < B 인 경우: (A, A+1 ... B-1) 사이 subdiv
            // ringIndex가 A > B 인 경우: (A, A+1 ... (마지막 Corner)) + (0 ... B-1)
            // 다만, 실제로 ringIndex가 wrap될 수 있으므로, 
            // 단순 비교가 어려울 때는 "폴리곤 한 바퀴"를 고려하는 로직을 짜야 함.

            // 여기서는 간단히 "ringIndex가 A와 B 사이"인 Subdiv만 골라서 SubdivEdge를 구성.
            // (필요하다면 더 정교한 로직으로 바꿀 수 있음)

            var edgeSubdiv = new List<Vector2>();
            if (ringA < ringB)
            {
                // A+1 <= x < B
                var between = subdivRecords
                    .Where(sr => sr.ringIndex > ringA && sr.ringIndex < ringB)
                    .Select(sr => sr.position);
                edgeSubdiv.AddRange(between);
            }
            else if (ringA > ringB)
            {
                // (A+1 ... maxRing) + (0 ... B-1)
                var between1 = subdivRecords
                    .Where(sr => sr.ringIndex > ringA)
                    .Select(sr => sr.position);
                var between2 = subdivRecords
                    .Where(sr => sr.ringIndex < ringB)
                    .Select(sr => sr.position);
                edgeSubdiv.AddRange(between1);
                edgeSubdiv.AddRange(between2);
            }
            // ringA == ringB 인 경우는 특수 케이스 - 일반적으로 corner가 같을 수는 없으나, 
            // 만약 ringIndex가 같다면 subdiv 없음.

            // SubdivEdge에 저장
            SubdivEdge newEdge = new SubdivEdge
            {
                // edgePoints: (CornerA ~ CornerB) 사이의 subdiv apex들
                edgePoints = edgeSubdiv.ToList()
            };

            nextPolygon.subdivEdges.Add(newEdge);
        }

        // 5) center & area 계산
        nextPolygon.center = CalculateCentroid(nextPolygon.points);
        nextPolygon.area = CalculatePolygonArea(nextPolygon.points);

        return nextPolygon;
    }


    private List<Vector2> CalculateConvexHull(List<Vector2> points)
    {
        // Convex Hull 알고리즘 (예: Graham's Scan 또는 Gift Wrapping 알고리즘)
        points = points.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
        List<Vector2> hull = new List<Vector2>();

        // Lower Hull
        foreach (var p in points)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }

        // Upper Hull
        int t = hull.Count + 1;
        for (int i = points.Count - 1; i >= 0; i--)
        {
            var p = points[i];
            while (hull.Count >= t && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }

        hull.RemoveAt(hull.Count - 1); // 마지막 점 제거 (중복)
        return hull;
    }

    private float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }

    private Vector2 CalculateCentroid(List<Vector2> points)
    {
        float x = 0, y = 0;
        foreach (var p in points)
        {
            x += p.x;
            y += p.y;
        }

        return new Vector2(x / points.Count, y / points.Count);
    }

    private float CalculatePolygonArea(List<Vector2> points)
    {
        float area = 0;
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % n];
            area += (current.x * next.y) - (next.x * current.y);
        }

        return Mathf.Abs(area) * 0.5f;
    }

    // ─────────────────────────────────────────────────────────────────
    // *** 컴파일 오류 해결: LabelRingDebugPoints 메서드 정의 ***
    // ringSubdiv 각 점에 대해 Corner/Subdiv 라벨을 붙여 debugPoints에 기록
    // ─────────────────────────────────────────────────────────────────
    private void LabelRingDebugPoints(List<Vector2> ringSubdiv)
    {
        for (int i = 0; i < ringSubdiv.Count; i++)
        {
            var pos2D = ringSubdiv[i];
            string label;
            if (cornerSet.Contains(pos2D))
            {
                int cornerLocal = cornerIndices.IndexOf(i);
                label = $"Corner[{cornerLocal}]";
            }
            else
            {
                int seg = segmentOfRingIndex[i];
                int subdivId = subdivCountPerSegment[seg]++;
                label = $"Subdiv(Corner {seg}->{(seg + 1) % segmentCount}) [{subdivId}]";
            }

            debugPoints.Add(new DebugPoint
            {
                pointName = label,
                position = new Vector3(pos2D.x, 0f, pos2D.y)
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // (기존) 코너 Apex, Subdiv Apex 생성 → 메시(verts/tris)
    // ─────────────────────────────────────────────────────────────────
    private Mesh GenerateMixedTriangles_OnePass(
        List<Vector2> ringSubdiv,
        CellPolygon poly,
        out List<ApexRecord> apexRecords,
        out int[] apexPerRingVertex,
        out Dictionary<Vector2, int> ringDict
    )
    {
        apexRecords = new List<ApexRecord>();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        ringDict = new Dictionary<Vector2, int>();
        for (int i = 0; i < ringSubdiv.Count; i++)
        {
            ringDict[ringSubdiv[i]] = i;
            verts.Add(new Vector3(ringSubdiv[i].x, 0f, ringSubdiv[i].y));
        }

        int n = ringSubdiv.Count;
        apexPerRingVertex = new int[n];
        for (int i = 0; i < n; i++)
            apexPerRingVertex[i] = -1;

        HashSet<Vector2> cornerUsed = new HashSet<Vector2>();

        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = ringSubdiv[i];
            Vector2 p1 = ringSubdiv[(i + 1) % n];

            // (A) Subdiv Apex => p0
            if (!checkSkipEdge(p0, p1))
            {
                float dist = Vector2.Distance(p0, p1);
                if (dist > 1e-5f)
                {
                    float height = dist * Mathf.Sqrt(3f) * 0.5f;
                    Vector2 mid = 0.5f * (p0 + p1);
                    Vector2 dir = (p1 - p0).normalized;
                    Vector2 normal = new Vector2(-dir.y, dir.x);

                    Vector2 test = mid + normal * (0.5f * height);
                    if (!IsPointInPolygon(test, ringSubdiv))
                        normal = -normal;

                    Vector2 apexPos = mid + normal * height;

                    int apexV = verts.Count;
                    verts.Add(new Vector3(apexPos.x, 0f, apexPos.y));

                    int i0 = ringDict[p0];
                    int i1 = ringDict[p1];
                    tris.Add(i0); // 기준 정점
                    tris.Add(apexV); // Apex 정점
                    tris.Add(i1); // 다음 정점


                    var rec = new ApexRecord
                    {
                        position = apexPos,
                        apexType = ApexType.Subdiv,
                        meshVertexIndex = apexV,
                        ringIndex = i // p0
                    };
                    apexRecords.Add(rec);

                    if (apexPerRingVertex[i] < 0)
                        apexPerRingVertex[i] = apexV;
                }
            }

            // (B) Corner Apex => p0
            if (cornerSet.Contains(p0) && !cornerUsed.Contains(p0))
            {
                cornerUsed.Add(p0);
                if (cornerNeighbors.TryGetValue(p0, out var pair))
                {
                    float distCA = Vector2.Distance(p0, pair.left);
                    float distCB = Vector2.Distance(p0, pair.right);
                    float side = Mathf.Min(distCA, distCB);

                    float height = side * Mathf.Sqrt(3f) * 0.5f;
                    Vector2 mid = 0.5f * (pair.left + pair.right);
                    Vector2 dir = (mid - p0).normalized;

                    Vector2 apexTest = p0 + dir * height;
                    if (!IsPointInPolygon(apexTest, ringSubdiv))
                    {
                        dir = -dir;
                        apexTest = p0 + dir * height;
                    }

                    int cV = ringDict[p0];
                    int aV = ringDict[pair.left];
                    int bV = ringDict[pair.right];

                    int apexV = verts.Count;
                    verts.Add(new Vector3(apexTest.x, 0f, apexTest.y));

                    // Corner -> 삼각형 2개
                    tris.Add(cV);
                    tris.Add(aV);
                    tris.Add(apexV);

                    tris.Add(cV);
                    tris.Add(apexV);
                    tris.Add(bV);

                    var rec = new ApexRecord
                    {
                        position = apexTest,
                        apexType = ApexType.Corner,
                        meshVertexIndex = apexV,
                        ringIndex = i
                    };
                    apexRecords.Add(rec);

                    apexPerRingVertex[i] = apexV;
                }
            }
        }

        // mesh
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();

        Vector2[] uv = new Vector2[mesh.vertexCount];
        for (int i = 0; i < uv.Length; i++)
        {
            Vector3 v3 = mesh.vertices[i];
            uv[i] = new Vector2(v3.x, v3.z);
        }

        mesh.uv = uv;

        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────
    // (추가) Subdiv Apex를 "양쪽 Corner Apex 직선" 위로 투영
    //       + Corner Apex끼리 기존 보정 로직
    // ─────────────────────────────────────────────────────────────────
    private void AdjustSubdivApexPositions(
        List<ApexRecord> apexRecords,
        int[] apexPerRingVertex,
        List<Vector2> ringSubdiv
    )
    {
        // (1) 기존: Corner끼리 Reposition
        var cornerIndicesInApex = new List<int>();
        for (int i = 0; i < apexRecords.Count; i++)
        {
            if (apexRecords[i].apexType == ApexType.Corner)
                cornerIndicesInApex.Add(i);
        }

        if (cornerIndicesInApex.Count >= 2)
        {
            for (int c = 0; c < cornerIndicesInApex.Count - 1; c++)
            {
                int cA = cornerIndicesInApex[c];
                int cB = cornerIndicesInApex[c + 1];
                RepositionSubdivApexBetween(apexRecords, cA, cB, false);
            }

            int lastC = cornerIndicesInApex[cornerIndicesInApex.Count - 1];
            int firstC = cornerIndicesInApex[0];
            if (lastC != firstC)
            {
                RepositionSubdivApexBetween(apexRecords, lastC, firstC, true);
            }
        }

        // (2) Subdiv Apex → "같은 세그먼트 양쪽 Corner Apex" 직선상 투영
        for (int i = 0; i < apexRecords.Count; i++)
        {
            if (apexRecords[i].apexType != ApexType.Subdiv)
                continue;

            int ringIdx = apexRecords[i].ringIndex;
            if (ringIdx < 0 || ringIdx >= ringSubdiv.Count)
                continue;

            // 세그먼트
            int seg = segmentOfRingIndex[ringIdx];
            if (seg < 0 || seg >= segmentCount)
                continue;

            // 세그먼트 양쪽 corner
            int cornerA = cornerIndices[seg];
            int cornerB = cornerIndices[(seg + 1) % segmentCount];

            int cornerApexA = apexPerRingVertex[cornerA];
            int cornerApexB = apexPerRingVertex[cornerB];
            if (cornerApexA < 0 || cornerApexB < 0)
                continue; // Corner Apex가 없으면 투영 불가

            // cornerApexA, cornerApexB를 apexRecords에서 찾는다
            var apexCornerA = apexRecords.FirstOrDefault(ar => ar.meshVertexIndex == cornerApexA);
            var apexCornerB = apexRecords.FirstOrDefault(ar => ar.meshVertexIndex == cornerApexB);

            if (apexCornerA.apexType != ApexType.Corner || apexCornerB.apexType != ApexType.Corner)
                continue;

            // 투영
            Vector2 subdivPos = apexRecords[i].position;
            Vector2 cornerPosA = apexCornerA.position;
            Vector2 cornerPosB = apexCornerB.position;

            Vector2 projected = ProjectPointOnLineSegment(subdivPos, cornerPosA, cornerPosB);

            // 반영
            apexRecords[i] = new ApexRecord
            {
                position = projected,
                apexType = apexRecords[i].apexType,
                meshVertexIndex = apexRecords[i].meshVertexIndex,
                ringIndex = apexRecords[i].ringIndex
            };
        }
    }

    /// <summary>
    /// 한 점을 선분에 Orthogonal Projection
    /// </summary>
    private Vector2 ProjectPointOnLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 AB = lineEnd - lineStart;
        float len2 = AB.sqrMagnitude;
        if (len2 < 1e-8f) return lineStart;

        float t = Vector2.Dot(point - lineStart, AB) / len2;
        t = Mathf.Clamp01(t);
        return lineStart + AB * t;
    }

    private void RepositionSubdivApexBetween(
        List<ApexRecord> apexRecords,
        int cornerAIndex,
        int cornerBIndex,
        bool wrap
    )
    {
        Vector2 cApos = apexRecords[cornerAIndex].position;
        Vector2 cBpos = apexRecords[cornerBIndex].position;
        int total = apexRecords.Count;

        if (!wrap)
        {
            if (cornerBIndex <= cornerAIndex + 1) return;
            int start = cornerAIndex + 1;
            int end = cornerBIndex - 1;
            if (end < start) return;
            int count = end - start + 1;
            for (int s = 0; s < count; s++)
            {
                float t = (s + 1f) / (count + 1f);
                Vector2 np = Vector2.Lerp(cApos, cBpos, t);

                var tmp = apexRecords[start + s];
                tmp.position = np;
                apexRecords[start + s] = tmp;
            }
        }
        else
        {
            List<int> idxList = new List<int>();
            for (int i = cornerAIndex + 1; i < total; i++)
            {
                if (i == cornerBIndex) break;
                idxList.Add(i);
            }

            if (cornerBIndex > 0)
            {
                for (int i = 0; i < cornerBIndex; i++)
                {
                    idxList.Add(i);
                }
            }

            int c = idxList.Count;
            for (int s = 0; s < c; s++)
            {
                float t = (s + 1f) / (c + 1f);
                Vector2 np = Vector2.Lerp(cApos, cBpos, t);

                var tmp = apexRecords[idxList[s]];
                tmp.position = np;
                apexRecords[idxList[s]] = tmp;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Subdiv Apex 위치 보정 후 -> 메시 정점 좌표 반영
    // ─────────────────────────────────────────────────────────────────
    private void UpdateMeshVertices(Mesh mesh, List<ApexRecord> apexRecords)
    {
        var v = mesh.vertices;
        for (int i = 0; i < apexRecords.Count; i++)
        {
            var ar = apexRecords[i];
            int idx = ar.meshVertexIndex;
            if (idx >= 0 && idx < v.Length)
            {
                v[idx] = new Vector3(ar.position.x, 0f, ar.position.y);
            }
        }

        mesh.vertices = v;
    }

    // ─────────────────────────────────────────────────────────────────
    // (기존) 연속된 ring i, i+1에 Apex 있으면 삼각형
    // ─────────────────────────────────────────────────────────────────
    private void AddAdditionalTriangles(
        Mesh mesh,
        List<Vector2> ringSubdiv,
        int[] apexPerRingVertex,
        Dictionary<Vector2, int> ringDict
    )
    {
        var oldTris = mesh.triangles;
        var newTris = new List<int>(oldTris);

        int n = ringSubdiv.Count;
        for (int i = 0; i < n; i++)
        {
            int nextI = (i + 1) % n;
            int apexA = apexPerRingVertex[i];
            int apexB = apexPerRingVertex[nextI];

            if (apexA < 0 || apexB < 0)
                continue;

            int ringVertexIndex = ringDict[ringSubdiv[nextI]];
            newTris.Add(apexA);
            newTris.Add(apexB);
            newTris.Add(ringVertexIndex);
        }

        mesh.triangles = newTris.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────
    // (추가) "코너 i + 왼쪽 Subdiv(i-1) + 왼쪽 Subdiv Apex(i-1)" 삼각형
    // ─────────────────────────────────────────────────────────────────
    private void AddCornerLeftSubdivTriangle(
        Mesh mesh,
        List<Vector2> ringSubdiv,
        int[] apexPerRingVertex,
        Dictionary<Vector2, int> ringDict
    )
    {
        var oldTris = mesh.triangles;
        var newTris = new List<int>(oldTris);

        int n = ringSubdiv.Count;
        for (int i = 0; i < n; i++)
        {
            // ring i가 Corner인지
            if (!cornerSet.Contains(ringSubdiv[i]))
                continue;

            // corner apex
            int cornerApexIdx = apexPerRingVertex[i];
            if (cornerApexIdx < 0)
                continue;

            int left = (i - 1 + n) % n;
            // 왼쪽이 Corner라면 Subdiv가 아니므로
            if (cornerSet.Contains(ringSubdiv[left]))
                continue;

            int leftSubdivApexIdx = apexPerRingVertex[left - 1];
            if (leftSubdivApexIdx < 0)
                continue;

            int cornerApexV = cornerApexIdx;
            int leftSubdivV = ringDict[ringSubdiv[left]];
            int leftSubdivApexV = leftSubdivApexIdx;

            newTris.Add(leftSubdivV);
            newTris.Add(leftSubdivApexV);
            newTris.Add(cornerApexV);
        }

        mesh.triangles = newTris.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────
    // 보조 메서드들
    // ─────────────────────────────────────────────────────────────────
    private void BuildSegmentMap(int ringCount)
    {
        if (segmentCount < 2)
        {
            for (int i = 0; i < ringCount; i++)
                segmentOfRingIndex[i] = 0;
            return;
        }

        for (int s = 0; s < segmentCount; s++)
        {
            int start = cornerIndices[s];
            int end = cornerIndices[(s + 1) % segmentCount];
            if (start < end)
            {
                for (int i = start; i <= end; i++)
                    segmentOfRingIndex[i % ringCount] = s;
            }
            else
            {
                for (int i = start; i < ringCount; i++)
                {
                    segmentOfRingIndex[i] = s;
                }

                for (int i = 0; i <= end; i++)
                {
                    segmentOfRingIndex[i] = s;
                }
            }
        }
    }

    private CellPolygon FindLargestPolygon(List<CellPolygon> polys)
    {
        float maxArea = float.MinValue;
        CellPolygon largest = default;
        foreach (var p in polys)
        {
            if (p.area > maxArea)
            {
                maxArea = p.area;
                largest = p;
            }
        }

        return largest;
    }

    private List<Vector2> BuildOrderedRing(CellPolygon poly)
    {
        var ring = new List<Vector2>();
        if (poly.points == null || poly.points.Count < 3)
            return ring;

        int n = poly.points.Count;
        for (int i = 0; i < n; i++)
        {
            ring.Add(poly.points[i]);
            if (i < poly.subdivEdges.Count)
            {
                ring.AddRange(poly.subdivEdges[i].edgePoints);
            }
        }

        return ring;
    }

    private List<Vector2> SubdivideEdgesByTriangleSide(List<Vector2> ring, float side)
    {
        var newRing = new List<Vector2>();
        int n = ring.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 st = ring[i];
            Vector2 ed = ring[(i + 1) % n];
            newRing.Add(st);

            float dist = Vector2.Distance(st, ed);
            if (dist <= side) continue;

            int numDiv = Mathf.FloorToInt(dist / side);
            float gap = dist / (numDiv + 1);
            Vector2 dir = (ed - st).normalized;
            for (int d = 1; d <= numDiv; d++)
            {
                Vector2 sp = st + dir * (gap * d);
                newRing.Add(sp);
            }
        }

        return newRing;
    }

    private void PrepareCornerNeighbors(List<Vector2> ringSubdiv, CellPolygon poly)
    {
        var ringIndex = new Dictionary<Vector2, int>();
        for (int i = 0; i < ringSubdiv.Count; i++)
        {
            ringIndex[ringSubdiv[i]] = i;
        }

        int n = ringSubdiv.Count;
        foreach (var cPos in poly.points)
        {
            if (!ringIndex.ContainsKey(cPos))
                continue;
            int cI = ringIndex[cPos];
            int left = (cI - 1 + n) % n;
            int right = (cI + 1) % n;
            cornerNeighbors[cPos] = (ringSubdiv[left], ringSubdiv[right]);
        }
    }

    private bool checkSkipEdge(Vector2 p0, Vector2 p1)
    {
        if (cornerSet.Contains(p0) && cornerSet.Contains(p1))
            return true;

        if (cornerSet.Contains(p0))
        {
            if (cornerNeighbors.TryGetValue(p0, out var pair))
            {
                if (pair.left == p1 || pair.right == p1)
                    return true;
            }
        }

        if (cornerSet.Contains(p1))
        {
            if (cornerNeighbors.TryGetValue(p1, out var pair2))
            {
                if (pair2.left == p0 || pair2.right == p0)
                    return true;
            }
        }

        return false;
    }

    private bool IsPointInPolygon(Vector2 pt, List<Vector2> polygon)
    {
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector2 v1 = polygon[i];
            Vector2 v2 = polygon[j];
            bool intersect = ((v1.y > pt.y) != (v2.y > pt.y)) &&
                             (pt.x < (v2.x - v1.x) * (pt.y - v1.y) / (v2.y - v1.y) + v1.x);
            if (intersect) inside = !inside;
        }

        return inside;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        // A) debugPoints
        Gizmos.color = Color.cyan;
        float size = apexRadius;
        foreach (var dp in debugPoints)
        {
            Gizmos.DrawSphere(dp.position, size);
            if (drawLabel) Handles.Label(dp.position + Vector3.up * 0.02f, dp.pointName);
        }

        // B) Mesh
        if (!drawGizmoMesh || mapDataCreator == null) return;
        var so = mapDataCreator.CurrentMapData;
        if (so == null) return;

        Gizmos.color = Color.yellow;
        foreach (var gmd in so.generatedMeshDataList)
        {
            if (gmd.vertices == null || gmd.triangles == null)
                continue;

            var vts = gmd.vertices;
            var tri = gmd.triangles;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 v0 = vts[tri[i + 0]];
                Vector3 v1 = vts[tri[i + 1]];
                Vector3 v2 = vts[tri[i + 2]];

                Gizmos.DrawLine(v0, v1);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v0);
            }
        }
    }
#endif
}