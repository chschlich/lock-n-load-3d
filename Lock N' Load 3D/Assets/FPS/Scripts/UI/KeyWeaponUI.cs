using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.FPS.Gameplay
{
    // displays the currently equipped key weapon and available keys
    // shows key name, color indicator, and hotkey numbers
    public class KeyWeaponUI : MonoBehaviour
    {
        [Header("References")]
        public KeyWeaponController WeaponController;
        
        [Header("Current Key Display")]
        public TextMeshProUGUI CurrentKeyNameText;
        public Image CurrentKeyColorIndicator;
        public TextMeshProUGUI CurrentKeyStatsText;
        
        void Start()
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
