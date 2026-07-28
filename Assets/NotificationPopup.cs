using UnityEngine;
using System.Collections;

public class NotificationPopup : MonoBehaviour
{
    [Header("Pengaturan Animasi")]
    [Tooltip("Durasi animasi pop up dan pop down dalam detik")]
    public float duration = 0.2f;

    private Coroutine activeCoroutine;
    private Vector3 originalScale;

    private void Awake()
    {
        // Menyimpan ukuran asli objek (biasanya Vector3.one atau 1,1,1)
        originalScale = transform.localScale;
        
        // Memastikan objek langsung tidak terlihat saat pertama kali game dimulai
        transform.localScale = Vector3.zero;
    }

    public void Show()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(ScaleOverTime(originalScale));
    }

    public void Hide()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(ScaleOverTime(Vector3.zero));
    }

    private IEnumerator ScaleOverTime(Vector3 targetScale)
    {
        Vector3 initialScale = transform.localScale;
        float time = 0;

        while (time < duration)
        {
            // Menggunakan Lerp untuk transisi linear yang halus
            transform.localScale = Vector3.Lerp(initialScale, targetScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }
}
