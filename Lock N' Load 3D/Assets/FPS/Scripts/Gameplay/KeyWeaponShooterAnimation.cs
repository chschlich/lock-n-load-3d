using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Handles recoil and aiming animations for key weapons.
    /// Provides offset values that KeyWeaponAnimationController adds to the weapon position.
    /// </summary>
    public class KeyWeaponShooterAnimation : MonoBehaviour
    {
        [Header("Weapon Recoil")]
        [Tooltip("This will affect how fast the recoil moves the weapon, the bigger the value, the fastest")]
        public float RecoilSharpness = 50f;

        [Tooltip("Base maximum distance the recoil can affect the weapon (scaled by weapon's RecoilForce)")]
        public float MaxRecoilDistance = 0.5f;

        [Tooltip("How fast the weapon goes back to it's original position after the recoil is finished")]
        public float RecoilRestitutionSharpness = 10f;

        [Header("Aiming")]
        [Tooltip("Speed at which the aiming animation is played")]
        public float AimingAnimationSpeed = 10f;

        [Tooltip("Position offset when aiming down sights")]
        public Vector3 AimOffset = new Vector3(0f, 0f, 0.1f);

        [Tooltip("FOV ratio when aiming (0.5 = half FOV)")]
        [Range(0f, 1f)]
        public float AimZoomRatio = 0.8f;

        [Header("References")]
        [Tooltip("Reference to player input handler for aim input")]
        public PlayerInputHandler InputHandler;

        [Tooltip("Reference to player camera for FOV changes")]
        public Camera PlayerCamera;

        [Tooltip("Default FOV of the camera")]
        public float DefaultFov = 60f;

        // Animation state
        private Vector3 m_WeaponRecoilLocalPosition;
        private Vector3 m_AccumulatedRecoil;
        private Vector3 m_AimingOffset;
        private bool m_IsAiming;

        // Public accessors for KeyWeaponAnimationController to read
        public Vector3 RecoilOffset => m_WeaponRecoilLocalPosition;
        public Vector3 AimingPositionOffset => m_AimingOffset;
        public bool IsAiming => m_IsAiming;

        void Start()
        {
            // Auto-find references if not assigned
            if (InputHandler == null)
            {
                InputHandler = FindFirstObjectByType<PlayerInputHandler>();
            }

            if (PlayerCamera == null)
            {
                var playerController = FindFirstObjectByType<PlayerCharacterController>();
                if (playerController != null)
                {
                    PlayerCamera = playerController.PlayerCamera;
                }
            }
        }

        /// <summary>
        /// Updates aiming and recoil animations.
        /// Called by KeyWeaponAnimationController.
        /// </summary>
        public void UpdateAnimations()
        {
            UpdateWeaponAiming();
            UpdateWeaponRecoil();
        }

        /// <summary>
        /// Called by KeyWeaponController when the weapon fires.
        /// </summary>
        public void AccumulateRecoil(float recoilForce)
        {
            float effectiveMaxDistance = MaxRecoilDistance * recoilForce;
            m_AccumulatedRecoil += Vector3.back * recoilForce;
            m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, effectiveMaxDistance);
        }

        /// <summary>
        /// Updates weapon position offset for aiming.
        /// </summary>
        void UpdateWeaponAiming()
        {
            m_IsAiming = InputHandler != null && InputHandler.GetAimInputHeld();

            if (m_IsAiming)
            {
                m_AimingOffset = Vector3.Lerp(m_AimingOffset, AimOffset, AimingAnimationSpeed * Time.deltaTime);

                if (PlayerCamera != null)
                {
                    float targetFov = DefaultFov * AimZoomRatio;
                    PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, targetFov,
                        AimingAnimationSpeed * Time.deltaTime);
                }
            }
            else
            {
                m_AimingOffset = Vector3.Lerp(m_AimingOffset, Vector3.zero, AimingAnimationSpeed * Time.deltaTime);

                if (PlayerCamera != null)
                {
                    PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, DefaultFov,
                        AimingAnimationSpeed * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Updates the weapon recoil animation.
        /// </summary>
        void UpdateWeaponRecoil()
        {
            if (m_WeaponRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
            {
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, m_AccumulatedRecoil,
                    RecoilSharpness * Time.deltaTime);
            }
            else
            {
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, Vector3.zero,
                    RecoilRestitutionSharpness * Time.deltaTime);
                m_AccumulatedRecoil = m_WeaponRecoilLocalPosition;
            }
        }

        /// <summary>
        /// Resets all animation state (call when switching weapons)
        /// </summary>
        public void ResetAnimationState()
        {
            m_WeaponRecoilLocalPosition = Vector3.zero;
            m_AccumulatedRecoil = Vector3.zero;
            m_AimingOffset = Vector3.zero;
            m_IsAiming = false;
        }
    }
}
