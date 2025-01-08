using UnityEngine;

public class HeightmapSampler : IHeightSampler
{
    private Texture2D _tex;
    private float _sizeX, _sizeZ, _maxH;

    public HeightmapSampler(Texture2D tex, float sizeX, float sizeZ, float maxH)
    {
        _tex = tex;
        _sizeX = sizeX;
        _sizeZ = sizeZ;
        _maxH  = maxH;
    }

    public float SampleHeight(float wx, float wz)
    {
        if (!_tex) return 0f;
        float u = Mathf.Clamp01(wx/_sizeX);
        float v = Mathf.Clamp01(wz/_sizeZ);
        Color c = _tex.GetPixelBilinear(u, v);
        return c.r * _maxH;
    }

    public float SampleHeightUV(float u, float v)
    {
        if (!_tex) return 0f;
        Color c = _tex.GetPixelBilinear(u, v);
        return c.r * _maxH;
    }
}