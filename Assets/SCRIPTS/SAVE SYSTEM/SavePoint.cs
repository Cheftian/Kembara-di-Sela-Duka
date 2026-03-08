using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SavePoint : MonoBehaviour
{
    [Header("Save Point Settings")]
    [Tooltip("Radius interaksi untuk save point")]
    [SerializeField] private float interactionRadius = 2.5f;

    private bool isPlayerInRange = false;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(interactionRadius * 2, interactionRadius);
    }

    private void Update()
    {
        // Validasi GameState agar interaksi hanya bisa dilakukan saat mode Play
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
            return;

        if (isPlayerInRange)
        {
            // Mendengarkan input dari pemain
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                TriggerSaveMenu();
            }
        }
    }

    private void TriggerSaveMenu()
    {
        if (SaveUIManager.Instance != null)
        {
            SaveUIManager.Instance.OpenSaveMenu();
        }
        else
        {
            Debug.LogWarning("SaveUIManager Instance is missing in the scene!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}