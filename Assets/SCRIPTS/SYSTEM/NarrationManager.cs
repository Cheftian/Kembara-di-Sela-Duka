using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI; 

public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

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
        public GameObject narrativePanel; // Text box khusus karakter ini
        public TextMeshProUGUI dialogueText; // Text komponen di text box ini
        public Image characterImage; // Komponen Image untuk Sprite wajah
        public List<ExpressionData> expressions; // List ekspresi wajah
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

    public void PlayNarration(NarrationData data)
    {
        if (data == null || isTransitioning || characters.Length == 0) return;

        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        
        linesQueue.Clear();
        foreach (NarrationData.DialogueStep step in data.dialogueSteps)
        {
            NarrationData.DialogueStep processedStep = step;
            // Ditambahkan fungsi fallback jika fungsi ProcessText Anda belum lengkap di potongan kode awal
            processedStep.dialogueText = ProcessText(step.dialogueText); 
            linesQueue.Enqueue(processedStep);
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
        
        CharacterUIConfig targetCharacter = System.Array.Find(characters, c => c.characterName == currentStep.characterName);

        if (targetCharacter.narrativePanel == null)
        {
            Debug.LogError($"Karakter dengan nama {currentStep.characterName} tidak ditemukan di NarrationManager!");
            return;
        }

        currentLineText = currentStep.dialogueText;

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
        // Matikan panel karakter lain yang tidak berbicara
        foreach (var character in characters)
        {
            if (character.characterName != targetCharacter.characterName && character.narrativePanel.activeSelf)
            {
                character.narrativePanel.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
                character.narrativePanel.SetActive(false);
            }
        }

        // LOGIKA PENYEMBUNYIAN SPRITE KARAKTER
        if (targetCharacter.characterImage != null)
        {
            // Jika ekspresi ditulis "-", matikan object gambar karakter
            if (expressionName == "-")
            {
                targetCharacter.characterImage.gameObject.SetActive(false);
            }
            else
            {
                // Jika bukan "-", pastikan gambar karakter aktif kembali
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
        targetCharacterActivePanelToNull(); // Fungsi pembantu opsional untuk reset panel aktif
        isTransitioning = false;
        GameManager.Instance.SetGameState(GameManager.GameState.Play); // Sesuai logika dasar game Anda
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
