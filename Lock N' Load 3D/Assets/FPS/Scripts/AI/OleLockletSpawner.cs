using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Spawns mini-locklets from Ole Locklet's hitbox faces.
    /// Spawn rate and count increase as health decreases.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class OleLockletSpawner : MonoBehaviour
    {
        [Header("Minion Prefab")]
        [Tooltip("The locklet prefab to spawn as minions")]
        public GameObject MinionPrefab;

        [Header("Spawn Audio")]
        [Tooltip("Sound effect played when a minion spawns")]
        public AudioClip SpawnSound;

        [Tooltip("Volume of spawn sound (0-10 multiplier)")]
        [Range(0f, 10f)]
        public float SpawnSoundVolume = 0.7f;
        
        [Tooltip("Spatial blend: 0 = 2D (same volume everywhere), 1 = 3D (quieter with distance)")]
        [Range(0f, 1f)]
        public float SpawnSoundSpatialBlend = 0.6f;
        
        [Header("Spawn Limits")]
        [Tooltip("Maximum total minions this boss can spawn (0 = unlimited)")]
        public int MaxTotalSpawns = 60;

        [Header("Spawn Timing")]
        [Tooltip("Time between spawns at full health")]
        public float BaseSpawnInterval = 5f;

        [Tooltip("Time between spawns at critical health")]
        public float MinSpawnInterval = 0.5f;

        [Header("Spawn Count")]
        [Tooltip("Number of minions spawned per cycle at full health")]
        public int BaseSpawnCount = 1;

        [Tooltip("Number of minions spawned per cycle at low health")]
        public int MaxSpawnCount = 5;

        [Header("Minion Settings")]
        [Tooltip("Scale of spawned minions")]
        public float MinionScale = 0.33f;

        [Tooltip("Minion health")]
        public float MinionHealth = 100f;

        [Tooltip("Minion detection range")]
        public float MinionDetectionRange = 50f;

        [Tooltip("Minion movement speed")]
        public float MinionMoveSpeed = 4f;
        
        [Header("Minion Audio - Hitmarker")]
        [Tooltip("Sound played when minion takes damage")]
        public AudioClip MinionHitmarkerSound;
        
        [Tooltip("Volume for hitmarker sound")]
        [Range(0f, 10f)]
        public float MinionHitmarkerVolume = 1f;
        
        [Header("Minion Audio - Death Status")]
        [Tooltip("Sound played during death flash cycles")]
        public AudioClip MinionStatusIndicatorSound;
        
        [Tooltip("Volume for status indicator sound")]
        [Range(0f, 10f)]
        public float MinionStatusIndicatorVolume = 1f;
        
        [Tooltip("Maximum audible distance for status indicator sounds")]
        [Range(10f, 500f)]
        public float MinionStatusIndicatorMaxDistance = 250f;
        
        [Header("Minion Audio - Footsteps")]
        [Tooltip("Footstep sound for spawned minions")]
        public AudioClip MinionFootstepSound;
        
        [Tooltip("Footstep volume")]
        [Range(0f, 5f)]
        public float MinionFootstepVolume = 0.5f;
        
        [Tooltip("Maximum audible distance for footstep sounds")]
        [Range(10f, 200f)]
        public float MinionFootstepMaxDistance = 50f;
        
        [Header("Minion Audio - Screen Shake")]
        [Tooltip("Enable screen shake for nearby players when footsteps occur")]
        public bool MinionEnableFootstepShake = false;
        
        [Tooltip("Maximum distance from minion that can cause screen shake")]
        [Range(1f, 50f)]
        public float MinionMaxShakeDistance = 10f;
        
        [Tooltip("Base shake intensity at minion position")]
        [Range(0f, 1f)]
        public float MinionShakeIntensity = 0.05f;
        
        [Tooltip("Duration of the shake effect in seconds")]
        [Range(0.05f, 0.5f)]
        public float MinionShakeDuration = 0.1f;
        

        private Health m_Health;
        private BoxCollider m_BoxCollider;
        private Coroutine m_SpawnCoroutine;
        private bool m_IsDead = false;
        private int m_TotalSpawned = 0;
        private List<GameObject> m_SpawnedMinions = new List<GameObject>();

        void Start()
        {
            m_Health = GetComponent<Health>();
            m_BoxCollider = GetComponent<BoxCollider>();

            if (m_Health == null)
            {
                Debug.LogError("OleLockletSpawner: No Health component found!");
                return;
            }

            if (m_BoxCollider == null)
            {
                Debug.LogWarning("OleLockletSpawner: No BoxCollider found, using transform position for spawns");
            }

            if (MinionPrefab == null)
            {
                Debug.LogError("OleLockletSpawner: MinionPrefab is not assigned!");
                return;
            }

            // Subscribe to death event
            m_Health.OnDie += OnDie;

            // Start spawning coroutine
            m_SpawnCoroutine = StartCoroutine(SpawnLoop());
        }

        void OnDestroy()
        {
            if (m_Health != null)
            {
                m_Health.OnDie -= OnDie;
            }
        }

        void OnDie()
        {
            m_IsDead = true;
            if (m_SpawnCoroutine != null)
            {
                StopCoroutine(m_SpawnCoroutine);
                m_SpawnCoroutine = null;
            }
            
            // Kill all surviving minions when Ole Locklet dies
            KillAllMinions();
        }
        
        void KillAllMinions()
        {
            foreach (var minion in m_SpawnedMinions)
            {
                if (minion != null)
                {
                    var health = minion.GetComponent<Health>();
                    if (health != null && health.CurrentHealth > 0)
                    {
                        health.Kill();
                    }
                }
            }
            m_SpawnedMinions.Clear();
        }

        IEnumerator SpawnLoop()
        {
            // Small initial delay before first spawn
            yield return new WaitForSeconds(1f);

            while (!m_IsDead && m_Health != null && m_Health.CurrentHealth > 0)
            {
                // Check if we've hit the spawn limit
                if (MaxTotalSpawns > 0 && m_TotalSpawned >= MaxTotalSpawns)
                {
                    Debug.Log($"OleLockletSpawner: Reached max spawn limit ({MaxTotalSpawns})");
                    yield break;
                }
                
                // Calculate health percentage (1 = full, 0 = dead)
                float healthPercent = m_Health.CurrentHealth / m_Health.MaxHealth;

                // Calculate current spawn interval (decreases as health decreases)
                float currentInterval = Mathf.Lerp(MinSpawnInterval, BaseSpawnInterval, healthPercent);

                // Calculate current spawn count (increases as health decreases)
                int currentCount = Mathf.RoundToInt(Mathf.Lerp(MaxSpawnCount, BaseSpawnCount, healthPercent));
                currentCount = Mathf.Max(1, currentCount);
                
                // Clamp to remaining spawn budget
                if (MaxTotalSpawns > 0)
                {
                    int remaining = MaxTotalSpawns - m_TotalSpawned;
                    currentCount = Mathf.Min(currentCount, remaining);
                }

                // Spawn minions
                SpawnMinions(currentCount);

                // Wait for next spawn cycle
                yield return new WaitForSeconds(currentInterval);
            }
        }

        void SpawnMinions(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = GetRandomSpawnPosition();
                
                // Spawn the minion
                GameObject minion = Instantiate(MinionPrefab, spawnPosition, Quaternion.identity);
                minion.transform.localScale = Vector3.one * MinionScale;

                // Apply minion stats
                ApplyMinionStats(minion);
                
                // Track spawned minion
                m_SpawnedMinions.Add(minion);
                m_TotalSpawned++;

                // Play spawn sound
                if (SpawnSound != null)
                {
                    AudioUtility.CreateSFX(SpawnSound, spawnPosition, AudioUtility.AudioGroups.EnemyDetection, SpawnSoundSpatialBlend, 1f, SpawnSoundVolume);
                }
            }
        }

        Vector3 GetRandomSpawnPosition()
        {
            if (m_BoxCollider == null)
            {
                // Fallback: spawn at transform position with small random offset
                return transform.position + new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                );
            }

            // Get box collider world-space parameters
            Vector3 center = transform.TransformPoint(m_BoxCollider.center);
            Vector3 size = Vector3.Scale(m_BoxCollider.size, transform.lossyScale);

            // Pick a random face (0=front, 1=back, 2=left, 3=right)
            int face = Random.Range(0, 4);

            Vector3 spawnPos = center;
            float halfX = size.x / 2f;
            float halfZ = size.z / 2f;

            // Random position along the face
            float randomAlongFace;

            switch (face)
            {
                case 0: // Front (+Z)
                    randomAlongFace = Random.Range(-halfX, halfX);
                    spawnPos += transform.forward * halfZ;
                    spawnPos += transform.right * randomAlongFace;
                    break;
                case 1: // Back (-Z)
                    randomAlongFace = Random.Range(-halfX, halfX);
                    spawnPos -= transform.forward * halfZ;
                    spawnPos += transform.right * randomAlongFace;
                    break;
                case 2: // Left (-X)
                    randomAlongFace = Random.Range(-halfZ, halfZ);
                    spawnPos -= transform.right * halfX;
                    spawnPos += transform.forward * randomAlongFace;
                    break;
                case 3: // Right (+X)
                    randomAlongFace = Random.Range(-halfZ, halfZ);
                    spawnPos += transform.right * halfX;
                    spawnPos += transform.forward * randomAlongFace;
                    break;
            }

            // Set Y to ground level (same as Ole Locklet's base)
            spawnPos.y = transform.position.y;

            return spawnPos;
        }

        void ApplyMinionStats(GameObject minion)
        {
            // Apply stats to MeleeEnemyController using reflection (to avoid assembly issues)
            foreach (var component in minion.GetComponents<MonoBehaviour>())
            {
                var type = component.GetType();
                if (type.Name == "MeleeEnemyController")
                {
                    var detectionField = type.GetField("DetectionRange");
                    var speedField = type.GetField("MoveSpeed");

                    if (detectionField != null)
                        detectionField.SetValue(component, MinionDetectionRange);
                    if (speedField != null)
                        speedField.SetValue(component, MinionMoveSpeed);

                    break;
                }
            }
            
            // Apply audio settings to LockletAudioController
            foreach (var component in minion.GetComponents<MonoBehaviour>())
            {
                var type = component.GetType();
                if (type.Name == "LockletAudioController")
                {
                    // Hitmarker settings
                    if (MinionHitmarkerSound != null)
                    {
                        var field = type.GetField("HitmarkerSound");
                        if (field != null) field.SetValue(component, MinionHitmarkerSound);
                    }
                    type.GetField("HitmarkerVolume")?.SetValue(component, MinionHitmarkerVolume);
                    
                    // Status indicator settings
                    if (MinionStatusIndicatorSound != null)
                    {
                        var field = type.GetField("StatusIndicatorSound");
                        if (field != null) field.SetValue(component, MinionStatusIndicatorSound);
                    }
                    type.GetField("StatusIndicatorVolume")?.SetValue(component, MinionStatusIndicatorVolume);
                    type.GetField("StatusIndicatorMaxDistance")?.SetValue(component, MinionStatusIndicatorMaxDistance);
                    
                    // Footstep settings
                    if (MinionFootstepSound != null)
                    {
                        var field = type.GetField("FootstepSound");
                        if (field != null) field.SetValue(component, MinionFootstepSound);
                    }
                    type.GetField("FootstepVolume")?.SetValue(component, MinionFootstepVolume);
                    type.GetField("FootstepMaxDistance")?.SetValue(component, MinionFootstepMaxDistance);
                    
                    // Screen shake settings
                    type.GetField("EnableFootstepShake")?.SetValue(component, MinionEnableFootstepShake);
                    type.GetField("MaxShakeDistance")?.SetValue(component, MinionMaxShakeDistance);
                    type.GetField("ShakeIntensity")?.SetValue(component, MinionShakeIntensity);
                    type.GetField("ShakeDuration")?.SetValue(component, MinionShakeDuration);
                    
                    break;
                }
            }
            
            // Apply health
            var health = minion.GetComponent<Health>();
            if (health != null)
            {
                health.MaxHealth = MinionHealth;
                health.Heal(MinionHealth);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Visualize spawn areas
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;

            Vector3 center = transform.TransformPoint(box.center);
            Vector3 size = Vector3.Scale(box.size, transform.lossyScale);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);

            float halfX = size.x / 2f;
            float halfZ = size.z / 2f;

            // Draw spawn face indicators
            // Front
            Gizmos.DrawCube(center + transform.forward * halfZ, new Vector3(size.x, 0.2f, 0.2f));
            // Back
            Gizmos.DrawCube(center - transform.forward * halfZ, new Vector3(size.x, 0.2f, 0.2f));
            // Left
            Gizmos.DrawCube(center - transform.right * halfX, new Vector3(0.2f, 0.2f, size.z));
            // Right
            Gizmos.DrawCube(center + transform.right * halfX, new Vector3(0.2f, 0.2f, size.z));
        }
    }
}

