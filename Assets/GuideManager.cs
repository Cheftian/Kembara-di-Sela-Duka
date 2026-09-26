using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GuideManager : MonoBehaviour
{
    public static GuideManager Instance { get; private set; }

    [System.Serializable]
    public struct GuideData
    {
        public string guideID;             // ID Unik untuk memanggil guide (misal: "Walk", "Jump")
        public Sprite guideSpriteEN;       // Gambar versi Bahasa Inggris
        public Sprite guideSpriteID;       // Gambar versi Bahasa Indonesia
        public KeyCode[] requiredKeys;     // Tombol syarat untuk menghentikan guide lebih cepat
        public float displayDuration;      // Durasi guide diam di layar sebelum otomatis hilang
    }

    [Header("UI Component")]
    [SerializeField] private Image guideImageDisplay; // UI Image komponen tempat menampilkan gambar

    [Header("Guide Database")]
    [SerializeField] private GuideData[] guides;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private GuideData currentGuide;
    private bool isGuideActive = false;
    private Coroutine activeGuideCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Memastikan UI Image memiliki CanvasGroup untuk efek Fade Alpha
        canvasGroup = guideImageDisplay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = guideImageDisplay.gameObject.AddComponent<CanvasGroup>();
        }

        // Sembunyikan UI di awal game
        canvasGroup.alpha = 0f;
        guideImageDisplay.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isGuideActive) return;

        // Cek apakah player menekan salah satu tombol syarat untuk menyudahi guide
        if (currentGuide.requiredKeys != null && currentGuide.requiredKeys.Length > 0)
        {
            foreach (KeyCode key in currentGuide.requiredKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    StopActiveGuide();
                    break;
                }
            }
        }
    }

    public void ShowGuide(string id)
    {
        GuideData targetGuide = System.Array.Find(guides, g => g.guideID == id);

        if (string.IsNullOrEmpty(targetGuide.guideID))
        {
            Debug.LogError($"Guide dengan ID '{id}' tidak ditemukan di GuideManager!");
            return;
        }

        if (activeGuideCoroutine != null)
        {
            StopCoroutine(activeGuideCoroutine);
        }

        activeGuideCoroutine = StartCoroutine(GuideSequence(targetGuide));
    }

    private IEnumerator GuideSequence(GuideData guide)
    {
        currentGuide = guide;
        isGuideActive = true;

        // Tentukan gambar berdasarkan bahasa aktif di NarrationManager
        // Jika NarrationManager.Instance bernilai null, default kembali menggunakan properti internal di sana jika bisa diakses
        Sprite selectedSprite = guide.guideSpriteID; // Default Indonesia
        
        if (NarrationManager.Instance != null)
        {
            // Menggunakan teknik refleksi karena variabel currentLanguage di NarrationManager bersifat private.
            // Jika Anda mengubah currentLanguage menjadi public / membuat Properti Getter-nya, 
            // Anda bisa langsung mengganti baris di bawah dengan: NarrationManager.Instance.CurrentLanguage
            var field = typeof(NarrationManager).GetField("currentLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var lang = field.GetValue(NarrationManager.Instance).ToString();
                if (lang == "English")
                {
                    selectedSprite = guide.guideSpriteEN;
                }
            }
        }

        guideImageDisplay.sprite = selectedSprite;
        guideImageDisplay.gameObject.SetActive(true);

        // --- FADE IN ---
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // --- DIAM UNTUK BEBERAPA SAAT ---
        yield return new WaitForSeconds(guide.displayDuration);

        // --- FADE OUT AUTOMATIC (Jika durasi habis tanpa ditekan tombol syarat) ---
        StopActiveGuide();
    }

    private void StopActiveGuide()
    {
        if (!isGuideActive) return;
        
        isGuideActive = false;
        if (activeGuideCoroutine != null) StopCoroutine(activeGuideCoroutine);
        activeGuideCoroutine = StartCoroutine(FadeOutSequence());
    }

    private IEnumerator FadeOutSequence()
    {
        // --- FADE OUT ---
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(startAlpha * (1f - (elapsed / fadeDuration)));
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        guideImageDisplay.gameObject.SetActive(false);
    }
}
