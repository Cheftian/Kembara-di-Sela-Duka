using UnityEngine;
using UnityEngine.Rendering; // Tambahan untuk Post-Processing Volume
using UnityEngine.Rendering.Universal; // Tambahan untuk URP Effects

[DefaultExecutionOrder(1000)]
public class CameraGlitchEffects : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float zoomFocusStrength = 1f;

    [Header("Camera Shake Settings")]
    [SerializeField] private float maxShakeMagnitude = 0.15f;
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Procedural Intensity")]
    [Tooltip("Kecepatan efek meningkat dari ringan hingga maksimal.")]
    [SerializeField] private float effectBuildUpSpeed = 1f;
    [Tooltip("Kecepatan efek kembali normal saat player berhenti dizzy berjalan.")]
    [SerializeField] private float effectFadeSpeed = 2f;
    [SerializeField] private float returnToOriginalSpeed = 5f;

    [Header("Procedural Zoom")]
    [Tooltip("Ukuran orthographic minimum saat efek mencapai intensitas maksimal.")]
    [SerializeField] private float maxZoomSize = 3.5f;

    [Header("Post-Processing Settings")]
    [Tooltip("Maksimal kepekatan Vignette saat kamera mencapai batas zoom terdekat")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVignetteIntensity = 0.45f;
    [Tooltip("Kecepatan Vignette menghilang saat kembali normal")]
    [SerializeField] private float vignetteFadeSpeed = 2f;

    private Camera cam;
    private CameraController cameraController;
    private float shakeTime;
    private Vector3 effectStartPosition;
    private Vector3 lastEffectBasePosition;
    private float effectStartCameraSize;
    private float effectIntensity;
    private bool effectSessionActive;

    // Variabel internal Post-Processing
    private Volume postProcessVolume;
    private Vignette vignetteEffect;
    private float targetVignetteIntensity = 0f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cameraController = GetComponent<CameraController>();
        
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        // Inisialisasi Post-Processing Volume pada Main Camera
        postProcessVolume = GetComponent<Volume>();
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignetteEffect);
        }
    }

    private void LateUpdate()
    {
        if (playerController == null || cam == null) return;

        bool isPlayerWalkingDizzy = playerController.IsDizzy && playerController.IsWalking;
        bool isDizzy = playerController.IsDizzy;
        ClampCurrentCameraPosition();

        if (isPlayerWalkingDizzy && !effectSessionActive)
        {
            effectSessionActive = true;
            effectStartPosition = transform.position;
            effectStartCameraSize = cam.orthographicSize;
        }

        if (isPlayerWalkingDizzy)
        {
            effectIntensity = Mathf.MoveTowards(effectIntensity, 1f, effectBuildUpSpeed * Time.deltaTime);
            shakeTime += Time.deltaTime * shakeFrequency;
            float currentShakeMagnitude = maxShakeMagnitude * effectIntensity;
            float shakeX = (Mathf.PerlinNoise(shakeTime, 0f) - 0.5f) * 2f * currentShakeMagnitude;
            float shakeY = (Mathf.PerlinNoise(0f, shakeTime) - 0.5f) * 2f * currentShakeMagnitude;

            targetVignetteIntensity = maxVignetteIntensity * effectIntensity;
            ApplyZoomTowardPlayer(effectStartCameraSize);
            lastEffectBasePosition = transform.position;
            transform.position += new Vector3(shakeX, shakeY, 0f);
            ClampCurrentCameraPosition();
        }
        else if (isDizzy && effectSessionActive)
        {
            shakeTime = 0f;
            targetVignetteIntensity = 0f;
            effectIntensity = 0f;
            cam.orthographicSize = effectStartCameraSize;
            transform.position = ClampPosition(lastEffectBasePosition, cam.orthographicSize);
        }
        else if (effectSessionActive)
        {
            effectIntensity = Mathf.MoveTowards(effectIntensity, 0f, effectFadeSpeed * Time.deltaTime);
            targetVignetteIntensity = Mathf.MoveTowards(targetVignetteIntensity, 0f, vignetteFadeSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, effectStartCameraSize, returnToOriginalSpeed * Time.deltaTime);

            Vector3 returnPosition = effectStartPosition;
            if (cameraController != null)
            {
                returnPosition = cameraController.ClampPositionToBoundaries(returnPosition, cam.orthographicSize);
            }

            transform.position = ClampPosition(
                Vector3.Lerp(transform.position, returnPosition, returnToOriginalSpeed * Time.deltaTime),
                cam.orthographicSize);

            if (effectIntensity <= 0.001f && Mathf.Abs(cam.orthographicSize - effectStartCameraSize) <= 0.001f && Vector3.Distance(transform.position, returnPosition) <= 0.001f)
            {
                transform.position = returnPosition;
                cam.orthographicSize = effectStartCameraSize;
                effectSessionActive = false;
            }
        }

        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.Override(targetVignetteIntensity);
        }

    }

    private void ApplyZoomTowardPlayer(float normalCameraSize)
    {
        float zoomedCameraSize = Mathf.Lerp(normalCameraSize, Mathf.Min(normalCameraSize, maxZoomSize), effectIntensity);
        cam.orthographicSize = zoomedCameraSize;

        Vector3 zoomFocusPosition = Vector3.Lerp(
            transform.position,
            playerController.transform.position,
            effectIntensity * zoomFocusStrength);
        zoomFocusPosition.z = transform.position.z;

        if (cameraController != null)
        {
            zoomFocusPosition = cameraController.ClampPositionToBoundaries(zoomFocusPosition, zoomedCameraSize);
        }

        transform.position = zoomFocusPosition;
    }

    private void ClampCurrentCameraPosition()
    {
        transform.position = ClampPosition(transform.position, cam.orthographicSize);
    }

    private Vector3 ClampPosition(Vector3 position, float orthographicSize)
    {
        if (cameraController != null)
        {
            position = cameraController.ClampPositionToBoundaries(position, orthographicSize);
        }

        position.z = transform.position.z;
        return position;
    }
}
