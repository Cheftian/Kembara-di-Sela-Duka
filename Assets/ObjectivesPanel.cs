using UnityEngine;
using System.Collections;

public class ObjectivesPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private Animator panelAnimator;
    
    [Tooltip("Tarik objek isi visual panel di sini (bukan root yang memegang script ini) agar objek ini bisa di-disable total saat pause")]
    [SerializeField] private GameObject panelContentObject;

    [Header("Positions")]
    [SerializeField] private float openXPosition = 105f;
    [SerializeField] private float closeXPosition = 748f;

    [Header("Animation Settings")]
    [SerializeField] private string openTrigger = "open";
    [SerializeField] private string closeTrigger = "close";
    [SerializeField] private string openStateName = "Objectives-open";
    [SerializeField] private string closeStateName = "Objectives-close";
    
    [Header("New State Settings (Diam/Looping)")]
    [SerializeField] private string openedStateName = "Objectives-opened"; 
    [SerializeField] private string closedStateName = "Objectives-closed"; 


    [Header("Auto Close Settings")]
    [SerializeField] private float autoCloseDelay = 4f; 
    private Coroutine autoCloseCoroutine;

    public bool IsOpen { get; private set; } = false;
    private bool isTransitioning = false;
    private int visualLayerIndex = -1;

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    void Start()
    {
        if (panelRectTransform == null) panelRectTransform = GetComponent<RectTransform>();
        if (panelAnimator == null) panelAnimator = GetComponent<Animator>();

        SetPanelXPosition(closeXPosition);
        
        if (panelAnimator != null)
        {
            panelAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            visualLayerIndex = panelAnimator.GetLayerIndex("VisualLayer");
            panelAnimator.Play(closedStateName, 0, 1f); 
        }

        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.currentState);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }

        ManageVisualLayerWeight();
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        if (panelContentObject == null) return;

        if (newState == GameManager.GameState.Pause)
        {
            float currentX = panelRectTransform.anchoredPosition.x;
            if (panelAnimator != null) panelAnimator.speed = 0f;
            panelContentObject.SetActive(false);
            SetPanelXPosition(currentX);
        }
        else 
        {
            panelContentObject.SetActive(true);
            if (panelAnimator != null)
            {
                panelAnimator.speed = 1f;
                if (isTransitioning) return; 

                string targetStaticState = IsOpen ? openedStateName : closedStateName;
                panelAnimator.Play(targetStaticState, 0, 0f);
            }
        }
    }

    public void TogglePanel()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play) return;
        if (isTransitioning) return; 

        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        isTransitioning = true;

        if (IsOpen)
        {
            if (panelAnimator != null) panelAnimator.SetTrigger(closeTrigger);
            IsOpen = false;
        }
        else
        {
            if (panelAnimator != null) panelAnimator.SetTrigger(openTrigger);
            IsOpen = true;
            autoCloseCoroutine = StartCoroutine(ClosePanelAfterDelay());
        }
    }

    private IEnumerator ClosePanelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoCloseDelay);

        if (IsOpen && (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Pause))
        {
            isTransitioning = true;
            if (panelAnimator != null) panelAnimator.SetTrigger(closeTrigger);
            IsOpen = false;
        }
    }

    public void ForceOpenPanel()
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        if (!IsOpen)
        {
            isTransitioning = true; 
            if (panelAnimator != null) panelAnimator.SetTrigger(openTrigger);
            IsOpen = true;
        }
        autoCloseCoroutine = StartCoroutine(ClosePanelAfterDelay());
    }

    public void ForceClosePanel()
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        if (IsOpen)
        {
            isTransitioning = true; 
            if (panelAnimator != null) panelAnimator.SetTrigger(closeTrigger);
            IsOpen = false;
        }
    }

    public void TriggerAnimationFinish(string type)
    {
        isTransitioning = false; // BEBASKAN KUNCI UTAMA (Tombol Tab kembali berfungsi)

        if (type == "close")
        {
            SetPanelXPosition(closeXPosition); 
            if (panelAnimator != null) panelAnimator.Play(closedStateName, 0, 0f); 

            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnPanelClosedReadyToSwitch();
            }
        }
        else if (type == "open")
        {
            SetPanelXPosition(openXPosition); 
            if (panelAnimator != null) panelAnimator.Play(openedStateName, 0, 0f); 
        }
    }
    private void ManageVisualLayerWeight()
    {
        if (panelAnimator == null || visualLayerIndex == -1) return;
        SetVisualLayerWeight(1f);
    }

    private void SetVisualLayerWeight(float weight)
    {
        if (panelAnimator != null && visualLayerIndex != -1)
        {
            panelAnimator.SetLayerWeight(visualLayerIndex, weight);
        }
    }

    private void SetPanelXPosition(float xPos)
    {
        if (panelRectTransform != null)
        {
            Vector3 currentPos = panelRectTransform.anchoredPosition;
            panelRectTransform.anchoredPosition = new Vector3(xPos, currentPos.y, currentPos.z);
        }
    }

    public void ResetTransitionLock()
    {
        isTransitioning = false; // Paksa buka kunci input tombol Tab
        
        // Pastikan animator mengunci posisi diam yang sesuai dengan status aslinya saat ini
        if (panelAnimator != null)
        {
            panelAnimator.speed = 1f;
            string targetStaticState = IsOpen ? openedStateName : closedStateName;
            panelAnimator.Play(targetStaticState, 0, 0f);
        }
    }
}
