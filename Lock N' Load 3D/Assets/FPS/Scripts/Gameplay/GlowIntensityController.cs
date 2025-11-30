using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Simple component to control glow intensity on projectiles and VFX.
    /// Uses MaterialPropertyBlock for real-time updates without modifying shared materials.
    /// </summary>
    public class GlowIntensityController : MonoBehaviour
    {
        [Header("Glow Settings")]
        [Tooltip("Intensity multiplier for emission glow (0 = no glow, 1 = original, >1 = brighter)")]
        [Range(0f, 50f)]
        public float Intensity = 1f;

        private Renderer[] m_Renderers;
        private MaterialPropertyBlock m_PropertyBlock;
        private Color[] m_OriginalEmissionColors;
        private bool m_IsInitialized = false;

        void OnEnable()
        {
            if (!m_IsInitialized)
            {
                Initialize();
                m_IsInitialized = true;
            }
        }

        void Start()
        {
            if (!m_IsInitialized)
            {
                Initialize();
                m_IsInitialized = true;
            }
        }

        void OnDisable()
        {
            // Reset to original values when disabled by clearing property blocks
            if (m_Renderers != null)
            {
                foreach (var renderer in m_Renderers)
                {
                    if (renderer != null)
                    {
                        // Clear property blocks to restore original material values
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            renderer.SetPropertyBlock(null, i);
                        }
                    }
                }
            }
            m_IsInitialized = false;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Update in real-time when values change in inspector
            if (m_IsInitialized && m_Renderers != null && m_PropertyBlock != null && m_OriginalEmissionColors != null)
            {
                ApplyGlowIntensity();
            }
            else if (Application.isPlaying)
            {
                Initialize();
            }
        }
#endif

        void Initialize()
        {
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            
            if (m_Renderers == null || m_Renderers.Length == 0)
            {
                Debug.LogWarning($"GlowIntensityController on {gameObject.name}: No renderers found.");
                return;
            }

            // Count total materials
            int totalMaterials = 0;
            foreach (var renderer in m_Renderers)
            {
                if (renderer != null && renderer.sharedMaterials != null)
                {
                    totalMaterials += renderer.sharedMaterials.Length;
                }
            }

            if (totalMaterials == 0)
            {
                Debug.LogWarning($"GlowIntensityController on {gameObject.name}: No materials found on renderers.");
                return;
            }

            m_PropertyBlock = new MaterialPropertyBlock();
            m_OriginalEmissionColors = new Color[totalMaterials];

            // Store original emission colors from shared materials
            int colorIndex = 0;
            foreach (var renderer in m_Renderers)
            {
                if (renderer == null || renderer.sharedMaterials == null)
                    continue;

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material sharedMaterial = renderer.sharedMaterials[i];
                    if (sharedMaterial != null)
                    {
                        Color originalEmission = Color.black;
                        if (sharedMaterial.IsKeywordEnabled("_EMISSION"))
                        {
                            originalEmission = sharedMaterial.GetColor("_EmissionColor");
                        }
                        else if (sharedMaterial.HasProperty("_EmissionColor"))
                        {
                            originalEmission = sharedMaterial.GetColor("_EmissionColor");
                        }
                        m_OriginalEmissionColors[colorIndex] = originalEmission;
                        colorIndex++;
                    }
                }
            }

            ApplyGlowIntensity();
        }

        void ApplyGlowIntensity()
        {
            if (m_Renderers == null || m_PropertyBlock == null || m_OriginalEmissionColors == null)
                return;

            int colorIndex = 0;
            foreach (var renderer in m_Renderers)
            {
                if (renderer == null || renderer.sharedMaterials == null)
                    continue;

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (colorIndex < m_OriginalEmissionColors.Length)
                    {
                        Color newEmissionColor = m_OriginalEmissionColors[colorIndex] * Intensity;
                        m_PropertyBlock.SetColor("_EmissionColor", newEmissionColor);
                        
                        renderer.SetPropertyBlock(m_PropertyBlock, i);
                        colorIndex++;
                    }
                }
            }
        }
    }
}
