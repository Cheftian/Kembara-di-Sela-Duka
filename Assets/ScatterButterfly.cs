using UnityEngine;

public class ScatterButterfly : MonoBehaviour
{
    [Header("Flight Speed")]
    [Tooltip("Rentang kecepatan terbang acak untuk kupu-kupu ini.")]
    public float minSpeed = 3f;
    public float maxSpeed = 6f;

    [Header("Distance & Fade")]
    [Tooltip("Batas jarak terbang (acak) sebelum kupu-kupu mulai menghilang.")]
    public float minFlightDistance = 2f;
    public float maxFlightDistance = 5f;
    [Tooltip("Kecepatan memudarnya opasitas kupu-kupu.")]
    public float fadeSpeed = 2f;

    [Header("Visual Orientation")]
    [Tooltip("Centang jika default sprite menghadap ke kanan.")]
    public bool spriteFacesRight = true;

    // Variabel Internal
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isFlying = false;
    private Vector3 flightDirection;
    private float targetDistance;
    private float distanceTraveled = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Matikan RevealerTool di awal game sebelum terbang
        RevealerTool tool = GetComponent<RevealerTool>();
        if (tool != null)
        {
            tool.enabled = false;
        }

        // Desinkronisasi awal animasi agar sayap tidak mengepak kompak
        if (animator != null)
        {
            animator.Update(Random.Range(0f, 5f));
        }
    }

    // Fungsi ini dipanggil oleh script Parent saat tombol S ditekan
    public void StartScatterFlight()
    {
        // 1. Aktifkan script RevealerTool jika ada pada GameObject ini
        RevealerTool tool = GetComponent<RevealerTool>();
        if (tool != null)
        {
            tool.enabled = true;
        }

        // 2. Tambahkan komponen Rigidbody2D secara runtime dan set ke Kinematic
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true; 
            rb.useFullKinematicContacts = false;
        }

        // 3. Tentukan arah semprotan 360 derajat yang acak (Unik untuk tiap kupu-kupu)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
        
        // Gabungkan arah dengan kecepatan acak
        float randomSpeed = Random.Range(minSpeed, maxSpeed);
        flightDirection = direction * randomSpeed;

        // 4. Tentukan batas jarak terbang sebelum memudar
        targetDistance = Random.Range(minFlightDistance, maxFlightDistance);

        // 5. Hadapkan sprite sesuai arah gerak horizontalnya (X positif atau negatif)
        HandleInitialFlip(direction.x);

        isFlying = true;
    }

    void Update()
    {
        if (!isFlying) return;

        // Hitung jarak pergerakan frame ini
        Vector3 movement = flightDirection * Time.deltaTime;
        transform.position += movement;
        distanceTraveled += movement.magnitude;

        // Jika jarak tempuh sudah melewati batas acak, turunkan opasitas (Fade Out)
        if (distanceTraveled >= targetDistance)
        {
            FadeAndDestroy();
        }
    }

    void HandleInitialFlip(float directionX)
    {
        Vector3 scale = transform.localScale;
        
        // Membalik sprite secara horizontal berdasarkan nilai X dari arah terbang
        if (directionX > 0.01f) // Bergerak ke Kanan
        {
            scale.x = spriteFacesRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        }
        else if (directionX < -0.01f) // Bergerak ke Kiri
        {
            scale.x = spriteFacesRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        }
        
        transform.localScale = scale;
    }

    void FadeAndDestroy()
    {
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a -= fadeSpeed * Time.deltaTime;
            spriteRenderer.color = currentColor;

            if (currentColor.a <= 0f)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
