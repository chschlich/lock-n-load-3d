using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Handles locklet-specific audio: hitmarker sounds and death status indicators.
    /// Attach to locklet prefabs.
    /// </summary>
    [RequireComponent(typeof(MeleeEnemyController))]
    public class LockletAudioController : MonoBehaviour
    {
        [Header("Hitmarker Audio")]
        [Tooltip("Sound played when locklet takes damage")]
        public AudioClip HitmarkerSound;
        
        [Tooltip("Volume for hitmarker sound (0-10x multiplier)")]
        [Range(0f, 10f)]
        public float HitmarkerVolume = 1f;
        
        [Header("Death Status Indicator Audio")]
        [Tooltip("Sound played during death flash cycles (must be 0.125s duration)")]
        public AudioClip StatusIndicatorSound;
        
        [Tooltip("Volume for status indicator sound (0-10x multiplier)")]
        [Range(0f, 10f)]
        public float StatusIndicatorVolume = 1f;
        
        private MeleeEnemyController m_EnemyController;
        private Health m_Health;
        private bool m_HasPlayedFinalIndicator = false;
        private int m_LastFlashCycle = -1;
        private bool m_IsDying = false;
        
        // Constants
        private const float REQUIRED_DURATION = 0.125f;
        private const float DURATION_TOLERANCE = 0.01f; // Allow 10ms tolerance
        
        void Awake()
        {
            m_EnemyController = GetComponent<MeleeEnemyController>();
            m_Health = GetComponent<Health>();
            
            // Validate status indicator duration
            ValidateStatusIndicatorDuration();
        }
        
        void OnEnable()
        {
            if (m_Health != null)
            {
                m_Health.OnDamaged += OnDamaged;
                m_Health.OnDie += OnDie;
            }
        }
        
        void OnDisable()
        {
            if (m_Health != null)
            {
                m_Health.OnDamaged -= OnDamaged;
                m_Health.OnDie -= OnDie;
            }
        }
        
        void Update()
        {
            if (!m_IsDying) return;
            
            // Play status indicator during death sequence
            PlayStatusIndicatorDuringDeath();
        }
        
        void OnDamaged(float damage, GameObject damageSource)
        {
            // Play hitmarker sound globally at player's position (bypasses audio mixer for direct volume control)
            if (HitmarkerSound != null)
            {
                // Play at camera position so it's always audible to player
                Vector3 playerPosition = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(HitmarkerSound, playerPosition, HitmarkerVolume);
            }
        }
        
        void OnDie()
        {
            m_IsDying = true;
            m_HasPlayedFinalIndicator = false;
            m_LastFlashCycle = -1;
        }
        
        void PlayStatusIndicatorDuringDeath()
        {
            if (StatusIndicatorSound == null || m_EnemyController == null) return;
            
            float timeSinceDeath = Time.time - m_EnemyController.TimeOfDeath;
            float timeUntilDestroy = m_EnemyController.DeathDuration - timeSinceDeath;
            
            // Check if we're in final overlay phase (last 0.65 seconds)
            if (timeUntilDestroy <= 0.65f)
            {
                // Play status indicator once when entering final overlay
                if (!m_HasPlayedFinalIndicator)
                {
                    // Use large rolloff distance (250 units) for wide audible range
                    AudioUtility.CreateSFX(StatusIndicatorSound, transform.position, 
                        AudioUtility.AudioGroups.DamageTick, 3f, 250f, StatusIndicatorVolume);
                    m_HasPlayedFinalIndicator = true;
                }
            }
            else
            {
                // During flashing phase: play at each flash cycle
                int currentFlashCycle = (int)(timeSinceDeath / m_EnemyController.DeathFlashInterval);
                
                if (currentFlashCycle != m_LastFlashCycle)
                {
                    m_LastFlashCycle = currentFlashCycle;
                    // Use large rolloff distance (250 units) for wide audible range
                    AudioUtility.CreateSFX(StatusIndicatorSound, transform.position, 
                        AudioUtility.AudioGroups.DamageTick, 3f, 250f, StatusIndicatorVolume);
                }
            }
        }
        
        void ValidateStatusIndicatorDuration()
        {
            if (StatusIndicatorSound != null)
            {
                float duration = StatusIndicatorSound.length;
                float expectedDuration = REQUIRED_DURATION;
                
                if (Mathf.Abs(duration - expectedDuration) > DURATION_TOLERANCE)
                {
                    Debug.LogWarning($"LockletAudioController on {gameObject.name}: StatusIndicatorSound duration is {duration}s, " +
                        $"but should be {expectedDuration}s (±{DURATION_TOLERANCE}s). Sound may not sync properly with flash animation.");
                }
            }
        }
    }
}

