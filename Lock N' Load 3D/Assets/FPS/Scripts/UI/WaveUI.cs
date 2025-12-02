using System.Collections;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class WaveUI : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Delay before first wave starts (seconds)")]
        public float InitialWaveDelay = 30f;
        
        [Header("Banner Settings")]
        public float BannerDuration = 2.5f;
        public float FadeDuration = 0.4f;
        
        [Header("Colors")]
        public Color TextColor = new Color(0.3f, 0.95f, 1f, 1f);
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.8f);
        public Color VictoryColor = new Color(1f, 0.85f, 0.3f, 1f);
        
        private Canvas m_Canvas;
        private GameObject m_BannerPanel;
        private Text m_BannerText;
        private CanvasGroup m_BannerCanvasGroup;
        
        private GameObject m_CornerPanel;
        private Text m_CornerWaveText;
        private Text m_CornerCountText;
        private CanvasGroup m_CornerCanvasGroup;
        
        private WaveManager m_WaveManager;
        private bool m_ShowingVictory = false;
        
        void Awake()
        {
            EventManager.AddListener<WaveStartEvent>(OnWaveStart);
            EventManager.AddListener<WaveCompleteEvent>(OnWaveComplete);
            EventManager.AddListener<AllWavesCompleteEvent>(OnAllWavesComplete);
            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
        }
        
        void Start()
        {
            m_WaveManager = FindFirstObjectByType<WaveManager>();
            
            // Apply initial delay to WaveManager
            if (m_WaveManager != null)
            {
                m_WaveManager.InitialDelay = InitialWaveDelay;
                Debug.Log($"WaveUI: Set initial wave delay to {InitialWaveDelay}s");
            }
            
            CreateOwnCanvas();
            CreateUI();
        }
        
        void OnDestroy()
        {
            EventManager.RemoveListener<WaveStartEvent>(OnWaveStart);
            EventManager.RemoveListener<WaveCompleteEvent>(OnWaveComplete);
            EventManager.RemoveListener<AllWavesCompleteEvent>(OnAllWavesComplete);
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        }
        
        void CreateOwnCanvas()
        {
            // Create our own canvas to avoid scaling issues with existing HUD
            var canvasObj = new GameObject("WaveUICanvas");
            m_Canvas = canvasObj.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = 100;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("WaveUI: Created dedicated canvas");
        }
        
        void CreateUI()
        {
            // === CENTER BANNER ===
            m_BannerPanel = new GameObject("WaveBanner");
            m_BannerPanel.transform.SetParent(m_Canvas.transform, false);
            
            var bannerRect = m_BannerPanel.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(300, 80);
            bannerRect.anchoredPosition = Vector2.zero;
            
            var bannerBg = m_BannerPanel.AddComponent<Image>();
            bannerBg.color = BackgroundColor;
            
            m_BannerCanvasGroup = m_BannerPanel.AddComponent<CanvasGroup>();
            m_BannerCanvasGroup.alpha = 0f;
            
            // Banner text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(m_BannerPanel.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            
            m_BannerText = textObj.AddComponent<Text>();
            m_BannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_BannerText.fontSize = 48;
            m_BannerText.fontStyle = FontStyle.Bold;
            m_BannerText.alignment = TextAnchor.MiddleCenter;
            m_BannerText.color = TextColor;
            m_BannerText.text = "WAVE 1";
            
            m_BannerPanel.SetActive(false);
            
            // === CORNER PANEL ===
            m_CornerPanel = new GameObject("WaveCorner");
            m_CornerPanel.transform.SetParent(m_Canvas.transform, false);
            
            var cornerRect = m_CornerPanel.AddComponent<RectTransform>();
            cornerRect.anchorMin = new Vector2(1, 1);
            cornerRect.anchorMax = new Vector2(1, 1);
            cornerRect.pivot = new Vector2(1, 1);
            cornerRect.sizeDelta = new Vector2(180, 70);
            cornerRect.anchoredPosition = new Vector2(-15, -15);
            
            var cornerBg = m_CornerPanel.AddComponent<Image>();
            cornerBg.color = BackgroundColor;
            
            m_CornerCanvasGroup = m_CornerPanel.AddComponent<CanvasGroup>();
            m_CornerCanvasGroup.alpha = 0f;
            
            // Wave text (top half - stretch to fill)
            var waveObj = new GameObject("WaveText");
            waveObj.transform.SetParent(m_CornerPanel.transform, false);
            
            var waveRect = waveObj.AddComponent<RectTransform>();
            waveRect.anchorMin = new Vector2(0, 0.5f);
            waveRect.anchorMax = new Vector2(1, 1);
            waveRect.pivot = new Vector2(0.5f, 0.5f);
            waveRect.offsetMin = Vector2.zero;
            waveRect.offsetMax = Vector2.zero;
            
            m_CornerWaveText = waveObj.AddComponent<Text>();
            m_CornerWaveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_CornerWaveText.fontSize = 24;
            m_CornerWaveText.fontStyle = FontStyle.Bold;
            m_CornerWaveText.alignment = TextAnchor.MiddleCenter;
            m_CornerWaveText.color = TextColor;
            m_CornerWaveText.text = "WAVE 1";
            m_CornerWaveText.raycastTarget = false;
            
            // Count text (bottom half - stretch to fill)
            var countObj = new GameObject("CountText");
            countObj.transform.SetParent(m_CornerPanel.transform, false);
            
            var countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0, 0);
            countRect.anchorMax = new Vector2(1, 0.5f);
            countRect.pivot = new Vector2(0.5f, 0.5f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            
            m_CornerCountText = countObj.AddComponent<Text>();
            m_CornerCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_CornerCountText.fontSize = 20;
            m_CornerCountText.alignment = TextAnchor.MiddleCenter;
            m_CornerCountText.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.9f);
            m_CornerCountText.text = "20 / 20";
            m_CornerCountText.raycastTarget = false;
            
            m_CornerPanel.SetActive(false);
            
            Debug.Log("WaveUI: UI created successfully");
        }
        
        void OnWaveStart(WaveStartEvent evt)
        {
            if (m_ShowingVictory) return;
            Debug.Log($"WaveUI: Wave {evt.WaveNumber} starting");
            StartCoroutine(ShowWaveBanner(evt.WaveNumber, evt.TotalEnemies));
        }
        
        void OnWaveComplete(WaveCompleteEvent evt)
        {
            Debug.Log($"WaveUI: Wave {evt.WaveNumber} complete");
        }
        
        void OnAllWavesComplete(AllWavesCompleteEvent evt)
        {
            Debug.Log("WaveUI: All waves complete");
            StartCoroutine(ShowVictory());
        }
        
        void OnEnemyKilled(EnemyKillEvent evt)
        {
            UpdateCount();
        }
        
        IEnumerator ShowWaveBanner(int wave, int total)
        {
            // Hide corner if visible
            if (m_CornerPanel.activeSelf)
            {
                yield return Fade(m_CornerCanvasGroup, 1, 0);
                m_CornerPanel.SetActive(false);
            }
            
            // Show banner
            m_BannerText.text = $"WAVE {wave}";
            m_BannerText.color = TextColor;
            m_BannerCanvasGroup.alpha = 0;
            m_BannerPanel.SetActive(true);
            
            yield return Fade(m_BannerCanvasGroup, 0, 1);
            yield return new WaitForSeconds(BannerDuration);
            yield return Fade(m_BannerCanvasGroup, 1, 0);
            
            m_BannerPanel.SetActive(false);
            
            // Show corner
            Debug.Log("WaveUI: Showing corner panel");
            m_CornerWaveText.text = $"WAVE {wave}";
            UpdateCount();
            m_CornerCanvasGroup.alpha = 0;
            m_CornerPanel.SetActive(true);
            
            yield return Fade(m_CornerCanvasGroup, 0, 1);
            Debug.Log($"WaveUI: Corner panel visible, alpha: {m_CornerCanvasGroup.alpha}");
        }
        
        IEnumerator Fade(CanvasGroup cg, float from, float to)
        {
            float t = 0;
            while (t < FadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }
            cg.alpha = to;
        }
        
        void UpdateCount()
        {
            if (m_WaveManager == null || !m_CornerPanel.activeSelf) return;
            int remaining = m_WaveManager.GetEnemiesRemaining();
            int total = m_WaveManager.TotalEnemiesThisWave;
            m_CornerCountText.text = $"{remaining} / {total}";
        }
        
        IEnumerator ShowVictory()
        {
            m_ShowingVictory = true;
            
            if (m_CornerPanel.activeSelf)
            {
                yield return Fade(m_CornerCanvasGroup, 1, 0);
                m_CornerPanel.SetActive(false);
            }
            
            m_BannerText.text = "VICTORY";
            m_BannerText.color = VictoryColor;
            m_BannerCanvasGroup.alpha = 0;
            m_BannerPanel.SetActive(true);
            
            yield return Fade(m_BannerCanvasGroup, 0, 1);
            yield return new WaitForSeconds(2f);
            
            EventManager.Broadcast(Events.AllObjectivesCompletedEvent);
        }
        
        void Update()
        {
            if (m_WaveManager != null && m_WaveManager.IsWaveActive && m_CornerPanel.activeSelf)
                UpdateCount();
        }
    }
}
