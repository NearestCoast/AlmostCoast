using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Character.IngameCharacters.Enemies;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters._Core;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Pathfinder : MonoBehaviour
{
    private Enemy masterEnemy;
    private PlayerCharacter targetCharacter;
    public PlayerCharacter TargetCharacter => targetCharacter;

    private Vector3 nextNodePoint;

    public Vector3 NextNodePoint
    {
        get => nextNodePoint;
        set
        {
            nextNodePoint = value;
            // Debug.Log("Come");
        }
    }
    public float DistanceToTargetCharacter => Vector3.Distance(transform.position, TargetCharacter.transform.position);
    public float DistanceToTarget => Vector3.Distance(transform.position, Target.position);
    public float FlattenedDistanceToTarget => Vector3.Distance(transform.position.XYZ3toX0Z3(), Target.position.XYZ3toX0Z3());
    public float FlattenedDistanceToNextNode => Vector3.Distance(transform.position.XYZ3toX0Z3(), NextNodePoint.XYZ3toX0Z3());
    public bool IsReachedToTarget => FlattenedDistanceToTarget < 0.5f; // 추후 노드사이즈로 변경
    public bool IsReachedToNextNode => FlattenedDistanceToNextNode < 0.5f; // 추후 노드사이즈로 변경

    private void Awake()
    {
        masterEnemy = GetComponentInParent<Enemy>();
        targetCharacter = FindObjectOfType<PlayerCharacter>();
        wanderingWayPoints = new List<Transform>();
        var enemySpot = transform.GetComponentInParent<Character>().transform.parent;
        foreach (var componentsInChild in enemySpot.GetComponentsInChildren<WanderingPointMasterNode>())
        {
            wanderingWayPoints.Add(componentsInChild.transform);
        }

        tempTarget = new GameObject("TempTarget").transform;
        tempTarget.SetParent(masterEnemy.transform.parent);

        surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 
                        1 << LayerMask.NameToLayer("GroundUnlit") | 
                        1 << LayerMask.NameToLayer("Wall");
    }

    private void Start()
    {
        Nodes = new List<Vector3>();
        NextNodePoint = transform.position;
    }

    [ShowInInspector] public Transform Target { get; set; }
    public List<Vector3> Nodes { get; set; }
    private NavMeshPath path;
    public int NextNodeIndex { get; set; }
    public bool IsPathComplete { get; set; }

    public void CalculatePath()
    {
        if (Target == null) return;

        path = new NavMeshPath();
        bool pathFound = NavMesh.CalculatePath(transform.position, Target.position, NavMesh.AllAreas, path);
        Nodes = new List<Vector3>(path.corners);
        
        if (pathFound && path.status == NavMeshPathStatus.PathComplete)
        {
            IsPathComplete = true;
            if (IsWandering)
            {
                NextNodeIndex = 0;
                if (Nodes.Count > NextNodeIndex)
                {
                    NextNodePoint = Nodes[NextNodeIndex];
                }
            }
            else
            {
                if (Nodes.Count > 1)
                {
                    NextNodePoint = Nodes[1];
                }
                else
                {
                    NextNodePoint = transform.position;
                }
            }
        }
        else
        {
            IsPathComplete = false;
            if (Nodes.Count > 1)
            {
                NextNodePoint = Nodes[1];
            }
            else
            {
                NextNodePoint = transform.position;
            }
        }
    }

    public void SetNextNode()
    {
        NextNodeIndex++;
        if (NextNodeIndex < Nodes.Count)
        {
            NextNodePoint = 
                (NextNodeIndex == Nodes.Count - 1) 
                    ? Target.position 
                    : Nodes[NextNodeIndex];
        }
    }

    private Vector3 FlattenPosition(Vector3 position)
    {
        return new Vector3(position.x, 0f, position.z);
    }

    public bool IsWandering { get; set; }

    
    public void SetTargetToPlayer()
    {
        Target = targetCharacter.transform;
    }

    public void SetTargetToSelf() => Target = transform;

    private Transform tempTarget;

    public void SetTargetTo(Vector3 position)
    {
        tempTarget.position = position;
        Target = tempTarget;
    }

    [SerializeField] private float searchUnit = 1;
    private LayerMask surfaceLayers;
    private readonly List<Vector3> candidateTargetPositions = new List<Vector3>();

    // 가중치 그룹을 저장할 Dictionary
    private readonly Dictionary<int, List<Vector3>> weightedTargetGroups = new Dictionary<int, List<Vector3>>();
    private Vector3 maxWeightPosition;

    public void SetTargetRandomly(Vector3 center, float radius)
    {
        maxWeightPosition = SearchTargetPlace();
        SetTargetTo(maxWeightPosition);

        Vector3 SearchTargetPlace()
        {
            candidateTargetPositions.Clear();
            weightedTargetGroups.Clear();

            // 원형 그리드 생성
            for (float x = -radius; x <= radius; x += searchUnit)
            {
                for (float z = -radius; z <= radius; z += searchUnit)
                {
                    Vector3 point = center + new Vector3(x, 0, z);
                    if (Vector3.Distance(center, point) > radius) continue; // 원 바깥 제외

                    // Ray 설정
                    Vector3 rayOrigin = new Vector3(point.x, transform.position.y + 100, point.z);
                    Ray ray = new Ray(rayOrigin, Vector3.down);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 200f, surfaceLayers))
                    {
                        // ledge 체크
                        if (!IsLedge(hit.point))
                        {
                            candidateTargetPositions.Add(hit.point);

                            // 가중치 계산
                            int heightDifference = (int)(hit.point.y - transform.position.y);
                            int weight = heightDifference <= 0 ? 0 : Mathf.Max(1, 10 - heightDifference); // 차이가 0보다 작거나 같으면 weight 0

                            if (!weightedTargetGroups.ContainsKey(weight))
                            {
                                weightedTargetGroups[weight] = new List<Vector3>();
                            }
                            weightedTargetGroups[weight].Add(hit.point);

                        }
                    }
                }
            }

            if (candidateTargetPositions.Count > 0)
            {
                // 각 가중치에 대해 지수적 증가 값을 사용하여 누적 확률 계산
                List<int> weights = weightedTargetGroups.Keys.ToList();
                List<float> cumulativeWeights = new List<float>();
                float totalWeight = 0;

                foreach (var weight in weights)
                {
                    // 가중치의 지수적 증가 값을 사용 (10^weight)
                    float exponentialWeight = Mathf.Pow(10, weight);
                    totalWeight += exponentialWeight;
                    cumulativeWeights.Add(totalWeight);
                }

                // 무작위로 선택된 확률 값
                float randomValue = Random.Range(0, totalWeight);

                // 무작위 값이 속하는 가중치 그룹 찾기
                int selectedWeightIndex = cumulativeWeights.FindIndex(cw => randomValue <= cw);
                int selectedWeight = weights[selectedWeightIndex];
                List<Vector3> selectedGroup = weightedTargetGroups[selectedWeight];

                // 선택된 그룹에서 임의의 점을 반환
                return selectedGroup[Random.Range(0, selectedGroup.Count)];
            }

            return Vector3.zero;
        }

        // ledge 판단 함수
        bool IsLedge(Vector3 point)
        {
            // 8방향 (상하좌우 및 대각선) 탐색
            Vector3[] directions =
            {
                Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized
            };

            foreach (var dir in directions)
            {
                // 각 방향으로 searchUnit의 절반만큼 물러난 지점 설정
                Vector3 checkOrigin = point + dir * (searchUnit / 2) - new Vector3(0, searchUnit / 2, 0);

                // 수평으로 point를 향해 레이 쏘기
                Ray ledgeRay = new Ray(checkOrigin, (point - checkOrigin).normalized);
                if (Physics.Raycast(ledgeRay, out _, searchUnit, surfaceLayers))
                {
                    // 레이에 hit이 발생하면 ledge로 간주
                    return true;
                }
            }

            // 레이에 hit이 발생하지 않으면 ledge가 아님
            return false;
        }
    }

    private List<Transform> wanderingWayPoints; // Wandering Waypoints 목록
    private Transform lastSelectedWaypoint; // 마지막으로 선택된 Waypoint 저장
    private bool isDescending = true; // 현재 내려가는 중인지 여부
    private List<Transform> previousWayPoints = new List<Transform>(); // 이전에 선택된 WayPoints 기록

    public void SetTargetToWayPoint()
    {
        Target = SetWanderWayPoint();
    }
    
    private Transform SetWanderWayPoint()
    {
        // Target이 없거나 wanderingWayPoints 목록에 속하지 않는 경우, 새로운 Target을 랜덤 선택
        if (Target == null || !IsTargetInWanderingWayPointsHierarchy())
        {
            int randomIndex = Random.Range(0, wanderingWayPoints.Count);
            Target = wanderingWayPoints[randomIndex];
            lastSelectedWaypoint = Target;
            previousWayPoints.Clear(); // 새로운 마스터를 선택하므로 기록 초기화
            isDescending = true; // 초기 상태에서는 하위 노드로 내려가는 방향 설정
            return Target;
        }

        if (isDescending)
        {
            // 하위로 내려가는 경우
            if (Target.childCount > 0)
            {
                List<Transform> sameLevelChildren = new List<Transform>();
                foreach (Transform child in Target)
                {
                    sameLevelChildren.Add(child);
                }

                int randomIndex = Random.Range(0, sameLevelChildren.Count);
                Target = sameLevelChildren[randomIndex];
            }
            else
            {
                // 더 이상 하위 노드가 없으므로, 방향을 올라가는 것으로 전환
                isDescending = false;
                Target = Target.parent; // 상위로 올라감
            }
        }
        else
        {
            // 상위로 올라가는 중인 경우
            if (Target.parent == null || Target == lastSelectedWaypoint)
            {
                // 이전에 선택된 적 없는 다른 WayPoint를 선택
                List<Transform> otherWayPoints = new List<Transform>(wanderingWayPoints);
                otherWayPoints.Remove(lastSelectedWaypoint);
                otherWayPoints.RemoveAll(wp => previousWayPoints.Contains(wp));

                if (otherWayPoints.Count > 0)
                {
                    int randomIndex = Random.Range(0, otherWayPoints.Count);
                    Target = otherWayPoints[randomIndex];
                    lastSelectedWaypoint = Target;
                    previousWayPoints.Add(Target); // 선택된 WayPoint 기록
                    isDescending = true; // 다른 WayPoint로 이동한 후, 다시 하위로 내려감
                }
                else
                {
                    // 모든 WayPoint가 사용되었으면 기록 초기화 후 재시작
                    previousWayPoints.Clear();
                    return SetWanderWayPoint(); // 재귀적으로 호출하여 새 WayPoint 선택
                }
            }
            else
            {
                // 상위 노드로 이동
                Target = Target.parent;
            }
        }

        return Target;

        // Target이 wanderingWayPoints 목록 중 하나의 하위 Transform인지 확인하는 로컬 함수
        bool IsTargetInWanderingWayPointsHierarchy()
        {
            Transform current = Target;
            while (current != null)
            {
                if (wanderingWayPoints.Contains(current))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
    
    private void OnDrawGizmos()
    {
        DrawCandidates();
        DrawMinWeightPoint();
        DrawPath();
        
        
        return;
        void DrawPath()
        {
            if (Nodes == null || Nodes.Count == 0) return;

            Gizmos.color = Color.red;
            for (int i = 0; i < Nodes.Count - 1; i++)
            {
                Gizmos.DrawLine(Nodes[i], Nodes[i + 1]);
            }

            // 각 코너에 구를 그려 경로의 노드 포인트를 표시
            Gizmos.color = Color.blue;
            foreach (var node in Nodes)
            {
                Gizmos.DrawSphere(node, 0.2f);
            }
        }

        void DrawCandidates()
        {
            Gizmos.color = Color.red;
            foreach (var position in candidateTargetPositions)
            {
                Gizmos.DrawSphere(position, 0.1f); // 작은 구체로 위치 표시
            }
            
        }

        void DrawMinWeightPoint()
        {
            // minWeightPosition이 설정되어 있으면 해당 위치에 Gizmo를 그리기
            if (maxWeightPosition != Vector3.zero)
            {
                Gizmos.color = Color.green; // Gizmo의 색상을 설정
                Gizmos.DrawSphere(maxWeightPosition, 1); // minWeightPosition에 반지름 0.5의 구를 그림
            }
        }
    }
}
