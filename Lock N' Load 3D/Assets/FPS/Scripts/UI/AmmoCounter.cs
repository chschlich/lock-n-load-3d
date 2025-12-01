using TMPro;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    [RequireComponent(typeof(FillBarColorChange))]
    public class AmmoCounter : MonoBehaviour
    {
        [Tooltip("CanvasGroup to fade the ammo UI")]
        public CanvasGroup CanvasGroup;

        [Tooltip("Image for the weapon icon")] public Image WeaponImage;

        [Tooltip("Image component for the background")]
        public Image AmmoBackgroundImage;

        [Tooltip("Image component to display fill ratio")]
        public Image AmmoFillImage;

        [Tooltip("Text for Weapon index")] 
        public TextMeshProUGUI WeaponIndexText;

        [Tooltip("Text for Bullet Counter")] 
        public TextMeshProUGUI BulletCounter;

        [Tooltip("Reload Text for Weapons with physical bullets")]
        public RectTransform Reload;

        [Header("Selection")] [Range(0, 1)] [Tooltip("Opacity when weapon not selected")]
        public float UnselectedOpacity = 0.5f;

        [Tooltip("Scale when weapon not selected")]
        public Vector3 UnselectedScale = Vector3.one * 0.8f;

        [Tooltip("Root for the control keys")] public GameObject ControlKeysRoot;

        [Header("Feedback")] [Tooltip("Component to animate the color when empty or full")]
        public FillBarColorChange FillBarColorChange;

        [Tooltip("Sharpness for the fill ratio movements")]
        public float AmmoFillMovementSharpness = 20f;

        public int WeaponCounterIndex { get; set; }

        PlayerWeaponsManager m_PlayerWeaponsManager;
        KeyWeaponController m_KeyWeaponController;
        WeaponController m_Weapon;

        void Awake()
        {
            EventManager.AddListener<AmmoPickupEvent>(OnAmmoPickup);
        }

        void OnAmmoPickup(AmmoPickupEvent evt)
        {
            if (evt.Weapon == m_Weapon)
            {
                BulletCounter.text = m_Weapon.GetCarriedPhysicalBullets().ToString();
            }
        }

        public void Initialize(WeaponController weapon, int weaponIndex)
        {
            m_Weapon = weapon;
            WeaponCounterIndex = weaponIndex;
            
            // Find managers first
            m_PlayerWeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            m_KeyWeaponController = FindFirstObjectByType<KeyWeaponController>();
            
            // Set weapon icon - use custom UI icon for key weapons if available
            Sprite iconToUse = weapon.WeaponIcon;
            bool isKeyWeapon = false;
            if (m_KeyWeaponController != null)
            {
                // Check if this weapon has a corresponding KeyWeaponData with custom UI icon
                KeyWeaponData keyData = m_KeyWeaponController.CurrentKey;
                if (keyData != null && keyData.WeaponUIIcon != null)
                {
                    iconToUse = keyData.WeaponUIIcon;
                    isKeyWeapon = true;
                }
            }
            WeaponImage.sprite = iconToUse;
            
            // Adjust positioning and scale for key weapon icons to fit better on the bar
            if (WeaponImage != null)
            {
                RectTransform imageRect = WeaponImage.GetComponent<RectTransform>();
                if (imageRect != null)
                {
                    // Reset to normal orientation (no flip)
                    Vector3 scale = imageRect.localScale;
                    scale.x = 1f;
                    imageRect.localScale = scale;
                    
                    // Adjust size for key weapons to fit better on the UI bar
                    if (isKeyWeapon)
                    {
                        // Reduce size slightly to ensure it fits within the bar
                        imageRect.localScale = Vector3.one * 0.85f;
                    }
                }
            }
            
            if (!weapon.HasPhysicalBullets)
                BulletCounter.transform.parent.gameObject.SetActive(false);
            else
                BulletCounter.text = weapon.GetCarriedPhysicalBullets().ToString();

            Reload.gameObject.SetActive(false);
            
            if (m_PlayerWeaponsManager == null && m_KeyWeaponController == null)
            {
                Debug.LogError("AmmoCounter: Could not find PlayerWeaponsManager or KeyWeaponController!");
            }

            // Only show weapon index for standard weapons, not key weapons
            if (m_KeyWeaponController != null)
            {
                // Hide weapon index text for key weapons (they use a different UI)
                if (WeaponIndexText != null)
                    WeaponIndexText.gameObject.SetActive(false);
                
                // Hide control keys root for key weapons (prevents black box flash on switch)
                if (ControlKeysRoot != null)
                    ControlKeysRoot.SetActive(false);
            }
            else if (WeaponIndexText != null)
            {
                WeaponIndexText.text = (WeaponCounterIndex + 1).ToString();
            }

            FillBarColorChange.Initialize(1f, m_Weapon.GetAmmoNeededToShoot());
        }

        void Update()
        {
            // Safety check - weapon may be destroyed during switch animation
            if (m_Weapon == null)
                return;
            
            float currenFillRatio = m_Weapon.CurrentAmmoRatio;
            AmmoFillImage.fillAmount = Mathf.Lerp(AmmoFillImage.fillAmount, currenFillRatio,
                Time.deltaTime * AmmoFillMovementSharpness);

            BulletCounter.text = m_Weapon.GetCarriedPhysicalBullets().ToString();

            // Check if this is the active weapon
            bool isActiveWeapon = false;
            if (m_KeyWeaponController != null)
            {
                isActiveWeapon = (m_Weapon == m_KeyWeaponController.CurrentWeaponController);
            }
            else if (m_PlayerWeaponsManager != null)
            {
                isActiveWeapon = (m_Weapon == m_PlayerWeaponsManager.GetActiveWeapon());
            }

            CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, isActiveWeapon ? 1f : UnselectedOpacity,
                Time.deltaTime * 10);
            transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? Vector3.one : UnselectedScale,
                Time.deltaTime * 10);
            
            // Only toggle ControlKeysRoot for standard weapons, not key weapons
            if (m_KeyWeaponController == null && ControlKeysRoot != null)
            {
                ControlKeysRoot.SetActive(!isActiveWeapon);
            }

            FillBarColorChange.UpdateVisual(currenFillRatio);

            Reload.gameObject.SetActive(m_Weapon.GetCarriedPhysicalBullets() > 0 && m_Weapon.GetCurrentAmmo() == 0 && m_Weapon.IsWeaponActive);
        }

        void Destroy()
        {
            EventManager.RemoveListener<AmmoPickupEvent>(OnAmmoPickup);
        }
    }
}