using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// MapDataSystem을 상속받아, MapDataSO의 offsetLineGroups를 기반으로
/// arrangedCellPolygons를 생성/관리하는 클래스.
///
/// 요구 사항:
/// 1) offsetLineGroup.lines가 2개 이하인 그룹은 폴리곤 생성 대상에서 제외
/// 2) 폴리곤 생성 시, Shoelace 공식 등을 이용해 면적(area)을 계산하고 MapDataSO.arrangedCellPolygons에 저장
/// 3) 생성 후, 면적이 일정 기준(minPolygonArea) 이하인 폴리곤은 제거
/// 4) (Vector3.zero)에 가장 가까운 폴리곤을 찾되, 폴리곤의 면적이 minAreaForPivot 이상인 폴리곤 중에서 찾음
/// 5) 해당 폴리곤의 (x, z)을 (0, 0)으로 이동하도록 모든 arrangedCellPolygons를 동일하게 이동
///
/// arrangedCellPolygons는 MapDataSO 내부에 존재하므로,
/// 직접 MapDataSO.arrangedCellPolygons를 Clear → Add → Remove 하여 관리합니다.
/// </summary>
public class OffsetLineArrangerSystem : MapDataSystem
{
    [FoldoutGroup("Settings"), SerializeField]
    [Tooltip("생성된 폴리곤 중 이 값보다 면적이 작은 폴리곤은 제거됩니다.")]
    private float minPolygonArea = 1f;

    [FoldoutGroup("Settings"), SerializeField]
    [Tooltip("원점(0,0)에 가장 가까운 폴리곤을 찾을 때, 이 값보다 면적이 작은 폴리곤은 제외합니다.")]
    private float minAreaForPivot = 10f;

    public override void Generate()
    {
        base.Generate();

        if (!IsReady)
            return;

        // MapDataSO 참조
        MapDataSO mapData = mapDataCreator.CurrentMapData;

        // offsetLineGroups 읽어오기
        List<OffsetLineGroup> offsetGroups = mapData.offsetLineGroups;

        // arrangedCellPolygons 초기화
        mapData.arrangedCellPolygons.Clear();

        // 1) offsetLineGroups 순회하며 폴리곤 생성
        foreach (OffsetLineGroup group in offsetGroups)
        {
            // 조건: lineSegments가 2개 이하인 경우 건너뛰기
            if (group.lines == null || group.lines.Count <= 2)
            {
                continue;
            }

            // 폴리곤 생성
            CellPolygon newPolygon = new CellPolygon
            {
                cellKey = group.cellKey,
                center = group.center,  // (x, y) -> 실제 (x, z)에 해당
                points = new List<Vector2>(),
                area = 0f
            };

            // OffsetLineGroup의 모든 선분(start, end)을 이어받아 points 구성
            foreach (var line in group.lines)
            {
                newPolygon.points.Add(line.start);
                newPolygon.points.Add(line.end);
            }

            // 면적 계산
            newPolygon.area = CalculateArea(newPolygon.points);

            // arrangedCellPolygons에 추가
            mapData.arrangedCellPolygons.Add(newPolygon);
        }

        // 2) 최소 면적 필터링
        int beforeCount = mapData.arrangedCellPolygons.Count;
        mapData.arrangedCellPolygons.RemoveAll(p => p.area < minPolygonArea);
        int afterCount = mapData.arrangedCellPolygons.Count;

        Debug.Log($"[{nameof(OffsetLineArrangerSystem)}] 폴리곤 생성/필터링 완료." +
                  $"\n  생성된 폴리곤 수: {beforeCount}" +
                  $"\n  최소면적({minPolygonArea}) 미만 제거 후: {afterCount}");

        // 3) '원점에서 가장 가까운 폴리곤' 후보: 면적이 minAreaForPivot 이상인 폴리곤
        List<CellPolygon> pivotCandidates = mapData.arrangedCellPolygons.FindAll(p => p.area >= minAreaForPivot);

        if (pivotCandidates.Count == 0)
        {
            Debug.Log($"[{nameof(OffsetLineArrangerSystem)}] 면적이 {minAreaForPivot} 이상인 폴리곤이 없어, 이동 로직을 스킵합니다.");
            return;
        }

        // 4) 후보 중 원점(0,0)에서 가장 가까운 폴리곤 찾기
        CellPolygon closestPolygon = null;
        float minSqrDist = float.MaxValue;

        foreach (var poly in pivotCandidates)
        {
            // 2D 상에서 (0,0)까지의 제곱 거리
            float sqrDist = poly.center.sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                closestPolygon = poly;
            }
        }

        // 가드(실제로는 pivotCandidates.Count > 0 이므로 null이 되긴 어렵지만 혹시 모르므로)
        if (closestPolygon == null)
        {
            Debug.Log($"[{nameof(OffsetLineArrangerSystem)}] pivotCandidates가 존재하지만 'closestPolygon'을 찾지 못함. 이동 스킵.");
            return;
        }

        // 5) "closestPolygon의 center.x, center.y"가 (0,0)이 되도록 모든 폴리곤 이동
        float shiftX = closestPolygon.center.x;
        float shiftZ = closestPolygon.center.y; // 2D 좌표에선 y가 실제 World의 z

        // 모든 arrangedCellPolygons를 동일한 양만큼 이동
        foreach (var poly in mapData.arrangedCellPolygons)
        {
            // center 이동
            poly.center = new Vector2(poly.center.x - shiftX, poly.center.y - shiftZ);

            // points 이동
            for (int i = 0; i < poly.points.Count; i++)
            {
                Vector2 p = poly.points[i];
                poly.points[i] = new Vector2(p.x - shiftX, p.y - shiftZ);
            }
        }

        // 이동 후, closestPolygon.center는 (0,0)이 됨
        Debug.Log($"[{nameof(OffsetLineArrangerSystem)}] " +
                  $"가장 가까운 폴리곤(key={closestPolygon.cellKey}), 면적={closestPolygon.area}, " +
                  $"(x,z)=({shiftX},{shiftZ}) → (0,0)로 이동 완료.\n" +
                  $"(검색 범위: area >= {minAreaForPivot})");
    }

    /// <summary>
    /// 2D 좌표 리스트로부터 면적을 구하는 함수 (Shoelace 공식)
    /// </summary>
    private float CalculateArea(List<Vector2> points)
    {
        float area = 0f;
        int count = points.Count;
        if (count < 3) return 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % count]; // 마지막은 첫 점과 연결
            area += (p1.x * p2.y - p2.x * p1.y);
        }
        return Mathf.Abs(area) * 0.5f;
    }

    /// <summary>
    /// (선택) Gizmo를 통해 arrangedCellPolygons를 장면에서 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        // MapDataCreator나 CurrentMapData가 없으면 중단
        if (mapDataCreator == null || mapDataCreator.CurrentMapData == null)
            return;

        // Gizmo 색상 설정
        Gizmos.color = Color.magenta;

        // arrangedCellPolygons 가져오기
        var polygons = mapDataCreator.CurrentMapData.arrangedCellPolygons;
        if (polygons == null || polygons.Count == 0)
            return;

        // 각 폴리곤을 화면에 그리기
        foreach (var poly in polygons)
        {
            if (poly.points == null || poly.points.Count < 2)
                continue;

            for (int i = 0; i < poly.points.Count; i++)
            {
                Vector2 start = poly.points[i];
                Vector2 end = poly.points[(i + 1) % poly.points.Count];

                // 2D point를 XZ plane으로 매핑
                Vector3 startPos = new Vector3(start.x, 0f, start.y);
                Vector3 endPos   = new Vector3(end.x,   0f, end.y);
                Gizmos.DrawLine(startPos, endPos);
            }
        }
    }
}
