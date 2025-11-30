using UnityEngine;

public class DistortionAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Speed of the distortion animation")]
    public float AnimationSpeed = 3f;
    
    [Tooltip("Minimum distortion strength")]
    public float MinDistortionStrength = 5f;
    
    [Tooltip("Maximum distortion strength")]
    public float MaxDistortionStrength = 15f;
    
    private Material m_Material;
    private float m_Time;
    
    void Start()
    {
        // Get the material from the MeshRenderer
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Use .material (not .sharedMaterial) to get an instance
            m_Material = renderer.material;
            
            // Verify material has distortion properties
            if (!m_Material.HasProperty("_DistortionStrength"))
            {
                Debug.LogError("Material does not support distortion!");
                enabled = false;
                return;
            }
            
            // Set initial distortion properties
            m_Material.SetFloat("_DistortionEnabled", 1f);
            m_Material.SetFloat("_DistortionStrengthScaled", 1f);
            
            Debug.Log("DistortionAnimator initialized successfully");
        }
        else
        {
            Debug.LogError("No MeshRenderer found!");
            enabled = false;
        }
    }
    
    void Update()
    {
        if (m_Material == null) return;
        
        // Animate distortion strength using sine wave
        m_Time += Time.deltaTime * AnimationSpeed;
        float strength = Mathf.Lerp(
            MinDistortionStrength, 
            MaxDistortionStrength, 
            (Mathf.Sin(m_Time) + 1f) * 0.5f
        );
        
        // Apply to material
        m_Material.SetFloat("_DistortionStrength", strength);
        m_Material.SetFloat("_DistortionEnabled", 1f);
        m_Material.SetFloat("_DistortionStrengthScaled", 1f);
    }
    
    void OnDestroy()
    {
        // Clean up material instance
        if (m_Material != null)
        {
            Destroy(m_Material);
        }
    }
}