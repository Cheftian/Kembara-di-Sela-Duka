using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI; // Ditambahkan untuk komponen Image

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
    // Menggunakan Queue berisi data step lengkap, bukan string saja
    private Queue<NarrationData.DialogueStep> linesQueue = new Queue<NarrationData.DialogueStep>();
    
    // Tracking panel dan text box yang saat ini sedang aktif secara visual
    private RectTransform activePanelRect;
    private TextMeshProUGUI activeDialogueText;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inisialisasi posisi dasar text box (Slide dari bawah layar)
        visiblePosition = Vector2.zero;
        hiddenPosition = new Vector2(0, -Screen.height);
        
        // Sembunyikan semua panel karakter di awal game ke posisi bawah layar
        foreach (var character in characters)
        {
            if (character.narrativePanel != null)
            {
                character.narrativePanel.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
                // Kembalikan skala ke 1 karena kita fokus ke transisi posisi slide
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
            processedStep.dialogueText = ProcessText(step.dialogueText);
            linesQueue.Enqueue(processedStep);
        }

        canProcessInput = false;
        StopAllCoroutines();
        
        // Mulai baris pertama sekaligus melakukan transisi masuk panel pertama
        DisplayNextLine(true); 
    }

    private void Update()
    {
        // Pengecekan activePanelRect memastikan ada panel yang sedang aktif berjalan
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
        
        // Cari konfigurasi UI karakter berdasarkan nama
        CharacterUIConfig targetCharacter = System.Array.Find(characters, c => c.characterName == currentStep.characterName);

        if (targetCharacter.narrativePanel == null)
        {
            Debug.LogError($"Karakter dengan nama {currentStep.characterName} tidak ditemukan di NarrationManager!");
            return;
        }

        // Ambil data teks ke variabel penampung sebelum UI di-reset
        currentLineText = currentStep.dialogueText;

        // Jalankan logika pergantian panel/karakter
        SetupCharacterUI(targetCharacter, currentStep.expressionName);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (isFirstLine)
        {
            // Jika baris pertama, biarkan transisi jalan dulu baru mulai mengetik
            StartCoroutine(TransitionIn());
        }
        else
        {
            // Jika baris berikutnya, langsung ketik teksnya
            typingCoroutine = StartCoroutine(TypeText(currentLineText));
        }
    }
    
private void SetupCharacterUI(CharacterUIConfig targetCharacter, string expressionName)
{
    // Matikan panel karakter lain yang tidak berbicara dengan mengembalikannya ke bawah layar
    foreach (var character in characters)
    {
        if (character.characterName != targetCharacter.characterName && character.narrativePanel.activeSelf)
        {
            character.narrativePanel.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
            character.narrativePanel.SetActive(false);
        }
    }

    // Ganti Sprite Ekspresi Wajah jika komponen gambarnya ada
    if (targetCharacter.characterImage != null && targetCharacter.expressions != null)
    {
        var foundExpression = targetCharacter.expressions.Find(e => e.expressionName == expressionName);
        if (foundExpression.expressionSprite != null)
        {
            targetCharacter.characterImage.sprite = foundExpression.expressionSprite;
        }
    }

    // Set panel dan teks yang aktif saat ini
    activePanelRect = targetCharacter.narrativePanel.GetComponent<RectTransform>();
    activeDialogueText = targetCharacter.dialogueText;

    if (!targetCharacter.narrativePanel.activeSelf)
    {
        activeDialogueText.text = "";
        activePanelRect.anchoredPosition = hiddenPosition; // Pastikan mulai dari bawah layar sebelum slide up
        targetCharacter.narrativePanel.SetActive(true);
    }
}
private IEnumerator TransitionIn()
{
    isTransitioning = true;
    float elapsed = 0;
    activeDialogueText.text = ""; 

    // Slide In: Bergerak dari bawah (hidden) ke tengah (visible)
    while (elapsed < transitionDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / transitionDuration;
        
        // transitionCurve.Evaluate(t) merubah nilai t linear menjadi lambat-cepat-lambat
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

    // Slide Out: Bergerak turun kembali ke bawah layar
    while (elapsed < transitionDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / transitionDuration;
        
        // Balik kurva untuk transisi keluar (dari visible ke hidden)
        float curveT = transitionCurve.Evaluate(t);
        
        activePanelRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, curveT);
        yield return null;
    }

    activePanelRect.anchoredPosition = hiddenPosition;
    
    foreach (var character in characters)
    {
        character.narrativePanel.SetActive(false);
    }

    activePanelRect = null;
    activeDialogueText = null;
    isTransitioning = false;

    yield return new WaitForSeconds(delayBeforePlayState);
    GameManager.Instance.SetGameState(GameManager.GameState.Play);
}

    private IEnumerator TypeText(string line)
    {
        activeDialogueText.text = "";
        isTyping = true;
        cancelTyping = false;

        int i = 0;
        while (i < line.Length)
        {
            if (cancelTyping)
            {
                activeDialogueText.text = line;
                yield return new WaitForEndOfFrame();
                break;
            }

            if (line[i] == '<')
            {
                int endTag = line.IndexOf('>', i);
                if (endTag != -1) i = endTag + 1;
            }
            else
            {
                i++;
            }

            activeDialogueText.text = line.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private IEnumerator EnableInputDelay()
    {
        yield return new WaitForEndOfFrame();
        canProcessInput = true;
    }

    private string ProcessText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string hexColor = "#" + ColorUtility.ToHtmlStringRGB(highlightColor);
        text = Regex.Replace(text, @"\*([^*]+)\*", "<b>$1</b>");
        text = Regex.Replace(text, @"_([^_]+)_", "<i>$1</i>");
        text = Regex.Replace(text, @"%([^%]+)%", $"<color={hexColor}>$1</color>");
        text = Regex.Replace(text, @"~([^~]+)~", "<smallcaps>$1</smallcaps>");
        return text;
    }
}
