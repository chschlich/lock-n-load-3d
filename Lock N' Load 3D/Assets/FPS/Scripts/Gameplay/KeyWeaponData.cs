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
        public Sprite KeyIcon;
        
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
        
        [Header("Crosshair")]
        [Tooltip("Crosshair sprite (leave null to use default)")]
        public Sprite CrosshairSprite;
        
        [Tooltip("Crosshair size in pixels")]
        public int CrosshairSize = 55;
        
        [Tooltip("Crosshair color (uses KeyColor if not set)")]
        public Color CrosshairColor = Color.white;
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
