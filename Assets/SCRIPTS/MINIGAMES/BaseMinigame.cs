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
    [SerializeField] protected NarrationData outroNarration;
    [SerializeField] private Button closeButton;

    protected InteractableObject linkedInteractable;
    protected bool isSolved = false;
    protected bool canPlayPuzzle = false;
    private bool isTransitioning = false;

    protected virtual void Awake()
    {

        rectTransform = GetComponent<RectTransform>();
        // Posisi tengah layar (visible)
        visiblePosition = Vector2.zero; 
        // Posisi bawah layar (hidden) - Sesuaikan dengan tinggi layar
        hiddenPosition = new Vector2(0, -Screen.height * 1.5f); 
        
        if (closeButton != null) 
            closeButton.onClick.AddListener(CloseMinigame);
    }

    public virtual void SetupMinigame(InteractableObject source)
    {
        // Pastikan rectTransform diisi jika belum (antisipasi Awake belum jalan)
        if (rectTransform == null) 
            rectTransform = GetComponent<RectTransform>();

        linkedInteractable = source;
        isSolved = false;
        canPlayPuzzle = false;
        
        // Sekarang aman untuk mengatur posisi
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

        // Jalankan narasi setelah animasi masuk selesai
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
        if (isSolved || isTransitioning) return;
        StartCoroutine(TransitionOut(false));
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
            StartCoroutine(TransitionOut(true));
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

    // Pindahkan logika narasi akhir untuk memanggil TransitionOut
    private IEnumerator PlayOutroSequence()
    {
        yield return new WaitForSeconds(0.5f);
        NarrationManager.Instance.PlayNarration(outroNarration);
        yield return new WaitUntil(() => GameManager.Instance.currentState == GameManager.GameState.Play);
        
        StartCoroutine(TransitionOut(true));
    }

    private IEnumerator PlayIntroSequence()
    {
        canPlayPuzzle = false; // Kunci mekanisme puzzle
        yield return new WaitForSeconds(0.2f); // Jeda singkat agar UI stabil

        NarrationManager.Instance.PlayNarration(introNarration);

        // Tunggu sampai narasi selesai (GameState kembali ke Play oleh NarrationManager)
        // Kita pantau GameState untuk mengetahui kapan narasi ditutup
        yield return new WaitUntil(() => GameManager.Instance.currentState == GameManager.GameState.Play);

        // Setelah narasi selesai, kembalikan ke Cutscene karena kita masih di dalam Minigame
        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        canPlayPuzzle = true; // Buka kunci mekanisme puzzle
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