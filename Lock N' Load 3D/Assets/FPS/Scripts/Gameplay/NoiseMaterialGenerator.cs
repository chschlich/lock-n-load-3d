using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// ScriptableObject that generates a dark purple noise texture material asset.
/// 
/// Usage:
/// 1. Right-click in Project window -> Create -> Materials -> Noise Material Generator
/// 2. Select the created asset in the Inspector
/// 3. Right-click the component header or use the three-dot menu -> "Generate Material"
/// 4. The material will be created in the same folder as the generator asset
/// 5. Manually assign the generated material to your distortion overlay's MeshRenderer component:
///    - Select the GameObject with the overlay (e.g., DistortionOverlay or DistortionFullOverlay)
///    - In the MeshRenderer component, drag the generated material to the Materials array
/// 6. Adjust opacity using the OverlayOpacity slider in the OverlayBase/OverlayFull/OverlayOutline component
/// </summary>
[CreateAssetMenu(fileName = "NoiseMaterialGenerator", menuName = "Materials/Noise Material Generator", order = 1)]
public class NoiseMaterialGenerator : ScriptableObject
{
    [Header("Material Settings")]
    [Tooltip("Name for the generated material asset")]
    public string MaterialName = "DarkPurple_NoiseMaterial";
    
    [Header("Noise Settings")]
    [Tooltip("Texture size (higher = more detail, but larger file size)")]
    [Range(256, 1024)]
    public int TextureSize = 512;
    
    [Tooltip("Noise detail level (number of noise layers)")]
    [Range(4, 10)]
    public int NoiseLayers = 8;
    
    [Header("Color Settings")]
    [Tooltip("Dark purple base color")]
    public Color BaseColor = new Color(0.3f, 0f, 0.5f, 1f);
    
    [Tooltip("Emission intensity multiplier")]
    [Range(1f, 10f)]
    public float EmissionIntensity = 2f;
    
    [ContextMenu("Generate Material")]
    private void GenerateMaterial()
    {
#if UNITY_EDITOR
        // Generate the noise texture
        Texture2D noiseTexture = GenerateNoiseTexture();
        
        // Load the shader (same as Projectile_purple.mat)
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            // Try loading by GUID if Find fails
            shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("0406db5a14f94604a8c57ccfbc9f3b46"));
        }
        
        if (shader == null)
        {
            Debug.LogError("Could not find the particle shader. Please ensure the shader exists.");
            return;
        }
        
        // Create the material
        Material material = new Material(shader);
        material.name = MaterialName;
        
        // Configure material properties
        material.SetColor("_BaseColor", BaseColor);
        material.SetColor("_Color", BaseColor);
        
        // Enable emission
        material.EnableKeyword("_EMISSION");
        material.SetFloat("_EmissionEnabled", 1f);
        Color emissionColor = BaseColor * EmissionIntensity;
        material.SetColor("_EmissionColor", emissionColor);
        
        // Set render queue to Transparent
        material.renderQueue = (int)RenderQueue.Transparent;
        
        // Configure blend modes for transparency
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Surface", 1); // Transparent surface
        
        // Enable transparent surface type
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        
        // Apply the noise texture
        material.SetTexture("_BaseMap", noiseTexture);
        material.SetTexture("_MainTex", noiseTexture);
        material.SetTextureScale("_BaseMap", Vector2.one);
        material.SetTextureOffset("_BaseMap", Vector2.zero);
        material.SetTextureScale("_MainTex", Vector2.one);
        material.SetTextureOffset("_MainTex", Vector2.zero);
        
        // Save the material as an asset
        string path = AssetDatabase.GetAssetPath(this);
        string directory = System.IO.Path.GetDirectoryName(path);
        string materialPath = System.IO.Path.Combine(directory, MaterialName + ".mat");
        
        // Create the texture asset first
        string texturePath = System.IO.Path.Combine(directory, MaterialName + "_Texture.asset");
        AssetDatabase.CreateAsset(noiseTexture, texturePath);
        
        // Then create the material asset
        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Material generated successfully at: {materialPath}");
        Debug.Log($"Texture generated successfully at: {texturePath}");
        
        // Select the generated material in the Project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = material;
#else
        Debug.LogWarning("Material generation is only available in the Unity Editor.");
#endif
    }
    
    private Texture2D GenerateNoiseTexture()
    {
        int size = TextureSize;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = MaterialName + "_NoiseTexture";
        
        // Generate detailed noise using multiple Perlin noise layers
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                
                // For seamless tiling, use toroidal wrapping
                float centerU = 0.5f;
                float centerV = 0.5f;
                
                float dx = u - centerU;
                float dy = v - centerV;
                
                // Wrap horizontally for seamless tiling
                if (dx > 0.5f) dx = dx - 1f;
                else if (dx < -0.5f) dx = dx + 1f;
                
                float angle = Mathf.Atan2(dy, dx);
                
                // Generate multiple noise layers for detailed appearance
                float noise = 0f;
                float totalWeight = 0f;
                
                // Create 6-8 noise layers at different frequencies
                for (int i = 0; i < NoiseLayers; i++)
                {
                    float frequency = Mathf.Pow(2f, i) * 4f; // 4, 8, 16, 32, 64, 128, 256, 512...
                    float weight = 1f / Mathf.Pow(2f, i); // Decreasing weight for higher frequencies
                    
                    // Use Perlin noise with different offsets for each layer
                    float offsetX = i * 50f;
                    float offsetY = i * 75f;
                    float layerNoise = Mathf.PerlinNoise(u * frequency + offsetX, v * frequency + offsetY);
                    
                    // Add some angular variation
                    float angularNoise = Mathf.Sin(angle * (i + 1) * 3f + u * frequency * 2f) * 0.5f + 0.5f;
                    layerNoise = (layerNoise + angularNoise) * 0.5f;
                    
                    noise += layerNoise * weight;
                    totalWeight += weight;
                }
                
                // Normalize the noise
                noise /= totalWeight;
                
                // Apply to base color with variation
                Color pixelColor = BaseColor;
                float brightness = 0.7f + noise * 0.3f; // 70% to 100% brightness variation
                pixelColor.r *= brightness;
                pixelColor.b *= brightness;
                
                // Add slight color variation
                float colorShift = (noise - 0.5f) * 0.15f;
                pixelColor.r = Mathf.Clamp01(pixelColor.r + colorShift);
                pixelColor.b = Mathf.Clamp01(pixelColor.b - colorShift * 0.3f);
                
                // Full alpha in texture (opacity controlled by material)
                pixelColor.a = 1.0f;
                
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        // Make texture seamless by blending edges
        for (int y = 0; y < size; y++)
        {
            Color leftPixel = texture.GetPixel(1, y);
            texture.SetPixel(0, y, leftPixel);
            texture.SetPixel(size - 1, y, leftPixel);
        }
        
        // Ensure perfect edge matching
        for (int y = 0; y < size; y++)
        {
            Color leftEdge = texture.GetPixel(0, y);
            Color rightEdge = texture.GetPixel(size - 1, y);
            Color blendedEdge = (leftEdge + rightEdge) * 0.5f;
            texture.SetPixel(0, y, blendedEdge);
            texture.SetPixel(size - 1, y, blendedEdge);
        }
        
        texture.Apply(false, false); // No mipmaps for seamless texture
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        
        return texture;
    }
}


