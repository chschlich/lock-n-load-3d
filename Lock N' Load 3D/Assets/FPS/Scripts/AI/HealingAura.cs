using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Provides a healing aura that heals nearby allies over time.
    /// Typically used on medic-type enemies like pink locklets.
    /// </summary>
    public class HealingAura : MonoBehaviour
    {
        [Header("Healing Settings")]
        [Tooltip("Radius of the healing aura")]
        public float HealRadius = 10f;
        
        [Tooltip("Health restored per second to allies in range")]
        public float HealPerSecond = 20f;
        
        [Tooltip("Time between heal ticks")]
        public float HealInterval = 0.5f;
        
        [Header("Visual Effects")]
        [Tooltip("Optional particle effect to show healing aura")]
        public GameObject AuraEffect;
        
        [Tooltip("Color of the debug sphere showing heal radius")]
        public Color GizmoColor = new Color(1f, 0.4f, 0.8f, 0.3f);
        
        private float m_TimeSinceLastHeal = 0f;
        private Actor m_Actor;
        private List<Collider> m_AllyColliders = new List<Collider>();
        
        void Start()
        {
            m_Actor = GetComponent<Actor>();
            
            // Spawn aura effect if provided
            if (AuraEffect != null)
            {
                GameObject effect = Instantiate(AuraEffect, transform.position, Quaternion.identity, transform);
                effect.transform.localScale = Vector3.one * (HealRadius / 5f); // Scale to match radius
            }
        }
        
        void Update()
        {
            m_TimeSinceLastHeal += Time.deltaTime;
            
            if (m_TimeSinceLastHeal >= HealInterval)
            {
                HealNearbyAllies();
                m_TimeSinceLastHeal = 0f;
            }
        }
        
        void HealNearbyAllies()
        {
            // Find all colliders in radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, HealRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                // Skip self
                if (hitCollider.gameObject == gameObject)
                    continue;
                
                // Only heal enemies (affiliation 1), never heal player (affiliation 0)
                Actor otherActor = hitCollider.GetComponent<Actor>();
                
                // Debug logging to see what we're trying to heal
                if (otherActor != null)
                {
                    Debug.Log($"Found actor: {hitCollider.gameObject.name}, Affiliation: {otherActor.Affiliation}");
                }
                
                if (otherActor != null && otherActor.Affiliation == 1)
                {
                    // Heal the ally
                    Health health = hitCollider.GetComponent<Health>();
                    if (health != null && health.CurrentHealth < health.MaxHealth)
                    {
                        float healAmount = HealPerSecond * HealInterval;
                        health.Heal(healAmount);
                        Debug.Log($"HEALED: {hitCollider.gameObject.name} for {healAmount} HP");
                    }
                }
            }
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw healing radius
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(transform.position, HealRadius);
            
            // Draw filled sphere for better visibility
            Color fillColor = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, GizmoColor.a * 0.3f);
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(transform.position, HealRadius);
        }
    }
}
