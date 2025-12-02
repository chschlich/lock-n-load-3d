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
    
    // burn + explosive projectile - AOE damage + burn effect (red key)
    public class BurnExplosiveProjectile : MonoBehaviour
    {
        public float ExplosionRadius = 8f;      // Bigger than purple key (5f)
        public float ExplosionDamage = 30f;
        public float BurnDuration = 3f;
        public float BurnDamagePerSecond = 10f;
        public GameObject ExplosionEffectPrefab;
        
        [Header("Screen Shake")]
        public float MaxShakeDistance = 12f;
        public float ShakeIntensity = 0.5f;
        public float ShakeDuration = 0.25f;
        
        void OnDestroy()
        {
            if (gameObject.scene.isLoaded)
            {
                Explode();
            }
        }
        
        void Explode()
        {
            // Find all damageable objects in radius (bigger than purple)
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, ExplosionRadius);
            
            foreach (var hitCollider in hitColliders)
            {
                var damageable = hitCollider.GetComponent<Damageable>();
                if (damageable)
                {
                    // Deal explosion damage
                    damageable.InflictDamage(ExplosionDamage, false, gameObject);
                    
                    // Apply burn effect to all enemies hit
                    var existingBurn = damageable.GetComponent<BurnEffect>();
                    if (existingBurn)
                    {
                        existingBurn.RefreshBurn(BurnDuration, BurnDamagePerSecond);
                    }
                    else
                    {
                        var burnEffect = damageable.gameObject.AddComponent<BurnEffect>();
                        burnEffect.Initialize(BurnDuration, BurnDamagePerSecond);
                    }
                }
            }
            
            // Spawn explosion effect
            if (ExplosionEffectPrefab)
            {
                Instantiate(ExplosionEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Apply screenshake
            ApplyScreenShake();
        }
        
        void ApplyScreenShake()
        {
            var projectileBase = GetComponent<ProjectileBase>();
            if (projectileBase == null || projectileBase.Owner == null) return;
            
            var playerController = projectileBase.Owner.GetComponent<PlayerCharacterController>();
            if (playerController == null || playerController.PlayerCamera == null) return;
            
            float distance = Vector3.Distance(transform.position, projectileBase.Owner.transform.position);
            
            if (distance > MaxShakeDistance) return;
            
            float falloff = 1f - (distance / MaxShakeDistance);
            float scaledIntensity = ShakeIntensity * falloff * falloff;
            
            CameraShake.ApplyShake(playerController.PlayerCamera, scaledIntensity, ShakeDuration);
        }
    }
    
    // teleport projectile - teleports player to hit location (green key)
    public class TeleportProjectile : MonoBehaviour
    {
        public GameObject Player;
        public float WarpFOVAmount = 20f;
        public float WarpDuration = 0.3f;
        
        private bool m_HasTeleported = false;
        private Vector3 m_HitPosition;
        private bool m_HasHitPosition = false;
        
        [Tooltip("Maximum search radius to find a valid teleport position")]
        public float MaxSearchRadius = 5f;
        
        [Tooltip("Step size for spiral search pattern")]
        public float SearchStep = 0.5f;
        
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
                
                if (controller != null && charController != null)
                {
                    // Find a safe teleport position
                    Vector3 safePosition;
                    if (FindSafeTeleportPosition(m_HitPosition, charController, out safePosition))
                    {
                        Debug.Log($"Attempting teleport from {Player.transform.position} to {safePosition}");
                        
                        // disable character controller temporarily to allow teleport
                        charController.enabled = false;
                        
                        Player.transform.position = safePosition;
                        
                        // re-enable character controller
                        charController.enabled = true;
                        
                        m_HasTeleported = true;
                        Debug.Log($"Teleported player to {safePosition}, new position: {Player.transform.position}");
                        
                        // apply fov warp effect
                        if (controller.PlayerCamera != null)
                        {
                            controller.StartCoroutine(ApplyWarpEffect(controller.PlayerCamera));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"TeleportProjectile: Could not find safe teleport position near {m_HitPosition}");
                    }
                }
                else
                {
                    Debug.LogError("TeleportProjectile: PlayerCharacterController or CharacterController not found!");
                }
            }
        }
        
        System.Collections.IEnumerator ApplyWarpEffect(Camera camera)
        {
            float originalFOV = camera.fieldOfView;
            float targetFOV = originalFOV + WarpFOVAmount;
            float elapsed = 0f;
            
            // warp out (increase fov)
            while (elapsed < WarpDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (WarpDuration * 0.5f);
                camera.fieldOfView = Mathf.Lerp(originalFOV, targetFOV, t);
                yield return null;
            }
            
            // warp back (return to normal fov)
            elapsed = 0f;
            while (elapsed < WarpDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (WarpDuration * 0.5f);
                camera.fieldOfView = Mathf.Lerp(targetFOV, originalFOV, t);
                yield return null;
            }
            
            // ensure we end at exactly the original fov
            camera.fieldOfView = originalFOV;
        }
        
        /// <summary>
        /// Finds a safe position to teleport the player, avoiding walls and obstacles
        /// </summary>
        bool FindSafeTeleportPosition(Vector3 targetPosition, CharacterController charController, out Vector3 safePosition)
        {
            // Get CharacterController dimensions
            float radius = charController.radius;
            float height = charController.height;
            float skinWidth = charController.skinWidth;
            
            // Offset position up to place player's feet at hit point
            Vector3 basePosition = targetPosition + Vector3.up * (height * 0.5f);
            
            // Check if the initial position is valid
            if (IsPositionValid(basePosition, radius, height, skinWidth))
            {
                safePosition = basePosition;
                return true;
            }
            
            // If initial position is invalid, search in a spiral pattern
            float currentRadius = SearchStep;
            int maxIterations = Mathf.CeilToInt(MaxSearchRadius / SearchStep);
            
            for (int i = 0; i < maxIterations; i++)
            {
                // Try 8 directions at this radius
                for (int angle = 0; angle < 360; angle += 45)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * currentRadius;
                    Vector3 testPosition = basePosition + offset;
                    
                    if (IsPositionValid(testPosition, radius, height, skinWidth))
                    {
                        safePosition = testPosition;
                        Debug.Log($"Found safe position {currentRadius:F1}m away from target (angle: {angle}°)");
                        return true;
                    }
                }
                
                currentRadius += SearchStep;
            }
            
            // No valid position found
            safePosition = basePosition;
            return false;
        }
        
        /// <summary>
        /// Checks if a position is valid for the player (no collisions with walls/obstacles)
        /// </summary>
        bool IsPositionValid(Vector3 position, float radius, float height, float skinWidth)
        {
            // Calculate capsule points (top and bottom sphere centers)
            Vector3 point1 = position + Vector3.up * radius; // Bottom
            Vector3 point2 = position + Vector3.up * (height - radius); // Top
            
            // Add a small buffer to the radius check
            float checkRadius = radius - skinWidth;
            
            // Check if the capsule would overlap with any colliders
            // Use the same layers that the CharacterController would collide with
            int layerMask = ~0; // Check all layers
            
            // Ignore triggers
            bool hitAnything = Physics.CheckCapsule(point1, point2, checkRadius, layerMask, QueryTriggerInteraction.Ignore);
            
            return !hitAnything;
        }
    }
}
