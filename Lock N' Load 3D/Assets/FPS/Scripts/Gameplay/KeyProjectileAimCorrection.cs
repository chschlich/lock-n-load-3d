using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Attach to key weapon projectiles (red, purple) to make them aim towards 
    /// the crosshair/screen center while still originating from the muzzle.
    /// This redirects the projectile's direction on spawn.
    /// </summary>
    [RequireComponent(typeof(ProjectileStandard))]
    public class KeyProjectileAimCorrection : MonoBehaviour
    {
        private ProjectileStandard m_Projectile;
        private bool m_HasCorrected = false;

        void Awake()
        {
            m_Projectile = GetComponent<ProjectileStandard>();
        }

        void Start()
        {
            // Correct aim direction on the first frame
            CorrectAimDirection();
        }

        void CorrectAimDirection()
        {
            if (m_HasCorrected) return;
            m_HasCorrected = true;

            Camera playerCamera = Camera.main;
            if (playerCamera == null) return;

            // Raycast from camera center to find where crosshair is pointing
            Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            Vector3 aimPoint;

            if (Physics.Raycast(cameraRay, out RaycastHit hit, 1000f))
            {
                aimPoint = hit.point;
            }
            else
            {
                // If nothing hit, aim at a point far along the camera's forward direction
                aimPoint = playerCamera.transform.position + playerCamera.transform.forward * 1000f;
            }

            // Calculate direction from current position (muzzle) to aim point
            Vector3 correctedDirection = (aimPoint - transform.position).normalized;

            // Update projectile's rotation to face the aim direction
            transform.rotation = Quaternion.LookRotation(correctedDirection);
            
            // CRITICAL: Also update the velocity in ProjectileStandard
            // The velocity was already set in OnShoot() before Start() runs,
            // so we must update it here to match the corrected direction
            if (m_Projectile != null)
            {
                m_Projectile.SetVelocityDirection(correctedDirection);
            }
        }
    }
}

