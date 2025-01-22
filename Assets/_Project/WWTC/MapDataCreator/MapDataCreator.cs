using System.Collections.Generic;
using System.Linq;           // For OfType, FirstOrDefault, etc.
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapDataCreator : MonoBehaviour
{
    [Title("Map Data Creator")]
    [FolderPath(AbsolutePath = false)]
    [SerializeField]
    private string folderPath = "Assets/";

    [SerializeField]
    private string fileName = "NewMapDataSO";

    /// <summary> MapDataSO 참조 </summary>
    [SerializeField]
    public MapDataSO CurrentMapData;

    [SerializeField]
    private int maxShrinkRepeat = 5; // 최대 반복 횟수

    /// <summary>
    /// 이 오브젝트에 달려 있는 MapDataSystem들을 순서대로 담아둘 리스트
    /// (Boundary, BoundaryChopper, CellPolygonCleaner, TestShrinker, OffsetLineIntersectionSplitter, ClosedSegmentPruner 등)
    /// </summary>
    [SerializeField, ReadOnly]
    private List<MapDataSystem> mapDataSystems = new List<MapDataSystem>();

    private void OnValidate()
    {
        // 현재 GameObject에 달려 있는 MapDataSystem을 Inspector 순서대로 수집
        mapDataSystems = new List<MapDataSystem>(GetComponents<MapDataSystem>());
    }

#if UNITY_EDITOR
    [HorizontalGroup("ButtonRow", 0.5f)]
    [Button("Create & Assign MapDataSO")]
    public void CreateNewMapDataSO()
    {
        // 새 MapDataSO를 에셋으로 생성 (에디터 전용)
        MapDataSO newMapData = ScriptableObject.CreateInstance<MapDataSO>();
        string rawPath = System.IO.Path.Combine(folderPath, fileName + ".asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(rawPath);

        AssetDatabase.CreateAsset(newMapData, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CurrentMapData = newMapData;

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newMapData;
    }
#endif

    // ─────────────────────────────────────────────────────
    // GenerateAllSystems:
    //   1) Boundary → BoundaryChopper → CellPolygonCleaner
    //   2) 반복(TestShrinker → OffsetLineIntersectionSplitter → ClosedSegmentPruner):
    //      => 닫힌 선분을 "CellPolygon"으로 변환 후, cellPolygons 갱신
    //      => prunerIterations에 폴리곤 기록
    // ─────────────────────────────────────────────────────
    [HorizontalGroup("ButtonRow", 0.5f)]
    [GUIColor(0, 1, 0)]
    [Button("Generate All Systems")]
    public void GenerateAllSystems()
    {
        if (CurrentMapData == null)
        {
            Debug.LogError("MapDataSO가 할당되지 않았습니다. 먼저 Create & Assign 버튼을 눌러 생성하거나, 직접 할당해주세요.");
            return;
        }

        // 필요 시 이전에 저장된 prunerIterations를 Clear
        CurrentMapData.prunerIterations.Clear();

        // (1) 선행 시스템: Boundary → BoundaryChopper → CellPolygonCleaner
        RunSystem<Boundary>();
        RunSystem<BoundaryChopper>();
        RunSystem<CellPolygonCleaner>();

        // 이후 CurrentMapData.cellPolygons 에 초기 폴리곤이 생성되었다고 가정

        // (2) 반복: Shrinker → IntersectionSplitter → Pruner
        TestShrinker shrinker = GetSystem<TestShrinker>();
        OffsetLineIntersectionSplitter splitter = GetSystem<OffsetLineIntersectionSplitter>();
        ClosedSegmentPruner pruner = GetSystem<ClosedSegmentPruner>();

        if (shrinker == null || splitter == null || pruner == null)
        {
            Debug.LogWarning("[GenerateAllSystems] 필요한 스크립트(TestShrinker, OffsetLineIntersectionSplitter, ClosedSegmentPruner)가 누락되었습니다.");
            return;
        }

        // 첫 offset
        float currentOffset = CurrentMapData.shrinkStartOffset;

        for (int iteration = 0; iteration < maxShrinkRepeat; iteration++)
        {
            // (a) Shrinker에 offset 세팅
            shrinker.SetOffset(currentOffset);

            // (b) 세 시스템 순차 실행
            shrinker.Generate();
            splitter.Generate();
            pruner.Generate();

            // (c) Pruner가 끝난 뒤, offsetLineGroups => "CellPolygon" 복원
            List<CellPolygon> newPolyList = BuildPolygonsFromClosedLines(
                CurrentMapData.offsetLineGroups,
                iteration // iterationIndex
            );

            // (d) 새로 복원된 폴리곤을 MapDataSO.cellPolygons로 갱신
            CurrentMapData.cellPolygons = newPolyList;

            // (e) prunerIterations에 기록(= "현재 단계의 폴리곤" 저장)
            SavePrunerIteration(iteration, currentOffset, newPolyList);

            // (f) 다음 offset 값을 설정 (처음 1회 이후엔 shrinkOffset 사용)
            currentOffset = CurrentMapData.shrinkOffset;

            // (선택) 더 이상 폴리곤이 없으면 중단
            if (newPolyList.Count == 0)
            {
                Debug.Log($"[GenerateAllSystems] No polygons remain after iteration={iteration}. Stop.");
                break;
            }
        }

        // 추가 후속 시스템들
        RunSystem<PrunerConverter>();
        RunSystem<LineDivider>();
        // RunSystem<TestMeshDataGenerator>();

        Debug.Log("[GenerateAllSystems] 모든 작업 완료.");
    }

    /// <summary>
    /// 선분 그룹(OffsetLineGroups)을 "닫힌 폴리곤"으로 복원한 뒤, 
    /// iterationIndex 등을 활용해 List<CellPolygon>을 만들어 반환
    /// 
    /// *** 여기서 폴리곤을 만든 직후, 시계(CW)라면 Reverse()하여
    ///     반시계(CCW)로 통일하는 로직을 추가 ***
    /// </summary>
    private List<CellPolygon> BuildPolygonsFromClosedLines(List<OffsetLineGroup> lineGroups, int iterationIndex)
    {
        var newPolygons = new List<CellPolygon>();

        foreach (var group in lineGroups)
        {
            if (group.lines == null || group.lines.Count == 0)
                continue;

            // 한 그룹이 하나의 폐곡선이라 가정
            List<Vector2> polygonPoints = ReconstructPolygonFromLines(group.lines);
            if (polygonPoints.Count < 3)
                continue;

            // ──────────────────────────────
            // (추가) CW/CCW 통일: CCW가 되도록
            // ──────────────────────────────
            if (IsClockwise(polygonPoints))
            {
                polygonPoints.Reverse(); // CW라면 Reverse -> CCW
            }

            float areaVal = ComputePolygonArea(polygonPoints);
            Vector2 centerVal = ComputePolygonCenter(polygonPoints, areaVal);

            CellPolygon poly = new CellPolygon
            {
                cellKey = group.cellKey, // 원본 polygon의 cellKey
                points  = polygonPoints,
                center  = centerVal,
                area    = Mathf.Abs(areaVal)
            };

            newPolygons.Add(poly);
        }

        return newPolygons;
    }

    /// <summary>
    /// group.lines가 "연결된 선분"이라 가정하고, 순서대로 이어서 폐곡선 정점을 얻는다.
    /// (실제로는 더 복잡한 정렬 로직이 필요할 수 있음)
    /// </summary>
    private List<Vector2> ReconstructPolygonFromLines(List<LineSegment2D> lines)
    {
        var points = new List<Vector2>();
        if (lines.Count == 0) return points;

        var first = lines[0];
        Vector2 startPt  = first.start;
        Vector2 currentEnd = first.end;
        points.Add(startPt);

        bool[] used = new bool[lines.Count];
        used[0] = true;
        int lineCount = lines.Count;

        while (true)
        {
            bool foundNext = false;

            for (int i = 0; i < lineCount; i++)
            {
                if (used[i]) continue;

                var ls = lines[i];
                // "currentEnd"와 이어진 선분 찾기 (start or end가 currentEnd와 같다면)
                if (Approximately(ls.start, currentEnd, 1e-5f))
                {
                    points.Add(ls.start);
                    points.Add(ls.end);
                    used[i] = true;
                    currentEnd = ls.end;
                    foundNext = true;
                    break;
                }
                else if (Approximately(ls.end, currentEnd, 1e-5f))
                {
                    // 뒤집힌 경우
                    points.Add(ls.end);
                    points.Add(ls.start);
                    used[i] = true;
                    currentEnd = ls.start;
                    foundNext = true;
                    break;
                }
            }

            if (!foundNext) break;
        }

        points = CleanUpPoints(points);
        return points;
    }

    private bool Approximately(Vector2 a, Vector2 b, float eps)
    {
        return (a - b).sqrMagnitude < eps * eps;
    }

    /// <summary>
    /// 중복 점 제거, 맨 마지막 점=처음 점이 같으면 제거
    /// </summary>
    private List<Vector2> CleanUpPoints(List<Vector2> raw)
    {
        var result = new List<Vector2>();
        for (int i = 0; i < raw.Count; i++)
        {
            var ptA = raw[i];
            var ptB = raw[(i + 1) % raw.Count];
            if (!Approximately(ptA, ptB, 1e-5f))
            {
                result.Add(ptA);
            }
        }

        if (result.Count > 2 && Approximately(result[0], result[result.Count - 1], 1e-5f))
        {
            result.RemoveAt(result.Count - 1);
        }
        return result;
    }

    private float ComputePolygonArea(List<Vector2> pts)
    {
        float area = 0f;
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % n];
            area += (a.x * b.y - b.x * a.y);
        }
        return area * 0.5f;
    }

    private Vector2 ComputePolygonCenter(List<Vector2> pts, float area)
    {
        float cx = 0f, cy = 0f;
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % n];
            float cross = (a.x * b.y - b.x * a.y);
            cx += (a.x + b.x) * cross;
            cy += (a.y + b.y) * cross;
        }
        float factor = 1f / (6f * area);
        cx *= factor;
        cy *= factor;
        return new Vector2(cx, cy);
    }

    /// <summary>
    /// 면적 기법으로 폴리곤이 시계(CW)인지 확인
    /// area<0이면 CW, area>0이면 CCW
    /// </summary>
    private bool IsClockwise(List<Vector2> pts)
    {
        float area = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            var c1 = pts[i];
            var c2 = pts[(i + 1) % pts.Count];
            area += (c1.x * c2.y - c2.x * c1.y);
        }
        return (area < 0f);
    }

    /// <summary>
    /// 반복 수행 후(혹은 매번) Pruner의 결과 폴리곤을 prunerIterations에 기록
    /// </summary>
    private void SavePrunerIteration(int iterationIndex, float offsetValue, List<CellPolygon> polygons)
    {
        PrunerIterationData iterationData = new PrunerIterationData
        {
            iterationIndex = iterationIndex,
            offsetValue    = offsetValue,
            polygons       = polygons
        };

        CurrentMapData.prunerIterations.Add(iterationData);

        Debug.Log($"[SavePrunerIteration] iteration={iterationIndex}, offset={offsetValue}, polygons={polygons.Count}");
    }

    /// <summary>
    /// 특정 타입(T)의 MapDataSystem을 찾아서 반환
    /// </summary>
    private T GetSystem<T>() where T : MapDataSystem
    {
        return mapDataSystems.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// T 타입 시스템을 찾아 Generate() 호출
    /// </summary>
    private void RunSystem<T>() where T : MapDataSystem
    {
        var system = GetSystem<T>();
        if (system != null)
        {
            system.Generate();
        }
    }
}
