using UnityEngine;

public class ClampedParallaxSmooth : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Tarik GameObject Player ke sini.")]
    [SerializeField] private Transform playerTransform;

    [Header("Player Movement Boundaries")]
    [Tooltip("Batas minimum koordinat X & Y pergerakan Player di dalam map.")]
    [SerializeField] private Vector2 playerMinPosition;
    
    [Tooltip("Batas maksimum koordinat X & Y pergerakan Player di dalam map.")]
    [SerializeField] private Vector2 playerMaxPosition;

    [Header("Background Boundaries")]
    [Tooltip("Posisi X & Y background saat Player berada di Batas Minimum (playerMinPosition)")]
    [SerializeField] private Vector2 bgMinPosition;
    
    [Tooltip("Posisi X & Y background saat Player berada di Batas Maksimum (playerMaxPosition)")]
    [SerializeField] private Vector2 bgMaxPosition;

    [Header("Smooth Settings")]
    [Tooltip("Semakin kecil nilainya, semakin responsif background mengikuti pergerakan player.")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothTime = 0.25f;

    [Header("Axis Settings")]
    [SerializeField] private bool scrollHorizontal = true;
    [SerializeField] private bool scrollVertical = false;

    private Vector3 currentVelocity = Vector3.zero;
    private float initialZ;

    void Start()
    {
        // Cari otomatis jika playerTransform belum diisi di Inspector (dengan tag "Player")
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        initialZ = transform.position.z;
    }

    // Menggunakan LateUpdate agar bergerak setelah Player selesai bergerak di Update/FixedUpdate
    void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        
        // Mulai dari posisi background saat ini
        float targetX = transform.position.x;
        float targetY = transform.position.y;

        // 1. Hitung posisi target ideal background berdasarkan posisi Player
        if (scrollHorizontal && playerMaxPosition.x != playerMinPosition.x)
        {
            // Ambil persentase posisi Player di antara batas kiri dan kanan map
            float tX = Mathf.InverseLerp(playerMinPosition.x, playerMaxPosition.x, playerPos.x);
            // Petakan persentase tersebut ke batas pergerakan background
            targetX = Mathf.Lerp(bgMinPosition.x, bgMaxPosition.x, tX);
        }

        if (scrollVertical && playerMaxPosition.y != playerMinPosition.y)
        {
            // Ambil persentase posisi Player di antara batas bawah dan atas map
            float tY = Mathf.InverseLerp(playerMinPosition.y, playerMaxPosition.y, playerPos.y);
            // Petakan persentase tersebut ke batas pergerakan background
            targetY = Mathf.Lerp(bgMinPosition.y, bgMaxPosition.y, tY);
        }

        Vector3 targetPosition = new Vector3(targetX, targetY, initialZ);

        // 2. Gerakkan secara smooth mengikuti target ideal tersebut
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref currentVelocity, 
            smoothTime
        );
    }
}
