using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// Static bridge for weapon audio volume settings.
    /// KeyWeaponAudioSettings (in fps.Gameplay) writes to this, WeaponController (in fps.Game) reads from it.
    /// This avoids cyclic assembly dependencies.
    /// </summary>
    public static class KeyWeaponVolumeBridge
    {
        // Master volume for firing sounds (set by KeyWeaponAudioSettings)
        public static float FiringVolume { get; set; } = 1f;
        
        // Master volume for impact sounds (set by KeyWeaponAudioSettings)
        public static float ImpactVolume { get; set; } = 1f;
        
        // Master volume for projectile travel sounds (set by KeyWeaponAudioSettings)
        public static float ProjectileTravelVolume { get; set; } = 1f;
    }
}

