using UnityEngine;

public class SceneTriggerOnEnable : MonoBehaviour
{
    [Header("Pengaturan Target Scene")]
    [Tooltip("Nama scene tujuan yang ingin dibuka (Pastikan sudah terdaftar di Build Settings).")]
    [SerializeField] private string targetSceneName;

    [Header("Pengaturan Transisi")]
    [Tooltip("Nama transisi yang sesuai dengan 'transitionName' di array availableTransitions milik SceneController.")]
    [SerializeField] private string transitionName = "RoomFadeIn";

    [Header("Konfigurasi Loading")]
    [Tooltip("Jika dicentang, scene akan berpindah menggunakan fungsi Loading Screen. Jika tidak, akan berpindah secara langsung.")]
    [SerializeField] private bool useLoadingScreen = true;

    private void OnEnable()
    {
        // Validasi agar tidak error jika lupa mengisi nama scene di Inspector
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[SceneTriggerOnEnable] Nama target scene di GameObject '{gameObject.name}' masih kosong!", this);
            return;
        }

        // Memastikan instance dari SceneController sudah siap di dalam scene
        if (SceneController.Instance == null)
        {
            Debug.LogError("[SceneTriggerOnEnable] SceneController.Instance tidak ditemukan di scene saat ini! Pastikan SceneController sudah ada dalam hirarki.", this);
            return;
        }

        // Eksekusi perpindahan scene berdasarkan pilihan konfigurasi
        if (useLoadingScreen)
        {
            SceneController.Instance.ChangeSceneWithLoading(transitionName, targetSceneName);
        }
        else
        {
            SceneController.Instance.ChangeSceneWithoutLoading(transitionName, targetSceneName);
        }
    }
}
