using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem runParticles;
    [SerializeField] private ParticleSystem landParticles;

    private PlayerController player;
    private Rigidbody2D rb;
    private bool wasGrounded;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        // KUNCI OTOMATIS: Memaksa partikel lari untuk "Play" di awal agar sistem internal Unity-nya aktif
        if (runParticles != null)
        {
            runParticles.Play();
            var emission = runParticles.emission;
            emission.enabled = false; // Matikan emisinya dulu, nanti dinyalakan lewat Update
        }
    }

    private void Update()
    {
        if (player == null || rb == null) return;

        bool isCurrentlyGrounded = player.isGrounded;

        // 1. Logika Partikel Berlari
        if (runParticles != null)
        {
            bool holdingShift = Input.GetKey(KeyCode.LeftShift);
            bool pressingMoveKeys = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
            bool notDizzy = !player.IsDizzy;

            bool shouldEmit = holdingShift && pressingMoveKeys && isCurrentlyGrounded && notDizzy;

            var emission = runParticles.emission;
            if (emission.enabled != shouldEmit)
            {
                emission.enabled = shouldEmit;
            }

            // Pengaman tambahan: Jika partikel mendadak berhenti (Stopped), paksa Play kembali
            if (shouldEmit && !runParticles.isPlaying)
            {
                runParticles.Play();
            }
        }

        // 2. Logika Partikel Mendarat
        if (!wasGrounded && isCurrentlyGrounded)
        {
            PlayParticle(landParticles);
        }

        wasGrounded = isCurrentlyGrounded;
    }

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle != null)
        {
            particle.Play();
        }
    }
}
