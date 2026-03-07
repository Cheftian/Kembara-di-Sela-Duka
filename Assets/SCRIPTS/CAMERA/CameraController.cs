using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float cameraSize = 5f;

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

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // CEK STATE: Hanya gerakkan kamera jika GameState adalah Play
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
        {
            return;
        }

        HandleCameraMovement();
        ApplyCameraSize();
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

        if (useBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minPosition.x, maxPosition.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minPosition.y, maxPosition.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }

    private void ApplyCameraSize()
    {
        if (cam != null)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, cameraSize, ref sizeVelocity, smoothTime);
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetCameraSize(float newSize) => cameraSize = newSize;
    public void SetManualControl(bool state) => canMoveManually = state;
}