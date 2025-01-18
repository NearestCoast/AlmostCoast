using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// UniTask 관련
using Cysharp.Threading.Tasks;
using System; // TimeSpan 사용
using System.Threading; // CancellationTokenSource 등

/// <summary>
/// Odin Inspector를 사용하여 MapDataSO를 생성/할당하는 에디터용 MonoBehaviour.
/// 여러 MapDataSystem(Boundary, BoundaryChopper 등)을 한 GameObject에 연달아 넣고,
/// 이 클래스에서 만든 버튼을 통해 각 시스템의 Generate 함수를 순차적으로 호출합니다.
///
/// * 수정: GenerateAllSystemsBatchRepeat 실행 시,
///   매 반복마다 MapDataSO의 seed를 무작위로 바꾸고,
///   취소 버튼(CancelBatchRepeat)으로 진행 중 작업을 중단할 수 있게 함.
/// </summary>
public class MapDataCreator : MonoBehaviour
{
    [Title("Map Data Creator")]

    [FolderPath(AbsolutePath = false)]
    [SerializeField]
    private string folderPath = "Assets/";

    [SerializeField]
    private string fileName = "NewMapDataSO";

    /// <summary>
    /// MapDataSO 참조
    /// </summary>
    [SerializeField]
    public MapDataSO CurrentMapData;

    /// <summary>
    /// 같은 GameObject에 달려 있는 MapDataSystem들을 순서대로 담아둘 리스트
    /// </summary>
    [SerializeField]
    private List<MapDataSystem> mapDataSystems = new List<MapDataSystem>();

    // ─────────────────────────────────────────────
    // Inspector에서 반복 횟수를 설정
    // ─────────────────────────────────────────────
    [Title("Batch Generate Settings")]
    [SerializeField, Min(1)]
    private int repeatCount = 3;  // GenerateAllSystems를 실행할 횟수

    // 반복 실행 시, MapDataSO.noiseSeed를 무작위 설정하기 위한 범위
    [FoldoutGroup("Random Seed Range")]
    [SerializeField] private int minRandomSeed = 0;
    [FoldoutGroup("Random Seed Range")]
    [SerializeField] private int maxRandomSeed = 999999;

    // 진행 중인 반복 실행을 취소하기 위한 CancellationTokenSource
    private CancellationTokenSource batchCTS;

    private void OnValidate()
    {
        // 현재 GameObject에 달려 있는 MapDataSystem 컴포넌트를
        // Inspector 순서(추가된 순서)대로 가져와 리스트에 담아둡니다.
        mapDataSystems = new List<MapDataSystem>(GetComponents<MapDataSystem>());
    }

    [HorizontalGroup("ButtonRow", 0.5f)]
    [Button("Create & Assign MapDataSO")]
    public void CreateNewMapDataSO()
    {
#if UNITY_EDITOR
        // 새 MapDataSO 생성
        MapDataSO newMapData = ScriptableObject.CreateInstance<MapDataSO>();
        string rawPath = System.IO.Path.Combine(folderPath, fileName + ".asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(rawPath);

        // 에셋 생성 및 저장
        AssetDatabase.CreateAsset(newMapData, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // CurrentMapData 갱신
        CurrentMapData = newMapData;

        // 에디터 포커스
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newMapData;
#else
        Debug.LogError("에셋 생성은 Unity 에디터 환경에서만 가능합니다.");
#endif
    }

    [HorizontalGroup("ButtonRow", 0.5f)]
    [GUIColor(0, 1, 0)] // 초록색
    [Button("Generate All Systems")]
    public void GenerateAllSystems()
    {
        if (CurrentMapData == null)
        {
            Debug.LogError("MapDataSO가 할당되지 않았습니다. Create & Assign 버튼을 눌러 새로 생성하거나, " +
                           "기존 에셋을 할당해주세요.");
            return;
        }

        foreach (var system in mapDataSystems)
        {
            if (system == null)
            {
                Debug.LogWarning("MapDataSystems 리스트에 null이 포함되어 있습니다. 스킵합니다.");
                continue;
            }
            system.Generate();
        }
    }

    // ─────────────────────────────────────────────
    // "GenerateAllSystemsBatchRepeat" 버튼과
    // "CancelBatchRepeat" 버튼을 가로로 두기
    // ─────────────────────────────────────────────
    [HorizontalGroup("BatchButtons", 0.5f)]
    [Button("GenerateAllSystems (Batch Repeat)")]
    private async UniTaskVoid GenerateAllSystemsBatchRepeat()
    {
        if (CurrentMapData == null)
        {
            Debug.LogError("MapDataSO가 할당되지 않았습니다. 먼저 CreateNewMapDataSO로 생성/할당해주세요.");
            return;
        }

        // 기존에 작업 중인 cts가 있다면, 먼저 Cancel하고 Dispose
        if (batchCTS != null)
        {
            batchCTS.Cancel();
            batchCTS.Dispose();
            batchCTS = null;
        }

        // 새로운 CTS 생성
        batchCTS = new CancellationTokenSource();
        var token = batchCTS.Token;

        Debug.Log($"[GenerateAllSystemsBatchRepeat] {repeatCount}회 반복 실행을 5초 간격으로 시작합니다.");

        for (int i = 1; i <= repeatCount; i++)
        {
            // 토큰이 취소되었는지 확인
            if (token.IsCancellationRequested)
            {
                Debug.Log("[GenerateAllSystemsBatchRepeat] 취소됨(Cancel).");
                break;
            }

            // ① 랜덤 seed 설정
            int newSeed = UnityEngine.Random.Range(minRandomSeed, maxRandomSeed + 1);
            CurrentMapData.noiseSeed = newSeed;

            Debug.Log($"[GenerateAllSystemsBatchRepeat] {i}/{repeatCount}회차: 무작위 seed={newSeed}로 설정.");

            // ② GenerateAllSystems 실행
            GenerateAllSystems();

            // ③ 5초 대기
            if (i < repeatCount)
            {
                Debug.Log("[GenerateAllSystemsBatchRepeat] 다음 실행까지 5초 대기 중...");
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    // 취소 시 예외 발생
                    Debug.Log("[GenerateAllSystemsBatchRepeat] 취소 예외(Canceled) catch.");
                    break;
                }
            }
        }

        Debug.Log("[GenerateAllSystemsBatchRepeat] 모든 반복 실행이 완료되었거나, 취소되었습니다.");

        // 작업 끝난 후 CTS 정리
        batchCTS.Dispose();
        batchCTS = null;
    }

    [HorizontalGroup("BatchButtons", 0.5f)]
    [Button("Cancel Batch Repeat")]
    private void CancelBatchRepeat()
    {
        if (batchCTS != null)
        {
            Debug.Log("[CancelBatchRepeat] 반복 실행 취소 요청.");
            batchCTS.Cancel();
        }
        else
        {
            Debug.Log("[CancelBatchRepeat] 현재 진행 중인 반복 실행이 없습니다.");
        }
    }
}
