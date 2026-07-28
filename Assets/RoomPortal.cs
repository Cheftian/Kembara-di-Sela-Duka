using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Target Konfigurasi")]
    [Tooltip("Target portal tujuan saat Player menekan W")]
    public RoomPortal targetPortal; 
    
    [Tooltip("Parent GameObject dari ruangan tempat portal ini berada")]
    public GameObject currentRoomParent;

    [Header("Pengaturan Posisi Muncul")]
    [Tooltip("Offset jarak X saat player muncul di portal ini (misal: -1 agar di kiri portal, 1 agar di kanan portal)")]
    public float spawnOffsetX = 0f;

    private bool playerIsInside = false;
    private Transform playerTransform;

    private void Update()
    {
        if (playerIsInside && Input.GetKeyDown(KeyCode.W))
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        if (targetPortal == null)
        {
            Debug.LogWarning("Target Portal belum dipasang pada " + gameObject.name);
            return;
        }

        RoomManager.Instance.SwitchRoom(playerTransform, this, targetPortal);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerIsInside = true;
            playerTransform = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerIsInside = false;
            playerTransform = null;
        }
    }
}
