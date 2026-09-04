using UnityEngine;

public class SpriteWiggle : MonoBehaviour
{
    [Header("Wiggle Settings")]
    [Range(0f, 90f)] [SerializeField] private float wiggleMaxAngle = 20f;
    [SerializeField] private float wiggleSpeed = 3f;

    [Header("Wind Turbulence (Organic Feel)")]
    [Range(0f, 5f)] [SerializeField] private float turbulenceIntensity = 0.5f;
    [SerializeField] private float turbulenceSpeed = 1.5f;

    private float startZRotation;
    private float randomOffset;

    void Start()
    {
        startZRotation = transform.localEulerAngles.z;
        if (startZRotation > 180f) startZRotation -= 360f;

        randomOffset = Random.Range(0f, 1000f);

        // Nonaktifkan script di awal, biarkan OnBecameVisible yang mengontrol keaktifannya
        enabled = false;
    }

    // Aktifkan perhitungan matematika hanya saat terlihat kamera
    private void OnBecameVisible()
    {
        enabled = true;
    }

    // Matikan total fungsi Update saat menghilang dari layar
    private void OnBecameInvisible()
    {
        enabled = false;
    }

    void Update()
    {
        float baseWiggle = Mathf.Sin((Time.time * wiggleSpeed) + randomOffset);

        float noiseTime = (Time.time * turbulenceSpeed) + randomOffset;
        float windTurbulence = (Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f) * turbulenceIntensity;

        float totalWiggle = (baseWiggle + windTurbulence) * wiggleMaxAngle;
        totalWiggle = Mathf.Clamp(totalWiggle, -wiggleMaxAngle, wiggleMaxAngle);

        transform.localRotation = Quaternion.Euler(0f, 0f, startZRotation + totalWiggle);
    }
}
