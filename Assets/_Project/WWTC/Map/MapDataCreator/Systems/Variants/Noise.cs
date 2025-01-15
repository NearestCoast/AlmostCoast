using UnityEngine;

/// <summary>
/// 노이즈(Perlin Noise 등)를 통해 높이값을 얻어오기 위한 클래스.
/// MapDataSystem을 상속받고, OnDrawGizmo와 GenerateSystem을 구현.
/// </summary>
[System.Serializable]
public class Noise : MapDataSystem
{
    [Header("Noise Settings")]
    [SerializeField]
    private float scale = 1f;

    [SerializeField]
    private float amplitude = 1f;

    public override void OnDrawGizmo()
    {
        if (!DrawGizmo) return;

        // 노이즈 관련 기즈모 (간단히 원점에 Sphere)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(Vector3.zero, 0.5f);
    }

    // 노이즈 값 샘플링
    public float GetValue(float x, float z)
    {
        float noiseVal = Mathf.PerlinNoise(x * scale, z * scale);
        return noiseVal * amplitude;
    }

    /// <summary>
    /// 부모 Generate 버튼이 클릭되면 이 로직이 실행됨
    /// </summary>
    protected override void GenerateSystem()
    {
        // 예) 노이즈 기반 HeightMap을 생성하거나, mapData에 어떤 정보를 기록
        Debug.Log($"Noise GenerateSystem: Using MapData [{mapData.name}]");
        // 간단 예시
        float sample = GetValue(10f, 5f);
        Debug.Log($"Noise sample (10,5): {sample}");
    }
}