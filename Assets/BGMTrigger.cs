using UnityEngine;
using UnityEngine.SceneManagement; // Wajib ditambahkan untuk mengontrol event Scene

public class AudioTrigger : MonoBehaviour
{
    public enum TriggerCondition
    {
        OnSceneStart,
        OnEnable,
        OnTriggerEnter
    }

    [Header("Trigger Settings")]
    [Tooltip("Pilih kapan audio ini akan dipicu.")]
    [SerializeField] private TriggerCondition condition;
    
    [Header("Audio Names (Kosongkan jika tidak ingin diputar)")]
    [Tooltip("Nama BGM yang terdaftar di AudioManager.")]
    [SerializeField] private string bgmName;
    
    [Tooltip("Nama Ambience yang terdaftar di AudioManager.")]
    [SerializeField] private string ambienceName;

    [Header("Collider Settings (Hanya untuk OnTriggerEnter)")]
    [Tooltip("Tag objek yang bisa memicu audio (biasanya 'Player').")]
    [SerializeField] private string targetTag = "Player";

    private void Awake()
    {
        // Daftarkan fungsi ke event SceneManager agar selalu terpanggil tiap scene baru dimuat
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        // Selalu bersihkan pendaftaran event saat objek dihancurkan untuk mencegah memory leak
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // KONDISI 1: Menggantikan fungsi Start bawaan agar mendukung reload scene
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (condition == TriggerCondition.OnSceneStart)
        {
            ExecuteAudioChange();
        }
    }

    // KONDISI 2: Ketika GameObject diaktifkan (SetActive(true))
    private void OnEnable()
    {
        if (condition == TriggerCondition.OnEnable)
        {
            ExecuteAudioChange();
        }
    }

    // KONDISI 3: Ketika Player menyentuh Collider (3D)
    private void OnTriggerEnter(Collider other)
    {
        if (condition == TriggerCondition.OnTriggerEnter && other.CompareTag(targetTag))
        {
            ExecuteAudioChange();
        }
    }

    // KONDISI 3: Ketika Player menyentuh Collider (2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (condition == TriggerCondition.OnTriggerEnter && other.CompareTag(targetTag))
        {
            ExecuteAudioChange();
        }
    }

    // Fungsi Utama Eksekusi Audio secara Kondisional
    private void ExecuteAudioChange()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager tidak ditemukan di scene!");
            return;
        }

        // Cek BGM: Hanya putar jika string tidak kosong/null
        if (!string.IsNullOrEmpty(bgmName))
        {
            AudioManager.Instance.PlayBGM(bgmName);
        }

        // Cek Ambience: Hanya putar jika string tidak kosong/null
        if (!string.IsNullOrEmpty(ambienceName))
        {
            AudioManager.Instance.PlayAmbience(ambienceName);
        }
    }
}
