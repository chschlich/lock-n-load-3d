using UnityEngine;

namespace Unity.FPS.Gameplay
{

    // scriptableobject that defines a unique "key" weapon type
    // each key has different projectile behavior, fire rate, and special abilities
    [CreateAssetMenu(fileName = "New Key Weapon", menuName = "FPS/Key Weapon Data")]
    public class KeyWeaponData : ScriptableObject
    {
        [Header("Key Identity")]
        public string KeyName = "Yellow Key";
        public Color KeyColor = Color.yellow;
        public Sprite KeyIcon;           // Normal key sprite (enhanced version)
        public Sprite KeyIconGlow;       // Glow key sprite (from effects folder)
        
        [Header("Weapon Stats")]
        [Tooltip("Base damage per shot")]
        public float Damage = 40f;
        
        [Tooltip("Time between shots in seconds")]
        public float FireRate = 0.5f;
        
        [Tooltip("Projectile speed")]
        public float ProjectileSpeed = 30f;
        
        [Tooltip("Movement speed multiplier while holding this key")]
        [Range(0.5f, 2f)]
        public float MovementSpeedMultiplier = 1f;
        
        [Tooltip("Movement speed multiplier while shooting")]
        [Range(0.5f, 1f)]
        public float MovementSpeedWhileShooting = 0.7f;
        
        [Header("Projectile Settings")]
        public GameObject ProjectilePrefab;
        
        [Tooltip("Number of projectiles per shot")]
        public int ProjectilesPerShot = 1;
        
        [Tooltip("Spread angle for multi-projectile shots")]
        public float SpreadAngle = 0f;
        
        [Header("Special Abilities")]
        public KeyAbilityType SpecialAbility = KeyAbilityType.None;
        
        [Tooltip("Ability-specific parameters")]
        public float AbilityPower = 1f;
        
        [Header("Weapon Model")]
        [Tooltip("3D model prefab for this weapon (leave null for no model)")]
        public GameObject WeaponModelPrefab;
        
        [Header("Visual Effects")]
        [Tooltip("Scale multiplier for the muzzle flash (1 = normal size, 0.5 = half size, etc.)")]
        [Range(0.1f, 2f)]
        public float MuzzleFlashScale = 1f;
        
        [Tooltip("Weapon icon shown above the ammo/overheat bar")]
        public Sprite WeaponUIIcon;
        
        [Header("Crosshair")]
        [Tooltip("Crosshair sprite (leave null to use default)")]
        public Sprite CrosshairSprite;
        
        [Tooltip("Crosshair size in pixels")]
        public int CrosshairSize = 55;
        
        [Tooltip("Crosshair color (uses KeyColor if not set)")]
        public Color CrosshairColor = Color.white;
        
        [Header("Projectile Audio")]
        [Tooltip("Looping sound while projectile is in flight")]
        public AudioClip ProjectileTravelSound;
        
        [Tooltip("Sound on impact (overrides prefab default if set)")]
        public AudioClip ProjectileImpactSound;
        
        [Tooltip("Volume for travel sound")]
        [Range(0f, 1f)]
        public float TravelSoundVolume = 0.5f;
        
        [Tooltip("Volume for impact sound")]
        [Range(0f, 1f)]
        public float ImpactSoundVolume = 1f;
        
        [Header("Screenshake (Explosive Only)")]
        [Tooltip("Maximum distance from explosion that causes screenshake")]
        public float MaxShakeDistance = 15f;
        
        [Tooltip("Base shake intensity at explosion center")]
        [Range(0f, 2f)]
        public float ShakeIntensity = 0.8f;
        
        [Tooltip("Duration of screenshake in seconds")]
        [Range(0.05f, 1f)]
        public float ShakeDuration = 0.3f;
    }
    
    public enum KeyAbilityType
    {
        None,           // standard shot
        Lifesteal,      // heals player on hit (pink key)
        Burn,           // damage over time poison effect (red key)
        Explosive,      // area damage on impact (purple key)
        Teleport,       // teleports player to hit location (green key)
    }
}
