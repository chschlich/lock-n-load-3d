using UnityEngine;
using System.Collections;

namespace Unity.FPS.Game
{
    /// <summary>
    /// Utility for applying camera shake effects.
    /// Automatically adds a shake component to cameras that need shaking.
    /// </summary>
    public static class CameraShake
    {
        /// <summary>
        /// Apply a shake effect to the specified camera.
        /// </summary>
        /// <param name="camera">The camera to shake</param>
        /// <param name="intensity">Shake strength (position offset magnitude)</param>
        /// <param name="duration">How long the shake lasts in seconds</param>
        public static void ApplyShake(Camera camera, float intensity, float duration)
        {
            if (camera == null) return;

            // Get or add the shake component to the camera
            CameraShakeComponent shakeComponent = camera.GetComponent<CameraShakeComponent>();
            if (shakeComponent == null)
            {
                shakeComponent = camera.gameObject.AddComponent<CameraShakeComponent>();
            }

            // Trigger the shake
            shakeComponent.Shake(intensity, duration);
        }
    }

    /// <summary>
    /// Component that handles the actual camera shake logic.
    /// Automatically added to cameras by CameraShake utility.
    /// </summary>
    public class CameraShakeComponent : MonoBehaviour
    {
        private float m_ShakeIntensity = 0f;
        private float m_ShakeDuration = 0f;
        private float m_ShakeTimer = 0f;
        private bool m_IsShaking = false;
        private Quaternion m_PreviousShakeRotation = Quaternion.identity;
        private Quaternion m_TargetShakeRotation = Quaternion.identity;
        private Camera m_Camera;
        private float m_NoiseOffsetX;
        private float m_NoiseOffsetY;
        private float m_NoiseOffsetZ;

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            // Random offsets for perlin noise to make each shake unique
            m_NoiseOffsetX = Random.Range(0f, 100f);
            m_NoiseOffsetY = Random.Range(0f, 100f);
            m_NoiseOffsetZ = Random.Range(0f, 100f);
        }

        /// <summary>
        /// Start a shake effect. Multiple shakes can stack.
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            // If already shaking, add to existing shake (stack effects)
            if (m_IsShaking)
            {
                m_ShakeIntensity = Mathf.Max(m_ShakeIntensity, intensity);
                m_ShakeDuration = Mathf.Max(m_ShakeDuration, duration);
                m_ShakeTimer = 0f;
            }
            else
            {
                m_ShakeIntensity = intensity;
                m_ShakeDuration = duration;
                m_ShakeTimer = 0f;
                m_IsShaking = true;
            }
        }

        void LateUpdate()
        {
            // Remove the previous shake rotation first
            transform.localRotation = Quaternion.Inverse(m_PreviousShakeRotation) * transform.localRotation;

            if (!m_IsShaking)
            {
                m_PreviousShakeRotation = Quaternion.identity;
                return;
            }

            m_ShakeTimer += Time.deltaTime;

            Quaternion newShakeRotation = Quaternion.identity;

            if (m_ShakeTimer >= m_ShakeDuration)
            {
                // Shake complete
                m_IsShaking = false;
            }
            else
            {
                // Calculate shake with decay over time
                float progress = m_ShakeTimer / m_ShakeDuration;
                float decayFactor = 1f - progress; // Linear decay from 1 to 0

                // Use Perlin noise for smoother shake (frequency of 10 for reasonable speed)
                float noiseFrequency = 10f;
                float time = m_ShakeTimer * noiseFrequency;
                
                float pitch = (Mathf.PerlinNoise(m_NoiseOffsetX + time, 0f) * 2f - 1f);
                float yaw = (Mathf.PerlinNoise(m_NoiseOffsetY + time, 0f) * 2f - 1f);
                float roll = (Mathf.PerlinNoise(m_NoiseOffsetZ + time, 0f) * 2f - 1f) * 0.5f;

                // Scale by intensity and decay
                float angleMultiplier = m_ShakeIntensity * 5f * decayFactor;
                pitch *= angleMultiplier;
                yaw *= angleMultiplier;
                roll *= angleMultiplier;

                newShakeRotation = Quaternion.Euler(pitch, yaw, roll);
            }

            // Apply new rotation and store it
            m_PreviousShakeRotation = newShakeRotation;
            transform.localRotation = transform.localRotation * m_PreviousShakeRotation;
        }

        void OnDisable()
        {
            // Remove any remaining rotation
            if (m_PreviousShakeRotation != Quaternion.identity)
            {
                transform.localRotation = Quaternion.Inverse(m_PreviousShakeRotation) * transform.localRotation;
                m_PreviousShakeRotation = Quaternion.identity;
            }
            m_IsShaking = false;
        }
    }
}

