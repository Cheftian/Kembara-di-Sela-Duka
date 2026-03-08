using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveUIManager : MonoBehaviour
{
    public static SaveUIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject saveLoadPanel;
    [SerializeField] private GameObject confirmPanel;

    [Header("Slot UI Elements")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[3];
    [SerializeField] private Button closeMenuButton;

    [Header("Confirm Panel UI")]
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private int selectedSlot = -1;
    private bool isSavingMode = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        saveLoadPanel.SetActive(false);
        confirmPanel.SetActive(false);

        // Menghubungkan fungsi ke setiap tombol slot
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i + 1; // Slot 1, 2, 3
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
        }

        yesButton.onClick.AddListener(ConfirmAction);
        noButton.onClick.AddListener(CloseConfirmPanel);
        if (closeMenuButton != null) closeMenuButton.onClick.AddListener(CloseMenu);
    }

    public void OpenSaveMenu()
    {
        isSavingMode = true;
        UpdateSlotVisuals();
        saveLoadPanel.SetActive(true);
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Cutscene); 
        }
    }

    public void OpenLoadMenu()
    {
        isSavingMode = false;
        UpdateSlotVisuals();
        saveLoadPanel.SetActive(true);
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        }
    }

    public void CloseMenu()
    {
        saveLoadPanel.SetActive(false);
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i + 1;
            if (SaveSystem.HasSave(slotIndex))
            {
                GameData data = SaveSystem.LoadGame(slotIndex);
                slotTexts[i].text = $"Slot {slotIndex}\n{data.currentScene}";
                slotButtons[i].interactable = true;
            }
            else
            {
                slotTexts[i].text = $"Slot {slotIndex}\n- Empty -";
                // Jika sedang mode Load, matikan tombol untuk slot yang kosong
                slotButtons[i].interactable = isSavingMode;
            }
        }
    }

    private void OnSlotClicked(int slot)
    {
        selectedSlot = slot;
        confirmPanel.SetActive(true);

        if (isSavingMode)
        {
            confirmMessageText.text = SaveSystem.HasSave(slot) 
                ? $"Overwrite save data in Slot {slot}?" 
                : $"Save game in Slot {slot}?";
        }
        else
        {
            confirmMessageText.text = $"Load game from Slot {slot}?\nUnsaved progress will be lost.";
        }
    }

    private void ConfirmAction()
    {
        confirmPanel.SetActive(false);
        
        if (isSavingMode)
        {
            ExecuteSave();
        }
        else
        {
            ExecuteLoad();
        }
    }

    private void ExecuteSave()
    {
        GameData newData = new GameData();

        // 1. Menyimpan Scene saat ini
        newData.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 2. Menyimpan Data dari GameManager
        if (GameManager.Instance != null)
        {
            newData.savedChapter = GameManager.Instance.currentChapter;
            newData.savedMemories = GameManager.Instance.memoriesCollected;
        }

        // 3. Menyimpan posisi Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            newData.playerPosition = player.transform.position;
        }

        // 4. Menyimpan status aktif/nonaktif InteractableObject
        InteractableObject[] allInteractables = FindObjectsByType<InteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        int savedObjectCount = 0;

        foreach (InteractableObject obj in allInteractables)
        {
            UniqueID uid = obj.GetComponent<UniqueID>();
            if (uid != null && !string.IsNullOrEmpty(uid.ID))
            {
                ObjectState state = new ObjectState();
                state.objectID = uid.ID;
                state.isActive = obj.gameObject.activeInHierarchy; 
                
                newData.savedObjects.Add(state);
                savedObjectCount++;
            }
        }
        if (UIManager.Instance != null)
        {
            newData.collectedKeys = UIManager.Instance.GetActiveKeys();
        }

        SaveSystem.SaveGame(newData, selectedSlot);
        UpdateSlotVisuals(); 
        
        Debug.Log($"Proses penyimpanan ke slot {selectedSlot} selesai. Chapter: {newData.savedChapter}, Memories: {newData.savedMemories}");
    }

    private void ExecuteLoad()
    {
        GameData loadedData = SaveSystem.LoadGame(selectedSlot);
        
        if (loadedData != null)
        {
            CloseMenu();
            
            // Menyerahkan proses transisi dan penerapan data ke SceneController
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadSavedGame(loadedData);
            }
            else
            {
                Debug.LogError("SceneController tidak ditemukan di dalam Scene!");
            }
        }
    }

    private void CloseConfirmPanel()
    {
        confirmPanel.SetActive(false);
    }
}