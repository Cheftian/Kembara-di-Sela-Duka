using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettings : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    // Nama parameter harus persis sama dengan nama di Exposed Parameters Mixer
    private const string BGM_PARAM = "BGMVolume";
    private const string SFX_PARAM = "SFXVolume";

    private AudioMixer mixer;

    private void Start()
    {
        // Ambil referensi mixer dari AudioManager
        if (AudioManager.Instance != null)
        {
            mixer = AudioManager.Instance.GetAudioMixer();
        }

        if (mixer == null)
        {
            Debug.LogError("Audio Mixer tidak ditemukan melalui AudioManager!");
            return;
        }

        // Ambil data volume yang tersimpan, atau set ke default (0.75f) jika belum ada data
        float savedBGM = PlayerPrefs.GetFloat(BGM_PARAM, 0.75f);
        float savedSFX = PlayerPrefs.GetFloat(SFX_PARAM, 0.75f);

        // Set posisi visual slider UI sesuai data yang dimuat
        if (bgmSlider != null)
        {
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // Terapkan volume ke Mixer saat pertama kali game dibuka
        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);
    }

    // Fungsi pengubah volume BGM & Ambience
    public void SetBGMVolume(float value)
    {
        // Rumus Log10 untuk konversi nilai slider (0-1) menjadi desibel (-80 hingga 0)
        float decibel = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;
        
        mixer.SetFloat(BGM_PARAM, decibel);
        PlayerPrefs.SetFloat(BGM_PARAM, value); // Simpan pengaturan
    }

    // Fungsi pengubah volume SFX
    public void SetSFXVolume(float value)
    {
        float decibel = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;

        mixer.SetFloat(SFX_PARAM, decibel);
        PlayerPrefs.SetFloat(SFX_PARAM, value); // Simpan pengaturan
    }
}
