using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FlatKit {
    public class FlatKitOutlineWithLayer : FlatKitOutline {
        [Tooltip("The layers on which the outline effect will be applied. Default is Everything.")]
        public LayerMask outlineLayers = ~0; // Default: Everything

        private static int layerMaskProperty = Shader.PropertyToID("_LayerMask");

        private new void SetMaterialProperties() {
            base.SetMaterialProperties();

            if (_effectMaterial == null) return;

            // LayerMask 설정을 머티리얼에 전달
            _effectMaterial.SetInt(layerMaskProperty, outlineLayers.value);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            // 현재 카메라로 보이는 오브젝트 중 LayerMask에 포함되지 않은 오브젝트는 렌더링하지 않음
            if (renderingData.cameraData.isSceneViewCamera && !settings.applyInSceneView) return;
            if (renderingData.cameraData.isPreviewCamera) return;
            if (_effectMaterial == null) return;

            // 머티리얼 설정 업데이트
            SetMaterialProperties();

            _fullScreenPass.Setup(
                _effectMaterial, 
                _requiresColor, 
                _injectedBeforeTransparents, 
                "Flat Kit Outline With Layer",
                renderingData
            );

            renderer.EnqueuePass(_fullScreenPass);
        }
    }
}