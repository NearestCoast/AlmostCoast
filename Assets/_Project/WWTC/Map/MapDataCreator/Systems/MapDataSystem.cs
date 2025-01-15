using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 모든 맵 데이터 시스템(Boundary, Noise 등)의 공통 부모 클래스.
/// 기즈모와 함께, 시스템 생성 로직을 수행하는 Generate 버튼을 가짐.
/// </summary>
[System.Serializable]
public abstract class MapDataSystem
{
    [ToggleLeft, SerializeField]
    private bool drawGizmo = true;

    [HideInInspector]
    protected MapDataSO mapData;

    /// <summary>
    /// OnDrawGizmo 호출 시 기즈모를 그릴지 여부
    /// </summary>
    public bool DrawGizmo
    {
        get => drawGizmo;
        set => drawGizmo = value;
    }

    /// <summary>
    /// MapDataSO를 주입받는 함수
    /// </summary>
    public virtual void SetMapData(MapDataSO data)
    {
        mapData = data;
    }

    /// <summary>
    /// Scene 뷰에서 기즈모를 그릴 때 호출 (자식이 오버라이드)
    /// </summary>
    public abstract void OnDrawGizmo();

    /// <summary>
    /// 이 시스템을 실제로 생성(갱신)하는 로직. 
    /// 자식 클래스에서 오버라이드하여 구현.
    /// </summary>
    protected abstract void GenerateSystem();

    /// <summary>
    /// 부모 클래스에 공용 Generate 버튼을 둠.
    /// </summary>
    [Button("Generate")]
    public void Generate()
    {
        if (mapData == null)
        {
            Debug.LogWarning($"{GetType().Name}: MapData is null. Please set MapDataSO first.");
            return;
        }

        // 실제 생성 로직을 자식 클래스에서 구현
        GenerateSystem();
    }
}