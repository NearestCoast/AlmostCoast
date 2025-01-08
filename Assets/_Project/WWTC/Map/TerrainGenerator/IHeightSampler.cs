public interface IHeightSampler
{
    float SampleHeight(float wx, float wz);  // 월드 좌표 -> 높이
    float SampleHeightUV(float u, float v);  // 0..1 범위 -> 높이
}