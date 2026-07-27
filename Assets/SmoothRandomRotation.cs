using System.Collections;
using UnityEngine;

public class SmoothRandomRotation : MonoBehaviour
{
    [Header("Rotation Limits (Degrees)")]
    [Tooltip("Batas rotasi maksimum ke kiri (Z positif).")]
    [SerializeField] private float maxLeftRotation = 45f;
    
    [Tooltip("Batas rotasi maksimum ke kanan (Z negatif).")]
    [SerializeField] private float maxRightRotation = 45f;

    [Header("Time Settings (Seconds)")]
    [Tooltip("Waktu minimum yang dibutuhkan untuk mencapai target rotasi baru.")]
    [SerializeField] private float minDuration = 0.5f;

    [Tooltip("Waktu maksimum yang dibutuhkan untuk mencapai target rotasi baru.")]
    [SerializeField] private float maxDuration = 2.0f;

    private float startZRotation;
    private float targetZRotation;
    private float currentVelocity;
    private float currentDuration;

    void Start()
    {
        // Catat rotasi awal objek (biasanya 0 jika baru dipasang)
        startZRotation = transform.localEulerAngles.z;
        
        // Memulai siklus penentuan target rotasi acak
        StartCoroutine(RandomRotationRoutine());
    }

    void Update()
    {
        // Mengambil sudut rotasi Z saat ini
        float currentZ = transform.localEulerAngles.z;

        // Mengonversi sudut Unity (0-360) ke sudut relatif (-180 sampai 180) agar perhitungan matematika SmoothDamp akurat
        if (currentZ > 180f) currentZ -= 360f;

        // Gerakkan rotasi Z secara halus menuju target menggunakan SmoothDamp
        float newZ = Mathf.SmoothDamp(currentZ, targetZRotation, ref currentVelocity, currentDuration);

        // Terapkan rotasi baru ke objek pada sumbu Z (cocok untuk game 2D/UI)
        transform.localRotation = Quaternion.Euler(0f, 0f, newZ);
    }

    /// <summary>
    /// Coroutine yang berjalan terus-menerus untuk mengacak sudut dan durasi rotasi
    /// </summary>
    private IEnumerator RandomRotationRoutine()
    {
        while (true)
        {
            // 1. Acak target rotasi baru berdasarkan batas kanan (-) dan kiri (+)
            // Contoh: jika maxRight=45 dan maxLeft=45, range-nya adalah -45 sampai +45
            targetZRotation = startZRotation + Random.Range(-maxRightRotation, maxLeftRotation);

            // 2. Acak durasi waktu (kecepatan) untuk mencapai target tersebut
            currentDuration = Random.Range(minDuration, maxDuration);

            // 3. Tunggu hingga durasi acak tersebut selesai sebelum mengacak target baru lagi
            yield return new WaitForSeconds(currentDuration);
        }
    }
}
