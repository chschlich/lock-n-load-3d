using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class MeleeEnemyController : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Detection range for finding the player")]
        public float DetectionRange = 15f;

        [Tooltip("Attack range (how close to get before stopping)")]
        public float AttackRange = 1.5f;

        [Header("Movement")]
        [Tooltip("Movement speed while chasing player")]
        public float MoveSpeed = 3.5f;

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

        private NavMeshAgent m_NavMeshAgent;
        private Health m_Health;
        private Actor m_Actor;
        private GameObject m_Target;
        private int m_PathDestinationNodeIndex;
        private Animator m_Animator;
        private ActorsManager m_ActorsManager;
        private EnemyManager m_EnemyManager;
        private bool m_IsPlayingWalkAnimation = false;

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
            }
        }

        void Update()
        {
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

            // disable the ai controller
            enabled = false;

            // destroy after 1.5 seconds (death animation is 40 frames)
            Destroy(gameObject, 1.5f);
        }

        void OnDestroy()
        {
            if (m_Health != null)
            {
                m_Health.OnDie -= OnDie;
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
