using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.FPS.UI;

namespace Unity.FPS.Gameplay
{
    // DEPRECATED: This class has been replaced by KeyWeaponToolbar
    // It now automatically creates the new toolbar and disables itself
    // 
    // The new toolbar shows all 7 key slots at the bottom of the screen
    // with visual feedback for selected, owned, and locked states.
    [System.Obsolete("Use KeyWeaponToolbar instead. This component auto-migrates to the new system.")]
    public class KeyWeaponUI : MonoBehaviour
    {
        [Header("References")]
        public KeyWeaponController WeaponController;
        
        [Header("Current Key Display (DEPRECATED)")]
        public TextMeshProUGUI CurrentKeyNameText;
        public Image CurrentKeyColorIndicator;
        public TextMeshProUGUI CurrentKeyStatsText;
        
        [Header("Migration Settings")]
        [Tooltip("If true, automatically creates the new toolbar and disables this UI")]
        public bool AutoMigrateToToolbar = true;
        
        private bool m_HasMigrated = false;
        
        void Start()
        {
            if (AutoMigrateToToolbar)
            {
                MigrateToToolbar();
                return;
            }
            
            // Legacy behavior (if migration is disabled)
            LegacyStart();
        }
        
        void MigrateToToolbar()
        {
            if (m_HasMigrated) return;
            m_HasMigrated = true;
            
            // Check if toolbar already exists
            var existingToolbar = FindFirstObjectByType<KeyWeaponToolbar>();
            if (existingToolbar != null)
            {
                Debug.Log("KeyWeaponUI: Toolbar already exists, disabling old UI.");
                DisableOldUI();
                return;
            }
            
            // Find a suitable canvas (parent or any HUD canvas)
            Canvas targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
            {
                // Find any screen-space canvas
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                        canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        if (canvas.name.Contains("HUD") || canvas.name.Contains("UI"))
                        {
                            targetCanvas = canvas;
                            break;
                        }
                    }
                }
                
                // Fallback to any canvas
                if (targetCanvas == null && canvases.Length > 0)
                {
                    targetCanvas = canvases[0];
                }
            }
            
            if (targetCanvas == null)
            {
                Debug.LogError("KeyWeaponUI: Could not find a Canvas for the toolbar!");
                return;
            }
            
            // Create the new toolbar
            GameObject toolbarObj = new GameObject("KeyWeaponToolbar");
            toolbarObj.transform.SetParent(targetCanvas.transform, false);
            
            RectTransform rect = toolbarObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 25f);
            
            KeyWeaponToolbar toolbar = toolbarObj.AddComponent<KeyWeaponToolbar>();
            
            Debug.Log("KeyWeaponUI: Migrated to new KeyWeaponToolbar system.");
            
            // Disable old UI elements
            DisableOldUI();
        }
        
        void DisableOldUI()
        {
            // Hide old UI elements
            if (CurrentKeyNameText != null)
                CurrentKeyNameText.gameObject.SetActive(false);
            if (CurrentKeyColorIndicator != null)
                CurrentKeyColorIndicator.gameObject.SetActive(false);
            if (CurrentKeyStatsText != null)
                CurrentKeyStatsText.gameObject.SetActive(false);
            
            // Disable this component
            this.enabled = false;
            
            // HIDE THE ENTIRE OLD UI GAMEOBJECT (including background panels)
            this.gameObject.SetActive(false);
            
            Debug.Log("KeyWeaponUI: Old UI completely hidden.");
        }
        
        // ========== LEGACY CODE BELOW (kept for backwards compatibility) ==========
        
        void LegacyStart()
        {
            // try to find the weapon controller if not assigned
            if (WeaponController == null)
            {
                WeaponController = FindFirstObjectByType<KeyWeaponController>();
                if (WeaponController == null)
                {
                    Debug.LogError("KeyWeaponUI: Could not find KeyWeaponController in scene!");
                }
                else
                {
                    Debug.Log("KeyWeaponUI: Found KeyWeaponController automatically");
                }
            }
        }
        
        void Update()
        {
            if (m_HasMigrated) return;
            if (WeaponController == null) return;
            
            UpdateCurrentKeyDisplay();
        }
        
        void UpdateCurrentKeyDisplay()
        {
            var currentKey = WeaponController.CurrentKey;
            if (currentKey == null)
            {
                if (CurrentKeyNameText) CurrentKeyNameText.text = "NO KEY";
                if (CurrentKeyColorIndicator) CurrentKeyColorIndicator.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                if (CurrentKeyStatsText) CurrentKeyStatsText.text = "";
                return;
            }
            
            // Update name
            if (CurrentKeyNameText)
            {
                CurrentKeyNameText.text = currentKey.KeyName.ToUpper();
                CurrentKeyNameText.color = currentKey.KeyColor;
            }
            
            // Update color indicator
            if (CurrentKeyColorIndicator)
            {
                CurrentKeyColorIndicator.color = currentKey.KeyColor;
            }
            
            // Update stats
            if (CurrentKeyStatsText)
            {
                string stats = $"DMG: {currentKey.Damage:F0} | ";
                stats += $"RATE: {1f/currentKey.FireRate:F1}/s | ";
                stats += $"ABILITY: {currentKey.SpecialAbility}";
                CurrentKeyStatsText.text = stats;
            }
        }
    }
}
