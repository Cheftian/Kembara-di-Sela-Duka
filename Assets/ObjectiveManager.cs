using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public enum Language { English, Indonesia }
    
    [Header("Localization Settings")]
    [SerializeField] private Language currentLanguage = Language.Indonesia;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI objectiveTextDisplay; 
    [Tooltip("Target panel objectives yang memiliki script open-close sebelumnya (Opsional)")]
    [SerializeField] private ObjectivesPanel objectivesPanel;

    private ObjectiveData currentActiveData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Dipanggil dari tombol toggle UI bahasa
    public void ToggleLanguage(bool isToggledOn)
    {
        currentLanguage = isToggledOn ? Language.English : Language.Indonesia;
        RefreshDisplay();
    }

    public void PlayObjective(ObjectiveData data)
    {
        if (data == null) return;

        currentActiveData = data;
        RefreshDisplay();
    }


    // Mengosongkan tampilan teks objektif (misal saat misi selesai)
    public void ClearObjective()
    {
        if (objectiveTextDisplay != null) objectiveTextDisplay.text = "";
        currentActiveData = null;
    }

    // Memperbarui UI teks berdasarkan bahasa aktif saat ini
    private void RefreshDisplay()
    {
        if (currentActiveData == null || objectiveTextDisplay == null) return;
        
        // Memilih teks bahasa dari properti tunggal ScriptableObject
        string textToShow = (currentLanguage == Language.English) ? currentActiveData.objectiveEN : currentActiveData.objectiveIDN;
        
        objectiveTextDisplay.text = textToShow;
    }
}
