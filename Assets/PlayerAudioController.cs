using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D rb;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip flipClip;

    // AudioSource Komponen
    private AudioSource movementAudioSource; // Khusus untuk Walk & Run
    private AudioSource effectsAudioSource;  // Khusus One-Shot (Jump, Land, Flip)

    // State tracking internal
    private float movementTimer = 0f;
    private bool wasGroundedLastFrame = true;
    private bool wasFlippingLastFrame = false;
    
    // KUNCI PERBAIKAN: Menyimpan status klip terakhir yang aktif
    private AudioClip lastActiveClip = null;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Setup AudioSource Utama (Untuk Walk & Run)
        movementAudioSource = GetComponent<AudioSource>();
        movementAudioSource.loop = false; // Kita handle perulangan manual agar bisa mendeteksi pergantian klip secara instan
        movementAudioSource.playOnAwake = false;
        movementAudioSource.pitch = 1.0f;

        // Setup AudioSource kedua untuk efek sekali putar
        effectsAudioSource = gameObject.AddComponent<AudioSource>();
        effectsAudioSource.playOnAwake = false;
        effectsAudioSource.loop = false;
        effectsAudioSource.pitch = 1.0f;
    }

    private void Start()
    {
        if (player != null)
        {
            wasGroundedLastFrame = player.isGrounded;
        }
    }

    private void Update()
    {
        if (player == null || rb == null) return;

        HandleFootsteps();
        HandleOneShotEffects();
    }

    private void HandleFootsteps()
    {
        // 1. Ambil input deteksi lari langsung dari kondisi: Bergerak + Di Tanah + Tombol Shift ditekan
        // Cara ini menjamin sfx lari langsung responsif tanpa menunggu state internal yang tertunda
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift);
        
        bool isWalking = player.isGrounded && isMoving && !isHoldingShift && !player.IsDizzy;
        bool isRunning = player.isGrounded && isMoving && isHoldingShift && !player.IsDizzy;
        bool isDizzyWalk = player.isGrounded && isMoving && player.IsDizzy;

        if (isWalking || isRunning || isDizzyWalk)
        {
            // Tentukan klip target berdasarkan input frame ini
            AudioClip targetClip = isRunning ? runClip : walkClip;

            if (targetClip != null)
            {
                // KUNCI UTAMA: Jika mendadak ganti jenis gerakan (misal: dari jalan ke lari, atau lari ke jalan)
                // Potong paksa audio lama dan langsung putar audio yang baru tanpa menunggu timer habis!
                if (targetClip != lastActiveClip)
                {
                    movementAudioSource.Stop();
                    movementAudioSource.clip = targetClip;
                    movementAudioSource.Play();
                    
                    movementTimer = targetClip.length; // Reset durasi timer untuk klip baru
                    lastActiveClip = targetClip;       // Catat status klip terbaru
                }
                else
                {
                    // Jika jenis gerakan masih sama (konstan), jalankan sistem hitung mundur loop normal Anda
                    movementTimer -= Time.deltaTime;

                    if (movementTimer <= 0f)
                    {
                        movementAudioSource.clip = targetClip;
                        movementAudioSource.Play();
                        movementTimer = targetClip.length; // Loop ulang saat lagu selesai penuh
                    }
                }
            }
        }
        else
        {
            // Jika karakter diam atau melompat, matikan suara langkah kaki seketika
            if (movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }
            movementTimer = 0f;
            lastActiveClip = null; // Kosongkan ingatan klip agar saat mulai bergerak lagi langsung responsif
        }
    }

    private void HandleOneShotEffects()
    {
        // 1. Deteksi JUMP
        if (wasGroundedLastFrame && !player.isGrounded && rb.linearVelocity.y > 0.1f)
        {
            PlayOneShot(jumpClip);
        }

        // 2. Deteksi LAND
        if (!wasGroundedLastFrame && player.isGrounded)
        {
            PlayOneShot(landClip);
        }

        // 3. Deteksi FLIP
        bool isCurrentlyFlipping = CheckIfFlipping();
        if (!wasFlippingLastFrame && isCurrentlyFlipping)
        {
            PlayOneShot(flipClip);
        }

        wasGroundedLastFrame = player.isGrounded;
        wasFlippingLastFrame = isCurrentlyFlipping;
    }

    private bool CheckIfFlipping()
    {
        if (player == null) return false;
        
        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        if (playerAnimator != null)
        {
            return playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Flip");
        }
        return false;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && effectsAudioSource != null)
        {
            effectsAudioSource.PlayOneShot(clip);
        }
    }
}
