using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    public class EnemyManager : MonoBehaviour
    {
        public List<EnemyController> Enemies { get; private set; }
        public List<GameObject> AllEnemies { get; private set; }
        public int NumberOfEnemiesTotal { get; private set; }
        public int NumberOfEnemiesRemaining => AllEnemies.Count;

        void Awake()
        {
            Enemies = new List<EnemyController>();
            AllEnemies = new List<GameObject>();
        }

        public void RegisterEnemy(EnemyController enemy)
        {
            Enemies.Add(enemy);
            AllEnemies.Add(enemy.gameObject);

            NumberOfEnemiesTotal++;
        }

        public void RegisterEnemy(GameObject enemy)
        {
            if (!AllEnemies.Contains(enemy))
            {
                AllEnemies.Add(enemy);
                NumberOfEnemiesTotal++;
            }
        }

        public void UnregisterEnemy(EnemyController enemyKilled)
        {
            int enemiesRemainingNotification = NumberOfEnemiesRemaining - 1;

            EnemyKillEvent evt = Events.EnemyKillEvent;
            evt.Enemy = enemyKilled.gameObject;
            evt.RemainingEnemyCount = enemiesRemainingNotification;
            EventManager.Broadcast(evt);

            // removes the enemy from the list, so that we can keep track of how many are left on the map
            Enemies.Remove(enemyKilled);
            AllEnemies.Remove(enemyKilled.gameObject);
        }

        public void UnregisterEnemy(GameObject enemyKilled)
        {
            int enemiesRemainingNotification = NumberOfEnemiesRemaining - 1;

            EnemyKillEvent evt = Events.EnemyKillEvent;
            evt.Enemy = enemyKilled;
            evt.RemainingEnemyCount = enemiesRemainingNotification;
            EventManager.Broadcast(evt);

            // removes the enemy from the list, so that we can keep track of how many are left on the map
            AllEnemies.Remove(enemyKilled);
        }
    }
}