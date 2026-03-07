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

    [Header("Triggers")]
    [SerializeField] private bool isMinigameTrigger = false;
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

    private IEnumerator InteractionSequence()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);

        // Eksekusi Minigame
        if (isMinigameTrigger && minigameObject != null)
        {
            // PENCARIAN DIUBAH MENJADI BASEMINIGAME
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

        // Eksekusi Narasi
        if (isNarrativeTrigger && narrationData != null)
        {
            NarrationManager.Instance.PlayNarration(narrationData);
        }

        yield return new WaitForSeconds(0.1f);

        if (isSingleUse && !isMinigameTrigger)
        {
            if (!isNarrativeTrigger) GameManager.Instance.SetGameState(GameManager.GameState.Play);
            Destroy(gameObject);
            yield break;
        }

        if (!isNarrativeTrigger && !isMinigameTrigger) 
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    // Fungsi ini akan dipanggil oleh RotationMinigame saat puzzle selesai
    public void CompleteMinigame()
    {
        foreach (GameObject obj in objectsToEnable) if (obj != null) obj.SetActive(true);
        foreach (GameObject obj in objectsToDisable) if (obj != null) obj.SetActive(false);

        GameManager.Instance.SetGameState(GameManager.GameState.Play);
        
        // Hapus objek interaksi agar tidak bisa diakses lagi setelah selesai
        Destroy(gameObject);
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