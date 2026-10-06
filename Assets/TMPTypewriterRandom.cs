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
        originalColor = textComponent.color;
    }

    private void OnEnable()
    {
        isFading = false; 
        textComponent.text = "";
        textComponent.color = originalColor;
        
        ResetAndStartCoroutine(TypeTextEffect());
    }

    public void StartHapusTeks()
    {
        if (isFading) return; 
        
        isFading = true;
        ResetAndStartCoroutine(FadeOutTextEffect());
    }

    private IEnumerator TypeTextEffect()
    {
        string currentDisplayedText = "";
        int i = 0;

        while (i < fullText.Length)
        {
            if (isFading) yield break;

            // DETEKSI RICH TEXT TAG (Contoh: <b>, <i>, <color=red>)
            if (fullText[i] == '<')
            {
                // Ambil seluruh tag dari '<' sampai '>' sekaligus
                string tag = "";
                while (i < fullText.Length && fullText[i] != '>')
                {
                    tag += fullText[i];
                    i++;
                }
                if (i < fullText.Length)
                {
                    tag += fullText[i]; // Tambahkan karakter '>'
                    i++;
                }

                // Langsung masukkan tag ke teks yang ditampilkan tanpa animasi acak
                currentDisplayedText += tag;
                textComponent.text = currentDisplayedText;
                continue; // Lanjut ke iterasi berikutnya (bisa berupa teks biasa atau tag lain)
            }

            // DETEKSI SPASI BIASA
            if (fullText[i] == ' ')
            {
                currentDisplayedText += " ";
                textComponent.text = currentDisplayedText;
                i++;
                continue;
            }

            // ANIMASI ACAK UNTUK HURUF BIASA
            for (int j = 0; j < randomCycles; j++)
            {
                char randomChar = RandomChars[Random.Range(0, RandomChars.Length)];
                textComponent.text = currentDisplayedText + randomChar;
                yield return new WaitForSeconds(randomSpeed);
            }

            // Tambahkan huruf asli yang benar
            currentDisplayedText += fullText[i];
            textComponent.text = currentDisplayedText;
            i++;

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
            float alpha = Mathf.Lerp(startColor.a, 0f, currentTime / fadeDuration);
            textComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        textComponent.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
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
