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
    public enum FaceDirection { Left, Right }
    public FaceDirection faceDirectionOnSpawn = FaceDirection.Right;

    private bool playerIsInside = false;
    private Transform playerTransform;
    private bool isTeleporting = false; 

    // BARU: Tempat menyimpan referensi script NotificationTrigger
    private NotificationTrigger notificationTrigger;

    private void Start()
    {
        // BARU: Ambil komponen NotificationTrigger yang ada di GameObject ini
        notificationTrigger = GetComponent<NotificationTrigger>();
    }

    private void Update()
    {
        if (playerIsInside && Input.GetKeyDown(KeyCode.W) && !isTeleporting)
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

        isTeleporting = true; 

        // BARU: Sembunyikan notifikasi dan matikan trigger-nya sebelum pindah ruangan
        if (notificationTrigger != null)
        {
            if (notificationTrigger.notification != null)
            {
                notificationTrigger.notification.Hide(); // Sembunyikan pop-up UI
            }
            notificationTrigger.enabled = false; // Matikan script NotificationTrigger
        }

        RoomManager.Instance.SwitchRoom(playerTransform, this, targetPortal);
    }

    public void ResetTeleportStatus()
    {
        isTeleporting = false;
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
            isTeleporting = false; 
        }
    }
}
