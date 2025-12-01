using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
    }

    [System.Serializable]
    public struct CrosshairData
    {
        [Tooltip("The image that will be used for this weapon's crosshair")]
        public Sprite CrosshairSprite;

        [Tooltip("The size of the crosshair image")]
        public int CrosshairSize;

        [Tooltip("The color of the crosshair image")]
        public Color CrosshairColor;
    }

    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Information")] [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string WeaponName;

        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite WeaponIcon;

        [Tooltip("Default data for the crosshair")]
        public CrosshairData CrosshairDataDefault;

        [Tooltip("Data for the crosshair when targeting an enemy")]
        public CrosshairData CrosshairDataTargetInSight;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        public GameObject WeaponRoot;

        [Tooltip("Tip of the weapon, where the projectiles are shot")]
        public Transform WeaponMuzzle;

        [Header("Shoot Parameters")] [Tooltip("The type of weapon wil affect how it shoots")]
        public WeaponShootType ShootType;

        [Tooltip("The projectile prefab")] public ProjectileBase ProjectilePrefab;

        [Tooltip("Minimum duration between two shots")]
        public float DelayBetweenShots = 0.5f;

        [Tooltip("Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)")]
        public float BulletSpreadAngle = 0f;

        [Tooltip("Amount of bullets per shot")]
        public int BulletsPerShot = 1;

        [Tooltip("Force that will push back the weapon after each shot")] [Range(0f, 2f)]
        public float RecoilForce = 1;

        [Tooltip("Ratio of the default FOV that this weapon applies while aiming")] [Range(0f, 1f)]
        public float AimZoomRatio = 1f;

        [Tooltip("Translation to apply to weapon arm when aiming with this weapon")]
        public Vector3 AimOffset;

        [Header("Ammo Parameters")]
        [Tooltip("Should the player manually reload")]
        public bool AutomaticReload = true;
        [Tooltip("Has physical clip on the weapon and ammo shells are ejected when firing")]
        public bool HasPhysicalBullets = false;
        [Tooltip("Number of bullets in a clip")]
        public int ClipSize = 30;
        [Tooltip("Bullet Shell Casing")]
        public GameObject ShellCasing;
        [Tooltip("Weapon Ejection Port for physical ammo")]
        public Transform EjectionPort;
        [Tooltip("Force applied on the shell")]
        [Range(0.0f, 5.0f)] public float ShellCasingEjectionForce = 2.0f;
        [Tooltip("Maximum number of shell that can be spawned before reuse")]
        [Range(1, 30)] public int ShellPoolSize = 1;
        [Tooltip("Amount of ammo reloaded per second")]
        public float AmmoReloadRate = 1f;

        [Tooltip("Delay after the last shot before starting to reload")]
        public float AmmoReloadDelay = 2f;

        [Tooltip("Maximum amount of ammo in the gun")]
        public int MaxAmmo = 8;

        [Header("Charging parameters (charging weapons only)")]
        [Tooltip("Trigger a shot when maximum charge is reached")]
        public bool AutomaticReleaseOnCharged;

        [Tooltip("Duration to reach maximum charge")]
        public float MaxChargeDuration = 2f;

        [Tooltip("Initial ammo used when starting to charge")]
        public float AmmoUsedOnStartCharge = 1f;

        [Tooltip("Additional ammo used when charge reaches its maximum")]
        public float AmmoUsageRateWhileCharging = 1f;

        [Header("Audio & Visual")] 
        [Tooltip("Optional weapon animator for OnShoot animations")]
        public Animator WeaponAnimator;

        [Tooltip("Prefab of the muzzle flash")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("Scale multiplier for the muzzle flash (1 = normal size, 0.5 = half size, etc.)")]
        public float MuzzleFlashScale = 1f;

        [Tooltip("Unparent the muzzle flash instance on spawn")]
        public bool UnparentMuzzleFlash;

        [Tooltip("Optional muzzle explosion effect GameObject (child of weapon prefab - plays on fire, e.g. for purple key)")]
        public GameObject MuzzleExplosionEffect;

        [Tooltip("sound played when shooting")]
        public AudioClip ShootSfx;

        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip ChangeWeaponSfx;

        [Tooltip("Continuous Shooting Sound")] public bool UseContinuousShootSound = false;
        public AudioClip ContinuousShootStartSfx;
        public AudioClip ContinuousShootLoopSfx;
        public AudioClip ContinuousShootEndSfx;
        AudioSource m_ContinuousShootAudioSource = null;
        bool m_WantsToShoot = false;

        public UnityAction OnShoot;
        public event Action OnShootProcessed;

        int m_CarriedPhysicalBullets;
        float m_CurrentAmmo;
        float m_LastTimeShot = Mathf.NegativeInfinity;
        public float LastChargeTriggerTimestamp { get; private set; }
        Vector3 m_LastMuzzlePosition;

        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; }
        public bool IsCharging { get; private set; }
        public float CurrentAmmoRatio { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public float CurrentCharge { get; private set; }
        public Vector3 MuzzleWorldVelocity { get; private set; }

        public float GetAmmoNeededToShoot() =>
            (ShootType != WeaponShootType.Charge ? 1f : Mathf.Max(1f, AmmoUsedOnStartCharge)) /
            (MaxAmmo * BulletsPerShot);

        public int GetCarriedPhysicalBullets() => m_CarriedPhysicalBullets;
        public int GetCurrentAmmo() => Mathf.FloorToInt(m_CurrentAmmo);
        
        /// <summary>
        /// Set the current ammo ratio for UI display only (used by overheat system)
        /// Does NOT affect actual m_CurrentAmmo to avoid blocking shooting
        /// </summary>
        public void SetAmmoRatio(float ratio)
        {
            // Only update the UI ratio, NOT the actual ammo count
            // This prevents the overheat heat level from blocking weapon firing
            CurrentAmmoRatio = Mathf.Clamp01(ratio);
        }

        AudioSource m_ShootAudioSource;

        public bool IsReloading { get; private set; }

        const string k_AnimAttackParameter = "Attack";

        private Queue<Rigidbody> m_PhysicalAmmoPool;

        void Awake()
        {
            m_CurrentAmmo = MaxAmmo;
            m_CarriedPhysicalBullets = HasPhysicalBullets ? ClipSize : 0;
            m_LastMuzzlePosition = WeaponMuzzle.position;

            m_ShootAudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, WeaponController>(m_ShootAudioSource, this,
                gameObject);

            if (UseContinuousShootSound)
            {
                m_ContinuousShootAudioSource = gameObject.AddComponent<AudioSource>();
                m_ContinuousShootAudioSource.playOnAwake = false;
                m_ContinuousShootAudioSource.clip = ContinuousShootLoopSfx;
                m_ContinuousShootAudioSource.outputAudioMixerGroup =
                    AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
                m_ContinuousShootAudioSource.loop = true;
            }

            if (HasPhysicalBullets)
            {
                m_PhysicalAmmoPool = new Queue<Rigidbody>(ShellPoolSize);

                for (int i = 0; i < ShellPoolSize; i++)
                {
                    GameObject shell = Instantiate(ShellCasing, transform);
                    shell.SetActive(false);
                    m_PhysicalAmmoPool.Enqueue(shell.GetComponent<Rigidbody>());
                }
            }
        }

        public void AddCarriablePhysicalBullets(int count) => m_CarriedPhysicalBullets = Mathf.Max(m_CarriedPhysicalBullets + count, MaxAmmo);

        void ShootShell()
        {
            Rigidbody nextShell = m_PhysicalAmmoPool.Dequeue();

            nextShell.transform.position = EjectionPort.transform.position;
            nextShell.transform.rotation = EjectionPort.transform.rotation;
            nextShell.gameObject.SetActive(true);
            nextShell.transform.SetParent(null);
            nextShell.collisionDetectionMode = CollisionDetectionMode.Continuous;
            nextShell.AddForce(nextShell.transform.up * ShellCasingEjectionForce, ForceMode.Impulse);

            m_PhysicalAmmoPool.Enqueue(nextShell);
        }

        void PlaySFX(AudioClip sfx) => AudioUtility.CreateSFX(sfx, transform.position, AudioUtility.AudioGroups.WeaponShoot, 0.0f);


        void Reload()
        {
            if (m_CarriedPhysicalBullets > 0)
            {
                m_CurrentAmmo = Mathf.Min(m_CarriedPhysicalBullets, ClipSize);
            }

            IsReloading = false;
        }

        public void StartReloadAnimation()
        {
            if (m_CurrentAmmo < m_CarriedPhysicalBullets)
            {
                GetComponent<Animator>().SetTrigger("Reload");
                IsReloading = true;
            }
        }

        void Update()
        {
            UpdateAmmo();
            UpdateCharge();
            UpdateContinuousShootSound();

            if (Time.deltaTime > 0)
            {
                MuzzleWorldVelocity = (WeaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
                m_LastMuzzlePosition = WeaponMuzzle.position;
            }
        }

        void UpdateAmmo()
        {
            if (AutomaticReload && m_LastTimeShot + AmmoReloadDelay < Time.time && m_CurrentAmmo < MaxAmmo && !IsCharging)
            {
                // reloads weapon over time
                m_CurrentAmmo += AmmoReloadRate * Time.deltaTime;

                // limits ammo to max value
                m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, MaxAmmo);

                IsCooling = true;
            }
            else
            {
                IsCooling = false;
            }

            if (MaxAmmo == Mathf.Infinity)
            {
                CurrentAmmoRatio = 1f;
            }
            else
            {
                CurrentAmmoRatio = m_CurrentAmmo / MaxAmmo;
            }
        }

        void UpdateCharge()
        {
            if (IsCharging)
            {
                if (CurrentCharge < 1f)
                {
                    float chargeLeft = 1f - CurrentCharge;

                    // Calculate how much charge ratio to add this frame
                    float chargeAdded = 0f;
                    if (MaxChargeDuration <= 0f)
                    {
                        chargeAdded = chargeLeft;
                    }
                    else
                    {
                        chargeAdded = (1f / MaxChargeDuration) * Time.deltaTime;
                    }

                    chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                    // See if we can actually add this charge
                    float ammoThisChargeWouldRequire = chargeAdded * AmmoUsageRateWhileCharging;
                    if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                    {
                        // Use ammo based on charge added
                        UseAmmo(ammoThisChargeWouldRequire);

                        // set current charge ratio
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                    }
                }
            }
        }

        void UpdateContinuousShootSound()
        {
            if (UseContinuousShootSound)
            {
                if (m_WantsToShoot && m_CurrentAmmo >= 1f)
                {
                    if (!m_ContinuousShootAudioSource.isPlaying)
                    {
                        m_ShootAudioSource.PlayOneShot(ShootSfx);
                        m_ShootAudioSource.PlayOneShot(ContinuousShootStartSfx);
                        m_ContinuousShootAudioSource.Play();
                    }
                }
                else if (m_ContinuousShootAudioSource.isPlaying)
                {
                    m_ShootAudioSource.PlayOneShot(ContinuousShootEndSfx);
                    m_ContinuousShootAudioSource.Stop();
                }
            }
        }

        public void ShowWeapon(bool show)
        {
            WeaponRoot.SetActive(show);

            if (show && ChangeWeaponSfx)
            {
                m_ShootAudioSource.PlayOneShot(ChangeWeaponSfx);
            }

            IsWeaponActive = show;
        }

        public void UseAmmo(float amount)
        {
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, MaxAmmo);
            m_CarriedPhysicalBullets -= Mathf.RoundToInt(amount);
            m_CarriedPhysicalBullets = Mathf.Clamp(m_CarriedPhysicalBullets, 0, MaxAmmo);
            m_LastTimeShot = Time.time;
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            m_WantsToShoot = inputDown || inputHeld;
            switch (ShootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();
                    }

                    return false;

                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }

                    return false;

                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        TryBeginCharge();
                    }

                    // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                    if (inputUp || (AutomaticReleaseOnCharged && CurrentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }

                    return false;

                default:
                    return false;
            }
        }

        bool TryShoot()
        {
            if (m_CurrentAmmo >= 1f
                && m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                HandleShoot();
                m_CurrentAmmo -= 1f;

                return true;
            }

            return false;
        }

        bool TryBeginCharge()
        {
            if (!IsCharging
                && m_CurrentAmmo >= AmmoUsedOnStartCharge
                && Mathf.FloorToInt((m_CurrentAmmo - AmmoUsedOnStartCharge) * BulletsPerShot) > 0
                && m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                UseAmmo(AmmoUsedOnStartCharge);

                LastChargeTriggerTimestamp = Time.time;
                IsCharging = true;

                return true;
            }

            return false;
        }

        bool TryReleaseCharge()
        {
            if (IsCharging)
            {
                HandleShoot();

                CurrentCharge = 0f;
                IsCharging = false;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Optional callback invoked right before spawning projectiles/effects.
        /// Used by key weapons to force animation update for correct muzzle position during aim.
        /// </summary>
        public System.Action OnBeforeShoot;
        
        /// <summary>
        /// Optional: Returns the aim position offset to apply when spawning effects.
        /// Used by key weapons to account for aim animation offset.
        /// </summary>
        public System.Func<Vector3> GetAimOffset;
        
        void HandleShoot()
        {
            // CRITICAL: Invoke callback to update weapon position before spawning effects
            // This ensures the muzzle position is correct during aim animation
            OnBeforeShoot?.Invoke();
            
            // Get any aim offset that should be applied to spawn positions
            Vector3 aimOffset = GetAimOffset?.Invoke() ?? Vector3.zero;
            
            // Calculate the actual spawn position (muzzle + aim offset in muzzle's local space)
            Vector3 spawnPosition = WeaponMuzzle.position + WeaponMuzzle.TransformDirection(aimOffset);
            
            int bulletsPerShotFinal = ShootType == WeaponShootType.Charge
                ? Mathf.CeilToInt(CurrentCharge * BulletsPerShot)
                : BulletsPerShot;

            // spawn all bullets with random direction
            for (int i = 0; i < bulletsPerShotFinal; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, spawnPosition,
                    Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // muzzle flash
            if (MuzzleFlashPrefab != null)
            {
                // Instantiate at corrected position (including aim offset)
                GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, spawnPosition,
                    WeaponMuzzle.rotation, WeaponMuzzle.transform);
                
                // Get all particle systems and configure them
                ParticleSystem[] particleSystems = muzzleFlashInstance.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particleSystems)
                {
                    var main = ps.main;
                    
                    // CRITICAL: Force Local space so particles follow the weapon during aim animation
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    
                    // Scale the muzzle flash if scale is not 1
                    if (MuzzleFlashScale != 1f)
                    {
                        // Handle startSize - works with both constants and curves
                        if (main.startSize.mode == ParticleSystemCurveMode.Constant)
                        {
                            main.startSize = main.startSize.constant * MuzzleFlashScale;
                        }
                        else if (main.startSize.mode == ParticleSystemCurveMode.TwoConstants)
                        {
                            main.startSize = new ParticleSystem.MinMaxCurve(
                                main.startSize.constantMin * MuzzleFlashScale,
                                main.startSize.constantMax * MuzzleFlashScale);
                        }
                        else
                        {
                            // For curves, use multiplier
                            main.startSizeMultiplier *= MuzzleFlashScale;
                        }
                        
                        // Handle startSpeed
                        if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
                        {
                            main.startSpeed = main.startSpeed.constant * MuzzleFlashScale;
                        }
                        else if (main.startSpeed.mode == ParticleSystemCurveMode.TwoConstants)
                        {
                            main.startSpeed = new ParticleSystem.MinMaxCurve(
                                main.startSpeed.constantMin * MuzzleFlashScale,
                                main.startSpeed.constantMax * MuzzleFlashScale);
                        }
                        else
                        {
                            main.startSpeedMultiplier *= MuzzleFlashScale;
                        }
                        
                        // Scale shape module if present
                        var shape = ps.shape;
                        if (shape.enabled)
                        {
                            shape.radius *= MuzzleFlashScale;
                            shape.scale *= MuzzleFlashScale;
                        }
                        
                        // Scale emission rate
                        var emission = ps.emission;
                        emission.rateOverTimeMultiplier *= MuzzleFlashScale;
                        emission.rateOverDistanceMultiplier *= MuzzleFlashScale;
                    }
                }
                
                // Scale transform if needed
                if (MuzzleFlashScale != 1f)
                {
                    muzzleFlashInstance.transform.localScale = Vector3.one * MuzzleFlashScale;
                }
                
                // Unparent the muzzleFlashInstance
                if (UnparentMuzzleFlash)
                {
                    muzzleFlashInstance.transform.SetParent(null);
                }

                Destroy(muzzleFlashInstance, 2f);
            }

            // Muzzle explosion effect (for purple key, etc.)
            if (MuzzleExplosionEffect != null)
            {
                // Ensure the MuzzleExplosionEffect GameObject and all parents are active
                if (!MuzzleExplosionEffect.activeInHierarchy)
                {
                    MuzzleExplosionEffect.SetActive(true);
                    Transform current = MuzzleExplosionEffect.transform;
                    while (current != null && !current.gameObject.activeInHierarchy)
                    {
                        current.gameObject.SetActive(true);
                        current = current.parent;
                    }
                }
                
                // Play ParticleSystem if present
                ParticleSystem[] particleSystems = MuzzleExplosionEffect.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem ps in particleSystems)
                {
                    // CRITICAL: Force Local space so particles follow the weapon during aim animation
                    var main = ps.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    
                    // Stop and clear first to ensure burst replays from beginning
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play();
                    ps.Emit(10);
                }
                
                // Play VisualEffect (VFX Graph) if present
                UnityEngine.VFX.VisualEffect[] visualEffects = MuzzleExplosionEffect.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
                foreach (var vfx in visualEffects)
                {
                    vfx.Stop();
                    vfx.Play();
                }
            }

            if (HasPhysicalBullets)
            {
                ShootShell();
                m_CarriedPhysicalBullets--;
            }

            m_LastTimeShot = Time.time;

            // play shoot SFX
            if (ShootSfx && !UseContinuousShootSound)
            {
                m_ShootAudioSource.PlayOneShot(ShootSfx);
            }

            // Trigger attack animation if there is any
            if (WeaponAnimator)
            {
                WeaponAnimator.SetTrigger(k_AnimAttackParameter);
            }

            OnShoot?.Invoke();
            OnShootProcessed?.Invoke();
        }

        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = BulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

            return spreadWorldDirection;
        }
    }
}