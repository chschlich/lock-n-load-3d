using UnityEngine;

namespace Unity.FPS.UI
{
    /// <summary>
    /// Automatically initializes the KeyWeaponToolbar on the GameHUD.
    /// Add this component to a GameObject in the scene (e.g., GameManager).
    /// It will find or create the toolbar on the HUD Canvas.
    /// </summary>
    public class KeyWeaponToolbarInitializer : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, creates toolbar automatically on Start")]
        public bool AutoInitialize = true;
        
        private KeyWeaponToolbar m_Toolbar;
        
        void Start()
        {
            if (AutoInitialize)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// Create and initialize the key weapon toolbar
        /// </summary>
        public void Initialize()
        {
            // Check if toolbar already exists
            m_Toolbar = FindFirstObjectByType<KeyWeaponToolbar>();
            if (m_Toolbar != null)
            {
                Debug.Log("KeyWeaponToolbarInitializer: Toolbar already exists, skipping creation.");
                return;
            }
            
            // Find the HUD Canvas
            Canvas hudCanvas = FindHUDCanvas();
            if (hudCanvas == null)
            {
                Debug.LogError("KeyWeaponToolbarInitializer: Could not find HUD Canvas!");
                return;
            }
            
            // Create the toolbar object
            GameObject toolbarObj = new GameObject("KeyWeaponToolbar");
            toolbarObj.transform.SetParent(hudCanvas.transform, false);
            
            // Add RectTransform (required for UI)
            RectTransform rect = toolbarObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 25f);
            
            // Add the toolbar component
            m_Toolbar = toolbarObj.AddComponent<KeyWeaponToolbar>();
            
            Debug.Log("KeyWeaponToolbarInitializer: Created KeyWeaponToolbar on HUD Canvas.");
        }
        
        /// <summary>
        /// Find the HUD Canvas in the scene
        /// </summary>
        private Canvas FindHUDCanvas()
        {
            // First, try to find by common HUD names
            string[] hudNames = { "HUD", "GameHUD", "UICanvas", "HUDCanvas" };
            
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            
            foreach (var canvas in allCanvases)
            {
                // Check for HUD-named canvas
                foreach (var name in hudNames)
                {
                    if (canvas.name.Contains(name))
                    {
                        // Make sure it's a screen-space canvas
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || 
                            canvas.renderMode == RenderMode.ScreenSpaceCamera)
                        {
                            return canvas;
                        }
                    }
                }
            }
            
            // Fallback: find any screen-space overlay canvas
            foreach (var canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvas;
                }
            }
            
            // Last resort: find any canvas
            if (allCanvases.Length > 0)
            {
                return allCanvases[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Get the toolbar instance (creates if needed)
        /// </summary>
        public KeyWeaponToolbar GetToolbar()
        {
            if (m_Toolbar == null)
            {
                Initialize();
            }
            return m_Toolbar;
        }
    }
}


