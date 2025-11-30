using UnityEngine;

/// <summary>
/// Simple script that controls animation speed.
/// Sets Time.timeScale to control the speed of all animations.
/// </summary>
public class AnimationSpeedController : MonoBehaviour
{
    [Tooltip("Animation speed multiplier (1.0 = normal speed, 2.0 = double speed, 0.5 = half speed)")]
    [Range(0f, 10f)]
    public float AnimationSpeed = 1.0f;
    
    void Update()
    {
        Time.timeScale = AnimationSpeed;
    }
}

