using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Pulse Settings (Constant)")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.05f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleMultiplier = 1.15f;
    [SerializeField] private float transitionSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isHovered = false;

    private void Start()
    {
        // Menyimpan ukuran asli tombol saat game dimulai
        originalScale = transform.localScale;
        targetScale = originalScale;
    }
    private void Update()
    {
        if (!isHovered)
        {
            // SOLUSI: Menggunakan Time.unscaledTime agar rumus sinus tetap bergerak saat game dipause
            float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            targetScale = originalScale + new Vector3(pulse, pulse, 0);
            
            // Terapkan skala secara langsung saat pulse
            transform.localScale = targetScale;
        }
        else
        {
            // Efek Hover (Menggunakan ukuran target hover)
            Vector3 hoverScale = originalScale * hoverScaleMultiplier;
            
            // SOLUSI: Menggunakan Time.unscaledDeltaTime agar transisi Lerp tidak membeku saat game dipause
            transform.localScale = Vector3.Lerp(transform.localScale, hoverScale, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    // Dipanggil otomatis oleh Unity saat kursor masuk ke area tombol
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // Dipanggil otomatis oleh Unity saat kursor keluar dari area tombol
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // Opsional: Reset skala jika tombol dinonaktifkan agar tidak bug
    private void OnDisable()
    {
        isHovered = false;
        transform.localScale = originalScale;
    }
}
