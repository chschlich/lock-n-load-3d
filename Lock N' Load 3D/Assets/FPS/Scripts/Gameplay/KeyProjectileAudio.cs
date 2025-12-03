using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Handles projectile audio for key weapons.
    /// Plays a looping travel sound while in flight and an impact sound on hit.
    /// </summary>
    public class KeyProjectileAudio : MonoBehaviour
    {
        private AudioSource m_TravelAudioSource;
        private AudioClip m_ImpactClip;
        private float m_ImpactVolume = 1f;
        
        // Static shared AudioSource for impact sounds (2D, centered audio)
        private static AudioSource s_ImpactAudioSource;
        private static float s_LastImpactTime = 0f;
        
        // Constants
        private const float IMPACT_COOLDOWN = 0.05f; // Minimum time between impact sounds (prevents distortion)

        void Awake()
        {
            // Create AudioSource for travel sound
            m_TravelAudioSource = gameObject.AddComponent<AudioSource>();
            m_TravelAudioSource.loop = true;
            m_TravelAudioSource.spatialBlend = 1f; // Full 3D sound
            m_TravelAudioSource.playOnAwake = false;
            m_TravelAudioSource.rolloffMode = AudioRolloffMode.Linear;
            m_TravelAudioSource.minDistance = 1f;
            m_TravelAudioSource.maxDistance = 50f;
            
            // Try to use projectile audio group if available
            var audioGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
            if (audioGroup != null)
            {
                m_TravelAudioSource.outputAudioMixerGroup = audioGroup;
            }
            
            // Create shared impact audio source if it doesn't exist
            EnsureImpactAudioSource();
        }
        
        static void EnsureImpactAudioSource()
        {
            if (s_ImpactAudioSource == null)
            {
                // Create a persistent GameObject for impact audio
                GameObject impactAudioObj = new GameObject("ProjectileImpactAudio");
                DontDestroyOnLoad(impactAudioObj);
                
                s_ImpactAudioSource = impactAudioObj.AddComponent<AudioSource>();
                s_ImpactAudioSource.playOnAwake = false;
                s_ImpactAudioSource.spatialBlend = 0f; // 2D audio - no stereo panning
                s_ImpactAudioSource.volume = 1f;
            }
        }

        /// <summary>
        /// Configure the audio clips and volumes for this projectile.
        /// Call this immediately after spawning the projectile.
        /// </summary>
        public void Configure(AudioClip travelClip, AudioClip impactClip, float travelVolume, float impactVolume)
        {
            m_ImpactClip = impactClip;
            m_ImpactVolume = impactVolume;

            // Start playing travel sound if provided
            if (travelClip != null && m_TravelAudioSource != null)
            {
                m_TravelAudioSource.clip = travelClip;
                // Apply master projectile travel volume from settings
                m_TravelAudioSource.volume = travelVolume * KeyWeaponAudioSettings.ProjectileTravelVolume;
                m_TravelAudioSource.Play();
            }
        }

        /// <summary>
        /// Called by ProjectileStandard before the projectile is destroyed.
        /// Uses shared 2D AudioSource with cooldown to prevent distortion from rapid impacts.
        /// </summary>
        public void PlayImpactSound(Vector3 position)
        {
            if (m_ImpactClip != null && s_ImpactAudioSource != null)
            {
                // Check cooldown to prevent audio buildup/distortion
                if (Time.time - s_LastImpactTime >= IMPACT_COOLDOWN)
                {
                    s_LastImpactTime = Time.time;
                    
                    // Apply master impact volume from settings, clamped to prevent distortion
                    float finalVolume = Mathf.Clamp01(m_ImpactVolume * KeyWeaponAudioSettings.ImpactVolume);
                    s_ImpactAudioSource.PlayOneShot(m_ImpactClip, finalVolume);
                }
            }
        }

        /// <summary>
        /// Returns the configured impact clip, or null if none set.
        /// Used by ProjectileStandard to check if it should skip its default impact sound.
        /// </summary>
        public AudioClip GetImpactClip()
        {
            return m_ImpactClip;
        }

        void OnDestroy()
        {
            // Stop travel sound gracefully
            if (m_TravelAudioSource != null && m_TravelAudioSource.isPlaying)
            {
                m_TravelAudioSource.Stop();
            }
        }
    }
}

