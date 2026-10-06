using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPTypewriterRandom : MonoBehaviour
{
    [Header("Pengaturan Mengetik (Maju)")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private int randomCycles = 3;
    [SerializeField] private float randomSpeed = 0.01f;

    [Header("Pengaturan Fade Out (Mundur)")]
    [Tooltip("Durasi waktu (detik) yang dibutuhkan teks untuk memudar sampai hilang.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private TMP_Text textComponent;
    private string fullText;
    private Coroutine currentCoroutine;
    private Color originalColor;
    private bool isFading = false;

    private const string RandomChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        fullText = textComponent.text;
        // Simpan warna asli teks (termasuk alpha penuh)
        originalColor = textComponent.color;
    }

    private void OnEnable()
    {
        isFading = false; 
        textComponent.text = "";
        // Kembalikan warna teks ke semula (tidak transparan) saat diaktifkan lagi
        textComponent.color = originalColor;
        
        ResetAndStartCoroutine(TypeTextEffect());
    }

    // PANGGIL FUNGSI INI DARI TIMELINE (MENGGUNAKAN SIGNAL) SEBELUM GAMEOBJECT DINONAKTIFKAN
    public void StartHapusTeks()
    {
        if (isFading) return; // Mencegah terpanggil dua kali
        
        isFading = true;
        ResetAndStartCoroutine(FadeOutTextEffect());
    }

    private IEnumerator TypeTextEffect()
    {
        string currentDisplayedText = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (isFading) yield break; // Hentikan ngetik jika perintah fade masuk

            if (fullText[i] == ' ')
            {
                currentDisplayedText += " ";
                textComponent.text = currentDisplayedText;
                continue;
            }

            for (int j = 0; j < randomCycles; j++)
            {
                char randomChar = RandomChars[Random.Range(0, RandomChars.Length)];
                textComponent.text = currentDisplayedText + randomChar;
                yield return new WaitForSeconds(randomSpeed);
            }

            currentDisplayedText += fullText[i];
            textComponent.text = currentDisplayedText;

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private IEnumerator FadeOutTextEffect()
    {
        float currentTime = 0f;
        Color startColor = textComponent.color;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            // Hitung nilai alpha baru secara bertahap dari 1 ke 0
            float alpha = Mathf.Lerp(startColor.a, 0f, currentTime / fadeDuration);
            
            // Terapkan warna baru dengan alpha yang sudah berkurang
            textComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }

        // Pastikan benar-benar transparan di akhir
        textComponent.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        // Opsional: Jika ingin otomatis mati setelah fade selesai, hapus komentar di bawah ini:
        // gameObject.SetActive(false);
    }

    private void ResetAndStartCoroutine(IEnumerator newCoroutine)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(newCoroutine);
    }
}
