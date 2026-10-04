using System.Collections;
using UnityEngine;

public class ScaleAndToggle : MonoBehaviour
{
    [Header("Idle Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    [Header("Interaction Settings")]
    [SerializeField, Min(0f)] private float scaleTransitionDuration = 0.5f;
    [SerializeField, Min(0f)] private float interactionStartScaleMultiplier = 0.2f;
    [SerializeField] private float interactionScaleMultiplier = 1.5f;
    [SerializeField] private float duration = 2f;

    [Header("Pickup Animation")]
    [SerializeField, Min(0f)] private float pickupPopHeight = 1f;
    [SerializeField, Min(0f)] private float pickupPopDuration = 0.4f;
    [SerializeField, Min(0f)] private float pickupSecondPopHeight = 0.35f;
    [SerializeField, Min(0f)] private float pickupSecondPopDuration = 0.18f;
    [SerializeField, Min(0f)] private float pickupScaleMultiplier = 0.5f;
    [Tooltip("Kecepatan Lerp saat objek mengikuti Player.")]
    [SerializeField, Min(0f)] private float pickupMoveSpeed = 5f;
    [SerializeField, Min(0f)] private float playerContactDistance = 0.2f;

    [Header("Toggle Animation")]
    [SerializeField, Min(0.01f)] private float toggleCameraSize = 10f;
    [Tooltip("Jeda tambahan setelah kamera tiba di titik spawn sebelum objek dimunculkan.")]
    [SerializeField, Min(0f)] private float toggleCameraFocusDuration = 0.5f;
    [SerializeField, Min(0f)] private float toggleCameraArrivalTolerance = 0.15f;
    [SerializeField, Min(0.01f)] private float toggleCameraArrivalTimeout = 5f;
    [SerializeField, Min(0f)] private float toggleAnimationDuration = 0.5f;
    [SerializeField, Min(0f)] private float toggleStartBelowOffset = 0.5f;
    [SerializeField, Min(0f)] private float toggleCameraShakeMagnitude = 0.08f;

    [Header("Toggle Settings")]
    [SerializeField] private GameObject[] objectsToToggle;

    private Vector3 originalScale;
    private bool isInteracting = false;
    private bool hasInteractedBefore = false;
    private RevealerTool revealerTool;
    private NotificationTrigger notificationTrigger;
    private Transform pickupRoot;
    private Vector3 pickupRootOriginalScale;
    private PlayerController interactingPlayer;
    private bool playerInputWasBlocked;

    void Awake()
    {
        originalScale = transform.localScale;
        pickupRoot = transform.parent != null ? transform.parent : transform;
        pickupRootOriginalScale = pickupRoot.localScale;
        revealerTool = GetComponent<RevealerTool>();
        notificationTrigger = GetComponent<NotificationTrigger>();

        if (revealerTool != null)
        {
            revealerTool.enabled = false;
        }
    }

    void Update()
    {
        // Cek interaksi tombol S
        if (Input.GetKeyDown(KeyCode.S) &&
            !isInteracting &&
            !hasInteractedBefore &&
            IsPlayerInsideInteractionCollider())
        {
            StartCoroutine(InteractRoutine());
        }

        // Jalankan efek membesar/mengecil otomatis jika tidak sedang interaksi
        if (!isInteracting)
        {
            HandleIdlePulse();
        }
    }

    // Fungsi untuk membuat objek membesar/mengecil otomatis (Idle)
    private void HandleIdlePulse()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale + new Vector3(scaleOffset, scaleOffset, 0);
    }

    // Coroutine untuk menangani efek interaksi tombol S
    private IEnumerator InteractRoutine()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        interactingPlayer = playerObject != null
            ? playerObject.GetComponent<PlayerController>()
            : null;

        if (interactingPlayer == null)
        {
            Debug.LogError("[ScaleAndToggle] PlayerController tidak ditemukan pada Player.", this);
            yield break;
        }

        playerInputWasBlocked = interactingPlayer.BlockInput;
        interactingPlayer.BeginInteractionSit();
        isInteracting = true;

        if (notificationTrigger != null)
        {
            notificationTrigger.HideNotification();
            notificationTrigger.enabled = false;
        }

        hasInteractedBefore = true;

        if (revealerTool != null)
        {
            revealerTool.enabled = true;
        }

        Vector3 startScale = originalScale * interactionStartScaleMultiplier;
        Vector3 targetScale = originalScale * interactionScaleMultiplier;
        transform.localScale = startScale;

        yield return ScaleOverTime(startScale, targetScale, scaleTransitionDuration);

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        yield return ScaleOverTime(targetScale, originalScale, scaleTransitionDuration);

        if (revealerTool != null)
        {
            revealerTool.enabled = false;
        }

        yield return StartCoroutine(PickupSequence());
    }

    private IEnumerator PickupSequence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[ScaleAndToggle] GameObject dengan tag 'Player' tidak ditemukan.", this);
            yield return StartCoroutine(FinishPlayerInteraction());
            isInteracting = false;
            yield break;
        }

        DisablePickupScriptsOnSelfAndParents();

        Vector3 startPosition = pickupRoot.position;
        Vector3 targetPosition = startPosition + Vector3.up * pickupPopHeight;
        yield return MoveRootOverTime(startPosition, targetPosition, pickupPopDuration);

        Vector3 smallScale = pickupRootOriginalScale * pickupScaleMultiplier;
        yield return SecondPopAndShrink(smallScale);

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        while (player != null && !IsTouchingPlayer(player.transform, playerColliders))
        {
            pickupRoot.position = Vector3.Lerp(
                pickupRoot.position,
                player.transform.position,
                Mathf.Clamp01(pickupMoveSpeed * Time.deltaTime));

            yield return null;
        }

        if (player == null)
        {
            Debug.LogError("[ScaleAndToggle] Player menghilang sebelum objek sempat menyentuhnya.", this);
            yield return StartCoroutine(FinishPlayerInteraction());
            isInteracting = false;
            yield break;
        }

        if (pickupRoot != null)
        {
            RemovePickupSpriteRenderers();
            yield return StartCoroutine(ToggleArrayObjectsWithAnimation());
            yield return StartCoroutine(FinishPlayerInteraction());
            Destroy(pickupRoot.gameObject);
        }
    }

    private IEnumerator FinishPlayerInteraction()
    {
        if (interactingPlayer == null) yield break;

        yield return StartCoroutine(interactingPlayer.EndInteractionSitAndWait());
        interactingPlayer.BlockInput = playerInputWasBlocked;
        interactingPlayer = null;
        isInteracting = false;
    }

    private bool IsPlayerInsideInteractionCollider()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider == null)
        {
            interactionCollider = GetComponentInParent<Collider2D>();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (interactionCollider == null || player == null || !interactionCollider.enabled)
        {
            return false;
        }

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null &&
                playerCollider.enabled &&
                interactionCollider.Distance(playerCollider).isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    private void RemovePickupSpriteRenderers()
    {
        RemoveSpriteRenderer(transform);

        if (pickupRoot != transform)
        {
            RemoveSpriteRenderer(pickupRoot);
        }
    }

    private void RemoveSpriteRenderer(Transform target)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.enabled = false;
        Destroy(spriteRenderer);
    }

    private IEnumerator ToggleArrayObjectsWithAnimation()
    {
        CameraController cameraController = CameraController.Instance;
        Transform previousCameraTarget = cameraController != null
            ? cameraController.CurrentTarget
            : null;
        float previousCameraSize = cameraController != null
            ? cameraController.BaseCameraSize
            : 0f;

        if (cameraController != null)
        {
            cameraController.SetCameraSize(toggleCameraSize);
        }

        if (objectsToToggle != null)
        {
            for (int i = 0; i < objectsToToggle.Length; i++)
            {
                GameObject obj = objectsToToggle[i];
                if (obj == null) continue;

                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                    continue;
                }

                if (cameraController != null)
                {
                    cameraController.SetTarget(obj.transform);
                    float elapsed = 0f;
                    while (!cameraController.HasReachedCurrentTarget(toggleCameraArrivalTolerance) &&
                           elapsed < toggleCameraArrivalTimeout)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (!cameraController.HasReachedCurrentTarget(toggleCameraArrivalTolerance))
                    {
                        Debug.LogWarning(
                            "[ScaleAndToggle] Kamera belum mencapai titik spawn sebelum batas waktu; animasi tetap dilanjutkan.",
                            this);
                    }

                    if (toggleCameraFocusDuration > 0f)
                    {
                        yield return new WaitForSeconds(toggleCameraFocusDuration);
                    }
                }

                obj.SetActive(true);

                Vector3 targetScale = obj.transform.localScale;
                Vector3 targetPosition = obj.transform.localPosition;
                obj.transform.localScale = Vector3.zero;
                obj.transform.localPosition = targetPosition + Vector3.down * toggleStartBelowOffset;

                if (cameraController != null)
                {
                    cameraController.TriggerShake(toggleAnimationDuration, toggleCameraShakeMagnitude);
                }

                yield return AnimateToggledObject(
                    obj.transform,
                    targetPosition,
                    targetScale,
                    toggleAnimationDuration);
            }
        }

        if (cameraController != null)
        {
            cameraController.SetTarget(previousCameraTarget);
            cameraController.SetCameraSize(previousCameraSize);
        }
    }

    private IEnumerator AnimateToggledObject(
        Transform toggledTransform,
        Vector3 targetPosition,
        Vector3 targetScale,
        float animationDuration)
    {
        if (animationDuration <= 0f)
        {
            toggledTransform.localPosition = targetPosition;
            toggledTransform.localScale = targetScale;
            yield break;
        }

        Vector3 startPosition = targetPosition + Vector3.down * toggleStartBelowOffset;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            if (toggledTransform == null) yield break;

            float progress = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            toggledTransform.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            toggledTransform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (toggledTransform != null)
        {
            toggledTransform.localPosition = targetPosition;
            toggledTransform.localScale = targetScale;
        }
    }

    private IEnumerator SecondPopAndShrink(Vector3 targetScale)
    {
        Vector3 startPosition = pickupRoot.position;
        Vector3 targetPosition = startPosition + Vector3.up * pickupSecondPopHeight;
        Vector3 startScale = pickupRoot.localScale;
        float transitionDuration = pickupSecondPopDuration;

        if (transitionDuration <= 0f)
        {
            pickupRoot.position = targetPosition;
            pickupRoot.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            pickupRoot.position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            pickupRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pickupRoot.position = targetPosition;
        pickupRoot.localScale = targetScale;
    }

    private void DisablePickupScriptsOnSelfAndParents()
    {
        Transform current = transform;
        while (current != null)
        {
            MonoBehaviour[] scripts = current.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this)
                {
                    script.enabled = false;
                }
            }

            current = current.parent;
        }
    }

    private IEnumerator MoveRootOverTime(Vector3 fromPosition, Vector3 toPosition, float transitionDuration)
    {
        if (transitionDuration <= 0f)
        {
            pickupRoot.position = toPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            pickupRoot.position = Vector3.LerpUnclamped(fromPosition, toPosition, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pickupRoot.position = toPosition;
    }

    private bool IsTouchingPlayer(Transform player, Collider2D[] playerColliders)
    {
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null && playerCollider.enabled &&
                Vector2.Distance(transform.position, playerCollider.ClosestPoint(transform.position)) <= playerContactDistance)
            {
                return true;
            }
        }

        return Vector3.Distance(transform.position, player.position) <= playerContactDistance;
    }

    private IEnumerator ScaleOverTime(Vector3 fromScale, Vector3 toScale, float transitionDuration)
    {
        if (transitionDuration <= 0f)
        {
            transform.localScale = toScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = toScale;
    }

    // Fungsi untuk mengaktifkan/menonaktifkan objek di dalam array
    private void ToggleArrayObjects()
    {
        if (objectsToToggle == null || objectsToToggle.Length == 0) return;

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                // Jika aktif menjadi nonaktif, jika nonaktif menjadi aktif
                obj.SetActive(!obj.activeSelf);
            }
        }
    }
}
