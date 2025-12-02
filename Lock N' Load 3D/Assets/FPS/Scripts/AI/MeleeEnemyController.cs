using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class MeleeEnemyController : MonoBehaviour
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

        [Header("Detection")]
        [Tooltip("Detection range for finding the player")]
        public float DetectionRange = 50f;

        [Tooltip("Attack range (how close to get before stopping)")]
        public float AttackRange = 1.5f;

        [Header("Movement")]
        [Tooltip("Movement speed while chasing player")]
        public float MoveSpeed = 4f;

        [Tooltip("Patrol movement speed")]
        public float PatrolSpeed = 2f;

        [Tooltip("How close to get before stopping")]
        public float StoppingDistance = 0.8f;

        [Header("Patrol")]
        [Tooltip("Optional patrol path")]
        public PatrolPath PatrolPath;

        [Header("Death")]
        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 2f;

        [Tooltip("VFX prefab spawned when enemy dies")]
        public GameObject DeathVfx;

        [Tooltip("Scale multiplier for death VFX")]
        public float DeathVfxScale = 3f;

        [Header("Flash on Hit")]
        [Tooltip("Body material to flash on hit (matches EnemyController)")]
        public Material BodyMaterial;

        [Tooltip("Flash gradient on hit")]
        [GradientUsage(true)]
        public Gradient OnHitBodyGradient;

        [Tooltip("Flash duration on hit")]
        public float FlashOnHitDuration = 0.5f;

        [Header("Death Flash")]
        [Tooltip("Delay between death flashes (higher = slower flashing)")]
        public float DeathFlashInterval = 0.3f;

        [Tooltip("Optional sound effect played at each death flash (leave empty for no sound)")]
        public AudioClip DeathFlashSound;

        private NavMeshAgent m_NavMeshAgent;
        private Health m_Health;
        private Actor m_Actor;
        private GameObject m_Target;
        private int m_PathDestinationNodeIndex;
        private Animator m_Animator;
        private ActorsManager m_ActorsManager;
        private EnemyManager m_EnemyManager;
        
        private System.Collections.Generic.List<RendererIndexData> m_BodyRenderers = new System.Collections.Generic.List<RendererIndexData>();
        private MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
        private float m_LastTimeDamaged = float.NegativeInfinity;
        private float m_TimeOfDeath = float.NegativeInfinity;
        private bool m_IsDying = false;
        private int m_LastDeathFlashCycle = -1;

        // Public properties for LockletAudioController access
        public bool IsDying => m_IsDying;
        public float TimeOfDeath => m_TimeOfDeath;

        void Start()
        {
            m_Health = GetComponent<Health>();
            m_Actor = GetComponent<Actor>();
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponentInChildren<Animator>();
            m_ActorsManager = FindAnyObjectByType<ActorsManager>();
            m_EnemyManager = FindAnyObjectByType<EnemyManager>();

            m_NavMeshAgent.speed = MoveSpeed;

            // register with enemymanager for objective tracking
            if (m_EnemyManager != null)
            {
                m_EnemyManager.RegisterEnemy(gameObject);
            }

            if (m_Health != null)
            {
                m_Health.OnDie += OnDie;
                m_Health.OnDamaged += OnDamaged;
            }

            // Initialize flash effect system (copied from EnemyController)
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                // If BodyMaterial is not set, flash ALL materials on ALL renderers
                if (BodyMaterial == null)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                    }
                }
                else
                {
                    // Only flash materials that match BodyMaterial
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        if (renderer.sharedMaterials[i] == BodyMaterial)
                        {
                            m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                        }
                    }
                }
            }

            m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();
        }

        void Update()
        {
            // Apply flash effect (damage or death)
            Color currentColor;
            if (m_IsDying)
            {
                // Death flash - repeated flashes with configurable interval, then solid red for last second
                float timeSinceDeath = Time.time - m_TimeOfDeath;
                float timeUntilDestroy = DeathDuration - timeSinceDeath;
                
                if (timeUntilDestroy <= 0.65f)
                {
                    // Last 0.65 seconds: solid bright red (very high emission)
                    currentColor = Color.red * 8f;
                }
                else
                {
                    // Repeated flashing: loop the hit flash gradient with custom interval
                    // Calculate which flash cycle we're in
                    int currentFlashCycle = (int)(timeSinceDeath / DeathFlashInterval);
                    
                    // Play sound at the start of each new flash cycle
                    if (DeathFlashSound != null && currentFlashCycle != m_LastDeathFlashCycle)
                    {
                        m_LastDeathFlashCycle = currentFlashCycle;
                        AudioUtility.CreateSFX(DeathFlashSound, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);
                    }
                    
                    // Use modulo to loop the flash every DeathFlashInterval
                    float loopTime = timeSinceDeath % DeathFlashInterval;
                    float t = loopTime / FlashOnHitDuration;
                    
                    // Flash pure red on/off using hit gradient timing
                    Color baseFlash = OnHitBodyGradient.Evaluate(t);
                    // Use baseFlash intensity (brightness) to lerp between black and red
                    float intensity = (baseFlash.r + baseFlash.g + baseFlash.b) / 3f;
                    currentColor = Color.red * 8f * intensity;
                }
            }
            else
            {
                // Normal damage flash effect
                currentColor = OnHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / FlashOnHitDuration);
            }
            
            m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
            foreach (var data in m_BodyRenderers)
            {
                data.Renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.MaterialIndex);
            }

            // Don't run AI logic if dying
            if (m_IsDying)
                return;

            DetectTarget();

            if (m_Target != null)
            {
                ChaseTarget();
                
                // always set Speed high to keep in walk state
                if (m_Animator != null)
                {
                    m_Animator.SetFloat("Speed", 1f);
                }
            }
            else if (PatrolPath != null)
            {
                Patrol();
                
                if (m_Animator != null)
                {
                    m_Animator.SetFloat("Speed", 1f);
                }
            }
            else
            {
                // no target - set speed to 0 for idle
                if (m_Animator != null)
                {
                    m_Animator.SetFloat("Speed", 0f);
                }
            }
        }

        void DetectTarget()
        {
            // find player via ActorsManager
            if (m_Target == null && m_ActorsManager != null && m_ActorsManager.Player != null)
            {
                GameObject player = m_ActorsManager.Player;
                float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                
                if (distanceToPlayer <= DetectionRange)
                {
                    m_Target = player;
                }
            }
            else if (m_Target != null)
            {
                // check if we should lose target
                float distanceToPlayer = Vector3.Distance(transform.position, m_Target.transform.position);
                if (distanceToPlayer > DetectionRange * 1.5f)
                {
                    m_Target = null;
                }
            }
        }

        void ChaseTarget()
        {
            if (m_Target == null || m_NavMeshAgent == null)
                return;

            // always keep moving toward target, never stop
            m_NavMeshAgent.isStopped = false;
            m_NavMeshAgent.speed = MoveSpeed;
            m_NavMeshAgent.SetDestination(m_Target.transform.position);
        }

        void Patrol()
        {
            if (PatrolPath == null || PatrolPath.PathNodes.Count == 0)
                return;

            m_NavMeshAgent.speed = PatrolSpeed;
            Vector3 destination = PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
            
            if (Vector3.Distance(transform.position, destination) <= 2f)
            {
                m_PathDestinationNodeIndex = (m_PathDestinationNodeIndex + 1) % PatrolPath.PathNodes.Count;
            }

            m_NavMeshAgent.SetDestination(destination);
        }

        void OnDie()
        {
            // Track death time for red flash effect
            m_TimeOfDeath = Time.time;
            m_IsDying = true;
            m_LastDeathFlashCycle = -1; // Reset flash cycle counter
            
            // unregister from enemymanager
            if (m_EnemyManager != null)
            {
                m_EnemyManager.UnregisterEnemy(gameObject);
            }
            
            // stop the enemy from moving
            if (m_NavMeshAgent != null)
            {
                m_NavMeshAgent.isStopped = true;
                m_NavMeshAgent.enabled = false;
            }

            // trigger death animation using the die trigger
            if (m_Animator != null)
            {
                m_Animator.SetTrigger("Die");
            }

            // Schedule death VFX to spawn after animation completes
            if (DeathVfx != null)
            {
                Invoke(nameof(SpawnDeathVfx), DeathDuration);
            }

            // destroy after death duration
            Destroy(gameObject, DeathDuration);
        }

        void SpawnDeathVfx()
        {
            if (DeathVfx != null)
            {
                // Get center of mesh bounds for VFX spawn position
                Vector3 spawnPosition = transform.position;
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    // Calculate bounds center from all renderers
                    Bounds combinedBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        combinedBounds.Encapsulate(renderers[i].bounds);
                    }
                    spawnPosition = combinedBounds.center;
                }
                
                GameObject vfx = Instantiate(DeathVfx, spawnPosition, Quaternion.identity);
                vfx.transform.localScale = Vector3.one * DeathVfxScale;
                Destroy(vfx, 5f);
            }
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // Set last damage time for flash effect
            m_LastTimeDamaged = Time.time;
            
            // Trigger hit animation
            if (m_Animator != null)
            {
                m_Animator.SetTrigger("OnDamaged");
            }
        }

        void OnDestroy()
        {
            if (m_Health != null)
            {
                m_Health.OnDie -= OnDie;
                m_Health.OnDamaged -= OnDamaged;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
