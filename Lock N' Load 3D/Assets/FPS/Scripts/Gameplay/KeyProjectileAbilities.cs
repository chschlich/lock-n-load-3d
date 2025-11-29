using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    // lifesteal projectile - heals player on hit (pink key)

    public class LifestealProjectile : MonoBehaviour
    {
        public float HealAmount = 5f;
        public GameObject Player;
        private bool m_HitEnemy = false;
        
        void Start()
        {
            Debug.Log($"LifestealProjectile: Started with HealAmount={HealAmount}, Player={Player?.name ?? "NULL"}");
        }
        
        public void OnEnemyHit()
        {
            m_HitEnemy = true;
            Debug.Log("LifestealProjectile: Enemy hit confirmed!");
        }
        
        void OnDestroy()
        {
            Debug.Log($"LifestealProjectile: OnDestroy - hit enemy = {m_HitEnemy}");
            
            // only heal if we hit an enemy
            if (m_HitEnemy && Player != null && gameObject.scene.isLoaded)
            {
                var health = Player.GetComponent<Health>();
                if (health)
                {
                    float currentHealth = health.CurrentHealth;
                    health.Heal(HealAmount);
                    Debug.Log($"Lifesteal: Healed {HealAmount} HP (was {currentHealth}, now {health.CurrentHealth})");
                }
            }
        }
    } 

    // burn projectile - applies damage over time poison effect (red key)

    public class BurnProjectile : MonoBehaviour
    {
        public float BurnDuration = 3f;
        public float BurnDamagePerSecond = 10f;
        private Damageable m_Target;
        
        public void OnEnemyHit(Damageable target)
        {
            if (target != null)
            {
                m_Target = target;
                // check if target already has burn effect
                var existingBurn = target.GetComponent<BurnEffect>();
                if (existingBurn)
                {
                    // refresh duration
                    existingBurn.RefreshBurn(BurnDuration, BurnDamagePerSecond);
                }
                else
                {
                    // add new burn effect
                    var burnEffect = target.gameObject.AddComponent<BurnEffect>();
                    burnEffect.Initialize(BurnDuration, BurnDamagePerSecond);
                }
                Debug.Log($"Applied burn to {target.name} - {BurnDamagePerSecond} dmg/sec for {BurnDuration}s");
            }
        }
    }

    // burn effect component - attached to enemies to apply dot
    public class BurnEffect : MonoBehaviour
    {
        private float m_Duration;
        private float m_DamagePerSecond;
        private float m_Timer;
        private float m_DamageTimer;
        private Damageable m_Damageable;
        
        public void Initialize(float duration, float damagePerSecond)
        {
            m_Duration = duration;
            m_DamagePerSecond = damagePerSecond;
            m_Timer = 0f;
            m_DamageTimer = 0f;
            m_Damageable = GetComponent<Damageable>();
        }
        
        public void RefreshBurn(float duration, float damagePerSecond)
        {
            m_Duration = duration;
            m_DamagePerSecond = Mathf.Max(m_DamagePerSecond, damagePerSecond);
            m_Timer = 0f;
        }
        
        void Update()
        {
            m_Timer += Time.deltaTime;
            m_DamageTimer += Time.deltaTime;
            
            // apply damage every second
            if (m_DamageTimer >= 1f)
            {
                if (m_Damageable)
                {
                    m_Damageable.InflictDamage(m_DamagePerSecond, false, gameObject);
                }
                m_DamageTimer = 0f;
            }
            
            // remove effect after duration
            if (m_Timer >= m_Duration)
            {
                Destroy(this);
            }
        }
    }
    
    // explosive projectile - aoe damage on impact (purple key)
    public class ExplosiveProjectile : MonoBehaviour
    {
        public float ExplosionRadius = 5f;
        public float ExplosionDamage = 20f;
        public GameObject ExplosionEffectPrefab;
        
        void OnDestroy()
        {
            if (gameObject.scene.isLoaded) // only explode if not scene unload
            {
                Explode();
            }
        }
        
        void Explode()
        {
            // find all damageable objects in radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, ExplosionRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                var damageable = hitCollider.GetComponent<Damageable>();
                if (damageable)
                {
                    damageable.InflictDamage(ExplosionDamage, false, gameObject);
                }
            }
            
            // spawn explosion effect
            if (ExplosionEffectPrefab)
            {
                Instantiate(ExplosionEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Debug.Log($"Explosion at {transform.position} - Radius: {ExplosionRadius}");
        }
    }
    
    // teleport projectile - teleports player to hit location (green key)
    public class TeleportProjectile : MonoBehaviour
    {
        public GameObject Player;
        private bool m_HasTeleported = false;
        private Vector3 m_HitPosition;
        private bool m_HasHitPosition = false;
        
        public void OnHit(Vector3 hitPosition)
        {
            m_HitPosition = hitPosition;
            m_HasHitPosition = true;
            Debug.Log($"TeleportProjectile.OnHit: Stored position {hitPosition}");
        }
        
        void OnDestroy()
        {
            Debug.Log($"TeleportProjectile.OnDestroy: HasTeleported={m_HasTeleported}, Player={Player?.name}, SceneLoaded={gameObject.scene.isLoaded}, HasHitPos={m_HasHitPosition}");
            
            if (!m_HasTeleported && m_HasHitPosition && Player != null && gameObject.scene.isLoaded)
            {
                // teleport player to projectile's hit position
                var controller = Player.GetComponent<PlayerCharacterController>();
                var charController = Player.GetComponent<CharacterController>();
                
                Debug.Log($"TeleportProjectile: Controller={controller?.name}, CharController={charController?.name}");
                
                if (controller != null)
                {
                    // offset position up to avoid clipping through floor
                    Vector3 teleportPos = m_HitPosition + Vector3.up * 1.5f;
                    
                    Debug.Log($"Attempting teleport from {Player.transform.position} to {teleportPos}");
                    
                    // disable character controller temporarily to allow teleport
                    if (charController != null)
                    {
                        charController.enabled = false;
                    }
                    
                    Player.transform.position = teleportPos;
                    
                    // re-enable character controller
                    if (charController != null)
                    {
                        charController.enabled = true;
                    }
                    
                    m_HasTeleported = true;
                    Debug.Log($"Teleported player to {teleportPos}, new position: {Player.transform.position}");
                }
                else
                {
                    Debug.LogError("TeleportProjectile: PlayerCharacterController not found!");
                }
            }
        }
    }
}
