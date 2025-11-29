using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class WeaponHUDManager : MonoBehaviour
    {
        [Tooltip("UI panel containing the layoutGroup for displaying weapon ammo")]
        public RectTransform AmmoPanel;

        [Tooltip("Prefab for displaying weapon ammo")]
        public GameObject AmmoCounterPrefab;

        PlayerWeaponsManager m_PlayerWeaponsManager;
        KeyWeaponController m_KeyWeaponController;
        List<AmmoCounter> m_AmmoCounters = new List<AmmoCounter>();
        WeaponController m_CurrentWeaponController;

        void Start()
        {
            // Try to find KeyWeaponController first
            m_KeyWeaponController = FindFirstObjectByType<KeyWeaponController>();
            
            if (m_KeyWeaponController != null)
            {
                Debug.Log("WeaponHUDManager: Using KeyWeaponController");
                // We'll update the weapon display each frame for key weapons
            }
            else
            {
                // Fall back to PlayerWeaponsManager
                m_PlayerWeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
                DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, WeaponHUDManager>(m_PlayerWeaponsManager,
                    this);

                WeaponController activeWeapon = m_PlayerWeaponsManager.GetActiveWeapon();
                if (activeWeapon)
                {
                    AddWeapon(activeWeapon, m_PlayerWeaponsManager.ActiveWeaponIndex);
                    ChangeWeapon(activeWeapon);
                }

                m_PlayerWeaponsManager.OnAddedWeapon += AddWeapon;
                m_PlayerWeaponsManager.OnRemovedWeapon += RemoveWeapon;
                m_PlayerWeaponsManager.OnSwitchedToWeapon += ChangeWeapon;
            }
        }
        
        void Update()
        {
            // Handle key weapon controller updates
            if (m_KeyWeaponController != null)
            {
                WeaponController currentWeapon = m_KeyWeaponController.CurrentWeaponController;
                
                if (currentWeapon != m_CurrentWeaponController)
                {
                    // Weapon changed, clear all old counters
                    foreach (var counter in m_AmmoCounters)
                    {
                        if (counter != null)
                            Destroy(counter.gameObject);
                    }
                    m_AmmoCounters.Clear();
                    
                    // Add single counter for current weapon
                    if (currentWeapon != null)
                    {
                        AddWeapon(currentWeapon, 0);
                        ChangeWeapon(currentWeapon);
                    }
                    
                    m_CurrentWeaponController = currentWeapon;
                }
            }
        }

        void AddWeapon(WeaponController newWeapon, int weaponIndex)
        {
            GameObject ammoCounterInstance = Instantiate(AmmoCounterPrefab, AmmoPanel);
            AmmoCounter newAmmoCounter = ammoCounterInstance.GetComponent<AmmoCounter>();
            DebugUtility.HandleErrorIfNullGetComponent<AmmoCounter, WeaponHUDManager>(newAmmoCounter, this,
                ammoCounterInstance.gameObject);

            newAmmoCounter.Initialize(newWeapon, weaponIndex);

            m_AmmoCounters.Add(newAmmoCounter);
        }

        void RemoveWeapon(WeaponController newWeapon, int weaponIndex)
        {
            int foundCounterIndex = -1;
            for (int i = 0; i < m_AmmoCounters.Count; i++)
            {
                if (m_AmmoCounters[i].WeaponCounterIndex == weaponIndex)
                {
                    foundCounterIndex = i;
                    Destroy(m_AmmoCounters[i].gameObject);
                }
            }

            if (foundCounterIndex >= 0)
            {
                m_AmmoCounters.RemoveAt(foundCounterIndex);
            }
        }

        void ChangeWeapon(WeaponController weapon)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(AmmoPanel);
        }
    }
}