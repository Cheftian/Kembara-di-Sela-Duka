using UnityEngine;

public enum VerticalCameraSizeDirection
{
    Up,
    Down
}

public enum HorizontalCameraSizeDirection
{
    Left,
    Right
}

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float maxFollowSpeed = 8f;
    [SerializeField] private float followAcceleration = 12f;
    [SerializeField] private float cameraSize = 5f;

    [Header("Vertical Camera Size")]
    [SerializeField] private bool enableVerticalCameraSize = false;
    [SerializeField] private VerticalCameraSizeDirection verticalCameraSizeDirection = VerticalCameraSizeDirection.Up;
    [Tooltip("Nilai Y absolut posisi kamera saat kamera mulai memperbesar ukurannya.")]
    [SerializeField] private float cameraSizeIncreaseHeight = 10f;
    [Tooltip("Jarak Y untuk setiap kenaikan ukuran kamera.")]
    [SerializeField] private float cameraSizeIncreaseYStep = 1f;
    [Tooltip("Nilai kenaikan ukuran kamera pada setiap langkah Y.")]
    [SerializeField] private float cameraSizeIncreasePerStep = 0.1f;
    [SerializeField] private float maxCameraSizeIncrease = 1f;
    [Tooltip("Waktu yang diperlukan ukuran kamera untuk mengikuti perubahan ketinggian.")]
    [SerializeField] private float verticalCameraSizeSmoothTime = 0.5f;

    [Header("Horizontal Camera Size")]
    [SerializeField] private bool enableHorizontalCameraSize = false;
    [SerializeField] private HorizontalCameraSizeDirection horizontalCameraSizeDirection = HorizontalCameraSizeDirection.Right;
    [Tooltip("Nilai X absolut posisi kamera saat kamera mulai memperbesar ukurannya.")]
    [SerializeField] private float cameraSizeIncreaseWidth = 10f;
    [Tooltip("Jarak X untuk setiap kenaikan ukuran kamera.")]
    [SerializeField] private float cameraSizeIncreaseXStep = 1f;
    [Tooltip("Nilai kenaikan ukuran kamera pada setiap langkah X.")]
    [SerializeField] private float cameraSizeIncreasePerXStep = 0.1f;
    [SerializeField] private float maxHorizontalCameraSizeIncrease = 1f;

    [Header("Manual Movement Settings")]
    [SerializeField] private bool canMoveManually = false;
    [SerializeField] private float manualMoveSpeed = 15f;
    [SerializeField] private float maxManualDistance = 5f;
    
    [Header("Camera Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private Vector2 minPosition;
    [SerializeField] private Vector2 maxPosition;

    [Header("Glitch Boundary Settings")]
    [Tooltip("Seberapa jauh batas bawah kamera dapat turun saat efek dizzy aktif.")]
    [SerializeField] private float glitchMinimumYDrop = 2f;
    [Tooltip("Kecepatan perubahan batas bawah kamera saat efek dizzy mulai/berakhir.")]
    [SerializeField] private float glitchBoundarySmoothTime = 0.35f;

    private Camera cam;
    private Vector3 currentVelocity = Vector3.zero;
    private float sizeVelocity = 0f;
    private float verticalCameraSizeVelocity = 0f;
    private float baseCameraSize;
    private float currentFollowSpeed;
    private float currentGlitchMinimumYDrop;
    private float glitchBoundaryVelocity;
    private bool glitchEffectActive;
    private float glitchFocusStrength;
    private Vector3 glitchFocusPosition;
    private float glitchCameraSize;
    private Vector3 glitchShakeOffset;

    public Vector2 MinPositionBound => minPosition;
    public Vector2 MaxPositionBound => maxPosition;
    public float CurrentCameraSize => cam != null ? cam.orthographicSize : cameraSize;

    public static CameraController Instance { get; private set; }
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;

    private void Awake()
    {
        Instance = this; // Memungkinkan RevealerTool memanggil skrip ini
        cam = GetComponent<Camera>();
        baseCameraSize = cameraSize;
    }

    private void LateUpdate()
    {
        // CEK STATE: Hanya gerakkan kamera jika GameState adalah Play
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
        {
            return;
        }

        UpdateCameraSizeForTargetHeight();
        UpdateGlitchBoundary();
        ApplyCameraSize();
        HandleCameraMovement();
    }

    private void HandleCameraMovement()
    {
        if (target == null) return;

        Vector3 targetPosition;
        Vector3 followPosition = target.position + offset;
        
        float moveX = 0;
        float moveY = 0;

        if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1;
        if (Input.GetKey(KeyCode.RightArrow)) moveX = 1;
        if (Input.GetKey(KeyCode.UpArrow)) moveY = 1;
        if (Input.GetKey(KeyCode.DownArrow)) moveY = -1;

        bool isInputting = moveX != 0 || moveY != 0;

        if (canMoveManually && isInputting)
        {
            Vector3 manualMoveStep = new Vector3(moveX, moveY, 0) * manualMoveSpeed;
            targetPosition = transform.position + manualMoveStep;

            Vector3 directionFromTarget = targetPosition - followPosition;
            directionFromTarget.z = 0; 

            if (directionFromTarget.magnitude > maxManualDistance)
            {
                targetPosition = followPosition + (directionFromTarget.normalized * maxManualDistance);
            }
        }
        else
        {
            targetPosition = followPosition;
        }

        if (glitchEffectActive && target != null)
        {
            Vector3 focusPosition = glitchFocusPosition;
            focusPosition.z = targetPosition.z;
            targetPosition = Vector3.Lerp(
                targetPosition,
                focusPosition,
                Mathf.Clamp01(glitchFocusStrength));
        }

        targetPosition = ClampPositionToBoundaries(targetPosition, cameraSize);

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget > 0.01f)
        {
            currentFollowSpeed = Mathf.MoveTowards(
                currentFollowSpeed,
                Mathf.Max(0.01f, maxFollowSpeed),
                Mathf.Max(0.01f, followAcceleration) * Time.deltaTime);
        }
        else
        {
            currentFollowSpeed = 0f;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            smoothTime,
            Mathf.Max(0.01f, currentFollowSpeed),
            Time.deltaTime);
        Vector3 boundedPosition = ClampPositionToBoundaries(transform.position, cam != null ? cam.orthographicSize : cameraSize);
        boundedPosition.z = targetPosition.z;
        transform.position = boundedPosition;

        if (glitchEffectActive)
        {
            Vector3 glitchPosition = transform.position + glitchShakeOffset;
            glitchPosition = ClampPositionToBoundaries(glitchPosition, cam != null ? cam.orthographicSize : cameraSize);
            glitchPosition.z = targetPosition.z;
            transform.position = glitchPosition;
        }

        // --- TAMBAHKAN KODE INI DI BARIS PALING BAWAH FUNGSI ---
        if (shakeTimer > 0)
        {
            // Guncang posisi kamera yang sudah dibatasi boundary tanpa merusak sistem follow target
            transform.position += (Vector3)Random.insideUnitCircle * shakeMagnitude;
            shakeTimer -= Time.deltaTime;
        }

    }

    private void ApplyCameraSize()
    {
        if (cam != null)
        {
            float targetSize = glitchEffectActive ? glitchCameraSize : cameraSize;
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVelocity, smoothTime);
        }
    }

    private void UpdateGlitchBoundary()
    {
        float targetDrop = glitchEffectActive ? glitchMinimumYDrop * glitchFocusStrength : 0f;
        currentGlitchMinimumYDrop = Mathf.SmoothDamp(
            currentGlitchMinimumYDrop,
            Mathf.Max(0f, targetDrop),
            ref glitchBoundaryVelocity,
            Mathf.Max(0.01f, glitchBoundarySmoothTime),
            Mathf.Infinity,
            Time.deltaTime);
    }

    private void UpdateCameraSizeForTargetHeight()
    {
        float desiredCameraSize = baseCameraSize;

        if (enableVerticalCameraSize && target != null)
        {
            float verticalDistanceFromStart = verticalCameraSizeDirection == VerticalCameraSizeDirection.Up
                ? transform.position.y - cameraSizeIncreaseHeight
                : cameraSizeIncreaseHeight - transform.position.y;

            if (verticalDistanceFromStart >= 0f && cameraSizeIncreaseYStep > 0f)
            {
                int increaseSteps = Mathf.FloorToInt(verticalDistanceFromStart / cameraSizeIncreaseYStep) + 1;
                float sizeIncrease = Mathf.Min(increaseSteps * cameraSizeIncreasePerStep, maxCameraSizeIncrease);
                desiredCameraSize += Mathf.Max(0f, sizeIncrease);
            }
        }

        if (enableHorizontalCameraSize && target != null)
        {
            float horizontalDistanceFromStart = horizontalCameraSizeDirection == HorizontalCameraSizeDirection.Right
                ? transform.position.x - cameraSizeIncreaseWidth
                : cameraSizeIncreaseWidth - transform.position.x;

            if (horizontalDistanceFromStart >= 0f && cameraSizeIncreaseXStep > 0f)
            {
                int increaseSteps = Mathf.FloorToInt(horizontalDistanceFromStart / cameraSizeIncreaseXStep) + 1;
                float sizeIncrease = Mathf.Min(increaseSteps * cameraSizeIncreasePerXStep, maxHorizontalCameraSizeIncrease);
                desiredCameraSize += Mathf.Max(0f, sizeIncrease);
            }
        }

        desiredCameraSize = LimitCameraSizeToBoundaries(desiredCameraSize);
        float smoothTime = Mathf.Max(0.01f, verticalCameraSizeSmoothTime);
        cameraSize = Mathf.SmoothDamp(
            cameraSize,
            desiredCameraSize,
            ref verticalCameraSizeVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime);
    }

    public Vector3 ClampPositionToBoundaries(Vector3 position, float orthographicSize)
    {
        if (!useBoundaries || cam == null) return position;

        float halfHeight = orthographicSize;
        float halfWidth = orthographicSize * cam.aspect;
        float minX = minPosition.x + halfWidth;
        float maxX = maxPosition.x - halfWidth;
        float minY = minPosition.y - currentGlitchMinimumYDrop + halfHeight;
        float maxY = maxPosition.y - halfHeight;

        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : (minPosition.x + maxPosition.x) * 0.5f;
        position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : (minPosition.y + maxPosition.y) * 0.5f;
        return position;
    }

    private float LimitCameraSizeToBoundaries(float desiredSize)
    {
        if (!useBoundaries || cam == null) return desiredSize;

        float maxSizeFromHeight = (maxPosition.y - (minPosition.y - currentGlitchMinimumYDrop)) * 0.5f;
        float maxSizeFromWidth = (maxPosition.x - minPosition.x) / (2f * cam.aspect);
        float maximumCameraSize = Mathf.Min(maxSizeFromHeight, maxSizeFromWidth);
        return Mathf.Min(desiredSize, Mathf.Max(0f, maximumCameraSize));
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    public void SetCameraSize(float newSize)
    {
        baseCameraSize = newSize;
        cameraSize = newSize;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    public void SetGlitchEffect(Vector3 focusPosition, float focusStrength, float desiredCameraSize, Vector3 shakeOffset)
    {
        glitchEffectActive = true;
        glitchFocusPosition = focusPosition;
        glitchFocusStrength = Mathf.Clamp01(focusStrength);
        glitchCameraSize = desiredCameraSize;
        glitchShakeOffset = shakeOffset;
    }

    public void ClearGlitchEffect()
    {
        glitchEffectActive = false;
        glitchFocusStrength = 0f;
        glitchFocusPosition = Vector3.zero;
        glitchShakeOffset = Vector3.zero;
    }

    public void SetManualControl(bool state) => canMoveManually = state;
}