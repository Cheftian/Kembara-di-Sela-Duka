using UnityEngine;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [System.Serializable]
    public struct ComicPanel
    {
        [Tooltip("Nama identifikasi panel komik (untuk referensi editor).")]
        public string panelName;

        [Tooltip("GameObject Panel UI komik yang ada di hirarki.")]
        public GameObject panelObject;

        [Tooltip("CanvasGroup pada panelObject untuk efek Fade In / Fade Out.")]
        public CanvasGroup panelCanvasGroup;

        [Tooltip("Kumpulan NarrationData yang akan diputar berurutan di panel ini.")]
        public NarrationData[] narrationsInPanel;
    }

    [Header("Cutscene Configuration")]
    [Tooltip("Daftar panel komik yang akan diputar berurutan dari awal hingga selesai di scene ini.")]
    [SerializeField] private ComicPanel[] comicPanels;

    [Header("Panel Fade Settings")]
    [SerializeField] private float panelFadeDuration = 0.5f;

    [Header("Auto Transition Settings")]
    [Tooltip("Waktu tunggu (dalam detik) setelah semua narasi di sebuah panel selesai sebelum otomatis pindah ke panel berikutnya.")]
    [SerializeField] private float delayAfterNarration = 1.5f;

    [Header("Cutscene Ending Settings")]
    [Tooltip("Nama scene berikutnya yang akan dimuat secara otomatis setelah panel terakhir selesai.")]
    [SerializeField] private string sceneNameAfterCutscene;
    [SerializeField] private GameManager.GameState stateAfterCutscene = GameManager.GameState.Play;

    [Header("Auto Play Settings")]
    [Tooltip("Jika dicentang, cutscene akan langsung dimulai otomatis saat scene dimuat.")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private float initialDelay = 0.5f; // Jeda singkat agar scene siap sepenuhnya

    private bool isPlayingCutscene = false;

    public bool IsPlayingCutscene => isPlayingCutscene;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ResetAllPanels();
    }

    private void Start()
    {
        // Jalankan cutscene otomatis saat scene dibuka
        if (autoPlayOnStart)
        {
            StartCoroutine(DelayedStartCutscene());
        }
    }

    private void ResetAllPanels()
    {
        if (comicPanels == null) return;

        foreach (var panel in comicPanels)
        {
            if (panel.panelObject != null)
            {
                if (panel.panelCanvasGroup != null)
                {
                    panel.panelCanvasGroup.alpha = 0f;
                    panel.panelCanvasGroup.blocksRaycasts = false;
                }
                panel.panelObject.SetActive(false);
            }
        }
    }

    private IEnumerator DelayedStartCutscene()
    {
        yield return new WaitForSeconds(initialDelay);
        StartCutscene();
    }

    /// <summary>
    /// Memulai seluruh rangkaian cutscene komik dari panel pertama.
    /// </summary>
    public void StartCutscene()
    {
        if (isPlayingCutscene || comicPanels == null || comicPanels.Length == 0) return;
        StartCoroutine(CutsceneSequenceRoutine());
    }

    private IEnumerator CutsceneSequenceRoutine()
    {
        isPlayingCutscene = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        }

        // Loop melalui setiap Panel Komik secara berurutan
        for (int i = 0; i < comicPanels.Length; i++)
        {
            ComicPanel currentPanel = comicPanels[i];

            if (currentPanel.panelObject == null)
            {
                Debug.LogWarning($"[CutsceneManager] Panel ke-{i} ({currentPanel.panelName}) GameObject-nya kosong!", this);
                continue;
            }

            // 1. Fade In dan Buka Panel Komik saat ini
            yield return StartCoroutine(FadePanelRoutine(currentPanel, true));

            // 2. Putar semua NarrationData yang ada di dalam panel ini secara berurutan
            if (currentPanel.narrationsInPanel != null && currentPanel.narrationsInPanel.Length > 0)
            {
                for (int j = 0; j < currentPanel.narrationsInPanel.Length; j++)
                {
                    NarrationData narration = currentPanel.narrationsInPanel[j];
                    if (narration == null) continue;

                    if (NarrationManager.Instance != null)
                    {
                        NarrationManager.Instance.PlayNarration(
                            narration, 
                            GameManager.GameState.Cutscene, 
                            GameManager.GameState.Cutscene
                        );

                        // Tunggu sampai NarrationManager selesai memutar narasi ini
                        while (NarrationManager.Instance.IsNarrating)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        Debug.LogError("[CutsceneManager] NarrationManager.Instance tidak ditemukan!", this);
                        yield break;
                    }
                }
            }

            // 3. SETELAH semua NarrationData selesai, tunggu sebentar secara otomatis sebelum ganti panel
            yield return new WaitForSeconds(delayAfterNarration);

            // 4. Fade Out dan Tutup Panel saat ini sebelum lanjut ke panel berikutnya
            yield return StartCoroutine(FadePanelRoutine(currentPanel, false));
        }

        // 5. Setelah panel terakhir selesai, ubah state dan muat scene baru
        isPlayingCutscene = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(stateAfterCutscene);
        }

        if (!string.IsNullOrEmpty(sceneNameAfterCutscene))
        {
            if (SceneController.Instance != null)
            {
                SceneController.Instance.ChangeSceneWithoutLoading(sceneNameAfterCutscene);
            }
            else
            {
                Debug.LogError("[CutsceneManager] SceneController.Instance tidak ditemukan untuk memuat scene baru!", this);
            }
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] Nama scene tujuan setelah cutscene (sceneNameAfterCutscene) kosong. Cutscene berakhir tanpa pindah scene.", this);
        }
    }

    private IEnumerator FadePanelRoutine(ComicPanel panel, bool fadeIn)
    {
        GameObject obj = panel.panelObject;
        CanvasGroup cg = panel.panelCanvasGroup;

        if (fadeIn)
        {
            obj.SetActive(true);
        }

        if (cg != null)
        {
            cg.blocksRaycasts = fadeIn;
            float startAlpha = fadeIn ? 0f : 1f;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
                yield return null;
            }
            cg.alpha = targetAlpha;
        }

        if (!fadeIn)
        {
            obj.SetActive(false);
        }
    }
}
