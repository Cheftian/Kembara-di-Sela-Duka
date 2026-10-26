using UnityEngine;

public class ChaseObstacle : MonoBehaviour
{
    public enum Direction { Left, Right }

    [Header("Movement Settings")]
    [SerializeField] private Direction moveDirection = Direction.Right;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float stopAtX = 10f;
    [SerializeField, Min(0f)] private float arrivalSfxFadeDuration = 1.5f;

    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeMagnitude = 0.1f;

    [Header("Toggle Objects on Stop")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    private bool isMoving = false;
    private bool hasStopped = false;
    private bool shouldShake = false;
    private AudioSource movementSfxSource;
    private bool isMovementSfxFading;
    
    // Menyimpan referensi kamera yang aktif untuk obstacle ini saja
    private CameraController assignedCamera; 

    private void Update()
    {
        // Jalankan camera shake menggunakan kamera yang sudah diassign oleh trigger
        if (shouldShake && assignedCamera != null)
        {
            assignedCamera.TriggerShake(Time.deltaTime * 2f, shakeMagnitude);
        }

        if (!isMoving || hasStopped) return;

        MoveObstacle();
        CheckStopCondition();
    }

    // Fungsi baru untuk menerima kamera dari ObstacleTrigger
    public void AssignCamera(CameraController cameraCtrl)
    {
        assignedCamera = cameraCtrl;
    }

    public void EnableCameraShakeOnly(bool state)
    {
        shouldShake = state;
    }

    public void StartChase()
    {
        if (!hasStopped && !isMoving)
        {
            isMoving = true;
            shouldShake = true;
            isMovementSfxFading = false;
            if (AudioManager.Instance != null)
            {
                movementSfxSource = AudioManager.Instance.PlayLoopingSFX("Void-Fog");
            }
        }
    }

    private void FadeOutMovementSfx()
    {
        if (movementSfxSource == null) return;

        isMovementSfxFading = true;
        AudioSource source = movementSfxSource;
        movementSfxSource = null;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX(source, arrivalSfxFadeDuration);
        }
        else
        {
            source.Stop();
        }
    }

    private void MoveObstacle()
    {
        float directionMultiplier = (moveDirection == Direction.Right) ? 1f : -1f;
        transform.Translate(Vector3.right * directionMultiplier * speed * Time.deltaTime);
    }

    private void CheckStopCondition()
    {
        bool reachedTarget = false;

        if (moveDirection == Direction.Right && transform.position.x >= stopAtX) reachedTarget = true;
        else if (moveDirection == Direction.Left && transform.position.x <= stopAtX) reachedTarget = true;

        if (reachedTarget)
        {
            StopObstacle();
            return;
        }

        float distanceToTarget = moveDirection == Direction.Right
            ? stopAtX - transform.position.x
            : transform.position.x - stopAtX;
        if (!isMovementSfxFading &&
            distanceToTarget <= speed * arrivalSfxFadeDuration)
        {
            FadeOutMovementSfx();
        }
    }

    private void StopObstacle()
    {
        isMoving = false;
        hasStopped = true;
        shouldShake = false;
        FadeOutMovementSfx();

        Vector3 finalPosition = transform.position;
        finalPosition.x = stopAtX;
        transform.position = finalPosition;

        ToggleGameObjects();
    }

    private void ToggleGameObjects()
    {
        foreach (GameObject obj in objectsToActivate) if (obj != null) obj.SetActive(true);
        foreach (GameObject obj in objectsToDeactivate) if (obj != null) obj.SetActive(false);
    }
    private void OnEnable()
    {
        // Fungsi ini otomatis berjalan saat baris obstacleScript.enabled = true di RoomManager dieksekusi
        isMoving = false;
        hasStopped = false;
        shouldShake = false;
        isMovementSfxFading = false;
        movementSfxSource = null;
    }

    private void OnDisable()
    {
        if (movementSfxSource == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX(movementSfxSource, 0f);
        }
        else
        {
            movementSfxSource.Stop();
        }

        movementSfxSource = null;
    }

}
