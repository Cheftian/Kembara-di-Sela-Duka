using UnityEngine;
using UnityEngine.Rendering; // Tambahan untuk Post-Processing Volume
using UnityEngine.Rendering.Universal; // Tambahan untuk URP Effects

public class CameraGlitchEffects : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController;

    [Header("Default Camera Settings")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float defaultCameraSize = 5f;

    [Header("Dizzy Zoom Progresif")]
    [SerializeField] private float maxZoomSize = 3.5f; 
    [SerializeField] private float zoomInSpeed = 0.5f; 
    [SerializeField] private float zoomOutSpeed = 2f;  
    [SerializeField] private float cameraFollowSpeed = 5f; 

    [Header("Camera Shake Settings")]
    [SerializeField] private float maxShakeMagnitude = 0.15f;
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Post-Processing Settings")]
    [Tooltip("Maksimal kepekatan Vignette saat kamera mencapai batas zoom terdekat")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVignetteIntensity = 0.45f;
    [Tooltip("Kecepatan Vignette menghilang saat kembali normal")]
    [SerializeField] private float vignetteFadeSpeed = 2f;

    private Camera cam;
    private float currentTargetSize;
    private Vector3 initialCameraPosition;
    private float aspectRatio = 16f / 9f; 
    private float shakeTime;

    // Variabel internal Post-Processing
    private Volume postProcessVolume;
    private Vignette vignetteEffect;
    private float targetVignetteIntensity = 0f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }
        if (playerTransform == null && playerController != null)
        {
            playerTransform = playerController.transform;
        }

        currentTargetSize = defaultCameraSize;
        cam.orthographicSize = defaultCameraSize;
        initialCameraPosition = transform.position;

        // Inisialisasi Post-Processing Volume pada Main Camera
        postProcessVolume = GetComponent<Volume>();
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignetteEffect);
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || playerController == null || cam == null) return;

        // 1. LOGIKA PROGRESIF ZOOM
        bool isPlayerWalkingDizzy = playerController.IsDizzy && playerController.IsWalking;

        if (isPlayerWalkingDizzy)
        {
            currentTargetSize -= zoomInSpeed * Time.deltaTime;
            currentTargetSize = Mathf.Max(currentTargetSize, maxZoomSize); 
        }
        else
        {
            currentTargetSize += zoomOutSpeed * Time.deltaTime;
            currentTargetSize = Mathf.Min(currentTargetSize, defaultCameraSize); 
        }

        cam.orthographicSize = currentTargetSize;

        // Hitung persentase progres zoom (0 saat normal, 1 saat zoom maksimal)
        float zoomProgress = Mathf.InverseLerp(defaultCameraSize, maxZoomSize, cam.orthographicSize);

        // 2. HITUNG BATAS DINAMIS (Batas Ruang Gerak Akibat Zoom In)
        float maxDeltaY = defaultCameraSize - cam.orthographicSize;
        float maxDeltaX = maxDeltaY * aspectRatio;

        float dynamicMinX = initialCameraPosition.x - maxDeltaX;
        float dynamicMaxX = initialCameraPosition.x + maxDeltaX;
        float dynamicMinY = initialCameraPosition.y - maxDeltaY;
        float dynamicMaxY = initialCameraPosition.y + maxDeltaY;

        // 3. PERGERAKAN MENGIKUTI PLAYER
        Vector3 dynamicOffset = defaultOffset;
        dynamicOffset.y += zoomProgress * 0.8f; 

        // Target posisi kamera mengikuti Player
        Vector3 targetCameraPosition = playerTransform.position + dynamicOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetCameraPosition, Time.deltaTime * cameraFollowSpeed);

        // 4. LOCK BOUNDARIES DINAMIS
        float clampedX = Mathf.Clamp(smoothedPosition.x, dynamicMinX, dynamicMaxX);
        float clampedY = Mathf.Clamp(smoothedPosition.y, dynamicMinY, dynamicMaxY);
        
        Vector3 finalBasePosition = new Vector3(clampedX, clampedY, transform.position.z);

        // 5. EFEK CAMERA SHAKE (GUNCANGAN PROGRESIF)
        if (isPlayerWalkingDizzy && zoomProgress > 0.05f)
        {
            shakeTime += Time.deltaTime * shakeFrequency;
            float currentShakeMagnitude = zoomProgress * maxShakeMagnitude;

            float shakeX = (Mathf.PerlinNoise(shakeTime, 0f) - 0.5f) * 2f * currentShakeMagnitude;
            float shakeY = (Mathf.PerlinNoise(0f, shakeTime) - 0.5f) * 2f * currentShakeMagnitude;

            Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0f);
            transform.position = finalBasePosition + shakeOffset;

            // Vignette mengikuti ketebalan zoom secara presisi
            targetVignetteIntensity = zoomProgress * maxVignetteIntensity;
        }
        else
        {
            transform.position = finalBasePosition;
            shakeTime = 0f;

            // Mengurangi vignette perlahan saat normal/idle
            targetVignetteIntensity = Mathf.MoveTowards(targetVignetteIntensity, 0f, Time.deltaTime * vignetteFadeSpeed);
        }

        // 6. TERAPKAN EFEK VIGNETTE KE LAYAR
        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.Override(targetVignetteIntensity);
        }
    }
}
