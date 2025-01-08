using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;  // OdinInspector 사용 시
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UniversalFastNoiseGenerator : MonoBehaviour
{
    //==========================================================
    // FastNoise 타입 확장
    //==========================================================
    public enum FastNoiseType
    {
        Value,
        ValueFractal,
        Perlin,
        PerlinFractal,
        Simplex,
        SimplexFractal,

        Cellular,    // 남겨둘 항목

        WhiteNoise,
        Cubic,
        CubicFractal
    }

    // FastNoise의 프랙탈 유형
    public enum FastFractalType
    {
        FBM,
        Billow,
        RigidMulti
    }

    //==========================================================
    // Inspector에 표시될 파라미터들
    //==========================================================
    [Title("Noise General Settings")]
    [EnumToggleButtons]
    public FastNoiseType noiseType = FastNoiseType.Perlin;

    // ------------------ 프랙탈 ------------------
    [FoldoutGroup("Fractal Params")]
    [ShowIf("@noiseType == FastNoiseType.ValueFractal " +
            "|| noiseType == FastNoiseType.PerlinFractal " +
            "|| noiseType == FastNoiseType.SimplexFractal " +
            "|| noiseType == FastNoiseType.CubicFractal")]
    [LabelText("Fractal Type")]
    public FastFractalType fractalType = FastFractalType.FBM;

    [FoldoutGroup("Fractal Params")]
    [ShowIf("@noiseType == FastNoiseType.ValueFractal " +
            "|| noiseType == FastNoiseType.PerlinFractal " +
            "|| noiseType == FastNoiseType.SimplexFractal " +
            "|| noiseType == FastNoiseType.CubicFractal")]
    [LabelText("Octaves"), Range(1, 8)]
    public int fractalOctaves = 3;

    [FoldoutGroup("Fractal Params")]
    [ShowIf("@noiseType == FastNoiseType.ValueFractal " +
            "|| noiseType == FastNoiseType.PerlinFractal " +
            "|| noiseType == FastNoiseType.SimplexFractal " +
            "|| noiseType == FastNoiseType.CubicFractal")]
    [LabelText("Lacunarity"), Range(1f, 5f)]
    public float fractalLacunarity = 2f;

    [FoldoutGroup("Fractal Params")]
    [ShowIf("@noiseType == FastNoiseType.ValueFractal " +
            "|| noiseType == FastNoiseType.PerlinFractal " +
            "|| noiseType == FastNoiseType.SimplexFractal " +
            "|| noiseType == FastNoiseType.CubicFractal")]
    [LabelText("Gain"), Range(0f, 1f)]
    public float fractalGain = 0.5f;


    // ------------------ Cellular ------------------
    [FoldoutGroup("Cellular Params")]
    [ShowIf("@noiseType == FastNoiseType.Cellular")]
    [LabelText("Distance Function")]
    public FastNoise.CellularDistanceFunction cellularDistanceFunc 
        = FastNoise.CellularDistanceFunction.Euclidean;

    [FoldoutGroup("Cellular Params")]
    [ShowIf("@noiseType == FastNoiseType.Cellular")]
    [LabelText("Return Type")]
    public FastNoise.CellularReturnType cellularReturnType 
        = FastNoise.CellularReturnType.CellValue;

    [FoldoutGroup("Cellular Params")]
    [ShowIf("@noiseType == FastNoiseType.Cellular")]
    [LabelText("Cellular Jitter"), Range(0f, 1f)]
    public float cellularJitter = 0.45f;

    [FoldoutGroup("Cellular Params")]
    [ShowIf("@noiseType == FastNoiseType.Cellular")]
    [LabelText("Cell Gradient Exponent")]
    [Range(0.1f, 5f)]
    public float cellularGradient = 1f; // 1=기본

    // ------------------ Domain Warp ------------------
    [FoldoutGroup("Domain Warp")]
    [LabelText("Use Domain Warp")]
    public bool useDomainWarp = false;

    [FoldoutGroup("Domain Warp")]
    [ShowIf("@this.useDomainWarp == true")]
    [LabelText("Warp Strength"), Range(0f, 50f)]
    public float domainWarpStrength = 10f;

    // ------------------ Noise Generator Settings ------------------
    [HorizontalGroup("Size", LabelWidth = 80)]
    [LabelText("Width")]
    public int textureWidth = 256;

    [HorizontalGroup("Size")]
    [LabelText("Height")]
    public int textureHeight = 256;

    [FoldoutGroup("Common Params")]
    [Range(0.001f, 100f), LabelText("Scale")]
    public float scale = 20f;

    [FoldoutGroup("Common Params")]
    [LabelText("OffsetX")]
    public float offsetX = 0f;

    [FoldoutGroup("Common Params")]
    [LabelText("OffsetY")]
    public float offsetY = 0f;

    [FoldoutGroup("Common Params")]
    [LabelText("Seed")]
    public int seed = 1337;

    // ------------------ Save Settings ------------------
    [FoldoutGroup("Save Settings")]
    [LabelText("Output Folder")]
    [ReadOnly]
    public string outputDirectory = "";

    [FoldoutGroup("Save Settings"), Button("Select Folder", ButtonSizes.Medium)]
    private void SelectFolder()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFolderPanel("Select Folder for Noise PNG", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            outputDirectory = path;
        }
        else
        {
            Debug.Log("[UniversalFastNoiseGenerator] Folder selection canceled.");
        }
#else
        Debug.LogWarning("Folder selection is only available in the Unity Editor.");
#endif
    }

    //==========================================================
    // 내부 FastNoise 인스턴스
    //==========================================================
    private FastNoise fastNoise;


    //==========================================================
    // OnValidate
    //==========================================================
    private void OnValidate()
    {
        if (fastNoise == null)
            fastNoise = new FastNoise();

        ConfigureFastNoise();
    }


    //==========================================================
    // ConfigureFastNoise
    //==========================================================
    private void ConfigureFastNoise()
    {
        fastNoise.SetSeed(seed);

        float frequency = 1f / Mathf.Max(0.0001f, scale);
        fastNoise.SetFrequency(frequency);

        // 1) FastNoiseType을 변환
        fastNoise.SetNoiseType(ConvertToFastNoiseEnum(noiseType));

        // 2) 프랙탈
        if (IsFractal(noiseType))
        {
            fastNoise.SetFractalOctaves(fractalOctaves);
            fastNoise.SetFractalLacunarity(fractalLacunarity);
            fastNoise.SetFractalGain(fractalGain);
            fastNoise.SetFractalType(ConvertFractalType(fractalType));
        }

        // 3) Cellular 세팅
        if (noiseType == FastNoiseType.Cellular)
        {
            fastNoise.SetCellularDistanceFunction(cellularDistanceFunc);
            fastNoise.SetCellularReturnType(cellularReturnType);
            fastNoise.SetCellularJitter(cellularJitter);
        }
    }


    //==========================================================
    // NoiseType 변환
    //==========================================================
    private FastNoise.NoiseType ConvertToFastNoiseEnum(FastNoiseType t)
    {
        switch (t)
        {
            case FastNoiseType.Value:           return FastNoise.NoiseType.Value;
            case FastNoiseType.ValueFractal:    return FastNoise.NoiseType.ValueFractal;
            case FastNoiseType.Perlin:          return FastNoise.NoiseType.Perlin;
            case FastNoiseType.PerlinFractal:   return FastNoise.NoiseType.PerlinFractal;
            case FastNoiseType.Simplex:         return FastNoise.NoiseType.Simplex;
            case FastNoiseType.SimplexFractal:  return FastNoise.NoiseType.SimplexFractal;

            case FastNoiseType.Cellular:        return FastNoise.NoiseType.Cellular;

            case FastNoiseType.WhiteNoise:      return FastNoise.NoiseType.WhiteNoise;
            case FastNoiseType.Cubic:           return FastNoise.NoiseType.Cubic;
            case FastNoiseType.CubicFractal:    return FastNoise.NoiseType.CubicFractal;
        }
        return FastNoise.NoiseType.Perlin;
    }


    //==========================================================
    // FractalType 변환
    //==========================================================
    private FastNoise.FractalType ConvertFractalType(FastFractalType ft)
    {
        switch (ft)
        {
            case FastFractalType.FBM:         return FastNoise.FractalType.FBM;
            case FastFractalType.Billow:      return FastNoise.FractalType.Billow;
            case FastFractalType.RigidMulti:  return FastNoise.FractalType.RigidMulti;
        }
        return FastNoise.FractalType.FBM;
    }

    //==========================================================
    // Fractal 여부 판별
    //==========================================================
    private bool IsFractal(FastNoiseType t)
    {
        return (t == FastNoiseType.ValueFractal ||
                t == FastNoiseType.PerlinFractal ||
                t == FastNoiseType.SimplexFractal ||
                t == FastNoiseType.CubicFractal);
    }

    //==========================================================
    // Generate & Save
    //==========================================================
    [Button("Generate & Save PNG", ButtonSizes.Large)]
    private void GenerateAndSavePNG()
    {
        // 1) 텍스처 생성
        Texture2D noiseTex = GenerateNoiseTexture();

        // 2) PNG 저장
        string pngPath = SaveTextureAsPNG(noiseTex);

        // 3) PNG와 같은 이름 + .json으로 파라미터 저장
        if (!string.IsNullOrEmpty(pngPath))
        {
            string baseWithoutExt = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(pngPath) ?? "",
                System.IO.Path.GetFileNameWithoutExtension(pngPath));
            SaveNoiseParametersAsJson(baseWithoutExt);
        }

        // 4) 씬 내 Renderer에 표시
        var renderer = GetComponent<Renderer>();
        if (renderer && renderer.material)
        {
            renderer.material.mainTexture = noiseTex;
        }
    }

    private Texture2D GenerateNoiseTexture()
    {
        if (fastNoise == null)
            fastNoise = new FastNoise();

        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

        // Domain Warp
        if (useDomainWarp)
        {
            fastNoise.SetGradientPerturbAmp(domainWarpStrength);
        }

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = x + offsetX;
                float ny = y + offsetY;

                if (useDomainWarp)
                {
                    fastNoise.GradientPerturb(ref nx, ref ny);
                }

                // FastNoise -> -1..1 범위 → 0..1 범위
                float val = (fastNoise.GetNoise(nx, ny) + 1f) * 0.5f;

                // Cellular인 경우 exponent 적용
                if (noiseType == FastNoiseType.Cellular)
                {
                    val = Mathf.Pow(val, cellularGradient);
                }

                val = Mathf.Clamp01(val);
                tex.SetPixel(x, y, new Color(val, val, val, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// PNG 저장 (중복 방지)
    /// </summary>
    private string SaveTextureAsPNG(Texture2D tex)
{
#if UNITY_EDITOR
    if (string.IsNullOrEmpty(outputDirectory))
    {
        Debug.LogWarning("Output directory not set. Please select a folder first!");
        return null;
    }

    // 노이즈 타입별 하위 폴더
    string subFolder = System.IO.Path.Combine(outputDirectory, noiseType.ToString());
    if (!System.IO.Directory.Exists(subFolder))
    {
        System.IO.Directory.CreateDirectory(subFolder);
    }

    // 파일 이름은 noiseType.png 
    string baseName = noiseType.ToString();
    string fileName = baseName + ".png";
    string fullPath = System.IO.Path.Combine(subFolder, fileName);

    int index = 0;
    while (System.IO.File.Exists(fullPath))
    {
        index++;
        string newName = $"{baseName}_{index}.png";
        fullPath = System.IO.Path.Combine(subFolder, newName);
    }

    byte[] pngData = tex.EncodeToPNG();
    System.IO.File.WriteAllBytes(fullPath, pngData);

    // 에셋 DB 갱신
    AssetDatabase.Refresh();

    // -------------------------------------------------------------------
    // [추가] 만약 저장 경로가 Assets 폴더 내부라면, TextureImporter를 통해 읽기/쓰기가 가능하도록 설정
    // -------------------------------------------------------------------
    // 1) 전체 경로가 "프로젝트/Assets" 경로 내에 있는지 확인
    if (fullPath.StartsWith(Application.dataPath))
    {
        // 2) Assets 폴더 기준의 상댓경로를 만든다.
        //    예) fullPath: "C:/ProjectPath/Assets/NoiseTextures/Perlin.png"
        //        assetPath: "Assets/NoiseTextures/Perlin.png"
        string assetPath = "Assets" + fullPath.Substring(Application.dataPath.Length);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            // Texture 타입 및 읽기/쓰기 플래그 설정
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;   // <--- 여기서 읽기/쓰기 가능하도록 설정
            importer.SaveAndReimport();

            Debug.Log($"[UniversalFastNoiseGenerator] 'isReadable=true' 설정 완료: {assetPath}");
        }
        else
        {
            Debug.LogWarning($"TextureImporter를 가져올 수 없습니다. 경로: {assetPath}");
        }
    }
    else
    {
        Debug.LogWarning("PNG가 Assets 폴더 밖에 저장되었으므로 TextureImporter 설정을 할 수 없습니다.");
    }

    Debug.Log($"[UniversalFastNoiseGenerator] PNG saved to: {fullPath}");
    return fullPath;
#else
    Debug.LogWarning("Saving PNG with folder selection is only available in Unity Editor.");
    return null;
#endif
}


    /// <summary>
    /// 노이즈 파라미터를 JSON에 직렬화해 png와 동일한 이름으로 저장
    /// </summary>
    private void SaveNoiseParametersAsJson(string baseWithoutExt)
    {
#if UNITY_EDITOR
        NoiseParams paramData = new NoiseParams()
        {
            // 노이즈 타입
            noiseType = noiseType.ToString(),

            fractalType = (IsFractal(noiseType)) ? fractalType.ToString() : "None",
            fractalOctaves = fractalOctaves,
            fractalLacunarity = fractalLacunarity,
            fractalGain = fractalGain,

            // Cellular
            cellularDistanceFunc = (noiseType == FastNoiseType.Cellular) 
                ? cellularDistanceFunc.ToString() 
                : "None",

            cellularReturnType = (noiseType == FastNoiseType.Cellular) 
                ? cellularReturnType.ToString() 
                : "None",

            cellularJitter = (noiseType == FastNoiseType.Cellular) 
                ? cellularJitter 
                : 0f,

            cellularGradient = (noiseType == FastNoiseType.Cellular) 
                ? cellularGradient 
                : 1f,

            // DomainWarp
            useDomainWarp = useDomainWarp,
            domainWarpStrength = domainWarpStrength,

            // 기타
            textureWidth = textureWidth,
            textureHeight = textureHeight,
            scale = scale,
            offsetX = offsetX,
            offsetY = offsetY,
            seed = seed
        };

        string jsonString = JsonUtility.ToJson(paramData, true);
        string jsonPath = baseWithoutExt + ".json";
        System.IO.File.WriteAllText(jsonPath, jsonString);

        Debug.Log($"[UniversalFastNoiseGenerator] JSON saved to: {jsonPath}");
        AssetDatabase.Refresh();
#else
        Debug.LogWarning("JSON Save is only available in Unity Editor.");
#endif
    }

    //==========================================================
    // JSON 직렬화용 클래스
    //==========================================================
    [System.Serializable]
    private class NoiseParams
    {
        public string noiseType;

        // Fractal
        public string fractalType;
        public int fractalOctaves;
        public float fractalLacunarity;
        public float fractalGain;

        // Cellular
        public string cellularDistanceFunc;
        public string cellularReturnType;
        public float cellularJitter;
        public float cellularGradient;

        // Domain Warp
        public bool useDomainWarp;
        public float domainWarpStrength;

        // Common
        public int textureWidth;
        public int textureHeight;
        public float scale;
        public float offsetX;
        public float offsetY;
        public int seed;
    }
}
