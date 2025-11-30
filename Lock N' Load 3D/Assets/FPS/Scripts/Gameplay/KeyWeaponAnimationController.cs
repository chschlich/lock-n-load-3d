using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Handles weapon movement animation for key weapons.
    /// Based on the proven PlayerWeaponsManager bob system, adapted for key weapons.
    /// Combines bob, recoil, and aiming offsets.
    /// </summary>
    public class KeyWeaponAnimationController : MonoBehaviour
    {
        [Header("Movement Animation")]
        [Tooltip("Frequency at which the weapon will move around when the player is moving")]
        public float BobFrequency = 10f;
        
        [Tooltip("How fast the weapon bob is applied, the bigger value the fastest")]
        public float BobSharpness = 10f;
        
        [Tooltip("Distance the weapon bobs when walking")]
        public float WalkBobAmount = 0.05f;
        
        [Tooltip("Distance the weapon bobs when sprinting (multiplier on WalkBobAmount)")]
        public float SprintBobMultiplier = 1.5f;
        
        [Header("References")]
        [Tooltip("The weapon transform to animate")]
        public Transform WeaponTransform;
        
        [Tooltip("Reference to player controller")]
        public PlayerCharacterController PlayerController;
        
        [Tooltip("Reference to shooter animation controller for recoil/aim offsets")]
        public KeyWeaponShooterAnimation ShooterAnimation;
        
        [Tooltip("Base position offset for the weapon (rest position)")]
        public Vector3 BaseWeaponPosition = new Vector3(0.2f, -0.15f, 0.35f);
        
        // Movement animation state
        private float m_WeaponBobFactor = 0f;
        private Vector3 m_LastCharacterPosition;
        private Vector3 m_CurrentWeaponBobOffset = Vector3.zero;
        
        void Start()
        {
            // Auto-find player controller if not assigned
            if (PlayerController == null)
            {
                PlayerController = GetComponentInParent<PlayerCharacterController>();
                if (PlayerController == null)
                {
                    PlayerController = FindFirstObjectByType<PlayerCharacterController>();
                }
            }
            
            // Auto-assign weapon transform if not set
            if (WeaponTransform == null)
            {
                WeaponTransform = transform;
            }
            
            // Initialize position tracking
            if (PlayerController != null)
            {
                m_LastCharacterPosition = PlayerController.transform.position;
            }
        }
        
        void LateUpdate()
        {
            UpdateWeaponMovementAnimation();
            
            // Update shooter animation (aiming, recoil)
            if (ShooterAnimation != null)
            {
                ShooterAnimation.UpdateAnimations();
            }
            
            // Apply all animation offsets to weapon position
            if (WeaponTransform != null)
            {
                Vector3 finalPosition = BaseWeaponPosition + m_CurrentWeaponBobOffset;
                
                if (ShooterAnimation != null)
                {
                    finalPosition += ShooterAnimation.RecoilOffset;
                    finalPosition += ShooterAnimation.AimingPositionOffset;
                }
                
                WeaponTransform.localPosition = finalPosition;
            }
        }
        
        /// <summary>
        /// Updates the weapon bob animation based on character movement
        /// </summary>
        void UpdateWeaponMovementAnimation()
        {
            if (PlayerController == null || Time.deltaTime <= 0f)
                return;
            
            // Calculate player velocity from position delta
            Vector3 playerCharacterVelocity =
                (PlayerController.transform.position - m_LastCharacterPosition) / Time.deltaTime;
            
            // Calculate a smoothed weapon bob amount based on how close to our max grounded movement velocity we are
            float characterMovementFactor = 0f;
            if (PlayerController.IsGrounded)
            {
                float maxSpeed = PlayerController.MaxSpeedOnGround * PlayerController.SprintSpeedModifier;
                
                KeyWeaponController keyWeaponController = PlayerController.GetComponent<KeyWeaponController>();
                if (keyWeaponController != null)
                {
                    maxSpeed *= keyWeaponController.GetCurrentMovementSpeedMultiplier();
                }
                
                characterMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude / maxSpeed);
            }
            
            // Smoothly interpolate bob factor
            m_WeaponBobFactor = Mathf.Lerp(m_WeaponBobFactor, characterMovementFactor, 
                BobSharpness * Time.deltaTime);
            
            // Calculate bob amount
            float sprintInfluence = characterMovementFactor;
            float bobMultiplier = Mathf.Lerp(1f, SprintBobMultiplier, sprintInfluence);
            float currentBobAmount = WalkBobAmount * bobMultiplier;
            
            // Calculate vertical and horizontal weapon bob values
            float frequency = BobFrequency;
            float hBobValue = Mathf.Sin(Time.time * frequency) * currentBobAmount * m_WeaponBobFactor;
            float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * currentBobAmount * m_WeaponBobFactor;
            
            // Apply weapon bob offset
            m_CurrentWeaponBobOffset.x = hBobValue;
            m_CurrentWeaponBobOffset.y = Mathf.Abs(vBobValue);
            m_CurrentWeaponBobOffset.z = 0f;
            
            // Update last position for next frame
            m_LastCharacterPosition = PlayerController.transform.position;
        }
    }
}
