using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class TrailDelayActivator : MonoBehaviour
    {
        [Tooltip("Name of the trail GameObject to activate after delay")]
        public string TrailObjectName = "Projectile_PurpleKeyTrail";
        
        [Tooltip("Delay in seconds before activating the trail")]
        public float Delay = 0.3f;
        
        private GameObject m_TrailObject;
        
        void Start()
        {
            // Find the trail GameObject by name in children
            Transform visualRoot = transform.Find("VisualRoot");
            if (visualRoot != null)
            {
                Transform trailTransform = visualRoot.Find(TrailObjectName);
                if (trailTransform != null)
                {
                    m_TrailObject = trailTransform.gameObject;
                    m_TrailObject.SetActive(false);
                    Invoke(nameof(ActivateTrail), Delay);
                }
                else
                {
                    Debug.LogWarning($"TrailDelayActivator: Could not find trail GameObject named '{TrailObjectName}' in VisualRoot");
                }
            }
            else
            {
                Debug.LogWarning("TrailDelayActivator: Could not find VisualRoot");
            }
        }
        
        void ActivateTrail()
        {
            if (m_TrailObject != null)
            {
                m_TrailObject.SetActive(true);
            }
        }
        
        void OnDestroy()
        {
            CancelInvoke();
        }
    }
}

