using UnityEngine;
using UnityEngine.EventSystems; // Wajib untuk mendeteksi hover

public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("SFX Names (Kosongkan jika tidak ingin dipakai)")]
    [Tooltip("Nama SFX saat kursor masuk/hover ke tombol.")]
    [SerializeField] private string hoverSFX = "Button-Hover";

    [Tooltip("Nama SFX saat tombol diklik.")]
    [SerializeField] private string clickSFX = "Button-Click";

    // Dipicu otomatis oleh Unity saat kursor masuk ke area tombol
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(hoverSFX) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hoverSFX);
        }
    }

    // Dipicu otomatis oleh Unity saat tombol diklik
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(clickSFX) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSFX);
        }
    }
}
