using UnityEngine;
using Unity.FPS.Game;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Unity.FPS.Gameplay
{
    // manages the player's key weapon inventory and switching
    // handles shooting logic for different key types

    public class KeyWeaponController : MonoBehaviour
    {
        [Header("Key Inventory")]
        [Tooltip("Maximum number of keys the player can carry")]
        public int MaxKeys = 4;
        
        [Tooltip("Starting key weapons (keys player starts with)")]
        public List<KeyWeaponData> StartingKeys = new List<KeyWeaponData>();
        
        [Header("References")]
        public Transform WeaponRoot;
        
        [Header("Player Movement")]
        public PlayerCharacterController PlayerController;
        
        [Header("Weapon Bob Animation Settings")]
        [Tooltip("Frequency at which the weapon will move around when the player is moving")]
        public float BobFrequency = 10f;
        
        [Tooltip("How fast the weapon bob is applied, the bigger value the fastest")]
        public float BobSharpness = 10f;
        
        [Tooltip("Distance the weapon bobs when walking")]
        public float WalkBobAmount = 0.05f;
        
        [Tooltip("Distance the weapon bobs when sprinting (multiplier on WalkBobAmount)")]
        public float SprintBobMultiplier = 1.5f;
        
        [Header("Weapon Recoil Settings")]
        [Tooltip("This will affect how fast the recoil moves the weapon, the bigger the value, the fastest")]
        public float RecoilSharpness = 50f;
        
        [Tooltip("Maximum distance the recoil can affect the weapon")]
        public float MaxRecoilDistance = 0.5f;
        
        [Tooltip("How fast the weapon goes back to its original position after the recoil is finished")]
        public float RecoilRestitutionSharpness = 10f;
        
        [Header("Weapon Aiming Settings")]
        [Tooltip("Speed at which the aiming animation is played")]
        public float AimingAnimationSpeed = 10f;
        
        [Tooltip("Position offset when aiming down sights")]
        public Vector3 AimOffset = new Vector3(0f, 0f, 0.1f);
        
        [Tooltip("FOV ratio when aiming (0.5 = half FOV)")]
        [Range(0f, 1f)]
        public float AimZoomRatio = 0.8f;
        
        // active keys
        private List<KeyWeaponData> m_KeyInventory = new List<KeyWeaponData>();
        private int m_CurrentKeyIndex = 0;
        private KeyWeaponData m_CurrentKey;
        private GameObject m_CurrentWeaponModel;
        private WeaponController m_CurrentWeaponController;
        private KeyWeaponShooterAnimation m_CurrentShooterAnimation;
        private KeyWeaponAnimationController m_CurrentAnimationController;
        
        // shooting state
        private HashSet<GameObject> m_ProcessedProjectiles = new HashSet<GameObject>();
        
        // input actions
    private InputAction m_FireAction;
    private InputAction m_Key1Action;
    private InputAction m_Key2Action;
    private InputAction m_Key3Action;
    private InputAction m_Key4Action;
    private InputAction m_Key5Action;
    private InputAction m_Key6Action;
    private InputAction m_NextWeaponAction;
    
    public KeyWeaponData CurrentKey => m_CurrentKey;
        public int CurrentKeyIndex => m_CurrentKeyIndex;
        public int KeyCount => m_KeyInventory.Count;
        public WeaponController CurrentWeaponController => m_CurrentWeaponController;
        public KeyWeaponData GetKeyAt(int index)
        {
            if (index >= 0 && index < m_KeyInventory.Count)
                return m_KeyInventory[index];
            return null;
        }
        
        void Start()
        {
            Debug.Log("KeyWeaponController: Starting initialization...");
            
            // set up input actions
            m_FireAction = InputSystem.actions.FindAction("Player/Fire");
            m_NextWeaponAction = InputSystem.actions.FindAction("Player/NextWeapon");
            
            if (m_FireAction == null)
                Debug.LogWarning("KeyWeaponController: Fire action not found!");
            if (m_NextWeaponAction == null)
                Debug.LogWarning("KeyWeaponController: NextWeapon action not found!");
            
            // create custom actions for keys 1-4
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                m_Key1Action = new InputAction(binding: "<Keyboard>/1");
                m_Key2Action = new InputAction(binding: "<Keyboard>/2");
                m_Key1Action = new InputAction(binding: "<Keyboard>/1");
                m_Key2Action = new InputAction(binding: "<Keyboard>/2");
                m_Key3Action = new InputAction(binding: "<Keyboard>/3");
                m_Key4Action = new InputAction(binding: "<Keyboard>/4");
                m_Key5Action = new InputAction(binding: "<Keyboard>/5");
                m_Key6Action = new InputAction(binding: "<Keyboard>/6");
                
                m_Key1Action.Enable();
                m_Key2Action.Enable();
                m_Key3Action.Enable();
                m_Key4Action.Enable();
                m_Key5Action.Enable();
                m_Key6Action.Enable();
                m_Key5Action.Enable();
                m_Key6Action.Enable();
                Debug.Log("KeyWeaponController: Keyboard actions enabled");
            }
            else
            {
                Debug.LogWarning("KeyWeaponController: No keyboard detected!");
            }
            
            if (m_FireAction != null) m_FireAction.Enable();
            if (m_NextWeaponAction != null) m_NextWeaponAction.Enable();
            
            // add starting keys
            Debug.Log($"KeyWeaponController: Adding {StartingKeys.Count} starting keys...");
            foreach (var key in StartingKeys)
            {
                if (key != null)
                {
                    AddKey(key);
                }
                else
                {
                    Debug.LogWarning("KeyWeaponController: Null key in StartingKeys!");
                }
            }
            
            // equip first key
            if (m_KeyInventory.Count > 0)
            {
                EquipKey(0);
                Debug.Log($"KeyWeaponController: Equipped first key - {m_CurrentKey?.KeyName ?? "NULL"}");
            }
            else
            {
                Debug.LogError("KeyWeaponController: No keys in inventory after adding starting keys!");
            }
        }
        
        void Update()
        {
            HandleKeySwitch();
            HandleShooting();
            ProcessNewProjectiles();
        }
        
        void ProcessNewProjectiles()
        {
            if (m_CurrentKey == null) return;
            
            // find all projectiles in the scene that belong to the player
            var projectiles = FindObjectsByType<ProjectileBase>(FindObjectsSortMode.None);
            foreach (var projectile in projectiles)
            {
                // check if this projectile belongs to the player and hasn't been processed yet
                if (projectile.Owner == PlayerController.gameObject && !m_ProcessedProjectiles.Contains(projectile.gameObject))
                {
                    // apply special ability to this projectile
                    ApplyProjectileAbility(projectile.gameObject);
                    m_ProcessedProjectiles.Add(projectile.gameObject);
                }
            }
            
            // clean up destroyed projectiles from the set
            m_ProcessedProjectiles.RemoveWhere(obj => obj == null);
        }
        
        void HandleKeySwitch()
        {
            // number keys 1-6 for weapon switching
            if (m_Key1Action != null && m_Key1Action.triggered && m_KeyInventory.Count > 0)
                EquipKey(0);
            if (m_Key2Action != null && m_Key2Action.triggered && m_KeyInventory.Count > 1)
                EquipKey(1);
            if (m_Key3Action != null && m_Key3Action.triggered && m_KeyInventory.Count > 2)
                EquipKey(2);
            if (m_Key4Action != null && m_Key4Action.triggered && m_KeyInventory.Count > 3)
                EquipKey(3);
            if (m_Key5Action != null && m_Key5Action.triggered && m_KeyInventory.Count > 4)
                EquipKey(4);
            if (m_Key6Action != null && m_Key6Action.triggered && m_KeyInventory.Count > 5)
                EquipKey(5);
            if (m_Key5Action != null && m_Key5Action.triggered && m_KeyInventory.Count > 4)
                EquipKey(4);
            if (m_Key6Action != null && m_Key6Action.triggered && m_KeyInventory.Count > 5)
                EquipKey(5);
            if (m_Key5Action != null && m_Key5Action.triggered && m_KeyInventory.Count > 4)
                EquipKey(4);
            if (m_Key6Action != null && m_Key6Action.triggered && m_KeyInventory.Count > 5)
                EquipKey(5);
            
            // mouse wheel switching using NextWeapon action
            if (m_NextWeaponAction != null)
            {
                float scroll = m_NextWeaponAction.ReadValue<float>();
                if (scroll > 0f)
                {
                    int nextIndex = (m_CurrentKeyIndex + 1) % m_KeyInventory.Count;
                    EquipKey(nextIndex);
                }
                else if (scroll < 0f)
                {
                    int nextIndex = (m_CurrentKeyIndex - 1 + m_KeyInventory.Count) % m_KeyInventory.Count;
                    EquipKey(nextIndex);
                }
            }
        }
        
        void HandleShooting()
        {
            if (m_CurrentKey == null || m_CurrentWeaponController == null)
            {
                return;
            }
            
            bool fireDown = m_FireAction != null && m_FireAction.WasPressedThisFrame();
            bool fireHeld = m_FireAction != null && m_FireAction.IsPressed();
            bool fireUp = m_FireAction != null && m_FireAction.WasReleasedThisFrame();
            
            // Let the WeaponController handle shooting logic
            // Check return value to know if we actually fired (same pattern as PlayerWeaponsManager)
            bool hasFired = m_CurrentWeaponController.HandleShootInputs(fireDown, fireHeld, fireUp);
            
            // Handle recoil accumulation directly (like PlayerWeaponsManager does)
            if (hasFired && m_CurrentShooterAnimation != null)
            {
                m_CurrentShooterAnimation.AccumulateRecoil(m_CurrentWeaponController.RecoilForce);
            }
        }
        
        void ApplyProjectileAbility(GameObject projectile)
        {
            Debug.Log($"ApplyProjectileAbility: Applying {m_CurrentKey.SpecialAbility} to {projectile.name}");
            
            // set projectile damage and speed from key data
            var projectileStandard = projectile.GetComponent<ProjectileStandard>();
            if (projectileStandard != null)
            {
                projectileStandard.Damage = m_CurrentKey.Damage;
                projectileStandard.Speed = m_CurrentKey.ProjectileSpeed;
                Debug.Log($"Set projectile damage={m_CurrentKey.Damage}, speed={m_CurrentKey.ProjectileSpeed}");
            }
            
            switch (m_CurrentKey.SpecialAbility)
            {
                case KeyAbilityType.Lifesteal:
                    var lifestealComp = projectile.AddComponent<LifestealProjectile>();
                    lifestealComp.HealAmount = m_CurrentKey.AbilityPower;
                    lifestealComp.Player = PlayerController.gameObject;
                    Debug.Log($"Added Lifesteal component with {m_CurrentKey.AbilityPower} heal");
                    break;
                    
                case KeyAbilityType.Burn:
                    var burnComp = projectile.AddComponent<BurnProjectile>();
                    burnComp.BurnDuration = m_CurrentKey.AbilityPower;
                    burnComp.BurnDamagePerSecond = m_CurrentKey.Damage * 0.3f;
                    Debug.Log($"Added Burn component - {burnComp.BurnDamagePerSecond} dmg/sec for {burnComp.BurnDuration}s");
                    break;
                    
                case KeyAbilityType.Explosive:
                    var explosiveComp = projectile.AddComponent<ExplosiveProjectile>();
                    explosiveComp.ExplosionRadius = m_CurrentKey.AbilityPower;
                    explosiveComp.ExplosionDamage = m_CurrentKey.Damage * 0.5f;
                    break;
                    
                case KeyAbilityType.Teleport:
                    var teleportComp = projectile.AddComponent<TeleportProjectile>();
                    teleportComp.Player = PlayerController.gameObject;
                    Debug.Log($"Added Teleport component");
                    break;
            }
        }
        
        public void EquipKey(int index)
        {
            if (index < 0 || index >= m_KeyInventory.Count) return;
            
            m_CurrentKeyIndex = index;
            m_CurrentKey = m_KeyInventory[index];
            
            // destroy old weapon model
            if (m_CurrentWeaponModel != null)
            {
                Debug.Log($"Destroying old weapon model: {m_CurrentWeaponModel.name}");
                Destroy(m_CurrentWeaponModel);
                m_CurrentWeaponModel = null;
                m_CurrentWeaponController = null;
            }
            
            // instantiate new weapon model
            Debug.Log($"EquipKey: WeaponModelPrefab={(m_CurrentKey.WeaponModelPrefab != null ? m_CurrentKey.WeaponModelPrefab.name : "NULL")}, WeaponRoot={(WeaponRoot != null ? WeaponRoot.name : "NULL")}");
            
            if (m_CurrentKey.WeaponModelPrefab != null && WeaponRoot != null)
            {
                Debug.Log($"Instantiating weapon model prefab: {m_CurrentKey.WeaponModelPrefab.name}");
                m_CurrentWeaponModel = Instantiate(m_CurrentKey.WeaponModelPrefab, WeaponRoot);
                
                // Position weapon in front of camera
                m_CurrentWeaponModel.transform.localPosition = new Vector3(0.2f, -0.15f, 0.35f);
                m_CurrentWeaponModel.transform.localRotation = Quaternion.identity;
                m_CurrentWeaponModel.transform.localScale = Vector3.one;
                m_CurrentWeaponModel.layer = 10; // set root object to weapon layer
                m_CurrentWeaponModel.SetActive(true);
                
                Debug.Log($"Weapon positioned at local pos {m_CurrentWeaponModel.transform.localPosition}");
                
                // Add and configure weapon animation controller
                var animationController = m_CurrentWeaponModel.GetComponent<KeyWeaponAnimationController>();
                if (animationController == null)
                {
                    animationController = m_CurrentWeaponModel.AddComponent<KeyWeaponAnimationController>();
                }
                animationController.WeaponTransform = m_CurrentWeaponModel.transform;
                animationController.PlayerController = PlayerController;
                animationController.BaseWeaponPosition = new Vector3(0.2f, -0.15f, 0.35f);
                
                // Copy bob animation settings from KeyWeaponController
                animationController.BobFrequency = BobFrequency;
                animationController.BobSharpness = BobSharpness;
                animationController.WalkBobAmount = WalkBobAmount;
                animationController.SprintBobMultiplier = SprintBobMultiplier;
                
                // Add and configure shooter animation controller (recoil + aiming)
                var shooterAnimation = m_CurrentWeaponModel.GetComponent<KeyWeaponShooterAnimation>();
                if (shooterAnimation == null)
                {
                    shooterAnimation = m_CurrentWeaponModel.AddComponent<KeyWeaponShooterAnimation>();
                }
                
                // Copy recoil settings from KeyWeaponController
                shooterAnimation.RecoilSharpness = RecoilSharpness;
                shooterAnimation.MaxRecoilDistance = MaxRecoilDistance;
                shooterAnimation.RecoilRestitutionSharpness = RecoilRestitutionSharpness;
                
                // Copy aiming settings from KeyWeaponController
                shooterAnimation.AimingAnimationSpeed = AimingAnimationSpeed;
                shooterAnimation.AimOffset = AimOffset;
                shooterAnimation.AimZoomRatio = AimZoomRatio;
                
                // Link shooter animation to animation controller
                animationController.ShooterAnimation = shooterAnimation;
                
                // Store references for animation and recoil
                m_CurrentShooterAnimation = shooterAnimation;
                m_CurrentAnimationController = animationController;
                
                // configure the weapon controller with key-specific stats
                var weaponController = m_CurrentWeaponModel.GetComponent<WeaponController>();
                if (weaponController != null)
                {
                    Debug.Log($"Configuring WeaponController for {m_CurrentKey.KeyName}");
                    
                    // store reference to current weapon controller
                    m_CurrentWeaponController = weaponController;
                    
                    // set weapon stats from key data
                    weaponController.WeaponName = m_CurrentKey.KeyName;
                    weaponController.DelayBetweenShots = m_CurrentKey.FireRate;
                    weaponController.BulletsPerShot = m_CurrentKey.ProjectilesPerShot;
                    weaponController.BulletSpreadAngle = m_CurrentKey.SpreadAngle;
                    weaponController.MuzzleFlashScale = m_CurrentKey.MuzzleFlashScale;
                    // ProjectileSpeed is set per-prefab in Inspector, not at runtime
                    Debug.Log($"Set MuzzleFlashScale to {weaponController.MuzzleFlashScale} for {m_CurrentKey.KeyName}");
                    
                    // get the ProjectileBase component from the projectile GameObject
                    if (m_CurrentKey.ProjectilePrefab != null)
                    {
                        var projectileBase = m_CurrentKey.ProjectilePrefab.GetComponent<ProjectileBase>();
                        if (projectileBase != null)
                        {
                            weaponController.ProjectilePrefab = projectileBase;
                        }
                        else
                        {
                            Debug.LogError($"No ProjectileBase component found on {m_CurrentKey.ProjectilePrefab.name}");
                        }
                    }
                    
                    // set owner to player
                    weaponController.Owner = PlayerController.gameObject;
                    
                    // configure crosshair with key color
                    var defaultCrosshair = weaponController.CrosshairDataDefault;
                    defaultCrosshair.CrosshairSprite = m_CurrentKey.CrosshairSprite;
                    defaultCrosshair.CrosshairSize = m_CurrentKey.CrosshairSize;
                    defaultCrosshair.CrosshairColor = new Color(
                        m_CurrentKey.CrosshairColor.r,
                        m_CurrentKey.CrosshairColor.g,
                        m_CurrentKey.CrosshairColor.b,
                        0.38f
                    );
                    weaponController.CrosshairDataDefault = defaultCrosshair;
                    
                    // keep automatic shooting for rapid fire
                    weaponController.ShootType = WeaponShootType.Automatic;
                    
                    // set up animator for shooting animations
                    var animator = m_CurrentWeaponModel.GetComponent<Animator>();
                    if (animator == null)
                    {
                        // Try to find animator in children (some weapon models might have it nested)
                        animator = m_CurrentWeaponModel.GetComponentInChildren<Animator>();
                    }
                    
                    if (animator == null)
                    {
                        // Add animator component if it doesn't exist
                        animator = m_CurrentWeaponModel.AddComponent<Animator>();
                        Debug.Log($"Added Animator component to {m_CurrentKey.KeyName} weapon model");
                    }
                    
                    // Assign animator to weapon controller (this enables shooting animations)
                    weaponController.WeaponAnimator = animator;
                }
                else
                {
                    Debug.LogWarning("No WeaponController found on weapon model!");
                }
                
                // make sure all renderers are on the correct layer
                var renderers = m_CurrentWeaponModel.GetComponentsInChildren<Renderer>(true);
                Debug.Log($"Found {renderers.Length} renderers in weapon model");
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                    renderer.gameObject.layer = 10; // weapon layer for WeaponCamera
                }
                
                // Find and configure MuzzleExplosionEffect - the prefab reference doesn't work after instantiation
                // so we need to find the child by looking for particle systems
                if (weaponController != null)
                {
                    // Look for any child with "MuzzleExplosion" or "Muzzle" in the name that has a ParticleSystem
                    Transform[] allChildren = m_CurrentWeaponModel.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in allChildren)
                    {
                        if (child.name.Contains("MuzzleExplosion") || child.name.Contains("VFX_Muzzle"))
                        {
                            ParticleSystem ps = child.GetComponent<ParticleSystem>();
                            if (ps != null)
                            {
                                // Found the muzzle explosion effect - assign it
                                weaponController.MuzzleExplosionEffect = child.gameObject;
                                
                                // Set correct layer for weapon camera
                                child.gameObject.layer = 10;
                                
                                // Make sure it's active
                                child.gameObject.SetActive(true);
                                
                                break;
                            }
                        }
                    }
                }
                
                Debug.Log($"Equipped weapon model for {m_CurrentKey.KeyName}: {m_CurrentWeaponModel.name} at world pos {m_CurrentWeaponModel.transform.position}, local pos {m_CurrentWeaponModel.transform.localPosition}, active: {m_CurrentWeaponModel.activeSelf}");
            }
            else
            {
                Debug.LogWarning($"Cannot equip weapon model - Prefab: {m_CurrentKey.WeaponModelPrefab != null}, Root: {WeaponRoot != null}");
            }
            
            // update player movement speed based on key
            if (PlayerController)
            {
                // you'll need to expose this in playercharactercontroller
                // PlayerController.MovementSpeedModifier = m_CurrentKey.MovementSpeedMultiplier;
            }
            
            Debug.Log($"Equipped {m_CurrentKey.KeyName}");
        }
        
        public bool AddKey(KeyWeaponData keyData)
        {
            if (m_KeyInventory.Count >= MaxKeys)
            {
                Debug.LogWarning("Key inventory full!");
                return false;
            }
            
            if (m_KeyInventory.Contains(keyData))
            {
                Debug.LogWarning("Already have this key!");
                return false;
            }
            
            m_KeyInventory.Add(keyData);
            Debug.Log($"Added {keyData.KeyName} to inventory");
            return true;
        }
        
        public float GetCurrentMovementSpeedMultiplier()
        {
            if (m_CurrentKey == null) return 1f;
            
            // check if currently shooting using new input system
            bool isShooting = m_FireAction != null && m_FireAction.IsPressed();
            if (isShooting)
                return m_CurrentKey.MovementSpeedMultiplier * m_CurrentKey.MovementSpeedWhileShooting;
            
            return m_CurrentKey.MovementSpeedMultiplier;
        }
    }
}
