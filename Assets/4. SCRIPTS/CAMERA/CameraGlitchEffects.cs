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
    [SerializeField] private float maxShakeMagnitude = 0.2f;
    [SerializeField] private float shakeFrequency = 5f;

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

    [Header("Dizzy Recovery Vignette")]
    [Tooltip("Kecepatan Vignette membesar saat animasi Sit dan masa recovery.")]
    [SerializeField] private float recoveryVignetteBuildSpeed = 0.5f;
    [Tooltip("Kecepatan Vignette mengecil saat animasi Stand.")]
    [SerializeField] private float recoveryVignetteFadeSpeed = 1f;
    [Tooltip("Kecepatan denyut Vignette selama fase Sit dan masa recovery.")]
    [SerializeField] private float recoveryVignettePulseSpeed = 3f;
    [Range(0f, 1f)]
    [Tooltip("Rentang perubahan intensitas Vignette saat berdenyut.")]
    [SerializeField] private float recoveryVignettePulseAmount = 0.2f;

    private CameraController cameraController;
    private float shakeTime;
    private float effectStartCameraSize;
    private float effectIntensity;
    private bool effectSessionActive;

    // Variabel internal Post-Processing
    private Volume postProcessVolume;
    private Vignette vignetteEffect;
    private float targetVignetteIntensity = 0f;
    private float recoveryVignettePulseTime;

    private void Start()
    {
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

    private void Update()
    {
        if (playerController == null) return;

        if (cameraController == null)
        {
            UpdateRecoveryVignette();
            return;
        }

        bool isPlayerWalkingDizzy = playerController.IsDizzy && playerController.IsWalking;
        bool isDizzy = playerController.IsDizzy;

        if (isPlayerWalkingDizzy && !effectSessionActive)
        {
            effectSessionActive = true;
            effectStartCameraSize = cameraController.CurrentCameraSize;
        }

        if (isPlayerWalkingDizzy)
        {
            effectIntensity = Mathf.MoveTowards(effectIntensity, 1f, effectBuildUpSpeed * Time.deltaTime);
            shakeTime += Time.deltaTime * shakeFrequency;
            float currentShakeMagnitude = maxShakeMagnitude * effectIntensity;
            float shakeX = (Mathf.PerlinNoise(shakeTime, 0f) - 0.5f) * 2f * currentShakeMagnitude;
            float shakeY = (Mathf.PerlinNoise(0f, shakeTime) - 0.5f) * 2f * currentShakeMagnitude;

            targetVignetteIntensity = maxVignetteIntensity * effectIntensity;
            float zoomedCameraSize = Mathf.Lerp(
                effectStartCameraSize,
                Mathf.Min(effectStartCameraSize, maxZoomSize),
                effectIntensity);
            cameraController.SetGlitchEffect(
                playerController.transform.position,
                effectIntensity * zoomFocusStrength,
                zoomedCameraSize,
                new Vector3(shakeX, shakeY, 0f));
        }
        else if (isDizzy && effectSessionActive)
        {
            shakeTime = 0f;
            targetVignetteIntensity = 0f;
            effectIntensity = 0f;
            cameraController.ClearGlitchEffect();
        }
        else if (effectSessionActive)
        {
            effectIntensity = Mathf.MoveTowards(effectIntensity, 0f, effectFadeSpeed * Time.deltaTime);
            targetVignetteIntensity = Mathf.MoveTowards(targetVignetteIntensity, 0f, vignetteFadeSpeed * Time.deltaTime);
            float returningCameraSize = Mathf.Lerp(
                effectStartCameraSize,
                Mathf.Min(effectStartCameraSize, maxZoomSize),
                effectIntensity);
            cameraController.SetGlitchEffect(
                playerController.transform.position,
                effectIntensity * zoomFocusStrength,
                returningCameraSize,
                Vector3.zero);

            if (effectIntensity <= 0.001f)
            {
                cameraController.ClearGlitchEffect();
                effectSessionActive = false;
            }
        }
        else
        {
            cameraController.ClearGlitchEffect();
        }

        UpdateRecoveryVignette();
    }

    private void UpdateRecoveryVignette()
    {
        if (playerController.IsDizzyRecovering)
        {
            bool isStanding = playerController.CurrentDizzyRecoveryPhase == PlayerController.DizzyRecoveryPhase.Stand;

            if (!isStanding)
            {
                recoveryVignettePulseTime += Time.deltaTime * recoveryVignettePulseSpeed;
            }
            else
            {
                recoveryVignettePulseTime = 0f;
            }

            float pulse = (Mathf.Sin(recoveryVignettePulseTime) + 1f) * 0.5f;
            float minimumPulseIntensity = maxVignetteIntensity * (1f - recoveryVignettePulseAmount);
            float recoveryTarget = isStanding
                ? 0f
                : Mathf.Lerp(minimumPulseIntensity, maxVignetteIntensity, pulse);
            float recoverySpeed = isStanding ? recoveryVignetteFadeSpeed : recoveryVignetteBuildSpeed;
            targetVignetteIntensity = Mathf.MoveTowards(
                targetVignetteIntensity,
                recoveryTarget,
                recoverySpeed * Time.deltaTime);
        }
        else
        {
            recoveryVignettePulseTime = 0f;
        }

        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.Override(targetVignetteIntensity);
        }
    }

}
