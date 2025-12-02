using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Centralized audio volume settings for all key weapons.
    /// Settings persist between sessions via PlayerPrefs.
    /// Add this component to a GameObject in your scene to access volume controls in Inspector.
    /// </summary>
    public class KeyWeaponAudioSettings : MonoBehaviour
    {
        private static KeyWeaponAudioSettings s_Instance;
        
        private const string PREF_FIRING_VOLUME = "KeyWeapon_FiringVolume";
        private const string PREF_PROJECTILE_TRAVEL_VOLUME = "KeyWeapon_ProjectileTravelVolume";
        private const string PREF_IMPACT_VOLUME = "KeyWeapon_ImpactVolume";
        private const string PREF_PULLOUT_VOLUME = "KeyWeapon_PullOutVolume";
        private const string PREF_OVERHEAT_VOLUME = "KeyWeapon_OverheatVolume";
        private const string PREF_COOLING_VOLUME = "KeyWeapon_CoolingVolume";
        
        [Header("Key Weapon Audio Volumes")]
        [Tooltip("Master volume for all weapon firing sounds")]
        [Range(0f, 5f)]
        [SerializeField] private float m_FiringVolume = 1f;
        
        [Tooltip("Master volume for projectile travel sounds (while flying)")]
        [Range(0f, 5f)]
        [SerializeField] private float m_ProjectileTravelVolume = 1f;
        
        [Tooltip("Master volume for projectile impact sounds (on hit)")]
        [Range(0f, 5f)]
        [SerializeField] private float m_ImpactVolume = 1f;
        
        [Tooltip("Master volume for weapon pull out sounds (drawing weapon)")]
        [Range(0f, 5f)]
        [SerializeField] private float m_PullOutVolume = 1f;
        
        [Tooltip("Master volume for weapon overheat sounds")]
        [Range(0f, 5f)]
        [SerializeField] private float m_OverheatVolume = 1f;
        
        [Tooltip("Master volume for weapon cooling sounds")]
        [Range(0f, 5f)]
        [SerializeField] private float m_CoolingVolume = 1f;
        
        // Static accessors for easy global access
        public static float FiringVolume
        {
            get
            {
                EnsureInstance();
                return s_Instance != null ? s_Instance.m_FiringVolume : 1f;
            }
        }
        
        public static float ProjectileTravelVolume
        {
            get
            {
                EnsureInstance();
                return s_Instance != null ? s_Instance.m_ProjectileTravelVolume : 1f;
            }
        }
        
        public static float ImpactVolume
        {
            get
            {
                EnsureInstance();
                return s_Instance != null ? s_Instance.m_ImpactVolume : 1f;
            }
        }
        
        public static float PullOutVolume
        {
            get
            {
                EnsureInstance();
                return s_Instance != null ? s_Instance.m_PullOutVolume : 1f;
            }
        }
        
        public static float OverheatVolume
        {
            get
            {
                EnsureInstance();
                return s_Instance != null ? s_Instance.m_OverheatVolume : 1f;
            }
        }
        
        public static float CoolingVolume
        {
            get
            {
                EnsureInstance();
                return s_Instance != null ? s_Instance.m_CoolingVolume : 1f;
            }
        }
        
        void Awake()
        {
            // Set up singleton
            if (s_Instance == null)
            {
                s_Instance = this;
                LoadSettings();
                SyncToBridge(); // Sync immediately on awake
            }
            else if (s_Instance != this)
            {
                Debug.LogWarning("Multiple KeyWeaponAudioSettings instances detected. Using the first one.");
            }
        }
        
        void Start()
        {
            // Sync again on Start to ensure bridge is populated
            SyncToBridge();
        }
        
        void OnEnable()
        {
            // Sync when enabled
            SyncToBridge();
        }
        
        void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }
        
        void Update()
        {
            // Continuously sync to bridge so WeaponController can access without reflection
            SyncToBridge();
        }
        
        /// <summary>
        /// Called when values are changed in the Inspector (Editor only)
        /// </summary>
        void OnValidate()
        {
            // Save settings when changed in Inspector
            if (Application.isPlaying)
            {
                SaveSettings();
                SyncToBridge();
            }
        }
        
        /// <summary>
        /// Sync current volume values to the static bridge for cross-assembly access
        /// </summary>
        void SyncToBridge()
        {
            KeyWeaponVolumeBridge.FiringVolume = m_FiringVolume;
            KeyWeaponVolumeBridge.ImpactVolume = m_ImpactVolume;
            KeyWeaponVolumeBridge.ProjectileTravelVolume = m_ProjectileTravelVolume;
        }
        
        /// <summary>
        /// Load volume settings from PlayerPrefs
        /// </summary>
        void LoadSettings()
        {
            m_FiringVolume = PlayerPrefs.GetFloat(PREF_FIRING_VOLUME, 1f);
            m_ProjectileTravelVolume = PlayerPrefs.GetFloat(PREF_PROJECTILE_TRAVEL_VOLUME, 1f);
            m_ImpactVolume = PlayerPrefs.GetFloat(PREF_IMPACT_VOLUME, 1f);
            m_PullOutVolume = PlayerPrefs.GetFloat(PREF_PULLOUT_VOLUME, 1f);
            m_OverheatVolume = PlayerPrefs.GetFloat(PREF_OVERHEAT_VOLUME, 1f);
            m_CoolingVolume = PlayerPrefs.GetFloat(PREF_COOLING_VOLUME, 1f);
        }
        
        /// <summary>
        /// Save volume settings to PlayerPrefs
        /// </summary>
        void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_FIRING_VOLUME, m_FiringVolume);
            PlayerPrefs.SetFloat(PREF_PROJECTILE_TRAVEL_VOLUME, m_ProjectileTravelVolume);
            PlayerPrefs.SetFloat(PREF_IMPACT_VOLUME, m_ImpactVolume);
            PlayerPrefs.SetFloat(PREF_PULLOUT_VOLUME, m_PullOutVolume);
            PlayerPrefs.SetFloat(PREF_OVERHEAT_VOLUME, m_OverheatVolume);
            PlayerPrefs.SetFloat(PREF_COOLING_VOLUME, m_CoolingVolume);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Ensure instance exists (try to find it if not set)
        /// </summary>
        static void EnsureInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = FindFirstObjectByType<KeyWeaponAudioSettings>();
                
                if (s_Instance == null)
                {
                    Debug.LogWarning("No KeyWeaponAudioSettings found in scene. Using default volumes. Add KeyWeaponAudioSettings component to a GameObject to control volumes.");
                }
            }
        }
        
        /// <summary>
        /// Reset all volumes to default (1.0)
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            m_FiringVolume = 1f;
            m_ProjectileTravelVolume = 1f;
            m_ImpactVolume = 1f;
            m_PullOutVolume = 1f;
            m_OverheatVolume = 1f;
            m_CoolingVolume = 1f;
            SaveSettings();
        }
    }
}

