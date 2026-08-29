using UnityEngine;
using TMPro;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public enum Language { English, Indonesia }
    
    [Header("Localization Settings")]
    [SerializeField] private Language currentLanguage = Language.Indonesia;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI objectiveTextDisplay; 
    [SerializeField] private ObjectivesPanel objectivesPanel;

    private ObjectiveData currentActiveData;
    private ObjectiveData nextPendingData; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ToggleLanguage(bool isToggledOn)
    {
        currentLanguage = isToggledOn ? Language.English : Language.Indonesia;
        RefreshDisplay();
    }

    public void PlayObjective(ObjectiveData data)
    {
        if (data == null) return;

        // Sekarang membaca IsOpen (Huruf Besar) dengan akurat
        if (objectivesPanel != null && objectivesPanel.IsOpen)
        {
            nextPendingData = data; 
            objectivesPanel.ForceClosePanel(); 
        }
        else
        {
            ExecuteObjectiveImmediate(data);
        }
    }

    public void OnPanelClosedReadyToSwitch()
    {
        if (nextPendingData != null)
        {
            // Alihkan eksekusi ke Coroutine agar kita bisa menyisipkan jeda waktu nyata (delay)
            StartCoroutine(DelayedSwitchSequence());
        }
    }

    private IEnumerator DelayedSwitchSequence()
    {
        // BERIKAN JEDA: Tunggu beberapa saat agar panel dipastikan benar-benar sudah bergeser sembunyi
        // Anda bisa menaikkan angka ini (misal ke 0.2f atau 0.3f) jika teks masih dirasa kurang lambat bergantinya
        yield return new WaitForSecondsRealtime(0.15f);

        if (nextPendingData != null)
        {
            // 1. Ganti data teks objektif setelah panel aman tersembunyi
            currentActiveData = nextPendingData;
            RefreshDisplay();
            nextPendingData = null; // Kosongkan antrean data

            // 2. Berikan jeda super singkat lagi agar render teks UI Unity selesai memproses karakter baru
            yield return new WaitForSecondsRealtime(0.05f);

            // 3. Setelah teks rapi, baru perintahkan panel untuk meluncur masuk terbuka kembali
            if (objectivesPanel != null)
            {
                objectivesPanel.ForceOpenPanel();
            }
        }
    }

    private void ExecuteObjectiveImmediate(ObjectiveData data)
    {
        currentActiveData = data;
        RefreshDisplay();

        if (objectivesPanel != null)
        {
            objectivesPanel.ForceOpenPanel();
        }
    }

    public void ClearObjective()
    {
        if (objectiveTextDisplay != null) objectiveTextDisplay.text = "";
        currentActiveData = null;
        nextPendingData = null;
    }

    private void RefreshDisplay()
    {
        if (currentActiveData == null || objectiveTextDisplay == null) return;
        string textToShow = (currentLanguage == Language.English) ? currentActiveData.objectiveEN : currentActiveData.objectiveIDN;
        objectiveTextDisplay.text = textToShow;
    }
}
