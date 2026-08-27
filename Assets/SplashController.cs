using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Header("Audio Settings")]
    [Tooltip("Nama SFX atau BGM splash yang terdaftar di AudioManager.")]
    [SerializeField] private string splashAudioName = "SplashIntro";

    [Header("Scene Destination")]
    [Tooltip("Nama scene berikutnya setelah splash selesai (misal MainMenu).")]
    [SerializeField] private string nextSceneName = "MainMenu";

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // Daftarkan fungsi ketika video selesai berputar
        videoPlayer.loopPointReached += OnVideoFinished;

        StartCoroutine(PrepareAndPlaySplash());
    }

    private IEnumerator PrepareAndPlaySplash()
    {
        // 1. Tunggu 1 frame untuk memastikan AudioManager (Singleton) sudah terinisialisasi di Scene
        yield return null;

        // 2. Putar audio melalui AudioManager
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(splashAudioName))
        {
            // Gunakan PlaySFX jika berupa sound effect tunggal, atau PlayBGM jika berupa musik panjang
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

    // Fungsi ini otomatis dipanggil saat durasi video berakhir
    private void OnVideoFinished(VideoPlayer source)
    {
        // Pindah ke Main Menu menggunakan SceneController yang sudah kamu buat sebelumnya
        if (SceneController.Instance != null)
        {
            SceneController.Instance.ChangeSceneByName(nextSceneName);
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
