using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FogManager : MonoBehaviour
{
    [System.Serializable]
    public class FogData
    {
        public GameObject fogObject;
        public Vector3 startPosition; 
        public float speedMultiplier;  
    }

    [Header("Pool & Prefab Settings")]
    [SerializeField] private GameObject fogPrefab;
    [SerializeField] private int totalFogCount = 40;

    [Header("Sorting Layer Settings")]
    [SerializeField] private int minSortingOrder = 10;
    [SerializeField] private int maxSortingOrder = 30;

    [Header("Color Settings (Berdasarkan Layer)")]
    [SerializeField] private Color darkLayerColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); 
    [SerializeField] private Color lightLayerColor = new Color(0.6f, 0.6f, 0.6f, 0.4f);

    [Header("Scale Settings (Berdasarkan Layer)")]
    [SerializeField] private float minScale = 2f;
    [SerializeField] private float maxScale = 7f;

    [Header("Spawn Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float safetyOffset = 5f; 

    [Header("Fog Behavior Settings")]
    [SerializeField] private float minSpeedMultiplier = 0.5f;
    [SerializeField] private float maxSpeedMultiplier = 1.5f;
    
    [Header("Control Settings")]
    [SerializeField] private float closeSpeed = 0.5f; 
    [SerializeField] private float openSpeed = 0.8f;  

    [Header("Player Interaction Settings")]
    [Tooltip("Seret objek Player yang memiliki PlayerController ke sini")]
    [SerializeField] private PlayerController playerController;
    [SerializeField, Min(0f)] private float interactionShakeMagnitude = 0.03f;

    [Header("Automatic Fog Cycle")]
    [SerializeField, Range(0f, 100f)] private float automaticCloseThresholdPercent = 80f;
    [SerializeField, Min(0f)] private float closedWaitDuration = 3f;
    [SerializeField] private GameObject objectToActivate;
    [SerializeField] private GameObject objectToDeactivate;

    [Header("Interaction Area")]
    [Tooltip("Ukuran BoxCollider2D yang dibuat otomatis jika objek ini belum memiliki collider.")]
    [SerializeField] private Vector2 generatedInteractionAreaSize = new Vector2(10f, 10f);

    private enum AutomaticCycleState
    {
        None,
        Closing,
        Waiting,
        Opening
    }

    private List<FogData> fogList = new List<FogData>();
    private float globalProgress = 0f; 
    private Vector3 targetCenter;
    private CameraController cameraController;
    private CameraGlitchEffects cameraGlitchEffects;
    private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();
    private AutomaticCycleState automaticCycleState;
    private float closedWaitTimer;
    private bool nextCycleActivatesObject = true;
    private bool isPlayerInsideCollider;
    private bool movementCapabilitiesOverridden;
    private bool previousCanRun;
    private bool previousCanJump;

    // Menandai status siklus interaksi tombol Q
    private bool isHoldingInteractionKey = false;
    private bool isCurrentPressAutomated;
    private bool isInteractionKeyArmed = true;
    private bool isShakeRecovering = false;
    private bool hasTriggeredSit = false;
    private bool isSitAnimationPending;
    private bool hasTriggeredStand = false;

    private void Awake()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider == null)
        {
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = generatedInteractionAreaSize;
            interactionCollider = boxCollider;
        }

        interactionCollider.isTrigger = true;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<CameraController>();
            cameraGlitchEffects = mainCamera.GetComponent<CameraGlitchEffects>();
        }
        if (cameraController == null)
        {
            cameraController = CameraController.Instance;
        }

        // Otomatis mencari player jika belum dimasukkan di Inspector
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        InitializeFog();
    }

    private void OnDisable()
    {
        cameraGlitchEffects?.SetFogAreaVignetteActive(false);
        if (isHoldingInteractionKey && cameraController != null)
        {
            cameraController.StopContinuousShake();
        }

        RestorePlayerMovementCapabilities();
        playerCollidersInside.Clear();
        isPlayerInsideCollider = false;
    }

    void Update()
    {
        targetCenter = mainCamera.transform.position;
        targetCenter.z = 0f;

        if (isPlayerInsideCollider)
        {
            HandleInputAndProgress();
        }
        else
        {
            HandleOutsideInteractionArea();
        }

        HandlePlayerStateBasedOnFog();
        AnimateFog();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController enteringPlayer = other.GetComponentInParent<PlayerController>();
        if (enteringPlayer == null ||
            (playerController != null && enteringPlayer != playerController))
        {
            return;
        }

        if (playerController == null)
        {
            playerController = enteringPlayer;
        }

        if (!playerCollidersInside.Add(other))
        {
            return;
        }

        if (playerCollidersInside.Count == 1)
        {
            previousCanRun = playerController.CanRun;
            previousCanJump = playerController.CanJump;
            playerController.CanRun = false;
            playerController.CanJump = false;
            movementCapabilitiesOverridden = true;
        }

        isPlayerInsideCollider = playerCollidersInside.Count > 0;
        if (isPlayerInsideCollider && cameraGlitchEffects != null)
        {
            cameraGlitchEffects.SetFogAreaVignetteActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerController == null ||
            other.GetComponentInParent<PlayerController>() != playerController)
        {
            return;
        }

        playerCollidersInside.Remove(other);
        isPlayerInsideCollider = playerCollidersInside.Count > 0;
        if (!isPlayerInsideCollider)
        {
            RestorePlayerMovementCapabilities();
            cameraGlitchEffects?.SetFogAreaVignetteActive(false);
        }
    }

    private void RestorePlayerMovementCapabilities()
    {
        if (!movementCapabilitiesOverridden || playerController == null) return;

        playerController.CanRun = previousCanRun;
        playerController.CanJump = previousCanJump;
        movementCapabilitiesOverridden = false;
    }

    private void HandleOutsideInteractionArea()
    {
        if (isHoldingInteractionKey)
        {
            isHoldingInteractionKey = false;
            isCurrentPressAutomated = false;
            if (cameraController != null)
            {
                cameraController.StopContinuousShake();
                isShakeRecovering = true;
            }
        }

        isInteractionKeyArmed = true;
        automaticCycleState = AutomaticCycleState.None;
        globalProgress = Mathf.MoveTowards(globalProgress, 0f, openSpeed * Time.deltaTime);

        if (isShakeRecovering && cameraController != null && cameraController.IsShakeSettled)
        {
            isShakeRecovering = false;
        }
    }

    void HandleInputAndProgress()
    {
        if (Input.GetKeyUp(KeyCode.Q))
        {
            isInteractionKeyArmed = true;
        }

        if (Input.GetKeyDown(KeyCode.Q) &&
            isInteractionKeyArmed &&
            !isShakeRecovering &&
            automaticCycleState == AutomaticCycleState.None)
        {
            isHoldingInteractionKey = true;
            isCurrentPressAutomated = false;
            isInteractionKeyArmed = false;
            if (cameraController != null)
            {
                cameraController.StartContinuousShake(interactionShakeMagnitude);
            }
        }

        if (isHoldingInteractionKey && Input.GetKey(KeyCode.Q))
        {
            if (!isCurrentPressAutomated &&
                automaticCycleState == AutomaticCycleState.None)
            {
                globalProgress = Mathf.MoveTowards(globalProgress, 1f, closeSpeed * Time.deltaTime);
                if (globalProgress >= automaticCloseThresholdPercent / 100f)
                {
                    automaticCycleState = AutomaticCycleState.Closing;
                    isCurrentPressAutomated = true;
                }
            }
        }
        else
        {
            if (isHoldingInteractionKey)
            {
                isHoldingInteractionKey = false;
                isCurrentPressAutomated = false;
                if (cameraController != null)
                {
                    cameraController.StopContinuousShake();
                    isShakeRecovering = true;
                }
            }

            if (automaticCycleState == AutomaticCycleState.None)
            {
                globalProgress = Mathf.MoveTowards(globalProgress, 0f, openSpeed * Time.deltaTime);
            }
        }

        if (isShakeRecovering && cameraController.IsShakeSettled)
        {
            isShakeRecovering = false;
        }

        UpdateAutomaticCycle();
    }

    private void UpdateAutomaticCycle()
    {
        switch (automaticCycleState)
        {
            case AutomaticCycleState.Closing:
                globalProgress = Mathf.MoveTowards(globalProgress, 1f, closeSpeed * Time.deltaTime);
                if (globalProgress >= 1f)
                {
                    globalProgress = 1f;
                    objectToActivate?.SetActive(nextCycleActivatesObject);
                    objectToDeactivate?.SetActive(!nextCycleActivatesObject);
                    nextCycleActivatesObject = !nextCycleActivatesObject;
                    closedWaitTimer = closedWaitDuration;
                    automaticCycleState = AutomaticCycleState.Waiting;
                }
                break;

            case AutomaticCycleState.Waiting:
                closedWaitTimer -= Time.deltaTime;
                if (closedWaitTimer <= 0f)
                {
                    automaticCycleState = AutomaticCycleState.Opening;
                }
                break;

            case AutomaticCycleState.Opening:
                globalProgress = Mathf.MoveTowards(globalProgress, 0f, openSpeed * Time.deltaTime);
                if (globalProgress <= 0f)
                {
                    globalProgress = 0f;
                    automaticCycleState = AutomaticCycleState.None;
                }
                break;
        }
    }

    void HandlePlayerStateBasedOnFog()
    {
        if (playerController == null) return;

        // KONDISI A: Sedang berproses menutup / menahan E / Kabut belum kembali bersih (progress > 0)
        if (isHoldingInteractionKey || globalProgress > 0f)
        {
            // Mengunci input kontrol pergerakan player
            playerController.BlockInput = true;

            // Picu animasi Sit tepat saat mulai menekan E (hanya sekali pemicuan)
            if (!hasTriggeredSit)
            {
                hasTriggeredSit = true;
                hasTriggeredStand = false; 
                isSitAnimationPending = true;
                StartCoroutine(PlaySitAfterFlipRoutine());
            }
        }
        // KONDISI B: Kabut sudah kembali normal sepenuhnya ke titik terjauhnya (progress == 0)
        else if (globalProgress <= 0f &&
                 hasTriggeredSit &&
                 !isSitAnimationPending &&
                 !hasTriggeredStand)
        {
            hasTriggeredStand = true;
            hasTriggeredSit = false; 

            // Jalankan urutan berdiri kembali secara bersih
            StartCoroutine(StandAndReleasePlayerRoutine());
        }
    }

    private IEnumerator PlaySitAfterFlipRoutine()
    {
        while (playerController.IsFlipping)
        {
            yield return null;
        }

        yield return StartCoroutine(playerController.PlayAnimationAndWait("Sit"));
        isSitAnimationPending = false;
    }

    private IEnumerator StandAndReleasePlayerRoutine()
    {
        // 1. Putar animasi "Stand" dan tunggu sampai durasinya selesai
        yield return StartCoroutine(playerController.PlayAnimationAndWait("Stand"));

        // 2. Lepas kunci input dan kembalikan status player ke kondisi normal (Idle)
        playerController.BlockInput = false;
        playerController.ResetToIdleState();
    }

    void InitializeFog()
    {
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        
        float dynamicBufferX = camWidth + maxScale + safetyOffset;
        float dynamicBufferY = camHeight + maxScale + safetyOffset;

        for (int i = 0; i < totalFogCount; i++)
        {
            GameObject obj = Instantiate(fogPrefab, transform);
            obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            int randomOrder = Random.Range(minSortingOrder, maxSortingOrder + 1);
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = randomOrder;
            }

            float layerPercentage = 0f;
            if (maxSortingOrder != minSortingOrder)
            {
                layerPercentage = (float)(randomOrder - minSortingOrder) / (maxSortingOrder - minSortingOrder);
            }

            if (spriteRenderer != null)
            {
                Color dynamicColor = Color.Lerp(darkLayerColor, lightLayerColor, layerPercentage);
                spriteRenderer.color = dynamicColor;
            }

            float dynamicScale = Mathf.Lerp(minScale, maxScale, layerPercentage);
            obj.transform.localScale = new Vector3(dynamicScale, dynamicScale, 1f);

            Vector3 spawnPos = GetCalculatedFarPosition(dynamicBufferX, dynamicBufferY);
            obj.transform.position = spawnPos;

            FogData data = new FogData();
            data.fogObject = obj;
            data.startPosition = obj.transform.position - mainCamera.transform.position; 
            data.speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);

            fogList.Add(data);
        }
    }

    void AnimateFog()
    {
        Vector3 camPos = mainCamera.transform.position;
        camPos.z = 0f;

        foreach (FogData fog in fogList)
        {
            Vector3 dynamicStartPos = camPos + fog.startPosition;
            float individualProgress = Mathf.Clamp01(globalProgress * fog.speedMultiplier);
            
            fog.fogObject.transform.position = Vector3.Lerp(dynamicStartPos, targetCenter, individualProgress);
        }
    }

    Vector3 GetCalculatedFarPosition(float bufferX, float bufferY)
    {
        Vector3 camPos = mainCamera.transform.position;
        int side = Random.Range(0, 4);
        float posX = 0f, posY = 0f;

        switch (side)
        {
            case 0: posX = Random.Range(-bufferX, bufferX); posY = bufferY; break;
            case 1: posX = Random.Range(-bufferX, bufferX); posY = -bufferY; break;
            case 2: posX = -bufferX; posY = Random.Range(-bufferY, bufferY); break;
            case 3: posX = bufferX; posY = Random.Range(-bufferY, bufferY); break;
        }

        return new Vector3(camPos.x + posX, camPos.y + posY, 0f);
    }
}
