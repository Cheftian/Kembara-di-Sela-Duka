using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Attach a CanvasGroup component attached to a full-screen black UI Panel.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    
    [Header("Audio Settings")]
    [Tooltip("Nama SFX atau BGM splash yang terdaftar di AudioManager.")]
    [SerializeField] private string splashAudioName = "SplashIntro";

    [Header("Scene Destination")]
    [Tooltip("Nama scene berikutnya setelah splash selesai (misal MainMenu).")]
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private string nextSceneTransitionName = "RoomFadeOut";


    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isSkipping = false;

    private void Start()
    {
        Application.targetFrameRate = 60; 
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f; // Ensure it starts invisible
        }

        // Daftarkan fungsi ketika video selesai berputar secara normal
        videoPlayer.loopPointReached += OnVideoFinished;

        StartCoroutine(PrepareAndPlaySplash());
    }

    private void Update()
    {
        // Detect ESC press to trigger the skip sequence
        if (Input.GetKeyDown(KeyCode.Escape) && !isSkipping)
        {
            StartCoroutine(SkipSplashSequence());
        }
    }

    private IEnumerator PrepareAndPlaySplash()
    {
        // 1. Tunggu 1 frame untuk memastikan AudioManager (Singleton) sudah terinisialisasi di Scene
        yield return null;

        // 2. Putar audio melalui AudioManager
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(splashAudioName))
        {
            AudioManager.Instance.PlaySFX(splashAudioName);
        }
        else
        {
            Debug.LogWarning("[SplashController] AudioManager tidak ditemukan atau nama audio kosong!");
        }

        // 3. Mainkan video (jika belum terputar otomatis oleh Play On Awake)
        if (videoPlayer != null && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    // Handles the ESC skip sequence with a clean fade out
    private IEnumerator SkipSplashSequence()
    {
        isSkipping = true;

        // Stop the video loop point from firing again
        videoPlayer.loopPointReached -= OnVideoFinished;

        // Fade audio out if your AudioManager supports it (Optional)
        // If your AudioManager doesn't have a Stop / Fade, you can comment this out
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(splashAudioName))
        {
            // AudioManager.Instance.StopSFX(splashAudioName); 
        }

        // Linear fade to black screen
        if (fadeCanvasGroup != null)
        {
            float currentTime = 0f;
            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, currentTime / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // Stop playback completely right before the jump
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        TriggerSceneChange();
    }

    // Fungsi ini otomatis dipanggil saat durasi video berakhir secara natural
    private void OnVideoFinished(VideoPlayer source)
    {
        if (!isSkipping)
        {
            TriggerSceneChange();
        }
    }

    private void TriggerSceneChange()
    {
        // Pindah ke Main Menu menggunakan SceneController yang sudah kamu buat sebelumnya
        if (SceneController.Instance != null)
        {
            SceneController.Instance.ChangeSceneByName(nextSceneTransitionName,nextSceneName);
        }
        else
        {
            // Jika SceneController belum ada di scene ini, gunakan LoadScene biasa
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
