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
    [SerializeField] private string openedStateName = "Objectives-opened"; // State diam saat terbuka
    [SerializeField] private string closedStateName = "Objectives-closed"; // State diam saat tertutup




    private bool isOpen = false; 
    private int visualLayerIndex = -1;

    private void OnEnable()
    {
        // Berlangganan ke event perubahan Game State dari GameManager
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        // Melepas langganan saat hancur
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    void Start()
    {
        if (panelRectTransform == null) panelRectTransform = GetComponent<RectTransform>();
        if (panelAnimator == null) panelAnimator = GetComponent<Animator>();

        SetPanelXPosition(closeXPosition);
        
        if (panelAnimator != null)
        {
            visualLayerIndex = panelAnimator.GetLayerIndex("VisualLayer");
            // SetVisualLayerWeight(0f); // <-- HAPUS / KOMENTARI BARIS INI
            panelAnimator.Play(closeStateName, 0, 1f); 
        }

        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.currentState);
        }
    }

    void Update()
    {
        // Jika game sedang tidak dalam mode Play, block input Tab
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
        else if (newState == GameManager.GameState.Play)
        {
            // Tentukan target ke state diam (Opened atau Closed)
            string targetStaticState = isOpen ? openedStateName : closedStateName;

            panelContentObject.SetActive(true);

            if (panelAnimator != null)
            {
                // Kunci langsung ke state diam yang sesuai
                panelAnimator.Play(targetStaticState, 0, 0f);
                panelAnimator.speed = 1f;
            }
        }
    }

    public void TogglePanel()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play) return;
        if (IsAnimationPlaying()) return;

        if (isOpen)
        {
            if (panelAnimator != null) panelAnimator.SetTrigger(closeTrigger);
            SetPanelXPosition(closeXPosition);
            isOpen = false;
        }
        else
        {
            if (panelAnimator != null) panelAnimator.SetTrigger(openTrigger);
            SetPanelXPosition(openXPosition);
            isOpen = true;
        }
    }

    private bool IsAnimationPlaying()
    {
        if (panelAnimator == null) return false;
        if (panelAnimator.IsInTransition(0)) return true;

        AnimatorStateInfo stateInfo = panelAnimator.GetCurrentAnimatorStateInfo(0);
        
        bool isPlayingOpen = stateInfo.IsName(openStateName) && stateInfo.normalizedTime < 1.0f;
        bool isPlayingClose = stateInfo.IsName(closeStateName) && stateInfo.normalizedTime < 1.0f;

        return isPlayingOpen || isPlayingClose;
    }

    private void ManageVisualLayerWeight()
    {
        if (panelAnimator == null || visualLayerIndex == -1) return;

        // Memaksa layer animasi idle tetap berjalan penuh (1) terus menerus
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
}
