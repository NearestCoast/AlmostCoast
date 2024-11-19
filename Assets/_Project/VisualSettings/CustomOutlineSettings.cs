using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FlatKit {
    [CreateAssetMenu(fileName = "CustomOutlineSettings", menuName = "FlatKit/Custom Outline Settings")]
    public class CustomOutlineSettings : OutlineSettings {
        [Tooltip("The layers on which the outline effect will be applied. Default is Everything.")]
        public LayerMask outlineLayers = ~0; // Default: Everything.

        private void OnEnable() {
            onSettingsChanged += UpdateEffectMaterial;
            onReset += ResetEffectMaterial;
        }

        private void OnDisable() {
            onSettingsChanged -= UpdateEffectMaterial;
            onReset -= ResetEffectMaterial;
        }

        private void UpdateEffectMaterial() {
            if (effectMaterial == null) {
                Debug.LogWarning("<b>[Flat Kit]</b> Effect material is not assigned.");
                return;
            }

            // Update the material to use the selected layers.
            effectMaterial.SetInt("_LayerMask", outlineLayers.value);
        }

        private void ResetEffectMaterial() {
            if (effectMaterial != null) {
                effectMaterial.SetInt("_LayerMask", ~0); // Reset to Everything.
            }
        }

        public bool IsLayerIncluded(int layer) {
            return (outlineLayers.value & (1 << layer)) != 0;
        }
    }
}