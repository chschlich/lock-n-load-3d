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
        }
        
        public void OnEnemyHit()
        {
            m_HitEnemy = true;
        }
        
        void OnDestroy()
        {
            // only heal if we hit an enemy
            if (m_HitEnemy && Player != null && gameObject.scene.isLoaded)
            {
                var health = Player.GetComponent<Health>();
                if (health)
                {
                    health.Heal(HealAmount);
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
        
        [Header("Screen Shake")]
        [Tooltip("Maximum distance from explosion that can cause screen shake")]
        public float MaxShakeDistance = 10f;
        
        [Tooltip("Base shake intensity at explosion center")]
        public float ShakeIntensity = 0.3f;
        
        [Tooltip("Duration of the shake effect in seconds")]
        public float ShakeDuration = 0.2f;
        
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
            
            // Apply screenshake to the player who fired this projectile
            ApplyScreenShake();
        }
        
        void ApplyScreenShake()
        {
            // Get the projectile owner (player who fired it)
            var projectileBase = GetComponent<ProjectileBase>();
            if (projectileBase == null || projectileBase.Owner == null) return;
            
            // Get the player's camera
            var playerController = projectileBase.Owner.GetComponent<PlayerCharacterController>();
            if (playerController == null || playerController.PlayerCamera == null) return;
            
            // Calculate distance from explosion to player
            float distance = Vector3.Distance(transform.position, projectileBase.Owner.transform.position);
            
            // Only shake if within range
            if (distance > MaxShakeDistance) return;
            
            // Simple falloff: at distance 0 = full intensity, at MaxShakeDistance = 0 intensity
            float falloff = 1f - (distance / MaxShakeDistance);
            float scaledIntensity = ShakeIntensity * falloff * falloff; // Squared falloff for more dramatic drop
            
            // Apply the shake
            CameraShake.ApplyShake(playerController.PlayerCamera, scaledIntensity, ShakeDuration);
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
