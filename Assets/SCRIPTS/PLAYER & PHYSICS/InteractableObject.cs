using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool isSingleUse = false;
    [SerializeField] private float interactionRadius = 2.5f;
    
    private bool hasInteracted = false;
    private bool isPlayerInRange = false;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private NarrationData lockedNarrationData;
    [Tooltip("All assigned objects must be active in the hierarchy to unlock this interaction.")]
    [SerializeField] private GameObject[] requiredKeyObjects;

    [Header("Triggers")]
    [SerializeField] private bool isMinigameTrigger = false;
    [SerializeField] private bool isSingleMinigame = false;
    [SerializeField] private GameObject minigameObject;
    [Space]
    [SerializeField] private bool isNarrativeTrigger = false;
    [SerializeField] private NarrationData narrationData;

    [Header("Object Toggling")]
    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(interactionRadius * 2, interactionRadius);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
            return;

        if (isPlayerInRange && !hasInteracted)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                StartCoroutine(InteractionSequence());
            }
        }
    }

    // Check if all required key objects are currently active in the scene
    private bool CheckUnlockCondition()
    {
        if (requiredKeyObjects == null || requiredKeyObjects.Length == 0) return false;

        foreach (GameObject keyObj in requiredKeyObjects)
        {
            if (keyObj == null || !keyObj.activeInHierarchy)
            {
                return false; 
            }
        }
        return true; 
    }

    private IEnumerator InteractionSequence()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);

        // Lock validation process
        if (isLocked)
        {
            if (CheckUnlockCondition())
            {
                isLocked = false; 
            }
            else
            {
                if (lockedNarrationData != null)
                {
                    NarrationManager.Instance.PlayNarration(lockedNarrationData);
                }
                else
                {
                    GameManager.Instance.SetGameState(GameManager.GameState.Play);
                }
                
                yield break; // Stop further interactions
            }
        }

        // Execute Minigame
        if (isMinigameTrigger && minigameObject != null)
        {
            BaseMinigame minigameScript = minigameObject.GetComponent<BaseMinigame>();
            
            if (minigameScript != null)
            {
                minigameScript.SetupMinigame(this);
            }
            else
            {
                Debug.LogWarning($"Object {minigameObject.name} tidak memiliki script turunan BaseMinigame!");
            }
            
            minigameObject.SetActive(true);
            yield break; 
        }

        // Execute Narration
        if (isNarrativeTrigger && narrationData != null)
        {
            NarrationManager.Instance.PlayNarration(narrationData);
        }

        if (isSingleUse && !isMinigameTrigger)
        {
            if (!isNarrativeTrigger) GameManager.Instance.SetGameState(GameManager.GameState.Play);
            
            // PERUBAHAN: Mengganti Destroy dengan SetActive(false)
            gameObject.SetActive(false);
            
            yield break;
        }

        if (!isNarrativeTrigger && !isMinigameTrigger) 
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    public void CompleteMinigame()
    {
        foreach (GameObject obj in objectsToEnable) if (obj != null) obj.SetActive(true);
        foreach (GameObject obj in objectsToDisable) if (obj != null) obj.SetActive(false);

        GameManager.Instance.SetGameState(GameManager.GameState.Play);
        
        if (isSingleMinigame)
        {
            // PERUBAHAN: Mengganti Destroy dengan SetActive(false)
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}