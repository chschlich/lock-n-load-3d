using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Attach to key weapon projectiles to make them aim towards 
    /// the crosshair/screen center while still originating from the muzzle.
    /// This redirects the projectile's direction on spawn.
    /// </summary>
    [RequireComponent(typeof(ProjectileStandard))]
    public class KeyProjectileAimCorrection : MonoBehaviour
    {
        private ProjectileStandard m_Projectile;
        private ProjectileBase m_ProjectileBase;
        private bool m_HasCorrected = false;

        void Awake()
        {
            m_Projectile = GetComponent<ProjectileStandard>();
            m_ProjectileBase = GetComponent<ProjectileBase>();
            
            // Subscribe to OnShoot to correct aim immediately when projectile is fired
            // This runs BEFORE Update/Start, so position hasn't been modified yet
            if (m_ProjectileBase != null)
            {
                m_ProjectileBase.OnShoot += CorrectAimDirection;
            }
        }

        void OnDestroy()
        {
            if (m_ProjectileBase != null)
            {
                m_ProjectileBase.OnShoot -= CorrectAimDirection;
            }
        }

        void CorrectAimDirection()
        {
            if (m_HasCorrected) return;
            m_HasCorrected = true;

            Camera playerCamera = Camera.main;
            if (playerCamera == null) return;

            // Use the ORIGINAL spawn position (InitialPosition) before any velocity offsets
            Vector3 spawnPosition = m_ProjectileBase != null ? m_ProjectileBase.InitialPosition : transform.position;

            // Raycast from camera center to find where crosshair is pointing
            Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            Vector3 aimPoint;

            // Use a layermask to ignore the player and weapon layers
            int layerMask = ~(LayerMask.GetMask("Player") | (1 << 10)); // Ignore player and weapon layer (10)
            
            if (Physics.Raycast(cameraRay, out RaycastHit hit, 1000f, layerMask))
            {
                aimPoint = hit.point;
            }
            else
            {
                // If nothing hit, aim at a point far along the camera's forward direction
                aimPoint = playerCamera.transform.position + playerCamera.transform.forward * 1000f;
            }

            // Calculate direction from spawn position to aim point
            Vector3 correctedDirection = (aimPoint - spawnPosition).normalized;
            
            // Safety check: ensure the corrected direction is roughly forward (within 90 degrees of camera forward)
            // This prevents the projectile from ever going backwards
            float dotWithCamera = Vector3.Dot(correctedDirection, playerCamera.transform.forward);
            if (dotWithCamera < 0.1f)
            {
                // Direction is too far off, just use camera forward
                correctedDirection = playerCamera.transform.forward;
            }

            // Update projectile's rotation to face the aim direction
            transform.rotation = Quaternion.LookRotation(correctedDirection);
            
            // Reset position to spawn position (undo any velocity offset that happened)
            transform.position = spawnPosition;
            
            // Update the velocity in ProjectileStandard
            if (m_Projectile != null)
            {
                m_Projectile.SetVelocityDirection(correctedDirection);
            }
        }
    }
}
