using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class WorldspaceHealthBar : MonoBehaviour
    {
        [Tooltip("Health component to track")] public Health Health;

        [Tooltip("Image component displaying health left")]
        public Image HealthBarImage;

        [Tooltip("The floating healthbar pivot transform")]
        public Transform HealthBarPivot;

        [Tooltip("Whether the health bar is visible when at full health or not")]
        public bool HideFullHealthBar = true;

        void Start()
        {
        }

        void Update()
        {
            if (Health == null || HealthBarImage == null)
                return;

            // update health bar value
            float fillAmount = Health.CurrentHealth / Health.MaxHealth;
            HealthBarImage.fillAmount = fillAmount;

            // rotate health bar to face the camera/player
            if (HealthBarPivot != null && Camera.main != null)
            {
                HealthBarPivot.LookAt(Camera.main.transform.position);
            }

            // hide health bar if needed
            if (HideFullHealthBar && HealthBarPivot != null)
            {
                bool shouldBeActive = fillAmount != 1;
                HealthBarPivot.gameObject.SetActive(shouldBeActive);
            }
        }
    }
}