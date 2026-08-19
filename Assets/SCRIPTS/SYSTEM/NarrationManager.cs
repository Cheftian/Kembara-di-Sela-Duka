using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; 

public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    // --- SISTEM LOCALIZATION SEDERHANA ---
    public enum Language { English, Indonesia }
    [Header("Localization Settings")]
    [SerializeField] private Language currentLanguage = Language.Indonesia; // Default bahasa
    
    // Menyimpan data ScriptableObject yang sedang aktif agar bisa di-refresh jika toggle ditekan di tengah dialog
    private NarrationData currentActiveData; 
    private int currentLineIndex = 0; 
    private GameManager.GameState stateAfterNarration = GameManager.GameState.Play;

    [System.Serializable]
    public struct ExpressionData
    {
        public string expressionName;
        public Sprite expressionSprite;
    }

    [System.Serializable]
    public struct CharacterUIConfig
    {
        public string characterName;
        public GameObject narrativePanel; 
        public TextMeshProUGUI dialogueText; 
        public Image characterImage; 
        public List<ExpressionData> expressions; 
    }

    [Header("Character Setup (Maksimal 3)")]
    [SerializeField] private CharacterUIConfig[] characters;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float delayBeforePlayState = 1.0f;

    private bool isTyping = false;
    private bool cancelTyping = false;
    private bool canProcessInput = false;
    private bool isTransitioning = false;

    private string currentLineText = "";
    
    private Coroutine typingCoroutine;
    private Queue<NarrationData.DialogueStep> linesQueue = new Queue<NarrationData.DialogueStep>();
    
    private RectTransform activePanelRect;
    private TextMeshProUGUI activeDialogueText;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    public bool IsNarrating => activePanelRect != null || linesQueue.Count > 0 || isTransitioning;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        visiblePosition = Vector2.zero;
        hiddenPosition = new Vector2(0, -Screen.height);
        
        foreach (var character in characters)
        {
            if (character.narrativePanel != null)
            {
                character.narrativePanel.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
                character.narrativePanel.GetComponent<RectTransform>().localScale = Vector3.one; 
                character.narrativePanel.SetActive(false);
            }
        }
    }

    // --- FUNGSI BARU UNTUK DIHUBUNGKAN KE TOMBOL TOGEL UI ---
    public void ToggleLanguage(bool isToggledOn)
    {
        // Contoh: Jika toggle bernilai true (ON) -> English, jika false (OFF) -> Indonesia
        currentLanguage = isToggledOn ? Language.English : Language.Indonesia;

        // Jika pemain mengubah bahasa SAAT dialog sedang berjalan, langsung update teks di layar
        if (IsNarrating && !isTransitioning && currentActiveData != null)
        {
            // Ambil data baris teks saat ini dari ScriptableObject
            NarrationData.DialogueStep currentStep = currentActiveData.dialogueSteps[currentLineIndex];
            
            // Pilih teks bahasa baru
            string rawText = (currentLanguage == Language.English) ? currentStep.dialogueEN : currentStep.dialogueID;
            currentLineText = ProcessText(rawText);

            if (isTyping)
            {
                // Jika sedang mengetik, stop mengetik teks lama dan mulai mengetik teks baru dari awal
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(currentLineText));
            }
            else
            {
                // Jika teks sudah selesai diketik, langsung ubah teks penuhnya
                activeDialogueText.text = currentLineText;
            }
        }
    }

    public void PlayNarration(
        NarrationData data,
        GameManager.GameState narrationState = GameManager.GameState.Cutscene,
        GameManager.GameState stateAfterNarration = GameManager.GameState.Play)
    {
        if (data == null || isTransitioning || characters.Length == 0) return;

        GameManager.Instance.SetGameState(narrationState);
        this.stateAfterNarration = stateAfterNarration;
        
        currentActiveData = data; // Simpan cache data yang sedang diputar
        currentLineIndex = -1; // Reset index baris teks (-1 karena akan langsung ditambah di DisplayNextLine)

        linesQueue.Clear();
        foreach (NarrationData.DialogueStep step in data.dialogueSteps)
        {
            linesQueue.Enqueue(step);
        }

        canProcessInput = false;
        StopAllCoroutines();
        
        DisplayNextLine(true); 
    }

    private void Update()
    {
        if (activePanelRect == null || !canProcessInput || isTransitioning) return;

        if (Input.anyKeyDown)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return;
            HandleInteraction();
        }
    }

    private void HandleInteraction()
    {
        if (isTyping)
        {
            cancelTyping = true;
        }
        else
        {
            DisplayNextLine(false);
        }
    }

    private void DisplayNextLine(bool isFirstLine)
    {
        if (linesQueue.Count == 0)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            StartCoroutine(EndNarrationSequence());
            return;
        }

        NarrationData.DialogueStep currentStep = linesQueue.Dequeue();
        currentLineIndex++; // Lacak baris text ke berapa yang sedang aktif saat ini
        
        CharacterUIConfig targetCharacter = System.Array.Find(characters, c => c.characterName == currentStep.characterName);

        if (targetCharacter.narrativePanel == null)
        {
            Debug.LogError($"Karakter dengan nama {currentStep.characterName} tidak ditemukan di NarrationManager!");
            return;
        }

        // LOGIKA MEMILIH BAHASA BERDASARKAN SYSTEM STATE
        string rawText = (currentLanguage == Language.English) ? currentStep.dialogueEN : currentStep.dialogueID;
        currentLineText = ProcessText(rawText);

        SetupCharacterUI(targetCharacter, currentStep.expressionName);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (isFirstLine)
        {
            StartCoroutine(TransitionIn());
        }
        else
        {
            typingCoroutine = StartCoroutine(TypeText(currentLineText));
        }
    }
    
    private void SetupCharacterUI(CharacterUIConfig targetCharacter, string expressionName)
    {
        foreach (var character in characters)
        {
            if (character.characterName != targetCharacter.characterName && character.narrativePanel.activeSelf)
            {
                character.narrativePanel.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
                character.narrativePanel.SetActive(false);
            }
        }

        if (targetCharacter.characterImage != null)
        {
            if (expressionName == "-")
            {
                targetCharacter.characterImage.gameObject.SetActive(false);
            }
            else
            {
                targetCharacter.characterImage.gameObject.SetActive(true);

                if (targetCharacter.expressions != null)
                {
                    var foundExpression = targetCharacter.expressions.Find(e => e.expressionName == expressionName);
                    if (foundExpression.expressionSprite != null)
                    {
                        targetCharacter.characterImage.sprite = foundExpression.expressionSprite;
                    }
                }
            }
        }

        activePanelRect = targetCharacter.narrativePanel.GetComponent<RectTransform>();
        activeDialogueText = targetCharacter.dialogueText;

        if (!targetCharacter.narrativePanel.activeSelf)
        {
            activeDialogueText.text = "";
            activePanelRect.anchoredPosition = hiddenPosition; 
            targetCharacter.narrativePanel.SetActive(true);
        }
    }

    private IEnumerator TransitionIn()
    {
        isTransitioning = true;
        float elapsed = 0;
        activeDialogueText.text = ""; 

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float curveT = transitionCurve.Evaluate(t);
            
            activePanelRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, curveT);
            yield return null;
        }

        activePanelRect.anchoredPosition = visiblePosition;
        isTransitioning = false;
        
        StartCoroutine(EnableInputDelay());
        typingCoroutine = StartCoroutine(TypeText(currentLineText)); 
    }

    private IEnumerator EndNarrationSequence()
    {
        canProcessInput = false;
        isTransitioning = true;
        activeDialogueText.text = ""; 

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float curveT = transitionCurve.Evaluate(t);
            
            activePanelRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, curveT);
            yield return null;
        }

        activePanelRect.anchoredPosition = hiddenPosition;
        targetCharacterActivePanelToNull(); 
        isTransitioning = false;
        currentActiveData = null; // Clear data cache saat narasi selesai
        GameManager.Instance.SetGameState(stateAfterNarration); 
    }

    private void targetCharacterActivePanelToNull()
    {
        activePanelRect = null;
        activeDialogueText = null;
    }

    private IEnumerator EnableInputDelay()
    {
        yield return new WaitForSeconds(delayBeforePlayState);
        canProcessInput = true;
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        cancelTyping = false;
        activeDialogueText.text = "";

        int i = 0;
        while (i < text.Length && !cancelTyping)
        {
            // Deteksi tag HTML Richtext TMP (seperti <b>, <color>, dll) agar tidak ikut terpotong saat ngetik
            if (text[i] == '<')
            {
                int closeIndex = text.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    i = closeIndex + 1;
                    continue;
                }
            }

            activeDialogueText.text = text.Substring(0, i + 1);
            i++;
            yield return new WaitForSeconds(typingSpeed);
        }

        activeDialogueText.text = text; // Tampilkan teks penuh jika di-skip
        isTyping = false;
        cancelTyping = false;
    }

    // Fungsi pembantu sederhana jika Anda belum memodifikasi regex kustom highlight text Anda
    private string ProcessText(string rawText)
    {
        return rawText;
    }
}
