using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private List<FogData> fogList = new List<FogData>();
    private float globalProgress = 0f; 
    private Vector3 targetCenter;

    // Menandai status siklus interaksi tombol E
    private bool isHoldingE = false;
    private bool hasTriggeredSit = false;
    private bool hasTriggeredStand = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Otomatis mencari player jika belum dimasukkan di Inspector
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        InitializeFog();
    }

    void Update()
    {
        targetCenter = mainCamera.transform.position;
        targetCenter.z = 0f;

        HandleInputAndProgress();
        HandlePlayerStateBasedOnFog();
        AnimateFog();
    }

    void HandleInputAndProgress()
    {
        // Deteksi input tahan tombol E
        if (Input.GetKey(KeyCode.E))
        {
            isHoldingE = true;
            globalProgress = Mathf.MoveTowards(globalProgress, 1f, closeSpeed * Time.deltaTime);
        }
        else
        {
            isHoldingE = false;
            globalProgress = Mathf.MoveTowards(globalProgress, 0f, openSpeed * Time.deltaTime);
        }
    }

    void HandlePlayerStateBasedOnFog()
    {
        if (playerController == null) return;

        // KONDISI A: Sedang berproses menutup / menahan E / Kabut belum kembali bersih (progress > 0)
        if (isHoldingE || globalProgress > 0f)
        {
            // Mengunci input kontrol pergerakan player
            playerController.BlockInput = true;

            // Picu animasi Sit tepat saat mulai menekan E (hanya sekali pemicuan)
            if (!hasTriggeredSit)
            {
                hasTriggeredSit = true;
                hasTriggeredStand = false; 
                
                StartCoroutine(playerController.PlayAnimationAndWait("Sit"));
            }
        }
        // KONDISI B: Kabut sudah kembali normal sepenuhnya ke titik terjauhnya (progress == 0)
        else if (globalProgress <= 0f && hasTriggeredSit && !hasTriggeredStand)
        {
            hasTriggeredStand = true;
            hasTriggeredSit = false; 

            // Jalankan urutan berdiri kembali secara bersih
            StartCoroutine(StandAndReleasePlayerRoutine());
        }
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
