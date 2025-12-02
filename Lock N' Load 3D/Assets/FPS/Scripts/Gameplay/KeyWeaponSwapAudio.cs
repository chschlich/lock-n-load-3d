using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Handles audio for weapon swapping animations.
    /// Add this component to key weapon prefabs and assign put-away/pull-out sounds.
    /// Sounds are triggered by KeyWeaponController during weapon switching.
    /// </summary>
    public class KeyWeaponSwapAudio : MonoBehaviour
    {
        [Header("Swap Sounds")]
        [Tooltip("Sound played when weapon is pulled out (drawn)")]
        public AudioClip PullOutSound;
        
        [Header("Volumes")]
        [Tooltip("Volume for pull-out sound")]
        [Range(0f, 5f)]
        public float PullOutVolume = 1f;

        private AudioSource m_AudioSource;

        void Awake()
        {
            // Create AudioSource for weapon swap sounds
            m_AudioSource = gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.spatialBlend = 0f; // 2D sound (first-person weapon)
            m_AudioSource.loop = false;
            
            // Try to use the weapon shoot audio group if available
            var audioGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
            if (audioGroup != null)
            {
                m_AudioSource.outputAudioMixerGroup = audioGroup;
            }
        }

        /// <summary>
        /// Play the pull-out sound when weapon is being drawn
        /// </summary>
        public void PlayPullOutSound()
        {
            if (PullOutSound != null && m_AudioSource != null)
            {
                // Apply master pull out volume from settings
                float finalVolume = PullOutVolume * KeyWeaponAudioSettings.PullOutVolume;
                m_AudioSource.PlayOneShot(PullOutSound, finalVolume);
            }
        }
    }
}

