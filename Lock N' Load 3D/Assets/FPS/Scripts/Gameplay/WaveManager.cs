using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Manages the wave-based enemy spawning system.
    /// Place this component on a GameObject in the scene.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [System.Serializable]
        public class WaveData
        {
            public int SpawnerCount = 4;
            public int TotalEnemies = 20;
            
            [Tooltip("Time between enemy spawns per spawner")]
            public float SpawnInterval = 0.67f; // ~1.5 enemies per second
        }
        
        [Header("Wave Configuration")]
        [Tooltip("Configuration for each wave")]
        public WaveData[] Waves = new WaveData[]
        {
            new WaveData { SpawnerCount = 4, TotalEnemies = 20, SpawnInterval = 0.67f },
            new WaveData { SpawnerCount = 6, TotalEnemies = 45, SpawnInterval = 0.67f },
            new WaveData { SpawnerCount = 8, TotalEnemies = 80, SpawnInterval = 0.67f }
        };
        
        [Header("Spawner Positions")]
        [Tooltip("Spawn point positions in world space (Room 2 is approximately at -13, 0, -15)")]
        public Vector3[] SpawnPoints = new Vector3[]
        {
            // Room 2 corners (approximate - adjust in editor)
            new Vector3(-23f, 0f, -5f),   // NW corner
            new Vector3(-3f, 0f, -5f),    // NE corner
            new Vector3(-23f, 0f, -25f),  // SW corner
            new Vector3(-3f, 0f, -25f),   // SE corner
            // Room 2 edges (for waves 2 and 3)
            new Vector3(-13f, 0f, -5f),   // North center
            new Vector3(-13f, 0f, -25f),  // South center
            new Vector3(-23f, 0f, -15f),  // West center
            new Vector3(-3f, 0f, -15f),   // East center
            // Room 2 center (for boss wave)
            new Vector3(-13f, 0f, -15f)   // Center of room
        };
        
        [Header("Debug Visualization")]
        [Tooltip("Color of spawn point gizmos in Scene view")]
        public Color GizmoColor = Color.red;
        
        [Tooltip("Size of spawn point gizmos")]
        public float GizmoSize = 1f;
        
        [Header("Enemy Prefabs")]
        [Tooltip("The locklet prefabs to spawn - one will be randomly chosen for each spawn")]
        public GameObject[] EnemyPrefabs;
        
        [Header("Ole Locklet (Boss Wave)")]
        [Tooltip("The Ole Locklet boss prefab")]
        public GameObject OleLockletPrefab;
        
        [Tooltip("Which wave is the boss wave (0-indexed, default 2 = Wave 3)")]
        public int BossWaveIndex = 2;
        
        [Tooltip("Where to spawn the boss")]
        public Vector3 BossSpawnPosition = new Vector3(-13f, 0f, -15f);
        
        [Header("Spawn Settings")]
        [Tooltip("Random position offset around spawn point")]
        public float SpawnRandomOffset = 1.5f;
        
        [Tooltip("Scale of spawned enemies (0.33 = 1/3 size)")]
        public float EnemyScale = 0.33f;
        
        [Tooltip("Delay before first wave starts (allow other UI to display first)")]
        public float InitialDelay = 30f;
        
        [Tooltip("Delay between waves")]
        public float DelayBetweenWaves = 5f;
        
        [Header("Locklet Stats Override")]
        [Tooltip("Override locklet detection range")]
        public float LockletDetectionRange = 50f;
        
        [Tooltip("Override locklet movement speed")]
        public float LockletMoveSpeed = 4f;
        
        [Tooltip("Override locklet health")]
        public float LockletHealth = 100f;
        
        [Header("Locklet Audio - Hitmarker")]
        [Tooltip("Sound played when locklet takes damage")]
        public AudioClip LockletHitmarkerSound;
        
        [Tooltip("Volume for hitmarker sound")]
        [Range(0f, 10f)]
        public float LockletHitmarkerVolume = 1f;
        
        [Header("Locklet Audio - Death Status")]
        [Tooltip("Sound played during death flash cycles")]
        public AudioClip LockletStatusIndicatorSound;
        
        [Tooltip("Volume for status indicator sound")]
        [Range(0f, 10f)]
        public float LockletStatusIndicatorVolume = 1f;
        
        [Tooltip("Maximum audible distance for status indicator sounds")]
        [Range(10f, 500f)]
        public float LockletStatusIndicatorMaxDistance = 250f;
        
        [Header("Locklet Audio - Footsteps")]
        [Tooltip("Footstep sound for spawned locklets")]
        public AudioClip LockletFootstepSound;
        
        [Tooltip("Footstep volume")]
        [Range(0f, 5f)]
        public float LockletFootstepVolume = 0.5f;
        
        [Tooltip("Maximum audible distance for footstep sounds")]
        [Range(10f, 200f)]
        public float LockletFootstepMaxDistance = 50f;
        
        [Header("Locklet Audio - Screen Shake")]
        [Tooltip("Enable screen shake for nearby players when footsteps occur")]
        public bool LockletEnableFootstepShake = false;
        
        [Tooltip("Maximum distance from locklet that can cause screen shake")]
        [Range(1f, 50f)]
        public float LockletMaxShakeDistance = 10f;
        
        [Tooltip("Base shake intensity at locklet position")]
        [Range(0f, 1f)]
        public float LockletShakeIntensity = 0.05f;
        
        [Tooltip("Duration of the shake effect in seconds")]
        [Range(0.05f, 0.5f)]
        public float LockletShakeDuration = 0.1f;
        
        
        // Public state
        public int CurrentWave { get; private set; } = 0;
        public int EnemiesSpawned { get; private set; } = 0;
        public int EnemiesKilled { get; private set; } = 0;
        public int TotalEnemiesThisWave { get; private set; } = 0;
        public bool IsWaveActive { get; private set; } = false;
        public bool AllWavesComplete { get; private set; } = false;
        
        // Private state
        private List<Coroutine> m_SpawnCoroutines = new List<Coroutine>();
        private int m_EnemiesRemainingToSpawn;
        private bool m_IsBossWave = false;
        private GameObject m_BossInstance = null;
        
        void Awake()
        {
            // Subscribe to enemy kill events
            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
        }
        
        void Start()
        {
            if (EnemyPrefabs == null || EnemyPrefabs.Length == 0)
            {
                Debug.LogError("WaveManager: EnemyPrefabs array is empty!");
                return;
            }
            
            // Disable any ObjectiveKillEnemies to prevent early game end
            DisableKillObjectives();
            
            // Create WaveUI if it doesn't exist (search by type name to avoid assembly issues)
            var waveUI = FindWaveUI();
            if (waveUI == null)
            {
                // Add WaveUI component dynamically
                var waveUIType = System.Type.GetType("Unity.FPS.UI.WaveUI, Assembly-CSharp");
                if (waveUIType != null)
                {
                    gameObject.AddComponent(waveUIType);
                    Debug.Log("WaveManager: Created WaveUI component");
                }
                else
                {
                    Debug.LogWarning("WaveManager: Could not find WaveUI type - make sure WaveUI.cs exists");
                }
            }
            
            // Start the first wave after initial delay
            StartCoroutine(StartFirstWaveAfterDelay());
        }
        
        void DisableKillObjectives()
        {
            // Find and disable all ObjectiveKillEnemies to prevent early game completion
            var killObjectives = FindObjectsByType<ObjectiveKillEnemies>(FindObjectsSortMode.None);
            foreach (var obj in killObjectives)
            {
                Debug.Log($"WaveManager: Disabling ObjectiveKillEnemies on {obj.gameObject.name}");
                obj.gameObject.SetActive(false);
            }
            
            // Also disable ObjectiveManager to prevent it from ending the game
            var objectiveManager = FindFirstObjectByType<ObjectiveManager>();
            if (objectiveManager != null)
            {
                Debug.Log("WaveManager: Disabling ObjectiveManager");
                objectiveManager.enabled = false;
            }
        }
        
        MonoBehaviour FindWaveUI()
        {
            // Find WaveUI by searching all MonoBehaviours
            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var b in allBehaviours)
            {
                if (b.GetType().Name == "WaveUI")
                    return b;
            }
            return null;
        }
        
        IEnumerator StartFirstWaveAfterDelay()
        {
            yield return new WaitForSeconds(InitialDelay);
            StartNextWave();
        }
        
        void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        }
        
        void OnEnemyKilled(EnemyKillEvent evt)
        {
            if (!IsWaveActive) return;
            
            EnemiesKilled++;
            
            // For boss waves, check if all enemies (boss + minions) are dead
            if (m_IsBossWave)
            {
                CheckBossWaveComplete();
                return;
            }
            
            // Check if wave is complete (all enemies spawned AND killed)
            if (EnemiesKilled >= TotalEnemiesThisWave && m_EnemiesRemainingToSpawn <= 0)
            {
                CompleteWave();
            }
        }
        
        void StartNextWave()
        {
            if (CurrentWave >= Waves.Length)
            {
                // All waves complete
                AllWavesComplete = true;
                
                // Broadcast all waves complete event (WaveUI handles victory display)
                EventManager.Broadcast(Events.AllWavesCompleteEvent);
                
                // Note: Victory is triggered by AllWavesCompleteEvent, not AllObjectivesCompletedEvent
                // to avoid conflict with existing objective system
                return;
            }
            
            // Check if this is a boss wave
            if (CurrentWave == BossWaveIndex && OleLockletPrefab != null)
            {
                StartBossWave();
                return;
            }
            
            WaveData waveData = Waves[CurrentWave];
            
            // Reset wave state
            EnemiesSpawned = 0;
            EnemiesKilled = 0;
            TotalEnemiesThisWave = waveData.TotalEnemies;
            m_EnemiesRemainingToSpawn = waveData.TotalEnemies;
            IsWaveActive = true;
            
            // Broadcast wave start event
            WaveStartEvent waveStartEvent = Events.WaveStartEvent;
            waveStartEvent.WaveNumber = CurrentWave + 1;
            waveStartEvent.TotalEnemies = waveData.TotalEnemies;
            EventManager.Broadcast(waveStartEvent);
            
            Debug.Log($"Wave {CurrentWave + 1} starting: {waveData.TotalEnemies} enemies from {waveData.SpawnerCount} spawners");
            
            // Calculate enemies per spawner
            int enemiesPerSpawner = waveData.TotalEnemies / waveData.SpawnerCount;
            int extraEnemies = waveData.TotalEnemies % waveData.SpawnerCount;
            
            // Start spawning from each active spawner
            m_SpawnCoroutines.Clear();
            for (int i = 0; i < waveData.SpawnerCount && i < SpawnPoints.Length; i++)
            {
                int enemiesToSpawn = enemiesPerSpawner + (i < extraEnemies ? 1 : 0);
                var coroutine = StartCoroutine(SpawnEnemiesFromPoint(SpawnPoints[i], enemiesToSpawn, waveData.SpawnInterval));
                m_SpawnCoroutines.Add(coroutine);
            }
        }
        
        void StartBossWave()
        {
            // Reset wave state for boss wave
            EnemiesSpawned = 1;
            EnemiesKilled = 0;
            TotalEnemiesThisWave = 1; // Just the boss initially - minions will add dynamically
            m_EnemiesRemainingToSpawn = 0;
            IsWaveActive = true;
            m_IsBossWave = true;
            
            // Broadcast wave start event
            WaveStartEvent waveStartEvent = Events.WaveStartEvent;
            waveStartEvent.WaveNumber = CurrentWave + 1;
            waveStartEvent.TotalEnemies = 1; // Display as 1 enemy (the boss)
            EventManager.Broadcast(waveStartEvent);
            
            Debug.Log($"Wave {CurrentWave + 1} (BOSS WAVE) starting: Ole Locklet!");
            
            // Spawn Ole Locklet
            GameObject boss = Instantiate(OleLockletPrefab, BossSpawnPosition, Quaternion.identity);
            m_BossInstance = boss;
            
            // Track boss death
            var health = boss.GetComponent<Health>();
            if (health != null)
            {
                health.OnDie += OnBossDied;
            }
        }
        
        void OnBossDied()
        {
            m_BossInstance = null;
            Debug.Log("Ole Locklet defeated! VICTORY!");
            
            // Boss defeated = instant victory, don't wait for minions
            CompleteWave();
        }
        
        void CheckBossWaveComplete()
        {
            if (!m_IsBossWave || !IsWaveActive) return;
            
            // Get current enemy count from EnemyManager using reflection (cross-assembly)
            int remainingEnemies = GetEnemyManagerRemainingCount();
            
            // Wave complete when all enemies (boss + minions) are dead
            if (remainingEnemies == 0)
            {
                CompleteWave();
            }
        }
        
        int GetEnemyManagerRemainingCount()
        {
            // Find EnemyManager by type name to avoid cross-assembly reference issues
            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var b in allBehaviours)
            {
                if (b.GetType().Name == "EnemyManager")
                {
                    // Use reflection to get NumberOfEnemiesRemaining property
                    var prop = b.GetType().GetProperty("NumberOfEnemiesRemaining");
                    if (prop != null)
                    {
                        return (int)prop.GetValue(b);
                    }
                }
            }
            return 0;
        }
        
        IEnumerator SpawnEnemiesFromPoint(Vector3 spawnPoint, int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                if (!IsWaveActive) yield break;
                
                // Add random offset
                Vector3 offset = new Vector3(
                    Random.Range(-SpawnRandomOffset, SpawnRandomOffset),
                    0f,
                    Random.Range(-SpawnRandomOffset, SpawnRandomOffset)
                );
                
                Vector3 spawnPosition = spawnPoint + offset;
                
                // Randomly select an enemy prefab
                GameObject prefabToSpawn = EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
                
                // Spawn the enemy (it will self-register with EnemyManager in its Start())
                GameObject enemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                
                // Don't override scale - let each prefab use its own scale settings
                // Don't override stats - let each prefab use its own health/speed settings
                
                EnemiesSpawned++;
                m_EnemiesRemainingToSpawn--;
                
                yield return new WaitForSeconds(interval);
            }
        }
        
        void CompleteWave()
        {
            IsWaveActive = false;
            m_IsBossWave = false;
            m_BossInstance = null;
            
            // Stop any remaining spawn coroutines
            foreach (var coroutine in m_SpawnCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            m_SpawnCoroutines.Clear();
            
            // Broadcast wave complete event
            WaveCompleteEvent waveCompleteEvent = Events.WaveCompleteEvent;
            waveCompleteEvent.WaveNumber = CurrentWave + 1;
            EventManager.Broadcast(waveCompleteEvent);
            
            Debug.Log($"Wave {CurrentWave + 1} complete!");
            
            CurrentWave++;
            
            // Start next wave after delay
            if (CurrentWave < Waves.Length)
            {
                StartCoroutine(StartNextWaveAfterDelay());
            }
            else
            {
                StartNextWave(); // This will trigger victory
            }
        }
        
        IEnumerator StartNextWaveAfterDelay()
        {
            yield return new WaitForSeconds(DelayBetweenWaves);
            StartNextWave();
        }
        
        /// <summary>
        /// Get the number of enemies remaining in the current wave
        /// </summary>
        public int GetEnemiesRemaining()
        {
            return TotalEnemiesThisWave - EnemiesKilled;
        }
        
        /// <summary>
        /// Apply locklet stats to spawned enemy (avoids cross-assembly reference issues)
        /// </summary>
        void ApplyLockletStats(GameObject enemy)
        {
            // Get MeleeEnemyController by searching components
            foreach (var component in enemy.GetComponents<MonoBehaviour>())
            {
                var type = component.GetType();
                if (type.Name == "MeleeEnemyController")
                {
                    // Use reflection to set properties
                    var detectionField = type.GetField("DetectionRange");
                    var speedField = type.GetField("MoveSpeed");
                    
                    if (detectionField != null)
                        detectionField.SetValue(component, LockletDetectionRange);
                    if (speedField != null)
                        speedField.SetValue(component, LockletMoveSpeed);
                    
                    break;
                }
            }
            
            // Apply audio settings to LockletAudioController
            foreach (var component in enemy.GetComponents<MonoBehaviour>())
            {
                var type = component.GetType();
                if (type.Name == "LockletAudioController")
                {
                    // Hitmarker settings
                    if (LockletHitmarkerSound != null)
                    {
                        var field = type.GetField("HitmarkerSound");
                        if (field != null) field.SetValue(component, LockletHitmarkerSound);
                    }
                    type.GetField("HitmarkerVolume")?.SetValue(component, LockletHitmarkerVolume);
                    
                    // Status indicator settings
                    if (LockletStatusIndicatorSound != null)
                    {
                        var field = type.GetField("StatusIndicatorSound");
                        if (field != null) field.SetValue(component, LockletStatusIndicatorSound);
                    }
                    type.GetField("StatusIndicatorVolume")?.SetValue(component, LockletStatusIndicatorVolume);
                    type.GetField("StatusIndicatorMaxDistance")?.SetValue(component, LockletStatusIndicatorMaxDistance);
                    
                    // Footstep settings
                    if (LockletFootstepSound != null)
                    {
                        var field = type.GetField("FootstepSound");
                        if (field != null) field.SetValue(component, LockletFootstepSound);
                    }
                    type.GetField("FootstepVolume")?.SetValue(component, LockletFootstepVolume);
                    type.GetField("FootstepMaxDistance")?.SetValue(component, LockletFootstepMaxDistance);
                    
                    // Screen shake settings
                    type.GetField("EnableFootstepShake")?.SetValue(component, LockletEnableFootstepShake);
                    type.GetField("MaxShakeDistance")?.SetValue(component, LockletMaxShakeDistance);
                    type.GetField("ShakeIntensity")?.SetValue(component, LockletShakeIntensity);
                    type.GetField("ShakeDuration")?.SetValue(component, LockletShakeDuration);
                    
                    break;
                }
            }
            
            // Get Health component
            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.MaxHealth = LockletHealth;
                health.Heal(LockletHealth);
            }
        }
        
        /// <summary>
        /// Force start a specific wave (for debugging)
        /// </summary>
        public void ForceStartWave(int waveIndex)
        {
            // Stop current wave
            IsWaveActive = false;
            foreach (var coroutine in m_SpawnCoroutines)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            m_SpawnCoroutines.Clear();
            
            CurrentWave = waveIndex;
            StartNextWave();
        }
        
        /// <summary>
        /// Draw spawn points in Scene view for easy positioning
        /// </summary>
        void OnDrawGizmos()
        {
            if (SpawnPoints == null) return;
            
            Gizmos.color = GizmoColor;
            for (int i = 0; i < SpawnPoints.Length; i++)
            {
                // Draw sphere at spawn point
                Gizmos.DrawWireSphere(SpawnPoints[i], GizmoSize);
                
                // Draw spawn number
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(SpawnPoints[i] + Vector3.up * (GizmoSize + 0.5f), $"Spawn {i + 1}");
                #endif
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (SpawnPoints == null) return;
            
            // When selected, draw filled spheres and connections
            Gizmos.color = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, 0.3f);
            for (int i = 0; i < SpawnPoints.Length; i++)
            {
                Gizmos.DrawSphere(SpawnPoints[i], GizmoSize);
                
                // Draw random offset area
                Gizmos.color = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, 0.1f);
                Gizmos.DrawCube(SpawnPoints[i], new Vector3(SpawnRandomOffset * 2, 0.1f, SpawnRandomOffset * 2));
                Gizmos.color = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, 0.3f);
            }
        }
    }
}

