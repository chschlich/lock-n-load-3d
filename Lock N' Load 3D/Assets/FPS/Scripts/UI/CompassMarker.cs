using Unity.FPS.AI;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class CompassMarker : MonoBehaviour
    {
        [Tooltip("Main marker image")] public Image MainImage;

        [Tooltip("Canvas group for the marker")]
        public CanvasGroup CanvasGroup;

        [Header("Enemy element")] [Tooltip("Default color for the marker")]
        public Color DefaultColor;

        [Tooltip("Alternative color for the marker")]
        public Color AltColor;

        [Header("Direction element")] [Tooltip("Use this marker as a magnetic direction")]
        public bool IsDirection;

        [Tooltip("Text content for the direction")]
        public TMPro.TextMeshProUGUI TextContent;

        EnemyController m_EnemyController;
        MeleeEnemyController m_MeleeEnemyController;

        public void Initialize(CompassElement compassElement, string textDirection)
        {
            if (IsDirection && TextContent)
            {
                TextContent.text = textDirection;
            }
            else
            {
                // Check for standard EnemyController (ranged enemies, turrets, etc.)
                m_EnemyController = compassElement.transform.GetComponent<EnemyController>();

                if (m_EnemyController)
                {
                    m_EnemyController.onDetectedTarget += DetectTarget;
                    m_EnemyController.onLostTarget += LostTarget;

                    LostTarget();
                }
                else
                {
                    // Check for MeleeEnemyController (Locklets)
                    m_MeleeEnemyController = compassElement.transform.GetComponent<MeleeEnemyController>();
                    
                    if (m_MeleeEnemyController)
                    {
                        m_MeleeEnemyController.onDetectedTarget += DetectTarget;
                        m_MeleeEnemyController.onLostTarget += LostTarget;
                        
                        LostTarget();
                    }
                }
            }
        }

        public void DetectTarget()
        {
            MainImage.color = AltColor;
        }

        public void LostTarget()
        {
            MainImage.color = DefaultColor;
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (m_EnemyController != null)
            {
                m_EnemyController.onDetectedTarget -= DetectTarget;
                m_EnemyController.onLostTarget -= LostTarget;
            }
            
            if (m_MeleeEnemyController != null)
            {
                m_MeleeEnemyController.onDetectedTarget -= DetectTarget;
                m_MeleeEnemyController.onLostTarget -= LostTarget;
            }
        }
    }
}