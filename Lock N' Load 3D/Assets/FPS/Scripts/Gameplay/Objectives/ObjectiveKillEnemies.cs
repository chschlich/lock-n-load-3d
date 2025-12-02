using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectiveKillEnemies : Objective
    {
        [Tooltip("Chose whether you need to kill every enemies or only a minimum amount")]
        public bool MustKillAllEnemies = true;

        [Tooltip("If MustKillAllEnemies is false, this is the amount of enemy kills required")]
        public int KillsToCompleteObjective = 5;

        [Tooltip("Start sending notification about remaining enemies when this amount of enemies is left")]
        public int NotificationEnemiesRemainingThreshold = 3;
        
        [Tooltip("If true, waits for wave system to complete all waves before completing objective")]
        public bool UseWaveSystem = true;

        int m_KillTotal;
        WaveManager m_WaveManager;

        protected override void Start()
        {
            base.Start();

            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
            EventManager.AddListener<AllWavesCompleteEvent>(OnAllWavesComplete);
            
            // Find wave manager if using wave system
            if (UseWaveSystem)
            {
                m_WaveManager = FindFirstObjectByType<WaveManager>();
            }

            // set a title and description specific for this type of objective, if it hasn't one
            if (string.IsNullOrEmpty(Title))
                Title = "Eliminate " + (MustKillAllEnemies ? "all the" : KillsToCompleteObjective.ToString()) +
                        " enemies";

            if (string.IsNullOrEmpty(Description))
                Description = UseWaveSystem ? "Survive all waves" : GetUpdatedCounterAmount();
        }

        void OnEnemyKilled(EnemyKillEvent evt)
        {
            if (IsCompleted)
                return;

            m_KillTotal++;
            
            // If using wave system, don't complete based on remaining count
            // The AllWavesCompleteEvent will handle completion
            if (UseWaveSystem && m_WaveManager != null)
            {
                // Just update the kill count display, don't complete
                UpdateObjective(string.Empty, GetUpdatedCounterAmount(), string.Empty);
                return;
            }

            if (MustKillAllEnemies)
                KillsToCompleteObjective = evt.RemainingEnemyCount + m_KillTotal;

            int targetRemaining = MustKillAllEnemies ? evt.RemainingEnemyCount : KillsToCompleteObjective - m_KillTotal;

            // update the objective text according to how many enemies remain to kill
            if (targetRemaining == 0)
            {
                CompleteObjective(string.Empty, GetUpdatedCounterAmount(), "Objective complete : " + Title);
            }
            else if (targetRemaining == 1)
            {
                string notificationText = NotificationEnemiesRemainingThreshold >= targetRemaining
                    ? "One enemy left"
                    : string.Empty;
                UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
            else
            {
                // create a notification text if needed, if it stays empty, the notification will not be created
                string notificationText = NotificationEnemiesRemainingThreshold >= targetRemaining
                    ? targetRemaining + " enemies to kill left"
                    : string.Empty;

                UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
        }
        
        void OnAllWavesComplete(AllWavesCompleteEvent evt)
        {
            if (IsCompleted)
                return;
                
            // All waves complete - now complete the objective
            CompleteObjective(string.Empty, GetUpdatedCounterAmount(), "All waves cleared!");
        }

        string GetUpdatedCounterAmount()
        {
            if (UseWaveSystem && m_WaveManager != null)
            {
                return $"Wave {m_WaveManager.CurrentWave + 1} - Kills: {m_KillTotal}";
            }
            return m_KillTotal + " / " + KillsToCompleteObjective;
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
            EventManager.RemoveListener<AllWavesCompleteEvent>(OnAllWavesComplete);
        }
    }
}