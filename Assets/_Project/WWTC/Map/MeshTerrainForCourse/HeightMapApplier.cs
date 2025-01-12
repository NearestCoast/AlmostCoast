using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor; // for EditorUtility.SetDirty
#endif

/// <summary>
/// 1) CompositeGridNodes를 읽고, (가장 긴 변 vs 가장 짧은 변) 정사각 오버랩 방식 적용은 동일.
/// 2) 하지만 먼저 heightMap 전체(또는 해당 오버랩 범위)에 대해 평균 grayscale(averageGray) 계산.
/// 3) CourseArea(true)인 노드는 y변경 X, 그 외만 (hVal - averageGray)*heightScale 적용.
/// </summary>
public class HeightMapApplier : MonoBehaviour
{
    [Header("References")]
    public PathDataSO pathData; // compositeGridNodes, heightAppliedPoints를 보유

    [FoldoutGroup("Settings"), Tooltip("heightMap(흑백)")]
    public Texture2D heightMap;

    [FoldoutGroup("Settings"), Tooltip("heightMap (hVal - averageGray)에 곱할 배율")]
    public float heightScale = 10f;

    [FoldoutGroup("Settings"), Tooltip("기존 y에 더할(add)지, 교체(replace)할지")]
    public bool additiveMode = true;

    // ========== 색상 선택 (Odin ColorPalette 사용) ==========
    [FoldoutGroup("Gizmo Colors"), ColorPalette]
    public Color courseAreaColor = Color.red;

    [FoldoutGroup("Gizmo Colors"), ColorPalette]
    public Color heightAppliedColor = Color.magenta;

    [FoldoutGroup("Actions")]
    [Button("Apply HeightMap (Square Overlap + Average-based)")]
    public void ApplyHeightMapSquareOverlap()
    {
        // 1) 체크
        if (pathData == null)
        {
            Debug.LogWarning("[HeightMapApplier] pathDataSO가 없음");
            return;
        }
        if (heightMap == null)
        {
            Debug.LogWarning("[HeightMapApplier] HeightMap 텍스처가 없음");
            return;
        }

        var oldNodes = pathData.CompositeGridNodes;
        if (oldNodes == null || oldNodes.Count == 0)
        {
            Debug.LogWarning("[HeightMapApplier] compositeGridNodes가 비어있음");
            return;
        }

        // ---------------------------------------------------------------------
        // (A) 먼저 heightMap 전체(또는 오버랩 부분)에서 평균 grayscale 구하기
        // ---------------------------------------------------------------------
        //  - boundingRect에서 '가장 긴 변' => largestSide
        //  - heightMap에서 '가장 짧은 변' => minSide
        //  - 그 영역 내의 픽셀을 (uPixel, vPixel) = [0..minSide], GetPixelBilinear(u,v)
        //  - 모든 픽셀 grayscale 합산 / 샘플수 => averageGray
        //  (주의: 대형 텍스처면 성능 부하. 필요시 stride 등으로 줄이자)

        Rect r = pathData.BoundingRect;
        float rectW = r.width;
        float rectH = r.height;
        if(rectW<=0f || rectH<=0f)
        {
            Debug.LogWarning("[HeightMapApplier] boundingRect가 유효하지 않음");
            return;
        }
        float largestSide = (rectW> rectH)? rectW : rectH;

        int texW = heightMap.width;
        int texH = heightMap.height;
        int minSide = (texW < texH)? texW : texH;

        // 실제로는 0..minSide-1 루프를 돌아 모든 픽셀 샘플링 (비례 변환+GetPixelBilinear)
        // 여기서는 간단히 "full sampling" 예시.
        float sumGray = 0f;
        int sampleCount=0;

        // (가정) for i in [0..minSide], j in [0..minSide]
        //        u= i/minSide, v= j/minSide
        for(int j=0; j< minSide; j++)
        {
            float v= (float)j/(float)minSide; 
            for(int i=0; i< minSide; i++)
            {
                float u= (float)i/(float)minSide;

                // 픽셀
                Color c= heightMap.GetPixelBilinear(u,v);
                sumGray += c.grayscale;
                sampleCount++;
            }
        }

        float averageGray= (sampleCount>0)? (sumGray/sampleCount) : 0.5f;
        Debug.Log($"[HeightMapApplier] averageGray={averageGray:F3} (sampleCount={sampleCount})");

        // ---------------------------------------------------------------------
        // (B) 새 리스트 (HeightAppliedPoints)
        // ---------------------------------------------------------------------
        List<GridNode> newHeights = new List<GridNode>(oldNodes.Count);

        // (C) 각 GridNode => CourseArea면 skip, 아니면 (hVal - averageGray)*scale
        for(int n=0; n< oldNodes.Count; n++)
        {
            GridNode src = oldNodes[n];
            GridNode dst = new GridNode(){
                i= src.i,
                j= src.j,
                isCourseArea= src.isCourseArea
            };

            // (C-1) 만약 isCourseArea => y값 그대로
            Vector3 p = src.position;
            if(src.isCourseArea)
            {
                dst.position= p;
                newHeights.Add(dst);
                continue; 
            }

            // (C-2) isCourseArea가 아닌 경우 => (x,z)->(uPixel,vPixel), hVal - averageGray
            float dx= p.x - r.xMin;
            float dz= p.z - r.yMin;

            // boundingRect의 largestSide -> minSide
            float uPixel = (dx / largestSide)* minSide;
            float vPixel = (dz / largestSide)* minSide;

            if(uPixel<0f) uPixel=0f; if(uPixel> minSide) uPixel=minSide;
            if(vPixel<0f) vPixel=0f; if(vPixel> minSide) vPixel=minSide;

            float u= uPixel / minSide;
            float v= vPixel / minSide;

            Color col= heightMap.GetPixelBilinear(u,v);
            float hVal= col.grayscale;

            float delta= (hVal - averageGray) * heightScale; 
            if(additiveMode)
                p.y += delta;
            else
                p.y = (src.position.y + delta); 
                // replace 모드여도, "src.position.y + delta" 
                // => src.position.y(원래 y) + (hVal-avg)*scale
                //   (만약 완전 replace하고 싶다면 p.y= delta).

            dst.position= p;
            newHeights.Add(dst);
        }

        // (D) SetHeightAppliedPoints
        pathData.ClearHeightAppliedPoints();
        pathData.SetHeightAppliedPoints(newHeights);

        Debug.Log($"[HeightMapApplier] SquareOverlap + average-based done. count={newHeights.Count}, additive={additiveMode}");
    }

    // =========================================================================
    // OnDrawGizmosSelected :
    //    compositeGridNodes => 코스이면 빨강, 아니면 파랑
    //    heightAppliedPoints => 보라
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        if(pathData==null) return;

        // compositeGridNodes
        var gridNodes= pathData.CompositeGridNodes;
        if(gridNodes!=null && gridNodes.Count>0)
        {
            foreach(var node in gridNodes)
            {
                Gizmos.color= node.isCourseArea? courseAreaColor : Color.blue;
                Gizmos.DrawSphere(node.position, 0.2f);
            }
        }

        // heightAppliedPoints
        var applied= pathData.HeightAppliedPoints;
        if(applied!=null && applied.Count>0)
        {
            Gizmos.color= heightAppliedColor;
            foreach(var node in applied)
            {
                Gizmos.DrawSphere(node.position, 0.2f);
            }
        }
    }
}
