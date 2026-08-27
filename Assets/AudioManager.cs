using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic; // Wajib untuk menggunakan Queue

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public struct SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Tooltip("Jika dicentang, BGM ini hanya main 1x dan BGM lain harus mengantre sampai BGM ini selesai.")]
        public bool playOnce; // Flag baru
    }

    [Header("Audio Collections")]
    [SerializeField] private SoundEffect[] bgmList;
    [SerializeField] private SoundEffect[] sfxList;
    [SerializeField] private SoundEffect[] ambienceList;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambienceSource;

    // Sistem Antrean untuk BGM
    private Queue<SoundEffect> bgmQueue = new Queue<SoundEffect>();
    private bool isBGMCoroutineRunning = false;
    private SoundEffect currentBGM;

    [Header("SFX Pool Settings")]
    [Tooltip("Jumlah awal AudioSource cadangan yang dibuat saat game start.")]
    [SerializeField] private int initialSfxPoolSize = 6;
    // List untuk menyimpan semua AudioSource SFX yang aktif di dalam pool
    private List<AudioSource> sfxPool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        if (bgmSource != null) bgmSource.loop = false; // Diubah ke false, kontrol loop diatur via Coroutine
        if (ambienceSource != null) ambienceSource.loop = true;
        if (sfxSource != null) sfxSource.loop = false;

        if (bgmSource != null && bgmGroup != null) bgmSource.outputAudioMixerGroup = bgmGroup;
        if (ambienceSource != null && bgmGroup != null) ambienceSource.outputAudioMixerGroup = bgmGroup;
        if (sfxSource != null && sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
    }

    public AudioMixer GetAudioMixer() => audioMixer;

    // ==========================================
    // LOGIKA PERUBAHAN BGM DENGAN ANTREAN
    // ==========================================
    public void PlayBGM(string soundName)
    {
        SoundEffect sound = FindSoundEffect(soundName, bgmList);
        if (sound.clip == null) return;

        // Jika lagu yang sama persis sedang dimainkan atau mengantre, abaikan agar tidak double
        if (bgmSource.clip == sound.clip && bgmSource.isPlaying) return;
        foreach (var queuedSound in bgmQueue)
        {
            if (queuedSound.clip == sound.clip) return;
        }

        // Masukkan permintaan BGM baru ke dalam antrean
        bgmQueue.Enqueue(sound);

        // Jika Coroutine pengatur antrean belum jalan, jalankan sekarang
        if (!isBGMCoroutineRunning)
        {
            StartCoroutine(BGMQueueProcessor());
        }
    }

    private IEnumerator BGMQueueProcessor()
    {
        isBGMCoroutineRunning = true;

        while (bgmQueue.Count > 0)
        {
            // Ambil lagu berikutnya dari antrean
            currentBGM = bgmQueue.Dequeue();

            bgmSource.clip = currentBGM.clip;
            bgmSource.loop = !currentBGM.playOnce; // Loop mati jika playOnce dicentang
            bgmSource.Play();

            // Jika lagu diset "Play Once", coroutine akan MENUNGGU hingga lagu ini selesai berputar
            if (currentBGM.playOnce)
            {
                // Tunggu sampai durasi lagu habis, ATAU sampai lagu dihentikan paksa/berubah clip
                yield return new WaitWhile(() => bgmSource.isPlaying && bgmSource.clip == currentBGM.clip);
            }
            else
            {
                // Jika lagu biasa (looping), coroutine berhenti di sini. 
                // Lagu ini akan terus berputar selamanya sampai ada PlayBGM() baru yang masuk antrean.
                isBGMCoroutineRunning = false;
                yield break;
            }
        }

        isBGMCoroutineRunning = false;
    }

    public void StopBGM()
    {
        bgmQueue.Clear(); // Bersihkan antrean jika dihentikan paksa
        if (bgmSource != null) bgmSource.Stop();
        isBGMCoroutineRunning = false;
    }

    // ==========================================
    // FUNGSI LAINNYA (TETAP SAMA)
    // ==========================================
    public void PlayAmbience(string soundName)
    {
        SoundEffect sound = FindSoundEffect(soundName, ambienceList);
        if (sound.clip == null) return;
        if (ambienceSource.clip == sound.clip && ambienceSource.isPlaying) return;

        ambienceSource.clip = sound.clip;
        ambienceSource.Play();
    }

    // Panggil fungsi ini di dalam Awake() setelah SetupAudioSources()
    private void InitializeSFXPool()
    {
        for (int i = 0; i < initialSfxPoolSize; i++)
        {
            CreateNewSFXSource();
        }
    }

    // Helper untuk melahirkan objek AudioSource baru yang terhubung ke SFX Mixer Group
    private AudioSource CreateNewSFXSource()
    {
        GameObject sfxObj = new GameObject($"SFX_Source_{sfxPool.Count}");
        sfxObj.transform.SetParent(this.transform); // Masukkan jadi child AudioManager agar rapi
        
        AudioSource newSource = sfxObj.AddComponent<AudioSource>();
        newSource.loop = false;
        newSource.playOnAwake = false;
        
        if (sfxGroup != null)
        {
            newSource.outputAudioMixerGroup = sfxGroup;
        }

        sfxPool.Add(newSource);
        return newSource;
    }

    // Fungsi untuk mencari AudioSource yang sedang menganggur (tidak memutar suara)
    private AudioSource GetAvailableSFXSource()
    {
        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (!sfxPool[i].isPlaying)
            {
                return sfxPool[i];
            }
        }

        // JIKA SEMUA SIBUK: Otomatis buat AudioSource baru secara dinamis
        return CreateNewSFXSource();
    }

    // ==========================================
    // UPDATE LOGIKA FUNGSI PLAYSFX UTAMA
    // ==========================================
    public void PlaySFX(string soundName)
    {
        SoundEffect sound = FindSoundEffect(soundName, sfxList);
        if (sound.clip == null) return;

        // Ambil AudioSource yang sedang bebas/tidak dipakai
        AudioSource freeSource = GetAvailableSFXSource();
        
        // Mainkan secara mandiri pada AudioSource tersebut sampai selesai tanpa memotong yang lain
        freeSource.clip = sound.clip;
        freeSource.Play();
    }

    public void StopAmbience() => ambienceSource?.Stop();

    // Fungsi helper pencari Struct SoundEffect lengkap
    private SoundEffect FindSoundEffect(string soundName, SoundEffect[] list)
    {
        SoundEffect sound = Array.Find(list, item => item.name.ToLower() == soundName.ToLower());
        if (sound.clip == null)
        {
            Debug.LogWarning($"Audio dengan nama '{soundName}' tidak ditemukan!");
        }
        return sound;
    }
}
