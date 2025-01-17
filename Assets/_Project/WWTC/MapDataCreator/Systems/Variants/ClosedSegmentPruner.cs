using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// MapDataSystem을 상속하여,
/// MapDataSO.offsetLineGroups 내 존재하는 라인 세그먼트들 중에서
/// 양쪽 끝점이 모두 닫힌 (startClosed && endClosed) 선분만 남기고,
/// 그렇지 않은 선분은 제거하는 클래스.
///
/// OnDrawGizmos에서는, 
///   "각 선분의 양 끝이 모두 닫혀있으면 => 녹색,
///    하나라도 열려있으면 => 빨간색" 
/// 로 그려준다.
///
/// 추가 요구사항:
/// - "열림/닫힘 판별" 버튼(CheckOpenClose)
/// - "열려있으면 제거" 버튼(RemoveOpenedSegments)
/// - Generate()는 위 두 버튼 기능을 순차적으로 호출
///
/// 변경:
/// - 이전에 true(닫힘)였던 점이라도, CheckOpenClose 때마다 무조건 false로 초기화한 뒤
///   다시 연결 여부에 따라 true로 세팅 (즉, "한번 닫힘이면 다시 열리지 않음" 제거).
/// </summary>
public class ClosedSegmentPruner : MapDataSystem
{
    [FoldoutGroup("Prune Settings")]
    [SerializeField]
    private int maxIterations = 5; // 최대 반복 횟수

    /// <summary>
    /// 좌표가 얼마나 가까우면 동일한 점으로 볼지 결정하는 허용 오차.
    /// (CheckOpenClose 시점에 MarkEndpointsIfConnectedOnePass에서 사용)
    /// </summary>
    [FoldoutGroup("Prune Settings")]
    [SerializeField]
    private float epsilon = 1e-5f;

    // -----------------------------
    // 1) 열림/닫힘 판별 버튼
    // -----------------------------
    [Button("Check Open/Close")]
    private void CheckOpenClose()
    {
        var so = mapDataCreator?.CurrentMapData;
        if (so == null || so.offsetLineGroups == null)
        {
            Debug.Log("[CheckOpenClose] No offsetLineGroups to check.");
            return;
        }

        // (A) 모든 선분에 대해 startClosed, endClosed를 **무조건** false로 초기화
        //     (기존에 true였던 것도 이번에는 다시 false로 되돌린 뒤, 재판단)
        for (int g = 0; g < so.offsetLineGroups.Count; g++)
        {
            var group = so.offsetLineGroups[g];
            if (group.lines == null) continue;

            for (int i = 0; i < group.lines.Count; i++)
            {
                var seg = group.lines[i];
                seg.startClosed = false;
                seg.endClosed   = false;

                // 수정한 seg를 리스트에 다시 할당
                group.lines[i] = seg;
            }
        }

        
        // (B) "서로 연결된 점(동일 좌표)"을 찾아서 `closed=true`로 설정
        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;
            MarkEndpointsIfConnectedOnePass(group.lines, epsilon);
        }

        // (C) 각 그룹별로 닫힘/열림 로그 출력
        int groupIndex = 0;
        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null) continue;

            int closedCount = 0;
            int openedCount = 0;

            foreach (var seg in group.lines)
            {
                if (seg.startClosed && seg.endClosed)
                    closedCount++;
                else
                    openedCount++;
            }

            Debug.Log($"[CheckOpenClose] Group[{groupIndex}] => closed={closedCount}, opened={openedCount}");
            groupIndex++;
        }
    }

    // -----------------------------
    // 2) 열린 선분 제거 버튼
    // -----------------------------
    [Button("Remove Opened Segments")]
    private void RemoveOpenedSegments()
    {
        var so = mapDataCreator?.CurrentMapData;
        if (so == null || so.offsetLineGroups == null)
        {
            Debug.Log("[RemoveOpenedSegments] No offsetLineGroups to process.");
            return;
        }

        int iteration = 0;
        bool removedSomething = true;

        // 반복 구조: 최대 maxIterations번
        while (removedSomething && iteration < maxIterations)
        {
            removedSomething = false;
            iteration++;

            int totalRemovedThisPass = 0;
            int totalGroupsProcessed = 0;

            // 그룹별로 순회하며 열린 선분 제거
            foreach (var group in so.offsetLineGroups)
            {
                if (group.lines == null) continue;

                List<LineSegment2D> pruned = new List<LineSegment2D>();
                int removedCount = 0;

                // "양 끝점이 모두 닫힌" 선분만 남기고, 아니면 제거
                foreach (var seg in group.lines)
                {
                    if (seg.startClosed && seg.endClosed)
                    {
                        pruned.Add(seg);
                    }
                    else
                    {
                        removedCount++;
                    }
                }

                // 이번 pass에서 선분이 제거되었다면, 다음 반복을 또 수행할 필요가 있음
                if (removedCount > 0)
                {
                    removedSomething = true;
                    totalRemovedThisPass += removedCount;
                }

                // 갱신
                group.lines = pruned;
                totalGroupsProcessed++;
            }

            Debug.Log($"[RemoveOpenedSegments] Iteration {iteration}, "
                    + $"Groups={totalGroupsProcessed}, Removed={totalRemovedThisPass}");
        }

        Debug.Log($"[RemoveOpenedSegments] Finished after {iteration} iteration(s) (max={maxIterations}).");
    }

    // -----------------------------
    // Generate() = (1)CheckOpenClose + (2)RemoveOpenedSegments
    // -----------------------------
    public override void Generate()
    {
        if (!IsReady) return;

        for (var i = 0; i < maxIterations; i++)
        {
            // 1) 열림/닫힘 판별 (닫힘 상태 실제 업데이트)
            CheckOpenClose();

            // 2) 열려있는 선분 제거
            RemoveOpenedSegments();
        }
    }

    // -----------------------------
    // OnDrawGizmos (각 선분 단위로 색상 결정)
    // -----------------------------
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return; // 부모 클래스의 protected 필드

        var so = mapDataCreator?.CurrentMapData;
        if (so == null || so.offsetLineGroups == null) return;

        // 그룹별 순회
        foreach (var group in so.offsetLineGroups)
        {
            if (group.lines == null || group.lines.Count == 0) 
                continue;

            // 각 선분별로, 양 끝점이 모두 닫혔으면 '초록색', 아니면 '빨간색'
            foreach (var seg in group.lines)
            {
                bool isSegmentClosed = (seg.startClosed && seg.endClosed);
                Gizmos.color = isSegmentClosed ? Color.green : Color.red;

                Vector3 a = new Vector3(seg.start.x, 0f, seg.start.y);
                Vector3 b = new Vector3(seg.end.x,   0f, seg.end.y);
                Gizmos.DrawLine(a, b);
            }
        }
    }

    // ================================================================================
    // ★ "한 번의 pass" 메서드: 선분 끝점이 서로 동일 좌표이면 closed=true로 설정
    // ================================================================================
    private bool MarkEndpointsIfConnectedOnePass(List<LineSegment2D> lines, float eps)
    {
        if (lines == null) return false;

        bool changed = false;
        int count = lines.Count;

        for (int i = 0; i < count; i++)
        {
            var si = lines[i];
            Vector2 sI = si.start;
            Vector2 eI = si.end;
            bool sIc   = si.startClosed;
            bool eIc   = si.endClosed;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;
                var sj = lines[j];
                Vector2 sJ = sj.start;
                Vector2 eJ = sj.end;
                bool sJc   = sj.startClosed;
                bool eJc   = sj.endClosed;

                // sI ~ sJ
                if ((sI - sJ).sqrMagnitude < eps*eps)
                {
                    if (!sIc) { sIc = true; changed = true; }
                    if (!sJc) { sJc = true; changed = true; }
                }
                // sI ~ eJ
                if ((sI - eJ).sqrMagnitude < eps*eps)
                {
                    if (!sIc) { sIc = true; changed = true; }
                    if (!eJc) { eJc = true; changed = true; }
                }
                // eI ~ sJ
                if ((eI - sJ).sqrMagnitude < eps*eps)
                {
                    if (!eIc) { eIc = true; changed = true; }
                    if (!sJc) { sJc = true; changed = true; }
                }
                // eI ~ eJ
                if ((eI - eJ).sqrMagnitude < eps*eps)
                {
                    if (!eIc) { eIc = true; changed = true; }
                    if (!eJc) { eJc = true; changed = true; }
                }

                // 다시 segJ에 반영
                sj.startClosed = sJc;
                sj.endClosed   = eJc;
                lines[j] = sj;
            }

            si.startClosed = sIc;
            si.endClosed   = eIc;
            lines[i] = si;
        }

        return changed;
    }
}
