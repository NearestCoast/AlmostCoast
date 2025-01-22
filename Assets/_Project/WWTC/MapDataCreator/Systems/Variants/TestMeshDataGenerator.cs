#if UNITY_EDITOR
using UnityEditor;  // Handles.Label
#endif

using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class TestMeshDataGenerator : MapDataSystem
{
    [FoldoutGroup("Triangle Settings")] [SerializeField, Min(0.01f)]
    private float triangleSide = 1f;

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

        apexCellPolygons = new List<CellPolygon>();
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

        so.cellPolygons = apexCellPolygons;
    }

    private List<CellPolygon> apexCellPolygons;
    private void ProcessPolygonRecursively(CellPolygon initialPoly, MapDataSO so, int groupIndex)
    {
        CellPolygon currentPoly = initialPoly;
        int iteration = 0;

        while (currentPoly != null && iteration < maxIteration)
        {
            // ring + subdiv
            List<Vector2> ring = BuildOrderedRing(currentPoly);
            List<Vector2> ringSubdiv = SubdivideEdgesByTriangleSide(ring, triangleSide);
            if (ringSubdiv.Count < 3)
                break;

            // Corner 준비
            cornerSet = new HashSet<Vector2>(currentPoly.points);
            cornerNeighbors = new Dictionary<Vector2, (Vector2, Vector2)>();
            PrepareCornerNeighbors(ringSubdiv, currentPoly);

            // cornerIndices
            cornerIndices.Clear();
            for (int i = 0; i < ringSubdiv.Count; i++)
            {
                if (cornerSet.Contains(ringSubdiv[i]))
                    cornerIndices.Add(i);
            }

            cornerIndices.Sort();
            segmentCount = cornerIndices.Count;

            if (segmentCount < 2)
            {
                Debug.LogWarning("[TestMeshDataGenerator] Not enough corners...");
                break;
            }

            // segmentOfRingIndex
            segmentOfRingIndex = new int[ringSubdiv.Count];
            subdivCountPerSegment = new int[segmentCount];
            subdivApexCountPerSegment = new int[segmentCount];

            BuildSegmentMap(ringSubdiv.Count);

            // (추가) ringSubdiv 디버그 라벨
            LabelRingDebugPoints(ringSubdiv);

            // 메시 생성
            Mesh mesh = GenerateMixedTriangles_OnePass(
                ringSubdiv,
                currentPoly,
                out List<ApexRecord> apexRecords,
                out int[] apexPerRingVertex,
                out Dictionary<Vector2, int> ringDict
            );

            if (mesh == null || mesh.vertexCount == 0)
                break;

            // Subdiv Apex를 보정하고 적용
            AdjustSubdivApexPositions(apexRecords, apexPerRingVertex, ringSubdiv);
            UpdateMeshVertices(mesh, apexRecords);
            AddAdditionalTriangles(mesh, ringSubdiv, apexPerRingVertex, ringDict);
            AddCornerLeftSubdivTriangle(mesh, ringSubdiv, apexPerRingVertex, ringDict);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // 결과 저장
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

            // 새로운 폴리곤 정의
            currentPoly = DefineNextPolygon(apexRecords);
            apexCellPolygons.Add(currentPoly);
            iteration++;
        }
    }

    private CellPolygon DefineNextPolygon(List<ApexRecord> apexRecords)
    {
        if (apexRecords == null || apexRecords.Count < 3)
            return null;

        // Step 1: ApexRecord 리스트에서 위치만 추출
        var positions = apexRecords.Select(apex => apex.position).ToList();

        // Step 2: Convex Hull 계산
        var convexHull = CalculateConvexHull(positions);

        if (convexHull.Count < 3)
            return null;

        // Step 3: 폴리곤 생성
        CellPolygon nextPolygon = new CellPolygon
        {
            points = convexHull,
            center = CalculateCentroid(convexHull),
            area = CalculatePolygonArea(convexHull)
        };
        
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
