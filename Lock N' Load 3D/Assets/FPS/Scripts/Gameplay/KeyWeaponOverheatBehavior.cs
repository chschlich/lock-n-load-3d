using UnityEngine;
using System.Collections.Generic;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Prefab-based overheat system for key weapons.
    /// Add this component to each weapon prefab and configure settings in the Inspector.
    /// </summary>
    public class KeyWeaponOverheatBehavior : MonoBehaviour
    {
        [System.Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        [Header("Heat Settings")]
        [Tooltip("Heat added per shot (0-1 scale)")]
        public float HeatPerShot = 0.12f;

        [Tooltip("Heat lost per second when not shooting")]
        public float HeatDecayRate = 0.35f;

        [Tooltip("Heat must drop below this to resume shooting after overheat")]
        public float OverheatCooldownThreshold = 0.3f;

        [Header("Visual - Emission")]
        [Tooltip("Multiplier for emission intensity")]
        [Range(0.1f, 3f)]
        public float EmissionIntensity = 1f;

        [Tooltip("Hot color when overheated (lerps from black to this)")]
        public Color HotColor = Color.red;

        [Header("Visual - Steam VFX")]
        [Tooltip("Steam VFX prefab to instantiate at muzzle")]
        public GameObject SteamVfxPrefab;

        [Tooltip("The emission rate for steam when overheated")]
        public float SteamVfxEmissionRateMax = 8f;

        [Header("Sound")]
        public AudioClip OverheatSound;
        public AudioClip CoolingSound;
        [Range(0f, 1f)]
        public float SoundVolume = 0.5f;

        [Header("Animation")]
        public float OverheatLiftAmount = 0.08f;
        public float OverheatLiftSpeed = 8f;
        public float OverheatTiltAngle = -10f;

        // Runtime values - stored separately so Inspector values aren't overwritten
        float m_HeatPerShot;
        float m_HeatDecayRate;
        float m_OverheatCooldownThreshold;

        // Public properties
        public float HeatLevel { get; private set; } = 0f;
        public bool IsOverheated { get; private set; } = false;
        public bool IsCooling { get; private set; } = false;
        public Vector3 OverheatPositionOffset { get; private set; } = Vector3.zero;
        public Quaternion OverheatRotationOffset { get; private set; } = Quaternion.identity;

        // Private state
        WeaponController m_WeaponController;
        AudioSource m_AudioSource;
        List<RendererIndexData> m_OverheatingRenderersData;
        MaterialPropertyBlock m_OverheatMaterialPropertyBlock;
        GameObject m_SteamVfxInstance;
        ParticleSystem m_SteamParticleSystem;
        float m_TimeSinceLastShot = 0f;
        float m_CurrentLiftOffset = 0f;
        float m_CurrentTiltAngle = 0f;
        float m_CurrentEmissionLerp = 0f;
        bool m_Initialized = false;
        bool m_PlayedOverheatSound = false;
        bool m_HasSteamVfx = false;

        public void Initialize()
        {
            if (m_Initialized) return;

            // Copy serialized values to runtime variables
            m_HeatPerShot = HeatPerShot;
            m_HeatDecayRate = HeatDecayRate;
            m_OverheatCooldownThreshold = OverheatCooldownThreshold;

            m_WeaponController = GetComponent<WeaponController>();

            SetupRenderers();
            SetupSteamVfx();
            SetupAudio();

            m_OverheatMaterialPropertyBlock = new MaterialPropertyBlock();
            m_Initialized = true;
            
            // Start with no emission
            m_CurrentEmissionLerp = 0f;
            ApplyEmissionColor(0f);
        }

        void SetupRenderers()
        {
            m_OverheatingRenderersData = new List<RendererIndexData>();
            
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    if (mat != null && mat.HasProperty("_EmissionColor"))
                    {
                        m_OverheatingRenderersData.Add(new RendererIndexData(renderer, i));
                        EnableEmissionOnMaterial(renderer, i);
                    }
                }
            }
        }

        void SetupSteamVfx()
        {
            if (SteamVfxPrefab == null) return;

            Transform spawnPoint = transform;
            if (m_WeaponController != null && m_WeaponController.WeaponMuzzle != null)
            {
                spawnPoint = m_WeaponController.WeaponMuzzle;
            }
            
            m_SteamVfxInstance = Instantiate(SteamVfxPrefab, spawnPoint);
            m_SteamVfxInstance.transform.localPosition = Vector3.zero;
            m_SteamVfxInstance.transform.localRotation = Quaternion.identity;
            m_SteamVfxInstance.name = "SteamVfx_Instance";
            
            SetLayerRecursively(m_SteamVfxInstance, 10);
            
            m_SteamParticleSystem = m_SteamVfxInstance.GetComponent<ParticleSystem>();
            if (m_SteamParticleSystem == null)
            {
                m_SteamParticleSystem = m_SteamVfxInstance.GetComponentInChildren<ParticleSystem>();
            }

            if (m_SteamParticleSystem != null)
            {
                // Set initial emission rate to 0
                var emission = m_SteamParticleSystem.emission;
                emission.rateOverTimeMultiplier = 0f;
                
                m_SteamParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                m_SteamVfxInstance.SetActive(true);
                m_HasSteamVfx = true;
            }
        }

        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        void SetupAudio()
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.loop = false;
            m_AudioSource.spatialBlend = 0f;
            
            var audioGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponOverheat);
            if (audioGroup != null)
            {
                m_AudioSource.outputAudioMixerGroup = audioGroup;
            }
        }

        void EnableEmissionOnMaterial(Renderer renderer, int materialIndex)
        {
            Material[] materials = renderer.materials;
            if (materialIndex < materials.Length && materials[materialIndex] != null)
            {
                materials[materialIndex].EnableKeyword("_EMISSION");
                materials[materialIndex].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        void ApplyEmissionColor(float intensity)
        {
            if (m_OverheatMaterialPropertyBlock == null) return;
            
            // Lerp from black to HotColor based on intensity
            Color emissionColor = Color.Lerp(Color.black, HotColor, intensity) * EmissionIntensity;
            m_OverheatMaterialPropertyBlock.SetColor("_EmissionColor", emissionColor);

            foreach (var data in m_OverheatingRenderersData)
            {
                if (data.Renderer != null)
                {
                    data.Renderer.SetPropertyBlock(m_OverheatMaterialPropertyBlock, data.MaterialIndex);
                }
            }
        }

        void Update()
        {
            if (!m_Initialized) return;

            m_TimeSinceLastShot += Time.deltaTime;

            // Decay heat over time
            if (m_TimeSinceLastShot > 0.1f && HeatLevel > 0f)
            {
                HeatLevel -= m_HeatDecayRate * Time.deltaTime;
                HeatLevel = Mathf.Max(0f, HeatLevel);

                if (IsOverheated && HeatLevel < m_OverheatCooldownThreshold)
                {
                    IsOverheated = false;
                    m_PlayedOverheatSound = false;
                }
            }

            IsCooling = HeatLevel > 0f && m_TimeSinceLastShot > 0.1f;

            // Smoothly animate emission color
            float targetEmission = HeatLevel;
            m_CurrentEmissionLerp = Mathf.Lerp(m_CurrentEmissionLerp, targetEmission, Time.deltaTime * 8f);
            ApplyEmissionColor(m_CurrentEmissionLerp);

            UpdateSteamVfx();
            UpdateAudio();
            UpdateOverheatAnimation();
            SyncWithUI();
        }

        void UpdateSteamVfx()
        {
            if (!m_HasSteamVfx || m_SteamParticleSystem == null) return;
            
            // Get the emission module (it's a struct, so we need to get it each time)
            var emission = m_SteamParticleSystem.emission;
            
            if (IsOverheated)
            {
                emission.rateOverTimeMultiplier = SteamVfxEmissionRateMax;
                if (!m_SteamParticleSystem.isPlaying)
                {
                    m_SteamParticleSystem.Play();
                }
            }
            else
            {
                emission.rateOverTimeMultiplier = 0f;
                if (m_SteamParticleSystem.isPlaying)
                {
                    m_SteamParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        void UpdateAudio()
        {
            if (m_AudioSource == null) return;

            if (CoolingSound != null)
            {
                bool shouldPlayCooling = IsCooling && !IsOverheated && HeatLevel > 0.1f;

                if (shouldPlayCooling && !m_AudioSource.isPlaying)
                {
                    m_AudioSource.clip = CoolingSound;
                    m_AudioSource.loop = true;
                    m_AudioSource.volume = SoundVolume * HeatLevel;
                    m_AudioSource.Play();
                }
                else if (!shouldPlayCooling && m_AudioSource.isPlaying && m_AudioSource.clip == CoolingSound)
                {
                    m_AudioSource.Stop();
                    m_AudioSource.loop = false;
                }
                else if (m_AudioSource.isPlaying && m_AudioSource.clip == CoolingSound)
                {
                    m_AudioSource.volume = SoundVolume * HeatLevel;
                }
            }

            if (IsOverheated && !m_PlayedOverheatSound && OverheatSound != null)
            {
                m_AudioSource.PlayOneShot(OverheatSound, SoundVolume);
                m_PlayedOverheatSound = true;
            }
        }

        void UpdateOverheatAnimation()
        {
            float targetLift = IsOverheated ? OverheatLiftAmount : 0f;
            float targetTilt = IsOverheated ? OverheatTiltAngle : 0f;

            m_CurrentLiftOffset = Mathf.Lerp(m_CurrentLiftOffset, targetLift, Time.deltaTime * OverheatLiftSpeed);
            m_CurrentTiltAngle = Mathf.Lerp(m_CurrentTiltAngle, targetTilt, Time.deltaTime * OverheatLiftSpeed);

            OverheatPositionOffset = new Vector3(0f, m_CurrentLiftOffset, 0f);
            OverheatRotationOffset = Quaternion.Euler(m_CurrentTiltAngle, 0f, 0f);
        }

        void SyncWithUI()
        {
            if (m_WeaponController != null)
            {
                m_WeaponController.SetAmmoRatio(1f - HeatLevel);
            }
        }

        /// <summary>
        /// Set heat state directly (used when restoring state after weapon switch)
        /// </summary>
        public void SetHeatState(float heat, bool overheated)
        {
            HeatLevel = Mathf.Clamp01(heat);
            IsOverheated = overheated;
            m_CurrentEmissionLerp = HeatLevel;
            
            // If overheated, make sure visual state matches
            if (IsOverheated)
            {
                m_PlayedOverheatSound = true; // Don't replay sound
            }
            
            ApplyEmissionColor(m_CurrentEmissionLerp);
        }

        public void AddHeat()
        {
            AddHeat(m_HeatPerShot);
        }

        public void AddHeat(float amount)
        {
            m_TimeSinceLastShot = 0f;
            HeatLevel += amount;
            HeatLevel = Mathf.Clamp01(HeatLevel);

            if (HeatLevel >= 1f && !IsOverheated)
            {
                IsOverheated = true;
            }
        }

        public void ResetHeat()
        {
            HeatLevel = 0f;
            IsOverheated = false;
            IsCooling = false;
            m_CurrentLiftOffset = 0f;
            m_CurrentTiltAngle = 0f;
            m_CurrentEmissionLerp = 0f;
            m_PlayedOverheatSound = false;
            OverheatPositionOffset = Vector3.zero;
            OverheatRotationOffset = Quaternion.identity;

            ApplyEmissionColor(0f);

            if (m_HasSteamVfx && m_SteamParticleSystem != null)
            {
                var emission = m_SteamParticleSystem.emission;
                emission.rateOverTimeMultiplier = 0f;
                m_SteamParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (m_AudioSource != null && m_AudioSource.isPlaying)
            {
                m_AudioSource.Stop();
            }
        }
    }
}
