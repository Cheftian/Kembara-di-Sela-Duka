using UnityEngine;

public class AdaptiveMovement : MonoBehaviour
{
    [System.Serializable]
    public struct ChapterMovementSettings
    {
        public GameManager.Chapter chapter;
        public float moveSpeed;
    }

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    [Header("Movement Profiles")]
    [SerializeField] private ChapterMovementSettings[] movementProfiles;

    [Header("Transition Settings")]
    [SerializeField] private float transitionSpeed = 2f;

    private float targetSpeed;
    private float currentVelocity;
    private GameManager.Chapter lastCheckedChapter;

    private void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // Set initial speed based on starting chapter
        UpdateTargetSpeed();
        lastCheckedChapter = GameManager.Instance.currentChapter;
    }

    private void Update()
    {
        // Hanya jalankan jika GameState adalah Play
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Play)
            return;

        // Cek jika chapter berubah di GameManager
        if (GameManager.Instance.currentChapter != lastCheckedChapter)
        {
            UpdateTargetSpeed();
            lastCheckedChapter = GameManager.Instance.currentChapter;
        }

        // Transisi halus antar nilai speed (SmoothDamp)
        float smoothSpeed = Mathf.SmoothDamp(GetCurrentPlayerSpeed(), targetSpeed, ref currentVelocity, 1f / transitionSpeed);
        
        playerController.UpdateMoveSpeed(smoothSpeed);
    }

    private void UpdateTargetSpeed()
    {
        GameManager.Chapter current = GameManager.Instance.currentChapter;

        foreach (var profile in movementProfiles)
        {
            if (profile.chapter == current)
            {
                targetSpeed = profile.moveSpeed;
                return;
            }
        }
    }

    private float GetCurrentPlayerSpeed()
    {
        // Reflection sederhana atau akses field moveSpeed di PlayerController
        // Karena kita sudah punya fungsi UpdateMoveSpeed, kita asumsikan sinkronisasi lewat targetSpeed
        return targetSpeed; 
    }
}