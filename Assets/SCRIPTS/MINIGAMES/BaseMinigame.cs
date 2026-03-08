using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public abstract class BaseMinigame : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    protected RectTransform rectTransform;
    protected Vector2 hiddenPosition;
    protected Vector2 visiblePosition;

    [Header("Base Narration Settings")]
    [SerializeField] protected NarrationData introNarration;
    [SerializeField] protected bool singleNarration = false;
    [SerializeField] protected NarrationData outroNarration;
    [SerializeField] protected Button closeButton;

    protected InteractableObject linkedInteractable;
    protected bool isSolved = false;
    protected bool canPlayPuzzle = false;
    protected bool isPlayingNarration = false;
    private bool isTransitioning = false;
    private int narrationValue = 0;

    protected virtual void Awake()
    {
        if (!singleNarration)
        {
            narrationValue = 1;  
        }
        else 
        {
            narrationValue = 0;  
        }

        rectTransform = GetComponent<RectTransform>();
        visiblePosition = Vector2.zero; 
        hiddenPosition = new Vector2(0, -Screen.height * 1.5f); 
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMinigame);
        }
    }

    public virtual void SetupMinigame(InteractableObject source)
    {
        if (rectTransform == null) 
            rectTransform = GetComponent<RectTransform>();

        linkedInteractable = source;
        isSolved = false;
        canPlayPuzzle = false;
        isPlayingNarration = false; 
        
        visiblePosition = Vector2.zero;
        hiddenPosition = new Vector2(0, -Screen.height * 1.5f);
        rectTransform.anchoredPosition = hiddenPosition;
    }

    protected virtual void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionIn());
    }

    private IEnumerator TransitionIn()
    {
        isTransitioning = true;
        float elapsed = 0;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, transitionCurve.Evaluate(t));
            yield return null;
        }

        rectTransform.anchoredPosition = visiblePosition;
        isTransitioning = false;

        if (introNarration != null)
        {
            StartCoroutine(PlayIntroSequence());
        }
        else
        {
            canPlayPuzzle = true;
        }
    }

    public virtual void CloseMinigame()
    {
        if (isTransitioning || isPlayingNarration) return;
        if (!isSolved && !canPlayPuzzle) return;

        StartCoroutine(TransitionOut(isSolved));
    }

    protected virtual void WinMinigame()
    {
        isSolved = true;
        canPlayPuzzle = false;

        if (outroNarration != null)
        {
            StartCoroutine(PlayOutroSequence());
        }
        else
        {
            if (closeButton != null) closeButton.interactable = true;
        }
    }

    private IEnumerator TransitionOut(bool isCompleting)
    {
        isTransitioning = true;
        float elapsed = 0;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, transitionCurve.Evaluate(t));
            yield return null;
        }

        rectTransform.anchoredPosition = hiddenPosition;
        isTransitioning = false;

        if (isCompleting)
        {
            FinalizeMinigame();
        }
        else
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    private IEnumerator PlayOutroSequence()
    {
        isPlayingNarration = true;
        if (closeButton != null) closeButton.interactable = false;

        // Jeda 0.5s di sini mungkin diperlukan agar visual transisi menang terlihat 
        // sebelum tertutup panel narasi. Jika tidak butuh, bisa dihapus.
        yield return new WaitForSeconds(0.5f); 
        
        NarrationManager.Instance.PlayNarration(outroNarration);
        
        yield return new WaitUntil(() => GameManager.Instance.currentState == GameManager.GameState.Play);
        
        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        
        isPlayingNarration = false;
        if (closeButton != null) closeButton.interactable = true;
    }

    private IEnumerator PlayIntroSequence()
    {
        canPlayPuzzle = false; 
        isPlayingNarration = true;
        
        if (closeButton != null) closeButton.interactable = false;

        bool shouldPlayNarration = false;

        if (narrationValue == 0)
        {
            shouldPlayNarration = true;
            if (singleNarration)
            {
                narrationValue += 2;    
            }
        }
        else if (narrationValue == 1) 
        {
            shouldPlayNarration = true;
        }

        if (shouldPlayNarration)
        {
            NarrationManager.Instance.PlayNarration(introNarration);
            yield return new WaitForSeconds(2f);
        }

        // Langsung kembalikan state tanpa jeda tambahan
        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        
        canPlayPuzzle = true; 
        isPlayingNarration = false;
        
        if (closeButton != null) closeButton.interactable = true;
    }

    private void FinalizeMinigame()
    {
        gameObject.SetActive(false);
        if (linkedInteractable != null)
        {
            linkedInteractable.CompleteMinigame();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    protected virtual void Update()
    {
        if (isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMinigame();
        }
    }
}