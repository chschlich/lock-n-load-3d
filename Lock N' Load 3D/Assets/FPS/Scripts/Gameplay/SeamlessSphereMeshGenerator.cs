using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple utility script that generates a seamless sphere mesh.
/// 
/// HOW TO USE:
/// 1. Add this script to a GameObject that has a MeshFilter component
/// 2. Click the "Generate Mesh" button in the Inspector, OR
/// 3. Right-click the component header -> "Generate Seamless Sphere Mesh"
/// 4. The mesh will be assigned to the MeshFilter
/// 5. Optionally click "Save Mesh as Asset" to save it as a .asset file in your project
/// </summary>
public class SeamlessSphereMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Tooltip("Number of segments around the sphere (higher = smoother)")]
    [Range(8, 64)]
    public int Segments = 32;
    
    [Tooltip("Number of rings from top to bottom (higher = smoother)")]
    [Range(4, 32)]
    public int Rings = 16;
    
    [Header("Save Options")]
    [Tooltip("Save the generated mesh as an asset file in the project")]
    public bool SaveAsAsset = false;
    
    [Tooltip("Path to save the mesh asset (relative to Assets folder)")]
    public string SavePath = "Assets/FPS/Art/Meshes/SeamlessSphere.asset";
    
    private Mesh m_GeneratedMesh;
    
    [ContextMenu("Generate Seamless Sphere Mesh")]
    public void GenerateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter component found! Please add a MeshFilter component first.");
            return;
        }
        
        // Create a new mesh
        Mesh mesh = new Mesh();
        mesh.name = "SeamlessSphere";
        
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        
        // Generate vertices with seamless UVs
        // Key: Use longitude/latitude mapping but ensure U wraps from 0 to 1 continuously
        for (int ring = 0; ring <= Rings; ring++)
        {
            float theta = ring * Mathf.PI / Rings; // 0 to PI (vertical)
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            
            for (int seg = 0; seg <= Segments; seg++)
            {
                float phi = seg * 2f * Mathf.PI / Segments; // 0 to 2PI (horizontal)
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
                float u = seg == Segments ? 0f : (float)seg / Segments; // Last segment uses U=0 for seamless wrap
                float v = (float)ring / Rings;
                uvs.Add(new Vector2(u, v));
            }
        }
        
        // Generate triangles
        for (int ring = 0; ring < Rings; ring++)
        {
            for (int seg = 0; seg < Segments; seg++)
            {
                int current = ring * (Segments + 1) + seg;
                int next = current + Segments + 1;
                
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
        
        // Assign to mesh filter
        meshFilter.sharedMesh = mesh;
        m_GeneratedMesh = mesh;
        
        Debug.Log($"Seamless sphere mesh generated! Vertices: {vertices.Count}, Triangles: {triangles.Count / 3}");
        
        // Save as asset if requested
        if (SaveAsAsset)
        {
            SaveMeshAsAsset();
        }
    }
    
    [ContextMenu("Save Mesh as Asset")]
    public void SaveMeshAsAsset()
    {
#if UNITY_EDITOR
        if (m_GeneratedMesh == null)
        {
            Debug.LogWarning("No mesh generated yet! Generate the mesh first.");
            return;
        }
        
        // Ensure the directory exists
        string directory = System.IO.Path.GetDirectoryName(SavePath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Create a new mesh and copy all data (can't save runtime mesh directly)
        Mesh meshToSave = new Mesh();
        meshToSave.name = "SeamlessSphere";
        meshToSave.vertices = m_GeneratedMesh.vertices;
        meshToSave.triangles = m_GeneratedMesh.triangles;
        meshToSave.uv = m_GeneratedMesh.uv;
        meshToSave.normals = m_GeneratedMesh.normals;
        meshToSave.bounds = m_GeneratedMesh.bounds;
        
        AssetDatabase.CreateAsset(meshToSave, SavePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Mesh saved as asset at: {SavePath}");
        
        // Select the saved mesh in the Project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = meshToSave;
#else
        Debug.LogWarning("Saving meshes as assets is only available in the Unity Editor.");
#endif
    }
}

