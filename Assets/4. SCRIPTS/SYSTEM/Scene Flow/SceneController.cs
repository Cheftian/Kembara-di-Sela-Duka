using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [System.Serializable]
    public struct TransitionData
    {
        [Tooltip("Nama transisi untuk dipanggil lewat kode (Contoh: RoomFadeIn, CrossFade)")]
        public string transitionName;
        
        [Tooltip("Nama Animation Clip yang ada di Animator")]
        public string animationName;

        [Tooltip("Nama SFX spesifik untuk transisi ini (Kosongkan jika tidak memakai SFX)")]
        public string transitionSFX;
    }

    [Header("Transition Lists")]
    [Tooltip("Daftar macam-macam transisi yang tersedia beserta SFX-nya")]
    [SerializeField] private TransitionData[] availableTransitions;

    [Header("Transition Flags (Atur per Scene)")]
    [Tooltip("Tulis nama TransitionName dari array untuk layar MEMBUKA saat scene baru dimulai. Otomatis berjalan.")]
    [SerializeField] private string startTransitionName = "RoomFadeOut";

    [Header("Transition Settings")]
    [SerializeField] private Animator transitionAnimator; // Tarik TransitionPanel di scene saat ini ke sini
    [SerializeField] private float transitionDelay = 1f;  

    [Header("Loading Configuration")]
    [SerializeField] private string loadingSceneName = "LoadingScene"; 
    [SerializeField] private float minLoadingTime = 2.5f; 

    private static string targetSceneName; 
    private static string activeExitTransition; // Menyimpan transisi keluar yang dilempar dari fungsi ChangeScene
    private static bool isProcessingLoad = false; 

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneController] Berhasil masuk ke Scene: {scene.name}");

        if (scene.name == loadingSceneName)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("[SceneController] Nama target scene kosong saat di LoadingScene!");
                return;
            }
            
            // Saat scene dibuka (termasuk loading scene), tetap mengambil otomatis dari Inspector
            if (!string.IsNullOrEmpty(startTransitionName))
            {
                PlayTransitionByName(startTransitionName);
            }

            if (!isProcessingLoad)
            {
                StartCoroutine(LoadTargetSceneInBackground());
            }
        }
        else
        {
            isProcessingLoad = false; 
            
            // Saat scene tujuan dibuka, otomatis memutar animasi dari teks Inspector scene tersebut
            if (!string.IsNullOrEmpty(startTransitionName))
            {
                PlayTransitionByName(startTransitionName);
            }
        }
    }

    public void ChangeSceneByName(string transitionName, string sceneName)
    {
        ChangeSceneWithLoading(transitionName, sceneName);
    }

    // --- FUNGSI PERGANTIAN SCENE DENGAN LOADING SCREEN ---
    public void ChangeSceneWithLoading(string transitionName, string sceneName)
    {
        if (!CanChangeScene(sceneName)) return;

        targetSceneName = sceneName;
        activeExitTransition = transitionName; // Mengambil murni dari nama transisi yang diketik di parameter fungsi

        StartCoroutine(TransitionToLoadingScene());
    }

    // --- FUNGSI PERGANTIAN SCENE SECARA LANGSUNG ---
    public void ChangeSceneWithoutLoading(string transitionName, string sceneName)
    {
        if (!CanChangeScene(sceneName)) return;

        targetSceneName = sceneName;
        activeExitTransition = transitionName; // Mengambil murni dari nama transisi yang diketik di parameter fungsi

        StartCoroutine(TransitionToSceneDirectly());
    }

    private bool CanChangeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneController] Nama scene kosong!");
            return false;
        }
        return true;
    }

    private IEnumerator TransitionToLoadingScene()
    {
        // Memutar transisi keluar murni berdasarkan kiriman parameter fungsi script luar
        if (!string.IsNullOrEmpty(activeExitTransition) && transitionAnimator != null && transitionAnimator.gameObject.activeInHierarchy)
        {
            PlayTransitionByName(activeExitTransition);
            yield return new WaitForSeconds(transitionDelay);
        }
        SceneManager.LoadScene(loadingSceneName);
    }

    private IEnumerator TransitionToSceneDirectly()
    {
        // Memutar transisi keluar murni berdasarkan kiriman parameter fungsi script luar
        if (!string.IsNullOrEmpty(activeExitTransition) && transitionAnimator != null && transitionAnimator.gameObject.activeInHierarchy)
        {
            PlayTransitionByName(activeExitTransition);
            yield return new WaitForSeconds(transitionDelay);
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private IEnumerator LoadTargetSceneInBackground()
    {
        isProcessingLoad = true;
        float startTime = Time.time;

        yield return null; 

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false; 
        }
        else
        {
            Debug.LogError("[SceneController] Gagal menginisialisasi LoadSceneAsync!");
            SceneManager.LoadScene(targetSceneName);
            isProcessingLoad = false;
            yield break;
        }

        while (asyncLoad.progress < 0.9f || (Time.time - startTime) < minLoadingTime)
        {
            yield return null; 
        }

        // Sebelum Loading Scene mengaktifkan scene tujuan asli, layar ditutup lagi menggunakan transisi keluar yang sama
        if (!string.IsNullOrEmpty(activeExitTransition) && transitionAnimator != null && transitionAnimator.gameObject.activeInHierarchy)
        {
            PlayTransitionByName(activeExitTransition);
            yield return new WaitForSeconds(transitionDelay);
        }

        asyncLoad.allowSceneActivation = true;
    }

    public void PlayTransitionByName(string transName)
    {
        if (transitionAnimator == null || !transitionAnimator.gameObject.activeInHierarchy) return;

        bool transitionFound = false;

        foreach (var trans in availableTransitions)
        {
            if (trans.transitionName == transName)
            {
                transitionFound = true;

                if (!string.IsNullOrEmpty(trans.transitionSFX))
                {
                    PlayTransitionSFX(trans.transitionSFX);
                }

                if (!string.IsNullOrEmpty(trans.animationName))
                {
                    transitionAnimator.Play(trans.animationName);
                }
                else
                {
                    Debug.LogWarning($"[SceneController] Transisi '{transName}' ditemukan, tetapi Animation Name kosong!");
                }
                
                break;
            }
        }

        if (!transitionFound)
        {
            Debug.LogWarning($"[SceneController] Transisi dengan nama '{transName}' tidak ditemukan di dalam array!");
        }
    }

    private void PlayTransitionSFX(string sfxName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxName))
        {
            AudioManager.Instance.PlaySFX(sfxName); 
        }
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Tambahkan fungsi ini di dalam script SceneController Anda
    public void ChangeSceneViaButtonString(string rawData)
    {
        // Memisahkan string berdasarkan tanda koma (Contoh input: "FadeIn,GameplayScene")
        string[] splitData = rawData.Split(',');

        if (splitData.Length >= 2)
        {
            string transName = splitData[0].Trim();
            string sceneName = splitData[1].Trim();
            
            ChangeSceneWithLoading(transName, sceneName);
        }
        else if (splitData.Length == 1)
        {
            // Jika lupa mengisi nama transisi (hanya isi nama scene saja)
            ChangeSceneWithLoading("", splitData[0].Trim());
        }
    }

}
