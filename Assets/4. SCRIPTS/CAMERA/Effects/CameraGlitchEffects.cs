using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraGlitchEffects : MonoBehaviour
{
    [Header("Fog Area Vignette")]
    [SerializeField, Range(0f, 1f)] private float targetVignetteIntensity = 0.25f;
    [SerializeField, Min(0f)] private float vignetteBuildUpSpeed = 0.5f;
    [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.05f;
    [SerializeField, Min(0f)] private float pulseSpeed = 0.5f;
    [SerializeField, Min(0f)] private float vignetteFadeSpeed = 0.5f;

    private Vignette vignette;
    private bool isFogAreaActive;
    private float currentIntensity;
    private float pulseTime;

    private void Awake()
    {
        Volume volume = GetComponent<Volume>();
        if (volume == null || volume.profile == null)
        {
            Debug.LogError("CameraGlitchEffects membutuhkan Volume dengan profile pada kamera.", this);
            enabled = false;
            return;
        }

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogError("Volume profile kamera tidak memiliki efek Vignette.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (isFogAreaActive)
        {
            if (currentIntensity < targetVignetteIntensity)
            {
                currentIntensity = Mathf.MoveTowards(
                    currentIntensity,
                    targetVignetteIntensity,
                    vignetteBuildUpSpeed * Time.deltaTime);
            }
            else
            {
                pulseTime += Time.deltaTime * pulseSpeed;
                float pulse = (Mathf.Sin(pulseTime) + 1f) * 0.5f;
                currentIntensity = Mathf.Lerp(
                    targetVignetteIntensity * (1f - pulseAmount),
                    targetVignetteIntensity,
                    pulse);
            }
        }
        else
        {
            currentIntensity = Mathf.MoveTowards(
                currentIntensity,
                0f,
                vignetteFadeSpeed * Time.deltaTime);
            pulseTime = 0f;
        }

        vignette.intensity.Override(currentIntensity);
    }

    public void SetFogAreaVignetteActive(bool isActive)
    {
        if (isFogAreaActive == isActive) return;

        isFogAreaActive = isActive;
        if (isActive)
        {
            pulseTime = 0f;
        }
    }
}
