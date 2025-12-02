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
        /// Spawns a separate AudioSource for the impact sound since this object will be destroyed.
        /// </summary>
        public void PlayImpactSound(Vector3 position)
        {
            if (m_ImpactClip != null)
            {
                // Apply master impact volume from settings
                float finalVolume = m_ImpactVolume * KeyWeaponAudioSettings.ImpactVolume;
                // Increased rolloff from 3f to 50f for better audibility
                AudioUtility.CreateSFX(m_ImpactClip, position, AudioUtility.AudioGroups.Impact, 1f, 50f, finalVolume);
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

