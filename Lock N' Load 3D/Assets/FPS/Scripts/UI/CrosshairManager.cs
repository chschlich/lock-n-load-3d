using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class CrosshairManager : MonoBehaviour
    {
        public Image CrosshairImage;
        public Sprite NullCrosshairSprite;
        public float CrosshairUpdateshrpness = 5f;
        
        [Header("Scale Settings")]
        [Tooltip("Global multiplier for all crosshair sizes")]
        [Range(0.1f, 2f)]
        public float CrosshairSizeMultiplier = 0.5f;  // 50% of original size

        PlayerWeaponsManager m_WeaponsManager;
        KeyWeaponController m_KeyWeaponController;
        bool m_WasPointingAtEnemy;
        RectTransform m_CrosshairRectTransform;
        CrosshairData m_CrosshairDataDefault;
        CrosshairData m_CrosshairDataTarget;
        CrosshairData m_CurrentCrosshair;
        Sprite m_BlasterCrosshairSprite;

        void Start()
        {
            m_WeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            m_KeyWeaponController = FindFirstObjectByType<KeyWeaponController>();
            
            // load blaster crosshair sprite (guid: e881aa18189b12f4e915fc5b14ddc421)
            m_BlasterCrosshairSprite = Resources.Load<Sprite>("Crosshair_Blaster_Center");
            if (m_BlasterCrosshairSprite == null)
            {
                // try loading from asset database path
                #if UNITY_EDITOR
                m_BlasterCrosshairSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FPS/Art/Textures/UI/Crosshair_Blaster_Center.png");
                #endif
            }
            
            // if using key weapon controller, set up crosshair
            if (m_KeyWeaponController != null && CrosshairImage != null)
            {
                CrosshairImage.enabled = true;
                m_CrosshairRectTransform = CrosshairImage.GetComponent<RectTransform>();
                
                UpdateKeyWeaponCrosshair();
                return;
            }
            
            // original weapon manager logic
            if (m_WeaponsManager != null)
            {
                DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, CrosshairManager>(m_WeaponsManager, this);

                OnWeaponChanged(m_WeaponsManager.GetActiveWeapon());

                m_WeaponsManager.OnSwitchedToWeapon += OnWeaponChanged;
            }
        }

        void Update()
        {
            // update key weapon crosshair
            if (m_KeyWeaponController != null)
            {
                UpdateKeyWeaponCrosshair();
                return;
            }
                
            if (m_WeaponsManager == null)
                return;
                
            UpdateCrosshairPointingAtEnemy(false);
            m_WasPointingAtEnemy = m_WeaponsManager.IsPointingAtEnemy;
        }

        void UpdateCrosshairPointingAtEnemy(bool force)
        {
            if (m_CrosshairDataDefault.CrosshairSprite == null)
                return;

            if ((force || !m_WasPointingAtEnemy) && m_WeaponsManager.IsPointingAtEnemy)
            {
                m_CurrentCrosshair = m_CrosshairDataTarget;
                CrosshairImage.sprite = m_CurrentCrosshair.CrosshairSprite;
                m_CrosshairRectTransform.sizeDelta = m_CurrentCrosshair.CrosshairSize * CrosshairSizeMultiplier * Vector2.one;
            }
            else if ((force || m_WasPointingAtEnemy) && !m_WeaponsManager.IsPointingAtEnemy)
            {
                m_CurrentCrosshair = m_CrosshairDataDefault;
                CrosshairImage.sprite = m_CurrentCrosshair.CrosshairSprite;
                m_CrosshairRectTransform.sizeDelta = m_CurrentCrosshair.CrosshairSize * CrosshairSizeMultiplier * Vector2.one;
            }

            CrosshairImage.color = Color.Lerp(CrosshairImage.color, m_CurrentCrosshair.CrosshairColor,
Time.deltaTime * CrosshairUpdateshrpness);

            m_CrosshairRectTransform.sizeDelta = Mathf.Lerp(m_CrosshairRectTransform.sizeDelta.x,
                m_CurrentCrosshair.CrosshairSize * CrosshairSizeMultiplier,
                Time.deltaTime * CrosshairUpdateshrpness) * Vector2.one;
        }

        void OnWeaponChanged(WeaponController newWeapon)
        {
            if (newWeapon)
            {
                CrosshairImage.enabled = true;
                m_CrosshairDataDefault = newWeapon.CrosshairDataDefault;
                m_CrosshairDataTarget = newWeapon.CrosshairDataTargetInSight;
                m_CrosshairRectTransform = CrosshairImage.GetComponent<RectTransform>();
                DebugUtility.HandleErrorIfNullGetComponent<RectTransform, CrosshairManager>(m_CrosshairRectTransform,
                    this, CrosshairImage.gameObject);
            }
            else
            {
                if (NullCrosshairSprite)
                {
                    CrosshairImage.sprite = NullCrosshairSprite;
                }
                else
                {
                    CrosshairImage.enabled = false;
                }
            }

            UpdateCrosshairPointingAtEnemy(true);
        }
        
        void UpdateKeyWeaponCrosshair()
        {
            if (m_KeyWeaponController == null || m_KeyWeaponController.CurrentKey == null)
                return;
                
            var currentKey = m_KeyWeaponController.CurrentKey;
            
            // use blaster crosshair sprite or key's custom sprite
            Sprite crosshairSprite = currentKey.CrosshairSprite != null ? currentKey.CrosshairSprite : m_BlasterCrosshairSprite;
            if (crosshairSprite != null && CrosshairImage.sprite != crosshairSprite)
            {
                CrosshairImage.sprite = crosshairSprite;
            }
            
            // use key color with transparency
            Color targetColor = currentKey.CrosshairColor;
            if (targetColor == Color.white) // if not customized, use key color
            {
                targetColor = new Color(currentKey.KeyColor.r, currentKey.KeyColor.g, currentKey.KeyColor.b, 0.38f);
            }
            
            CrosshairImage.color = Color.Lerp(CrosshairImage.color, targetColor, Time.deltaTime * CrosshairUpdateshrpness);
            
            // set size (with global multiplier)
            float targetSize = currentKey.CrosshairSize * CrosshairSizeMultiplier;
            if (m_CrosshairRectTransform != null)
            {
                m_CrosshairRectTransform.sizeDelta = Vector2.Lerp(
                    m_CrosshairRectTransform.sizeDelta,
                    Vector2.one * targetSize,
                    Time.deltaTime * CrosshairUpdateshrpness
                );
            }
        }
    }
}