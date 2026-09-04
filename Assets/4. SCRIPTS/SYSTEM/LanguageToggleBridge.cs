using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class LanguageToggleBridge : MonoBehaviour
{
    private Toggle toggle;
    private Image toggleImage;

    [Header("Sprite Settings")]
    [Tooltip("Target Image yang akan diganti spritenya (biasanya objek 'Background' milik Toggle)")]
    [SerializeField] private Image targetGraphicImage;
    
    [Tooltip("Sprite saat kondisi ON (Bahasa Inggris)")]
    [SerializeField] private Sprite spriteEnglish;
    
    [Tooltip("Sprite saat kondisi OFF (Bahasa Indonesia)")]
    [SerializeField] private Sprite spriteIndonesia;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        
        // Jika Target Graphic belum diisi di Inspector, otomatis ambil dari target bawaan Toggle
        if (targetGraphicImage == null && toggle.targetGraphic != null)
        {
            targetGraphicImage = toggle.targetGraphic.GetComponent<Image>();
        }
    }

    void Start()
    {
        if (GameSettingsManager.Instance != null)
        {
            // Lepas listener sementara agar tidak memicu event ganda saat inisialisasi awal
            toggle.onValueChanged.RemoveListener(OnToggleChanged);

            // Set kondisi default ON (English) atau OFF (Indonesia) berdasarkan data tersimpan
            toggle.isOn = GameSettingsManager.Instance.IsEnglish;

            // Update visual sprite pertama kali saat scene dimuat
            UpdateToggleVisual(toggle.isOn);

            // Pasang kembali listener untuk mendeteksi klik pemain
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    void OnToggleChanged(bool value)
    {
        if (GameSettingsManager.Instance != null)
        {
            // Kirim status ke global manager
            GameSettingsManager.Instance.SetLanguage(value);
            
            // Ubah visual sprite secara instan
            UpdateToggleVisual(value);
        }
    }

    private void UpdateToggleVisual(bool isEn)
    {
        if (targetGraphicImage == null) return;

        // JIKA TRUE (ON) -> Pakai Sprite English, JIKA FALSE (OFF) -> Pakai Sprite Indonesia
        targetGraphicImage.sprite = isEn ? spriteEnglish : spriteIndonesia;
    }

    void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
