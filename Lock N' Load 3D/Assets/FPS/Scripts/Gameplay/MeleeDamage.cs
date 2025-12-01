using UnityEngine;
using Unity.FPS.Game;

public class MeleeDamage : MonoBehaviour
{
    [Tooltip("Damage dealt to the player per second while in contact")]
    public float DamagePerSecond = 10f;

    [Tooltip("Time between damage ticks")]
    public float DamageInterval = 0.5f;

    private float m_LastDamageTime;
    private ActorsManager m_ActorsManager;

    private void Start()
    {
        m_ActorsManager = FindAnyObjectByType<ActorsManager>();
    }

    private void OnTriggerStay(Collider other)
    {
        // check if we hit the player
        if (m_ActorsManager != null && m_ActorsManager.Player != null && Time.time >= m_LastDamageTime + DamageInterval)
        {
            // check if the collider belongs to the player
            if (other.gameObject == m_ActorsManager.Player || other.transform.IsChildOf(m_ActorsManager.Player.transform))
            {
                // try to get the player's health component
                Health playerHealth = m_ActorsManager.Player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // deal damage
                    playerHealth.TakeDamage(DamagePerSecond * DamageInterval, gameObject);
                    m_LastDamageTime = Time.time;
                }
            }
        }
    }
}
