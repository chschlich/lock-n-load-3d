using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.FPS.Gameplay;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.FPS.UI
{
    /// <summary>
    /// Main toolbar controller for the key weapon hotbar.
    /// Displays all 7 key slots at the bottom-center of the screen.
    /// Creates UI programmatically - no prefab required.
    /// 
    /// REPLACES: KeyWeaponUI.cs and KeyWeaponHUD prefab
    /// Add this component to a Canvas or UI element to create the toolbar.
    /// </summary>
    public class KeyWeaponToolbar : MonoBehaviour
    {
        [Header("Old UI (will be disabled)")]
        [Tooltip("Reference to old KeyWeaponUI to disable (optional - will auto-find)")]
        public MonoBehaviour OldKeyWeaponUI;
        
        [Header("Layout Settings")]
        [Tooltip("Number of slots to create")]
        public int SlotCount = 6;
        
        [Tooltip("Size of each slot in pixels")]
        public Vector2 SlotSize = new Vector2(50f, 50f);  // 7% smaller than original 54
        
        [Tooltip("Spacing between slots")]
        public float SlotSpacing = 6f;
        
        [Tooltip("Distance from bottom of screen")]
        public float BottomOffset = 25f;
        
        [Tooltip("Horizontal offset (negative = left, positive = right)")]
        public float HorizontalOffset = -50f;  // Slightly left to account for weapon model on right
        
        [Tooltip("Anchor position: 0=Left, 0.5=Center, 1=Right")]
        [Range(0f, 1f)]
        public float HorizontalAnchor = 0.5f;
        
        [Header("Visual Settings")]
        [Tooltip("Corner radius for slot backgrounds (using built-in sprite)")]
        public float CornerRadius = 8f;
        
        [Tooltip("Selection frame thickness")]
        public float SelectionFrameThickness = 3f;
        
        [Header("Default Key Icons (for unowned slots)")]
        [Tooltip("Sprites for each key slot when not owned. Assign in order: Yellow, Pink, Red, Purple, Green, Blue, Orange")]
        public Sprite[] DefaultKeyIcons;
        
        [Header("Font Settings")]
        [Tooltip("Font asset for hotkey numbers (leave null to use default)")]
        public TMP_FontAsset HotkeyFont;
        
        // Runtime references
        private KeyWeaponController m_KeyWeaponController;
        private List<KeyWeaponSlot> m_Slots = new List<KeyWeaponSlot>();
        private int m_LastSelectedIndex = -1;
        private RectTransform m_SlotsContainer;
        private bool m_IsInitialized = false;
        
        void Start()
        {
            // Disable old KeyWeaponUI if it exists
            DisableOldUI();
            
            // Ensure EventSystem exists (required for UI hover/click events)
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("KeyWeaponToolbar: Created EventSystem for UI interaction");
            }
            
            // Find the KeyWeaponController
            m_KeyWeaponController = FindFirstObjectByType<KeyWeaponController>();
            
            if (m_KeyWeaponController == null)
            {
                Debug.LogWarning("KeyWeaponToolbar: KeyWeaponController not found!");
            }
            
            // Build the UI
            BuildToolbarUI();
            
            // Initial update
            UpdateAllSlots();
            
            m_IsInitialized = true;
        }
        
        /// <summary>
        /// Disable the old KeyWeaponUI and KeyWeaponHUD to prevent duplicates
        /// Only hides KEY-RELATED UI elements, preserves everything else
        /// </summary>
        private void DisableOldUI()
        {
            // Disable explicitly assigned old UI
            if (OldKeyWeaponUI != null)
            {
                OldKeyWeaponUI.enabled = false;
                Debug.Log("KeyWeaponToolbar: Disabled old KeyWeaponUI (assigned)");
            }
            
            // Auto-find and disable any KeyWeaponUI components (but not their GameObjects)
            var oldUIs = FindObjectsByType<KeyWeaponUI>(FindObjectsSortMode.None);
            foreach (var oldUI in oldUIs)
            {
                oldUI.enabled = false;
                Debug.Log($"KeyWeaponToolbar: Disabled old KeyWeaponUI script on {oldUI.gameObject.name}");
            }
            
            // Only hide objects that are specifically KEY-related by name
            // This preserves ammo counters, health bars, crosshairs, etc.
            string[] keyRelatedNames = new string[]
            {
                "CurrentKeyText", "CurrentKeyNameText", "KeyNameText",
                "ColorIndicator", "CurrentKeyColorIndicator", "KeyColorIndicator",
                "KeyStatsText", "CurrentKeyStatsText",
                "KeyWeaponDisplay", "KeyDisplay",
                "NoKeyEquipped", "NoKeyText"
            };
            
            // Search in children of this object
            foreach (Transform child in transform)
            {
                // Skip our created elements
                if (child.name.StartsWith("KeySlot_")) continue;
                if (child.name == "SlotsContainer") continue;
                if (child.name == "ToolbarBackground") continue;
                
                // Only hide if name matches key-related patterns
                foreach (string keyName in keyRelatedNames)
                {
                    if (child.name.Contains(keyName) || child.name == keyName)
                    {
                        child.gameObject.SetActive(false);
                        Debug.Log($"KeyWeaponToolbar: Hid old key UI element: {child.name}");
                        break;
                    }
                }
            }
            
            // Also search globally for standalone key UI objects
            var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == transform) continue;
                
                foreach (string keyName in keyRelatedNames)
                {
                    if (t.name == keyName || t.name.Contains(keyName))
                    {
                        t.gameObject.SetActive(false);
                        Debug.Log($"KeyWeaponToolbar: Hid global key UI element: {t.name}");
                        break;
                    }
                }
            }
        }
        
        void Update()
        {
            if (!m_IsInitialized) return;
            
            if (m_KeyWeaponController == null)
            {
                m_KeyWeaponController = FindFirstObjectByType<KeyWeaponController>();
                if (m_KeyWeaponController == null) return;
            }
            
            // Update position based on offset values (allows live editing in Inspector)
            UpdateToolbarPosition();
            
            // Check if selection changed
            int currentIndex = m_KeyWeaponController.CurrentKeyIndex;
            if (currentIndex != m_LastSelectedIndex)
            {
                m_LastSelectedIndex = currentIndex;
                UpdateSelection();
            }
            
            // Update ownership state (in case player picks up new keys)
            UpdateOwnership();
        }
        
        /// <summary>
        /// Update toolbar position based on offset settings
        /// </summary>
        private void UpdateToolbarPosition()
        {
            RectTransform mainRect = GetComponent<RectTransform>();
            if (mainRect == null) return;
            
            // Update anchors
            mainRect.anchorMin = new Vector2(HorizontalAnchor, 0f);
            mainRect.anchorMax = new Vector2(HorizontalAnchor, 0f);
            mainRect.pivot = new Vector2(HorizontalAnchor, 0f);
            
            // Update position
            mainRect.anchoredPosition = new Vector2(HorizontalOffset, BottomOffset);
        }
        
        /// <summary>
        /// Build the entire toolbar UI programmatically
        /// </summary>
        private void BuildToolbarUI()
        {
            // Setup the main container (this object)
            RectTransform mainRect = GetComponent<RectTransform>();
            if (mainRect == null)
            {
                mainRect = gameObject.AddComponent<RectTransform>();
            }
            
            // Position at bottom center
            mainRect.anchorMin = new Vector2(0.5f, 0f);
            mainRect.anchorMax = new Vector2(0.5f, 0f);
            mainRect.pivot = new Vector2(0.5f, 0f);
            
            float totalWidth = (SlotSize.x * SlotCount) + (SlotSpacing * (SlotCount - 1));
            mainRect.sizeDelta = new Vector2(totalWidth, SlotSize.y + 10f);
            
            // Set anchor based on HorizontalAnchor setting
            mainRect.anchorMin = new Vector2(HorizontalAnchor, 0f);
            mainRect.anchorMax = new Vector2(HorizontalAnchor, 0f);
            mainRect.pivot = new Vector2(HorizontalAnchor, 0f);
            mainRect.anchoredPosition = new Vector2(HorizontalOffset, BottomOffset);
            
            // Create slots container with HorizontalLayoutGroup
            GameObject containerObj = new GameObject("SlotsContainer");
            containerObj.transform.SetParent(transform, false);
            
            m_SlotsContainer = containerObj.AddComponent<RectTransform>();
            m_SlotsContainer.anchorMin = Vector2.zero;
            m_SlotsContainer.anchorMax = Vector2.one;
            m_SlotsContainer.sizeDelta = Vector2.zero;
            m_SlotsContainer.anchoredPosition = Vector2.zero;
            
            HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            // Create each slot
            m_Slots.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                CreateSlot(i);
            }
        }
        
        /// <summary>
        /// Create a single slot with all its UI elements
        /// </summary>
        private void CreateSlot(int index)
        {
            // Main slot object
            GameObject slotObj = new GameObject($"KeySlot_{index + 1}");
            slotObj.transform.SetParent(m_SlotsContainer, false);
            
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = SlotSize;
            
            KeyWeaponSlot slot = slotObj.AddComponent<KeyWeaponSlot>();
            
            // Add an invisible Image to the slot itself to catch mouse events
            Image slotRaycastImage = slotObj.AddComponent<Image>();
            slotRaycastImage.color = new Color(0, 0, 0, 0);  // Fully transparent
            slotRaycastImage.raycastTarget = true;  // MUST be enabled for hover detection
            
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);
            bgImage.raycastTarget = true;  // Enable raycast for hover detection
            slot.BackgroundImage = bgImage;
            
            // Selection Frame - white border using Outline component
            // We'll add the outline to the background image when selected
            GameObject frameObj = new GameObject("SelectionFrame");
            frameObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform frameRect = frameObj.AddComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.sizeDelta = Vector2.zero;
            frameRect.anchoredPosition = Vector2.zero;
            
            // Create a border using 4 thin white rectangles
            float borderThickness = 2f;
            
            // Top border
            CreateBorderEdge(frameObj.transform, "TopBorder", 
                new Vector2(0, 1), new Vector2(1, 1), 
                new Vector2(0, -borderThickness/2), new Vector2(0, borderThickness));
            
            // Bottom border  
            CreateBorderEdge(frameObj.transform, "BottomBorder",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, borderThickness/2), new Vector2(0, borderThickness));
            
            // Left border
            CreateBorderEdge(frameObj.transform, "LeftBorder",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(borderThickness/2, 0), new Vector2(borderThickness, 0));
            
            // Right border
            CreateBorderEdge(frameObj.transform, "RightBorder",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-borderThickness/2, 0), new Vector2(borderThickness, 0));
            
            Image frameImage = frameObj.AddComponent<Image>();
            frameImage.color = new Color(1, 1, 1, 0); // Invisible container
            frameImage.raycastTarget = false;
            frameObj.SetActive(false);
            slot.SelectionFrame = frameImage;
            
            // Key Icon (using a simple shape as placeholder)
            GameObject iconObj = new GameObject("KeyIcon");
            iconObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(SlotSize.x * 0.65f, SlotSize.y * 0.65f);  // 65% of slot size
            iconRect.anchoredPosition = Vector2.zero;
            
            Image iconImage = iconObj.AddComponent<Image>();
            // Use Unity's built-in Knob sprite as a placeholder key shape
            iconImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            slot.KeyIconImage = iconImage;
            
            // Glow Icon Layer (behind the normal icon)
            GameObject glowIconObj = new GameObject("KeyIconGlow");
            glowIconObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform glowIconRect = glowIconObj.AddComponent<RectTransform>();
            glowIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowIconRect.sizeDelta = new Vector2(SlotSize.x * 0.65f, SlotSize.y * 0.65f);  // Same size as normal icon
            glowIconRect.anchoredPosition = Vector2.zero;
            
            Image glowIconImage = glowIconObj.AddComponent<Image>();
            glowIconImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            glowIconImage.color = new Color(1f, 1f, 1f, 0f);  // Start transparent
            glowIconImage.raycastTarget = false;
            slot.KeyIconGlowImage = glowIconImage;
            
            // Position glow behind the normal icon
            int iconSiblingIndex = iconObj.transform.GetSiblingIndex();
            glowIconObj.transform.SetSiblingIndex(iconSiblingIndex);  // Pushes icon forward
            
            // Locked Overlay
            GameObject lockedObj = new GameObject("LockedOverlay");
            lockedObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform lockedRect = lockedObj.AddComponent<RectTransform>();
            lockedRect.anchorMin = Vector2.zero;
            lockedRect.anchorMax = Vector2.one;
            lockedRect.sizeDelta = Vector2.zero;
            lockedRect.anchoredPosition = Vector2.zero;
            
            Image lockedImage = lockedObj.AddComponent<Image>();
            lockedImage.color = new Color(0f, 0f, 0f, 0.6f);
            lockedImage.raycastTarget = false;
            lockedObj.SetActive(false);
            slot.LockedOverlay = lockedImage;
            
            // Hotkey Number Text
            GameObject textObj = new GameObject("HotkeyText");
            textObj.transform.SetParent(slotObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(0f, 0f);
            textRect.pivot = new Vector2(0f, 0f);
            textRect.sizeDelta = new Vector2(20f, 16f);
            textRect.anchoredPosition = new Vector2(3f, 2f);
            
            TextMeshProUGUI hotkeyText = textObj.AddComponent<TextMeshProUGUI>();
            hotkeyText.text = (index + 1).ToString();
            hotkeyText.fontSize = 12f;
            hotkeyText.fontStyle = FontStyles.Bold;
            hotkeyText.color = Color.white;
            hotkeyText.alignment = TextAlignmentOptions.BottomLeft;
            hotkeyText.raycastTarget = false;
            
            if (HotkeyFont != null)
            {
                hotkeyText.font = HotkeyFont;
            }
            
            slot.HotkeyText = hotkeyText;
            
            // Initialize the slot
            slot.Initialize(index);
            
            // Set default icon if available
            if (DefaultKeyIcons != null && index < DefaultKeyIcons.Length && DefaultKeyIcons[index] != null)
            {
                Sprite normalSprite = DefaultKeyIcons[index];
                Sprite glowSprite = FindGlowSprite(normalSprite);
                slot.SetKeyIcon(normalSprite, glowSprite);
            }
            
            m_Slots.Add(slot);
        }
        
        /// <summary>
        /// Helper to create a border edge (thin white rectangle)
        /// </summary>
        private void CreateBorderEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta)
        {
            GameObject edgeObj = new GameObject(name);
            edgeObj.transform.SetParent(parent, false);
            
            RectTransform edgeRect = edgeObj.AddComponent<RectTransform>();
            edgeRect.anchorMin = anchorMin;
            edgeRect.anchorMax = anchorMax;
            edgeRect.pivot = new Vector2(0.5f, 0.5f);
            edgeRect.anchoredPosition = position;
            edgeRect.sizeDelta = sizeDelta;
            
            Image edgeImage = edgeObj.AddComponent<Image>();
            edgeImage.color = Color.white;
            edgeImage.raycastTarget = false;
        }
        
        /// <summary>
        /// Update all slots with current state
        /// </summary>
        private void UpdateAllSlots()
        {
            UpdateOwnership();
            UpdateSelection();
        }
        
        /// <summary>
        /// Update which slots show as owned based on player inventory
        /// </summary>
        private void UpdateOwnership()
        {
            if (m_KeyWeaponController == null) return;
            
            for (int i = 0; i < m_Slots.Count; i++)
            {
                // Check if player owns this key
                bool isOwned = false;
                KeyWeaponData keyData = m_KeyWeaponController.GetKeyAt(i);
                
                if (keyData != null)
                {
                    isOwned = true;
                    // Update color from actual key data if available
                    m_Slots[i].SetKeyColor(keyData.KeyColor);
                    
                    // Load both normal and glow sprites from KeyWeaponData
                    if (keyData.KeyIcon != null)
                    {
                        m_Slots[i].SetKeyIcon(keyData.KeyIcon, keyData.KeyIconGlow);
                    }
                }
                else
                {
                    // Use default icon for unowned slots
                    if (DefaultKeyIcons != null && i < DefaultKeyIcons.Length && DefaultKeyIcons[i] != null)
                    {
                        Sprite normalSprite = DefaultKeyIcons[i];
                        Sprite glowSprite = FindGlowSprite(normalSprite);
                        
                        m_Slots[i].SetKeyIcon(normalSprite, glowSprite);
                    }
                }
                
                m_Slots[i].SetOwned(isOwned);
            }
        }
        
        /// <summary>
        /// Update which slot is shown as selected
        /// </summary>
        private void UpdateSelection()
        {
            if (m_KeyWeaponController == null) return;
            
            int selectedIndex = m_KeyWeaponController.CurrentKeyIndex;
            
            for (int i = 0; i < m_Slots.Count; i++)
            {
                m_Slots[i].SetSelected(i == selectedIndex);
            }
        }
        
        /// <summary>
        /// Manually set a slot's color (useful for customization)
        /// </summary>
        public void SetSlotColor(int index, Color color)
        {
            if (index >= 0 && index < m_Slots.Count)
            {
                m_Slots[index].SetKeyColor(color);
            }
        }
        
        /// <summary>
        /// Get the slot at a specific index
        /// </summary>
        public KeyWeaponSlot GetSlot(int index)
        {
            if (index >= 0 && index < m_Slots.Count)
            {
                return m_Slots[index];
            }
            return null;
        }
        
        /// <summary>
        /// Find the glow sprite that matches a normal sprite
        /// Converts "Yellow-key-enhanced" to "Yellow-key-glow" and loads from effects folder
        /// </summary>
        private Sprite FindGlowSprite(Sprite normalSprite)
        {
            if (normalSprite == null) return null;
            
            // Get the sprite name and try to find the glow version
            string normalName = normalSprite.name;
            
            // Replace "-enhanced" with "-glow" or just append "-glow"
            string glowName = normalName.Replace("-enhanced", "-glow");
            if (glowName == normalName)
            {
                // Didn't find "-enhanced", just append "-glow"
                glowName = normalName + "-glow";
            }
            
            // Try to load from Resources (if sprites are in Resources folder)
            // Otherwise try to find it by searching all loaded sprites
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite sprite in allSprites)
            {
                if (sprite.name == glowName)
                {
                    return sprite;
                }
            }
            
            // If not found, return null and log warning
            Debug.LogWarning($"KeyWeaponToolbar: Could not find glow sprite '{glowName}' for normal sprite '{normalName}'");
            return null;
        }
    }
}

