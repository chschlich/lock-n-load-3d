using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public enum OverlayMode
{
    Outline,  // Edge-focused gradient (for outline/ring effect)
    Full      // Uniform opacity across entire surface
}

[ExecuteAlways]
public class DistortionPropertyWrapper : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Play animation in edit mode/prefab preview (enables ExecuteAlways behavior)")]
    public bool PlayAnimation = true;
    
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second")]
    public float RotationSpeed = 90f;
    
    [Tooltip("Rotation axis")]
    public Vector3 RotationAxis = Vector3.up;
    
    [Header("Glow Settings")]
    [Tooltip("Glow intensity multiplier")]
    public float GlowIntensity = 2f;
    
    [Tooltip("Enable pulsing glow")]
    public bool PulsingGlow = true;
    
    [Tooltip("Glow pulse speed")]
    public float GlowPulseSpeed = 2f;
    
    [Header("Overlay Settings")]
    [Tooltip("Overlay mode: Outline = edge-focused ring effect, Full = uniform coverage")]
    public OverlayMode OverlayMode = OverlayMode.Full;
    
    [Tooltip("Opacity/transparency of the overlay (0 = fully transparent, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float OverlayOpacity = 0.2f;
    
    [Tooltip("Edge thickness for Outline mode (0 = thin edge, 1 = thick edge)")]
    [Range(0f, 1f)]
    public float OutlineThickness = 0.2f;
    
    private Material m_Material;
    private Texture2D m_GeneratedBaseTexture;
    private Color m_BaseEmissionColor;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // Auto-detect overlay mode based on GameObject name if not explicitly set
        if (gameObject.name.Contains("Outline") || gameObject.name.Contains("Ring"))
        {
            OverlayMode = OverlayMode.Outline;
        }
        else if (gameObject.name.Contains("Full"))
        {
            OverlayMode = OverlayMode.Full;
        }
        else if (gameObject.name.Contains("Overlay"))
        {
            // If scale is > 1.0, it's likely an outline (slightly larger than parent)
            // If scale is 1.0, it's likely a full overlay (same size as parent)
            if (transform.localScale.x > 1.01f || transform.localScale.y > 1.01f || transform.localScale.z > 1.01f)
            {
                OverlayMode = OverlayMode.Outline;
            }
            else
            {
                OverlayMode = OverlayMode.Full;
            }
        }
        
        // Generate seamless sphere mesh FIRST (replaces Unity's default sphere with UV seam)
        GenerateSeamlessSphereMesh();
        
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            m_Material = renderer.material;
            if (m_Material != null)
            {
                SetupMaterial();
            }
        }
    }
    
    void SetupMaterial()
    {
        if (m_Material == null) return;
        
        // Set render queue based on overlay mode
        // Outline renders first, Full overlay renders on top
        int baseQueue = (int)RenderQueue.Transparent;
        if (OverlayMode == OverlayMode.Outline)
        {
            m_Material.renderQueue = baseQueue; // Outline renders first
        }
        else
        {
            m_Material.renderQueue = baseQueue + 1; // Full overlay renders on top
        }
        
        // Disable depth writing so it doesn't occlude itself
        m_Material.SetInt("_ZWrite", 0);
        
        // Set base color alpha for overlay opacity
        Color baseColor = m_Material.GetColor("_BaseColor");
        baseColor.a = OverlayOpacity;
        m_Material.SetColor("_BaseColor", baseColor);
        
        // Generate a procedural base texture to fix clipping issues
        GenerateBaseTexture();
        
        // Enable emission for glow
        m_Material.EnableKeyword("_EMISSION");
        m_Material.SetFloat("_EmissionEnabled", 1f);
        
        // Set initial glow color
        m_BaseEmissionColor = new Color(5.656854f, 0f, 5.656854f, 0.3f) * GlowIntensity;
        m_Material.SetColor("_EmissionColor", m_BaseEmissionColor);
    }
    
    void GenerateSeamlessSphereMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) return;
        
        // Create a new mesh
        Mesh mesh = new Mesh();
        mesh.name = "SeamlessSphere";
        
        int segments = 32; // Higher = smoother but more vertices
        int rings = 16;
        
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        
        // Generate vertices with seamless UVs
        // Key: Use longitude/latitude mapping but ensure U wraps from 0 to 1 continuously
        for (int ring = 0; ring <= rings; ring++)
        {
            float theta = ring * Mathf.PI / rings; // 0 to PI (vertical)
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            
            for (int seg = 0; seg <= segments; seg++)
            {
                float phi = seg * 2f * Mathf.PI / segments; // 0 to 2PI (horizontal)
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                
                // Vertex position
                // Scale by 0.5 to match Unity's default sphere primitive radius (0.5 units)
                Vector3 pos = new Vector3(
                    cosPhi * sinTheta * 0.5f,
                    cosTheta * 0.5f,
                    sinPhi * sinTheta * 0.5f
                );
                vertices.Add(pos);
                
                // Seamless UV: U wraps continuously, V goes from 0 to 1
                // For seamless wrapping, the last segment should have U=0 (same as first)
                // This ensures the texture wraps perfectly without a visible seam
                float u = seg == segments ? 0f : (float)seg / segments; // Last segment uses U=0 for seamless wrap
                float v = (float)ring / rings;
                uvs.Add(new Vector2(u, v));
            }
        }
        
        // Generate triangles
        for (int ring = 0; ring < rings; ring++)
        {
            for (int seg = 0; seg < segments; seg++)
            {
                int current = ring * (segments + 1) + seg;
                int next = current + segments + 1;
                
                // First triangle
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                
                // Second triangle
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Apply to mesh filter
        meshFilter.mesh = mesh;
    }
    
    void GenerateBaseTexture()
    {
        // Create a procedural gradient texture that works well on spheres
        // This avoids UV mapping issues that cause clipping
        int size = 256; // Texture size
        m_GeneratedBaseTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        m_GeneratedBaseTexture.name = "GeneratedBaseTexture";
        
        // Get the base color from the material
        Color baseColor = m_Material.GetColor("_BaseColor");
        
        // Generate a seamless texture that tiles properly
        // Unity's sphere has a UV seam at the edges, so we need seamless tiling
        // The key is to make left/right edges match and use Repeat wrap mode
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Use normalized coordinates (0-1)
                float u = x / (float)size;
                float v = y / (float)size;
                
                // For seamless tiling, we need to ensure u=0 and u=1 match
                // Calculate distance from center with toroidal wrapping (seamless)
                float centerU = 0.5f;
                float centerV = 0.5f;
                
                // Calculate wrapped distance using toroidal distance (seamless)
                float dx = u - centerU;
                float dy = v - centerV;
                
                // Wrap horizontally for seamless tiling (toroidal)
                if (dx > 0.5f) dx = dx - 1f;
                else if (dx < -0.5f) dx = dx + 1f;
                
                // Calculate radial distance (normalized 0-1)
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // *2 to normalize to 0-1
                distance = Mathf.Clamp01(distance);
                
                Color pixelColor = baseColor;
                
                // Apply different gradient logic based on overlay mode
                if (OverlayMode == OverlayMode.Outline)
                {
                    // Outline mode: Edge-focused gradient (opaque at edges, transparent in center)
                    // Create a ring/outline effect
                    float edgeDistance = Mathf.Abs(distance - 1f); // Distance from edge (0 = edge, 1 = center)
                    float edgeGradient = 1f - Mathf.Pow(edgeDistance / OutlineThickness, 2f);
                    edgeGradient = Mathf.Clamp01(edgeGradient);
                    pixelColor.a = edgeGradient * OverlayOpacity;
                }
                else // Full mode
                {
                    // Full mode: Set texture alpha to 1.0, let material's _BaseColor.a control opacity
                    // This prevents double-multiplication of alpha values
                    pixelColor.a = 1.0f;
                }
                
                // Add subtle variation that's also seamless
                float angle = Mathf.Atan2(dy, dx);
                float ripple = Mathf.Sin(angle * 4f + distance * 8f) * 0.1f + 1f;
                pixelColor = new Color(pixelColor.r * ripple, pixelColor.g * ripple, pixelColor.b * ripple, pixelColor.a);
                
                m_GeneratedBaseTexture.SetPixel(x, y, pixelColor);
            }
        }
        
        // CRITICAL FIX: Make the texture truly seamless by copying edge pixels
        // After generation, explicitly copy the rightmost column to match leftmost
        for (int y = 0; y < size; y++)
        {
            // Get the pixel just before the right edge (to avoid the actual edge)
            Color leftPixel = m_GeneratedBaseTexture.GetPixel(1, y);
            // Set the leftmost pixel to match what should be at the right edge
            m_GeneratedBaseTexture.SetPixel(0, y, leftPixel);
            // Set the rightmost pixel to match the leftmost
            m_GeneratedBaseTexture.SetPixel(size - 1, y, leftPixel);
        }
        
        // CRITICAL: Explicitly ensure left and right edges match perfectly
        // This prevents mipmap selection issues at the UV seam (as per Unity forum solution)
        // The GPU can't determine mip level at the seam, so we ensure edges are identical
        for (int y = 0; y < size; y++)
        {
            // Get both edge pixels
            Color leftEdge = m_GeneratedBaseTexture.GetPixel(0, y);
            Color rightEdge = m_GeneratedBaseTexture.GetPixel(size - 1, y);
            
            // Average them to ensure perfect matching
            Color blendedEdge = (leftEdge + rightEdge) * 0.5f;
            
            // Set both edges to the same value
            m_GeneratedBaseTexture.SetPixel(0, y, blendedEdge);
            m_GeneratedBaseTexture.SetPixel(size - 1, y, blendedEdge);
        }
        
        m_GeneratedBaseTexture.Apply(false, false); // false = no mipmaps (critical for seam fix)
        m_GeneratedBaseTexture.wrapMode = TextureWrapMode.Repeat; // Use Repeat for seamless tiling
        m_GeneratedBaseTexture.filterMode = FilterMode.Point; // Point filter avoids mipmap selection issues
        
        // Apply to material - set BOTH _BaseMap and _MainTex (shader might use either)
        if (m_Material != null)
        {
            m_Material.SetTexture("_BaseMap", m_GeneratedBaseTexture);
            m_Material.SetTexture("_MainTex", m_GeneratedBaseTexture); // Also set _MainTex!
            m_Material.SetTextureScale("_BaseMap", Vector2.one);
            m_Material.SetTextureOffset("_BaseMap", Vector2.zero);
            m_Material.SetTextureScale("_MainTex", Vector2.one);
            m_Material.SetTextureOffset("_MainTex", Vector2.zero);
        }
    }
    
    void Update()
    {
        // Only run animation logic if in play mode OR if PlayAnimation is enabled in edit mode
        if (!Application.isPlaying && !PlayAnimation) return;
        
        // Handle rotation
        transform.Rotate(RotationAxis, RotationSpeed * Time.deltaTime, Space.Self);
        
        // Handle pulsing glow
        if (PulsingGlow && m_Material != null)
        {
            float glowFactor = (Mathf.Sin(Time.time * GlowPulseSpeed) + 1f) * 0.5f;
            glowFactor = Mathf.Lerp(0.5f, 1f, glowFactor); // Pulse between 50% and 100%
            
            Color emissionColor = m_BaseEmissionColor * glowFactor;
            m_Material.SetColor("_EmissionColor", emissionColor);
        }
    }
    
    void OnWillRenderObject()
    {
        // Only run in edit mode if PlayAnimation is enabled
        if (!Application.isPlaying && !PlayAnimation) return;
        
        // Update material right before rendering to ensure changes are visible (works in prefab preview)
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return;
        
        // Re-initialize if material is null (can happen in prefab preview)
        if (m_Material == null)
        {
            // Ensure seamless mesh is generated (in case it wasn't in Start)
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && (meshFilter.mesh == null || meshFilter.mesh.name != "SeamlessSphere"))
            {
                GenerateSeamlessSphereMesh();
            }
            
            m_Material = renderer.material;
            if (m_Material != null)
            {
                SetupMaterial();
            }
        }
        
        // Update opacity if it changed
        if (m_Material != null)
        {
            Color baseColor = m_Material.GetColor("_BaseColor");
            if (Mathf.Abs(baseColor.a - OverlayOpacity) > 0.001f)
            {
                baseColor.a = OverlayOpacity;
                m_Material.SetColor("_BaseColor", baseColor);
            }
        }
    }
    
    void OnDestroy()
    {
        if (m_Material != null)
        {
            Destroy(m_Material);
        }
        
        if (m_GeneratedBaseTexture != null)
        {
            Destroy(m_GeneratedBaseTexture);
        }
    }
}
