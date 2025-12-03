using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Handles locklet-specific audio: hitmarker sounds, death status indicators, and footsteps.
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
        
        [Tooltip("Maximum audible distance for status indicator sounds")]
        [Range(10f, 500f)]
        public float StatusIndicatorMaxDistance = 250f;
        
        [Header("Footstep Audio")]
        [Tooltip("Sound played for each footstep while walking")]
        public AudioClip FootstepSound;
        
        [Tooltip("Volume for footstep sound (0-5x multiplier)")]
        [Range(0f, 5f)]
        public float FootstepVolume = 0.5f;
        
        [Tooltip("Maximum audible distance for footstep sounds")]
        [Range(10f, 200f)]
        public float FootstepMaxDistance = 50f;
        
        [Header("Footstep Screen Shake")]
        [Tooltip("Enable screen shake for nearby players when footsteps occur")]
        public bool EnableFootstepShake = false;
        
        [Tooltip("Maximum distance from locklet that can cause screen shake")]
        [Range(1f, 50f)]
        public float MaxShakeDistance = 10f;
        
        [Tooltip("Base shake intensity at locklet position")]
        [Range(0f, 1f)]
        public float ShakeIntensity = 0.05f;
        
        [Tooltip("Duration of the shake effect in seconds")]
        [Range(0.05f, 0.5f)]
        public float ShakeDuration = 0.1f;
        
        [Tooltip("Name of the walk animation state in the Animator")]
        public string WalkStateName = "Walk";
        
        [Tooltip("First footstep timing in walk animation cycle (0-1 normalized time). Frame 0/20 = 0.0")]
        [Range(0f, 1f)]
        public float FootstepTiming1 = 0.05f;
        
        [Tooltip("Second footstep timing in walk animation cycle (0-1 normalized time). Frame 10/20 = 0.5")]
        [Range(0f, 1f)]
        public float FootstepTiming2 = 0.5f;
        
        [Tooltip("Debug: Print normalized time to console to help find footstep timings")]
        public bool DebugAnimationTiming = false;
        
        private MeleeEnemyController m_EnemyController;
        private Health m_Health;
        private NavMeshAgent m_NavMeshAgent;
        private Animator m_Animator;
        private bool m_HasPlayedFinalIndicator = false;
        private int m_LastFlashCycle = -1;
        private bool m_IsDying = false;
        
        // Footstep animation sync tracking
        private float m_LastAnimNormalizedTime = 0f;
        private bool m_PlayedFootstep1ThisCycle = false;
        private bool m_PlayedFootstep2ThisCycle = false;
        
        // Dedicated AudioSource for footsteps (3D spatial, per-instance)
        private AudioSource m_FootstepAudioSource;
        
        // Static shared AudioSource for hitmarkers (2D, centered audio)
        private static AudioSource s_HitmarkerAudioSource;
        private static float s_LastHitmarkerTime = 0f;
        
        // Constants
        private const float REQUIRED_DURATION = 0.125f;
        private const float DURATION_TOLERANCE = 0.01f; // Allow 10ms tolerance
        private const float HITMARKER_COOLDOWN = 0.08f; // Minimum time between hitmarker sounds (prevents distortion)
        
        void Awake()
        {
            m_EnemyController = GetComponent<MeleeEnemyController>();
            m_Health = GetComponent<Health>();
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponentInChildren<Animator>();
            
            // Create shared hitmarker audio source if it doesn't exist
            EnsureHitmarkerAudioSource();
            
            // Create dedicated footstep audio source for this locklet
            CreateFootstepAudioSource();
            
            // Validate status indicator duration
            ValidateStatusIndicatorDuration();
        }
        
        void CreateFootstepAudioSource()
        {
            m_FootstepAudioSource = gameObject.AddComponent<AudioSource>();
            m_FootstepAudioSource.playOnAwake = false;
            m_FootstepAudioSource.spatialBlend = 1f; // Full 3D spatial audio
            m_FootstepAudioSource.rolloffMode = AudioRolloffMode.Linear;
            m_FootstepAudioSource.minDistance = 1f;
            m_FootstepAudioSource.maxDistance = FootstepMaxDistance;
            m_FootstepAudioSource.volume = FootstepVolume;
        }
        
        static void EnsureHitmarkerAudioSource()
        {
            if (s_HitmarkerAudioSource == null)
            {
                // Create a persistent GameObject for hitmarker audio
                GameObject hitmarkerAudioObj = new GameObject("LockletHitmarkerAudio");
                DontDestroyOnLoad(hitmarkerAudioObj);
                
                s_HitmarkerAudioSource = hitmarkerAudioObj.AddComponent<AudioSource>();
                s_HitmarkerAudioSource.playOnAwake = false;
                s_HitmarkerAudioSource.spatialBlend = 0f; // 2D audio - no stereo panning
                s_HitmarkerAudioSource.volume = 1f;
            }
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
            // Handle footsteps while walking (only if not dying)
            if (!m_IsDying)
            {
                HandleFootsteps();
            }
            else
            {
                // Play status indicator during death sequence
                PlayStatusIndicatorDuringDeath();
            }
        }
        
        void HandleFootsteps()
        {
            if (FootstepSound == null || m_Animator == null || m_FootstepAudioSource == null) return;
            
            // Update audio source settings dynamically (allows real-time slider changes)
            m_FootstepAudioSource.volume = FootstepVolume;
            m_FootstepAudioSource.maxDistance = FootstepMaxDistance;
            
            // Get current animation state info from base layer (layer 0)
            AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're in the Walk state
            if (!stateInfo.IsName(WalkStateName))
            {
                // Not walking, reset footstep tracking
                m_PlayedFootstep1ThisCycle = false;
                m_PlayedFootstep2ThisCycle = false;
                m_LastAnimNormalizedTime = 0f;
                return;
            }
            
            // Get normalized time (0-1 within the animation, loops back to 0)
            float normalizedTime = stateInfo.normalizedTime % 1f;
            
            // Debug: Print normalized time every 0.1 interval
            if (DebugAnimationTiming && Mathf.Abs(normalizedTime - Mathf.Round(normalizedTime * 10f) / 10f) < 0.02f)
            {
                Debug.Log($"{gameObject.name}: Walk normalized time = {normalizedTime:F2}");
            }
            
            // Detect when animation loops (normalized time wraps from ~1 back to ~0)
            if (normalizedTime < m_LastAnimNormalizedTime - 0.5f)
            {
                // Animation looped, reset footstep flags for new cycle
                m_PlayedFootstep1ThisCycle = false;
                m_PlayedFootstep2ThisCycle = false;
                
                if (DebugAnimationTiming)
                {
                    Debug.Log($"{gameObject.name}: Animation looped - resetting footstep flags");
                }
            }
            m_LastAnimNormalizedTime = normalizedTime;
            
            // Check for footstep 1 timing (detection window: 0.15 = ~3 frames for 20-frame animation)
            if (!m_PlayedFootstep1ThisCycle && normalizedTime >= FootstepTiming1 && normalizedTime < FootstepTiming1 + 0.15f)
            {
                PlayFootstep();
                m_PlayedFootstep1ThisCycle = true;
            }
            
            // Check for footstep 2 timing (detection window: 0.15 = ~3 frames for 20-frame animation)
            if (!m_PlayedFootstep2ThisCycle && normalizedTime >= FootstepTiming2 && normalizedTime < FootstepTiming2 + 0.15f)
            {
                PlayFootstep();
                m_PlayedFootstep2ThisCycle = true;
            }
        }
        
        void PlayFootstep()
        {
            if (m_FootstepAudioSource != null && FootstepSound != null)
            {
                // Clamp volume to 1.0 for PlayOneShot to prevent distortion
                float clampedVolume = Mathf.Clamp01(FootstepVolume);
                m_FootstepAudioSource.PlayOneShot(FootstepSound, clampedVolume);
                
                if (DebugAnimationTiming)
                {
                    Debug.Log($"{gameObject.name}: Footstep played at normalized time {m_LastAnimNormalizedTime:F3}");
                }
                
                // Apply proximity-based screenshake if enabled
                if (EnableFootstepShake)
                {
                    ApplyFootstepScreenShake();
                }
            }
        }
        
        void ApplyFootstepScreenShake()
        {
            // Find the player
            var actorsManager = FindAnyObjectByType<ActorsManager>();
            if (actorsManager == null || actorsManager.Player == null)
            {
                if (DebugAnimationTiming)
                {
                    Debug.LogWarning($"{gameObject.name}: FootstepShake - ActorsManager or Player not found");
                }
                return;
            }
            
            // Get the player's camera
            var playerController = actorsManager.Player.GetComponent<PlayerCharacterController>();
            if (playerController == null || playerController.PlayerCamera == null)
            {
                if (DebugAnimationTiming)
                {
                    Debug.LogWarning($"{gameObject.name}: FootstepShake - PlayerController or Camera not found");
                }
                return;
            }
            
            // Calculate distance from locklet to player
            float distance = Vector3.Distance(transform.position, actorsManager.Player.transform.position);
            
            // Only shake if within range
            if (distance > MaxShakeDistance)
            {
                if (DebugAnimationTiming)
                {
                    Debug.Log($"{gameObject.name}: FootstepShake - Too far away ({distance:F1} > {MaxShakeDistance})");
                }
                return;
            }
            
            // Distance-based falloff: at distance 0 = full intensity, at MaxShakeDistance = 0 intensity
            float falloff = 1f - (distance / MaxShakeDistance);
            float scaledIntensity = ShakeIntensity * falloff * falloff; // Squared falloff for more dramatic drop
            
            if (DebugAnimationTiming)
            {
                Debug.Log($"{gameObject.name}: FootstepShake - Distance: {distance:F1}, Intensity: {scaledIntensity:F3}");
            }
            
            // Apply the shake
            CameraShake.ApplyShake(playerController.PlayerCamera, scaledIntensity, ShakeDuration);
        }
        
        void OnDamaged(float damage, GameObject damageSource)
        {
            // Play hitmarker sound as 2D centered audio (no stereo panning)
            // Uses cooldown to prevent distortion from rapid fire weapons
            if (HitmarkerSound != null && s_HitmarkerAudioSource != null)
            {
                // Check cooldown to prevent audio buildup/distortion
                if (Time.time - s_LastHitmarkerTime >= HITMARKER_COOLDOWN)
                {
                    s_LastHitmarkerTime = Time.time;
                    
                    // Volume is clamped to 1.0 max to prevent distortion
                    float clampedVolume = Mathf.Clamp01(HitmarkerVolume);
                    s_HitmarkerAudioSource.PlayOneShot(HitmarkerSound, clampedVolume);
                }
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
                    // Use configurable rolloff distance for wide audible range
                    AudioUtility.CreateSFX(StatusIndicatorSound, transform.position, 
                        AudioUtility.AudioGroups.DamageTick, 3f, StatusIndicatorMaxDistance, StatusIndicatorVolume);
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
                    // Use configurable rolloff distance for wide audible range
                    AudioUtility.CreateSFX(StatusIndicatorSound, transform.position, 
                        AudioUtility.AudioGroups.DamageTick, 3f, StatusIndicatorMaxDistance, StatusIndicatorVolume);
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

