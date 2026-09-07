using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float cameraSize = 5f;

    [Header("Vertical Camera Size")]
    [SerializeField] private bool enableVerticalCameraSize = false;
    [Tooltip("Nilai Y absolut posisi kamera saat kamera mulai memperbesar ukurannya.")]
    [SerializeField] private float cameraSizeIncreaseHeight = 10f;
    [Tooltip("Jarak Y untuk setiap kenaikan ukuran kamera.")]
    [SerializeField] private float cameraSizeIncreaseYStep = 1f;
    [Tooltip("Nilai kenaikan ukuran kamera pada setiap langkah Y.")]
    [SerializeField] private float cameraSizeIncreasePerStep = 0.1f;
    [SerializeField] private float maxCameraSizeIncrease = 1f;

    [Header("Manual Movement Settings")]
    [SerializeField] private bool canMoveManually = false;
    [SerializeField] private float manualMoveSpeed = 15f;
    [SerializeField] private float maxManualDistance = 5f;
    
    [Header("Camera Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private Vector2 minPosition;
    [SerializeField] private Vector2 maxPosition;

    private Camera cam;
    private Vector3 currentVelocity = Vector3.zero;
    private float sizeVelocity = 0f;
    private float baseCameraSize;

    public Vector2 MinPositionBound => minPosition;
    public Vector2 MaxPositionBound => maxPosition;

    private void Awake()
    {
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

        targetPosition = ClampPositionToBoundaries(targetPosition, cameraSize);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        Vector3 boundedPosition = ClampPositionToBoundaries(transform.position, cam != null ? cam.orthographicSize : cameraSize);
        boundedPosition.z = targetPosition.z;
        transform.position = boundedPosition;
    }

    private void ApplyCameraSize()
    {
        if (cam != null)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, cameraSize, ref sizeVelocity, smoothTime);
        }
    }

    private void UpdateCameraSizeForTargetHeight()
    {
        if (!enableVerticalCameraSize || target == null)
        {
            cameraSize = LimitCameraSizeToBoundaries(baseCameraSize);
            return;
        }

        float heightAboveStart = transform.position.y - cameraSizeIncreaseHeight;
        if (heightAboveStart < 0f || cameraSizeIncreaseYStep <= 0f)
        {
            cameraSize = LimitCameraSizeToBoundaries(baseCameraSize);
            return;
        }

        int increaseSteps = Mathf.FloorToInt(heightAboveStart / cameraSizeIncreaseYStep) + 1;
        float sizeIncrease = Mathf.Min(increaseSteps * cameraSizeIncreasePerStep, maxCameraSizeIncrease);
        cameraSize = LimitCameraSizeToBoundaries(baseCameraSize + Mathf.Max(0f, sizeIncrease));
    }

    public Vector3 ClampPositionToBoundaries(Vector3 position, float orthographicSize)
    {
        if (!useBoundaries || cam == null) return position;

        float halfHeight = orthographicSize;
        float halfWidth = orthographicSize * cam.aspect;
        float minX = minPosition.x + halfWidth;
        float maxX = maxPosition.x - halfWidth;
        float minY = minPosition.y + halfHeight;
        float maxY = maxPosition.y - halfHeight;

        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : (minPosition.x + maxPosition.x) * 0.5f;
        position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : (minPosition.y + maxPosition.y) * 0.5f;
        return position;
    }

    private float LimitCameraSizeToBoundaries(float desiredSize)
    {
        if (!useBoundaries || cam == null) return desiredSize;

        float maxSizeFromHeight = (maxPosition.y - minPosition.y) * 0.5f;
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
    public void SetManualControl(bool state) => canMoveManually = state;
}