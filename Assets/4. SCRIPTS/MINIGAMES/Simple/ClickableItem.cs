using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableItem : MonoBehaviour, IPointerDownHandler
{
    [Header("Visual States")]
    [Tooltip("Objek A: Visual awal yang akan dimatikan saat ditekan")]
    [SerializeField] private GameObject defaultState;
    
    [Tooltip("Objek B: Visual baru yang akan dinyalakan saat ditekan")]
    [SerializeField] private GameObject clickedState;

    private bool isClicked = false;
    private SimpleMinigame minigameManager;

    private void Awake()
    {
        // Mencari manajer minigame di hierarki atasnya
        minigameManager = GetComponentInParent<SimpleMinigame>();
    }

    private void OnEnable()
    {
        // Memastikan status klik di-reset jika panel ditutup lalu dibuka lagi sebelum selesai
        if (minigameManager != null && !isClicked)
        {
            if (defaultState != null) defaultState.SetActive(true);
            if (clickedState != null) clickedState.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isClicked) return;
        if (minigameManager != null && !minigameManager.CanPlay) return;

        isClicked = true;

        // Pergantian visual
        if (defaultState != null) defaultState.SetActive(false);
        if (clickedState != null) clickedState.SetActive(true);

        // Lapor ke manajer bahwa objek ini sudah ditekan
        if (minigameManager != null)
        {
            minigameManager.CheckWinCondition();
        }
    }

    // Properti untuk dibaca oleh SimpleMinigame
    public bool IsResolved => isClicked;

    // Fungsi opsional jika ingin me-reset puzzle dari awal
    public void ResetItem()
    {
        isClicked = false;
        if (defaultState != null) defaultState.SetActive(true);
        if (clickedState != null) clickedState.SetActive(false);
    }
}