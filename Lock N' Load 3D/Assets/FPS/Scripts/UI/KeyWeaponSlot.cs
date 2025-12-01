using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.FPS.UI
{
    /// <summary>
    /// Individual slot in the key weapon toolbar.
    /// Displays a key icon with a glowing version that fades in when selected.
    /// Scales up when selected.
    /// </summary>
    public class KeyWeaponSlot : MonoBehaviour
    {
        [Header("UI References")]
        public Image BackgroundImage;
        public Image SelectionFrame;
        public Image KeyIconImage;        // Normal key sprite (always visible)
        public Image KeyIconGlowImage;    // Glow sprite (fades in behind)
        public Image LockedOverlay;
        public TextMeshProUGUI HotkeyText;

        [Header("Visual Settings")]
        public float KeyIconRotation = 50f;  // Rotation angle for key icon
        public float UnselectedOpacity = 0.425f;  // 42.5% opacity (10% less than before)
        public float LockedOpacity = 0.3f;
        public float GlowFadeSpeed = 8f;  // Speed of glow fade
        public float TransitionSpeed = 10f;  // Animation speed
        
        // State
        private int m_SlotIndex;
        private bool m_IsOwned = false;
        private bool m_IsSelected = false;
        private Color m_KeyColor = Color.white;
        private CanvasGroup m_CanvasGroup;
        
        // Animation targets
        private float m_TargetIconScale = 1f;
        private float m_TargetOpacity = 1f;
        private float m_TargetGlowAlpha = 0f;

        void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        void Update()
        {
            // Recalculate targets every frame to pick up Inspector changes
            RecalculateTargets();
            
            // Smooth icon scale transition
            if (KeyIconImage != null)
            {
                float currentScale = KeyIconImage.transform.localScale.x;
                float newScale = Mathf.Lerp(currentScale, m_TargetIconScale, Time.deltaTime * TransitionSpeed);
                KeyIconImage.transform.localScale = Vector3.one * newScale;
                
                // Glow scales with the icon
                if (KeyIconGlowImage != null)
                {
                    KeyIconGlowImage.transform.localScale = Vector3.one * newScale;
                }
            }
            
            // Smooth opacity transition
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(m_CanvasGroup.alpha, m_TargetOpacity, Time.deltaTime * TransitionSpeed);
            }
            
            // Smooth glow fade
            if (KeyIconGlowImage != null)
            {
                Color glowColor = KeyIconGlowImage.color;
                glowColor.a = Mathf.Lerp(glowColor.a, m_TargetGlowAlpha, Time.deltaTime * GlowFadeSpeed);
                KeyIconGlowImage.color = glowColor;
            }
        }
        
        /// <summary>
        /// Recalculate target values based on current state
        /// </summary>
        private void RecalculateTargets()
        {
            // Determine scale target based on selection
            if (m_IsSelected && m_IsOwned)
            {
                // Selected: 1.5x scale (50% larger)
                m_TargetIconScale = 1.5f;
            }
            else if (m_IsOwned)
            {
                // Non-selected but owned: 1.2x scale (20% larger than default)
                m_TargetIconScale = 1.2f;
            }
            else
            {
                // Locked: 1.0x scale
                m_TargetIconScale = 1f;
            }
        }

        /// <summary>
        /// Initialize the slot with its index
        /// </summary>
        public void Initialize(int index)
        {
            m_SlotIndex = index;
            
            // Apply rotation to key icon
            if (KeyIconImage != null)
            {
                KeyIconImage.transform.localRotation = Quaternion.Euler(0f, 0f, KeyIconRotation);
            }
            
            // Apply same rotation to glow
            if (KeyIconGlowImage != null)
            {
                KeyIconGlowImage.transform.localRotation = Quaternion.Euler(0f, 0f, KeyIconRotation);
                // Start with glow invisible
                Color glowColor = KeyIconGlowImage.color;
                glowColor.a = 0f;
                KeyIconGlowImage.color = glowColor;
            }
        }

        /// <summary>
        /// Set the key color (for reference)
        /// </summary>
        public void SetKeyColor(Color color)
        {
            m_KeyColor = color;
            UpdateVisuals();
        }

        /// <summary>
        /// Set the key icon sprites (normal and glow versions)
        /// </summary>
        public void SetKeyIcon(Sprite normalSprite, Sprite glowSprite)
        {
            if (KeyIconImage != null && normalSprite != null)
            {
                KeyIconImage.sprite = normalSprite;
            }
            
            if (KeyIconGlowImage != null && glowSprite != null)
            {
                KeyIconGlowImage.sprite = glowSprite;
            }
        }

        /// <summary>
        /// Set whether this slot's weapon is owned/available
        /// </summary>
        public void SetOwned(bool owned)
        {
            m_IsOwned = owned;
            UpdateVisuals();
        }

        /// <summary>
        /// Set whether this slot is currently selected
        /// </summary>
        public void SetSelected(bool selected)
        {
            m_IsSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Update all visual elements based on current state
        /// </summary>
        private void UpdateVisuals()
        {
            // Show/hide locked overlay
            if (LockedOverlay != null)
            {
                LockedOverlay.gameObject.SetActive(!m_IsOwned);
            }
            
            // Show/hide selection frame (white border)
            if (SelectionFrame != null)
            {
                SelectionFrame.gameObject.SetActive(m_IsSelected && m_IsOwned);
            }
            
            // Key icon color - white for custom sprites
            if (KeyIconImage != null)
            {
                if (m_IsOwned)
                {
                    KeyIconImage.color = Color.white;
                }
                else
                {
                    // Dimmed for locked
                    KeyIconImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
            }
            
            // Background color
            if (BackgroundImage != null)
            {
                BackgroundImage.color = new Color(0f, 0f, 0f, 0.5f);
            }
            
            // Hotkey text color
            if (HotkeyText != null)
            {
                HotkeyText.color = m_IsOwned ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
            
            // Set opacity and glow targets (scale handled in RecalculateTargets)
            if (m_IsSelected && m_IsOwned)
            {
                m_TargetOpacity = 1f;
                m_TargetGlowAlpha = 0.6f;  // Fade in glow at 60% opacity
            }
            else if (m_IsOwned)
            {
                m_TargetOpacity = UnselectedOpacity;  // 25% less opacity
                m_TargetGlowAlpha = 0f;  // Fade out glow when not selected
            }
            else
            {
                m_TargetOpacity = LockedOpacity;
                m_TargetGlowAlpha = 0f;
            }
        }

        public int GetIndex() => m_SlotIndex;
        public bool IsSelected() => m_IsSelected;
        public bool IsOwned() => m_IsOwned;
        public Color GetKeyColor() => m_KeyColor;
    }
}
