using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject narrativePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;

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
    private bool isTransitioning = false; // Mencegah input saat panel bergerak
    
    private Coroutine typingCoroutine;
    private Queue<string> linesQueue = new Queue<string>();
    
    private RectTransform panelRect;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        panelRect = narrativePanel.GetComponent<RectTransform>();
        
        // Inisialisasi posisi
        visiblePosition = Vector2.zero;
        hiddenPosition = new Vector2(0, -Screen.height);
        
        panelRect.anchoredPosition = hiddenPosition;
        narrativePanel.SetActive(false);
    }

    public void PlayNarration(NarrationData data)
    {
        if (data == null || isTransitioning) return;

        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        
        linesQueue.Clear();
        foreach (string line in data.dialogueLines)
        {
            linesQueue.Enqueue(ProcessText(line));
        }

        // PASTIKAN TEKS KOSONG sebelum panel muncul
        dialogueText.text = ""; 

        narrativePanel.SetActive(true);
        canProcessInput = false;
        
        StopAllCoroutines();
        StartCoroutine(TransitionIn());
    }

    private IEnumerator TransitionIn()
    {
        isTransitioning = true;
        float elapsed = 0;

        // Kosongkan teks sekali lagi untuk memastikan kebersihan visual selama transisi
        dialogueText.text = ""; 

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            panelRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, transitionCurve.Evaluate(t));
            yield return null;
        }

        panelRect.anchoredPosition = visiblePosition;
        isTransitioning = false;
        
        StartCoroutine(EnableInputDelay());
        // Baru panggil baris pertama setelah animasi selesai
        DisplayNextLine(); 
    }

    private void Update()
    {
        if (!narrativePanel.activeSelf || !canProcessInput || isTransitioning) return;

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
            DisplayNextLine();
        }
    }

    private void DisplayNextLine()
    {
        if (linesQueue.Count == 0)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            StartCoroutine(EndNarrationSequence());
            return;
        }

        string currentLine = linesQueue.Dequeue();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentLine));
    }

    private IEnumerator EndNarrationSequence()
    {
        canProcessInput = false;
        isTransitioning = true;
        
        // KOSONGKAN TEKS sebelum panel turun ke bawah
        dialogueText.text = ""; 

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            panelRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, transitionCurve.Evaluate(t));
            yield return null;
        }

        panelRect.anchoredPosition = hiddenPosition;
        narrativePanel.SetActive(false);
        isTransitioning = false;

        yield return new WaitForSeconds(delayBeforePlayState);
        GameManager.Instance.SetGameState(GameManager.GameState.Play);
    }

    private IEnumerator TypeText(string line)
    {
        dialogueText.text = "";
        isTyping = true;
        cancelTyping = false;

        int i = 0;
        while (i < line.Length)
        {
            if (cancelTyping)
            {
                dialogueText.text = line;
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

            dialogueText.text = line.Substring(0, i);
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