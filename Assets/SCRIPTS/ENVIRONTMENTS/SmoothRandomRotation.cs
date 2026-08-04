using System.Collections;
using UnityEngine;

public class SmoothRandomRotation : MonoBehaviour
{
    [Header("Rotation Limits (Degrees)")]
    [SerializeField] private float maxLeftRotation = 45f;
    [SerializeField] private float maxRightRotation = 45f;

    [Header("Time Settings (Seconds)")]
    [SerializeField] private float minDuration = 0.5f;
    [SerializeField] private float maxDuration = 2.0f;

    private float startZRotation;
    private float targetZRotation;
    private float currentVelocity;
    private float currentDuration;

    private Coroutine randomRotationCoroutine;
    private bool isVisible = false;

    void Start()
    {
        startZRotation = transform.localEulerAngles.z;
        
        // Nonaktifkan script di awal, biarkan OnBecameVisible yang menyalakannya saat kamera melihatnya
        enabled = false; 
    }

    // Dipanggil otomatis oleh Unity saat objek masuk ke dalam pandangan kamera (termasuk Scene View Editor)
    private void OnBecameVisible()
    {
        isVisible = true;
        enabled = true; // Nyalakan fungsi Update()

        if (randomRotationCoroutine == null)
        {
            randomRotationCoroutine = StartCoroutine(RandomRotationRoutine());
        }
    }

    // Dipanggil otomatis oleh Unity saat objek benar-benar keluar dari pandangan semua kamera
    private void OnBecameInvisible()
    {
        isVisible = false;
        enabled = false; // Matikan fungsi Update() untuk menghemat CPU

        if (randomRotationCoroutine != null)
        {
            StopCoroutine(randomRotationCoroutine);
            randomRotationCoroutine = null;
        }
    }

    void Update()
    {
        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180f) currentZ -= 360f;

        float newZ = Mathf.SmoothDamp(currentZ, targetZRotation, ref currentVelocity, currentDuration);
        transform.localRotation = Quaternion.Euler(0f, 0f, newZ);
    }

    private IEnumerator RandomRotationRoutine()
    {
        while (isVisible)
        {
            targetZRotation = startZRotation + Random.Range(-maxRightRotation, maxLeftRotation);
            currentDuration = Random.Range(minDuration, maxDuration);
            yield return new WaitForSeconds(currentDuration);
        }
    }
}
