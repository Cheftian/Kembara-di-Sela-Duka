using UnityEngine;

public class PlayerParticleController : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem runParticles;
    
    [Header("Jump & Land Particle Systems (Dual Setup)")]
    [Tooltip("Partikel pertama yang aktif saat lompat/mendarat (misal: Semburan Debu)")]
    [SerializeField] private ParticleSystem jumpParticlesA;
    [Tooltip("Partikel kedua yang aktif saat lompat/mendarat (misal: Serpihan Batu / Spark)")]
    [SerializeField] private ParticleSystem jumpParticlesB;

    [Header("Jump Trail Settings")]
    [Tooltip("Seret GameObject Child yang memiliki komponen Trail Renderer ke sini")]
    [SerializeField] private TrailRenderer jumpTrail;

    [Header("Particle Rotation Settings")]
    [Tooltip("Sudut semburan partikel dari tanah (misal: 45 derajat)")]
    [SerializeField] private float emissionAngle = 45f;

    [Header("Ground & Platform Surface")]
    [SerializeField] private LayerMask particleSurfaceLayers;
    [SerializeField] private float surfaceProbeDistance = 0.15f;
    [SerializeField] private float surfaceOffset = 0.03f;

    private PlayerController player;
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private bool wasGrounded;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        if (particleSurfaceLayers.value == 0)
        {
            particleSurfaceLayers = LayerMask.GetMask("Ground", "Platform");
        }

        if (runParticles != null)
        {
            runParticles.Play();
            var emission = runParticles.emission;
            emission.enabled = false; 
        }

        if (jumpTrail != null)
        {
            jumpTrail.emitting = false;
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

            if (shouldEmit && !runParticles.isPlaying)
            {
                runParticles.Play();
            }

            if (shouldEmit)
            {
                UpdateParticleRotation();
            }
        }

        // 2. Logika Trail Melompat
        if (jumpTrail != null)
        {
            bool shouldTrail = !isCurrentlyGrounded;
            if (jumpTrail.emitting != shouldTrail)
            {
                jumpTrail.emitting = shouldTrail;
            }
        }

        // 3. Logika Partikel Mendarat (Land) - Tetap di sini karena deteksi land sudah akurat
        if (!wasGrounded && isCurrentlyGrounded)
        {
            PlayJumpParticles();
        }

        wasGrounded = isCurrentlyGrounded;
    }

    private void UpdateParticleRotation()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;

        if (horizontalInput != 0f)
        {
            float targetZRotation = (horizontalInput > 0f) ? -emissionAngle : emissionAngle;
            runParticles.transform.localRotation = Quaternion.Euler(0f, 0f, targetZRotation);
        }
    }

    // KUNCI UTAMA: Fungsi ini sekarang PUBLIC agar bisa dipanggil oleh PlayerController saat lepas landas
    public void PlayJumpParticles()
    {
        bool particleAHasSurface = TryPositionAtSurface(jumpParticlesA);
        bool particleBHasSurface = TryPositionAtSurface(jumpParticlesB);

        if (!particleAHasSurface && !particleBHasSurface)
        {
            return;
        }

        PlayParticleSystem(jumpParticlesA);
        PlayParticleSystem(jumpParticlesB);
    }

    private bool TryPositionAtSurface(ParticleSystem particleSystem)
    {
        if (particleSystem == null || playerCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Vector2 rayOrigin = playerBounds.center;
        float rayDistance = playerBounds.extents.y + Mathf.Max(0f, surfaceProbeDistance);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, particleSurfaceLayers);

        if (hit.collider == null || hit.normal.y <= 0.5f)
        {
            return false;
        }

        Vector3 particlePosition = particleSystem.transform.position;
        particlePosition.x = playerBounds.center.x;
        particlePosition.y = hit.point.y + surfaceOffset;
        particleSystem.transform.position = particlePosition;
        return true;
    }

    private static void PlayParticleSystem(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.Stop();
        particleSystem.Play();
    }
}
