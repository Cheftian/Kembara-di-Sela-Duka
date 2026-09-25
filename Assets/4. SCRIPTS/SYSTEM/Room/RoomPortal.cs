using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Target Konfigurasi")]
    [Tooltip("Target portal tujuan saat Player menekan tombol yang dipilih")]
    public RoomPortal targetPortal; 

    public enum PortalKey { W, S }

    [Tooltip("Tombol yang digunakan untuk mengaktifkan portal. W memakai transisi room biasa, S memakai FlashIn dan FlashOut.")]
    [SerializeField] private PortalKey activationKey = PortalKey.W;
    
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
        if (!playerIsInside || isTeleporting)
        {
            return;
        }

        KeyCode selectedKey = activationKey == PortalKey.S ? KeyCode.S : KeyCode.W;
        if (Input.GetKeyDown(selectedKey))
        {
            if (activationKey == PortalKey.S)
            {
                TeleportPlayerWithFlash();
            }
            else
            {
                TeleportPlayer();
            }
        }
    }

    private void TeleportPlayer()
    {
        if (!HasValidTargetPortal())
        {
            return;
        }

        isTeleporting = true; 

        // Sembunyikan notifikasi sebelum pindah ruangan
        if (notificationTrigger != null)
        {
            notificationTrigger.HideNotification(); // Sembunyikan pop-up UI
        }

        RoomManager.Instance.SwitchRoom(playerTransform, this, targetPortal);
    }

    private void TeleportPlayerWithFlash()
    {
        if (!HasValidTargetPortal())
        {
            return;
        }

        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager.Instance tidak ditemukan saat teleport dengan tombol S.", this);
            return;
        }

        isTeleporting = true;

        if (notificationTrigger != null)
        {
            notificationTrigger.HideNotification();
        }

        RoomManager.Instance.SwitchRoomWithFlash(playerTransform, this, targetPortal);
    }

    public bool TeleportPlayerFromGlitch(Transform player)
    {
        if (targetPortal == null)
        {
            Debug.LogWarning("Target Portal belum dipasang pada " + gameObject.name);
            return false;
        }

        if (player == null)
        {
            Debug.LogError("Player tidak ditemukan saat teleport dari glitch.", this);
            return false;
        }

        if (isTeleporting)
        {
            Debug.LogWarning("Teleport glitch dibatalkan karena portal sedang teleporting: " + gameObject.name, this);
            return false;
        }

        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager.Instance tidak ditemukan saat teleport dari glitch.", this);
            return false;
        }

        isTeleporting = true;
        RoomPortal destinationPortal = targetPortal == this ? this : targetPortal;
        RoomManager.Instance.SwitchRoomFromGlitch(player, this, destinationPortal);
        return true;
    }

    private bool HasValidTargetPortal()
    {
        if (targetPortal == null)
        {
            Debug.LogWarning("Target Portal belum dipasang pada " + gameObject.name);
            return false;
        }

        if (targetPortal == this)
        {
            Debug.LogWarning("Target Portal tidak boleh sama dengan portal sumber: " + gameObject.name, this);
            return false;
        }

        return true;
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
