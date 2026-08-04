using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Transition Flags (Atur per Scene)")]
    [Tooltip("Jika dicentang, layar akan membuka (terang) saat scene ini baru dimulai.")]
    [SerializeField] private bool runFadeOutOnStart = true;
    
    [Tooltip("Jika dicentang, layar akan menutup (gelap) sebelum berpindah ke scene berikutnya.")]
    [SerializeField] private bool runFadeInOnExit = true;

    [Header("Transition Settings")]
    [SerializeField] private Animator transitionAnimator; // Tarik TransitionPanel di scene saat ini ke sini
    [SerializeField] private float transitionDelay = 1f;   

    [Header("Loading Configuration")]
    [SerializeField] private string loadingSceneName = "LoadingScene"; 
    [SerializeField] private float minLoadingTime = 2.5f; 

    // Variabel wajib STATIC agar nama scene tujuan tidak hilang saat objek hancur saat pindah scene
    private static string targetSceneName; 
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

        // 1. JIKA MASUK KE LOADING SCENE
        if (scene.name == loadingSceneName)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("[SceneController] Nama target scene kosong saat di LoadingScene!");
                return;
            }
            
            // Cek flag apakah LoadingScene diizinkan membuka layar secara halus
            if (runFadeOutOnStart)
            {
                PlayFadeOutAnimation();
            }

            if (!isProcessingLoad)
            {
                StartCoroutine(LoadTargetSceneInBackground());
            }
        }
        // 2. JIKA MASUK KE SCENE TUJUAN ASLI (Gameplay / Main Menu)
        else
        {
            isProcessingLoad = false; 
            
            // Cek flag apakah scene tujuan diizinkan membuka layar secara halus
            if (runFadeOutOnStart)
            {
                PlayFadeOutAnimation();
            }
        }
    }

    // Fungsi utama untuk berpindah scene lewat tombol UI
    public void ChangeSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneController] Nama scene kosong!");
            return;
        }

        targetSceneName = sceneName;
        StartCoroutine(TransitionToLoadingScene());
    }

    // Coroutine untuk menutup layar scene lama sebelum masuk ke Loading Scene
    private IEnumerator TransitionToLoadingScene()
    {
        // Cek flag apakah scene saat ini diizinkan menutup layar secara halus sebelum keluar
        if (runFadeInOnExit && transitionAnimator != null && transitionAnimator.gameObject.activeInHierarchy)
        {
            transitionAnimator.Play("Room_FadeIn"); // Layar lama menutup/menggelap halus
            yield return new WaitForSeconds(transitionDelay);
        }

        SceneManager.LoadScene(loadingSceneName);
    }

    // Coroutine penahan yang berjalan di dalam LoadingScene
    private IEnumerator LoadTargetSceneInBackground()
    {
        isProcessingLoad = true;
        float startTime = Time.time;

        // Berikan jeda 1 frame agar Unity merender Video di LoadingScene terlebih dahulu
        yield return null; 

        // Memuat scene target di background dan langsung mengunci perpindahannya
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

        // Loop penahan berdasarkan kemajuan data dan durasi waktu minimal video
        while (asyncLoad.progress < 0.9f || (Time.time - startTime) < minLoadingTime)
        {
            yield return null; 
        }

        // SEBELUM PINDAH: Cek flag apakah scene loading diizinkan menutup layar dengan transisi hitam
        if (runFadeInOnExit && transitionAnimator != null && transitionAnimator.gameObject.activeInHierarchy)
        {
            transitionAnimator.Play("Room_FadeIn"); // Layar menutup kembali
            yield return new WaitForSeconds(transitionDelay);
        }

        // Aktifkan scene tujuan asli
        asyncLoad.allowSceneActivation = true;
    }

    private void PlayFadeOutAnimation()
    {
        if (transitionAnimator != null && transitionAnimator.gameObject.activeInHierarchy)
        {
            transitionAnimator.Play("Room_FadeOut"); // Layar menjadi terang / membuka ruangan
        }
    }

    // Fungsi untuk keluar dari aplikasi game
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
