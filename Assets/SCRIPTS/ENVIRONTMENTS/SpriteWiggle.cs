using UnityEngine;

public class SpriteWiggle : MonoBehaviour
{
    [Header("Wiggle Settings")]
    [Tooltip("Batas maksimum ayunan ke kanan dan kiri (dalam derajat).")]
    [Range(0f, 90f)]
    [SerializeField] private float wiggleMaxAngle = 20f;

    [Tooltip("Kecepatan dasar ayunan ilalang.")]
    [SerializeField] private float wiggleSpeed = 3f;

    [Header("Wind Turbulence (Organic Feel)")]
    [Tooltip("Semakin besar nilainya, ayunan akan semakin acak dan tidak monoton.")]
    [Range(0f, 5f)]
    [SerializeField] private float turbulenceIntensity = 0.5f;

    [Tooltip("Kecepatan perubahan pola acak angin.")]
    [SerializeField] private float turbulenceSpeed = 1.5f;

    private float startZRotation;
    private float randomOffset;

    void Start()
    {
        // Mencatat rotasi awal sumbu Z
        startZRotation = transform.localEulerAngles.z;
        if (startZRotation > 180f) startZRotation -= 360f;

        // Memberikan offset acak agar setiap ilalang tidak bergerak serentak/kembar
        randomOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        // 1. Hitung ayunan dasar menggunakan gelombang Sinus (Symmetric Wave)
        float baseWiggle = Mathf.Sin((Time.time * wiggleSpeed) + randomOffset);

        // 2. Tambahkan variasi acak yang halus menggunakan Perlin Noise (Simulasi angin alami)
        float noiseTime = (Time.time * turbulenceSpeed) + randomOffset;
        float windTurbulence = (Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f) * turbulenceIntensity;

        // 3. Gabungkan ayunan dasar dengan turbulensi angin, lalu kalikan dengan batas sudut maksimum
        float totalWiggle = (baseWiggle + windTurbulence) * wiggleMaxAngle;

        // 4. Batasi (Clamp) agar total ayunan tidak pernah melewati batas ekstrem akibat turbulensi
        totalWiggle = Mathf.Clamp(totalWiggle, -wiggleMaxAngle, wiggleMaxAngle);

        // 5. Aplikasikan rotasi ke sumbu Z
        transform.localRotation = Quaternion.Euler(0f, 0f, startZRotation + totalWiggle);
    }
}
