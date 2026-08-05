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

    private bool isTeleporting = false; 
private void Update()
{
    // UBAH: Tambahkan kondisi !isTeleporting di dalam IF
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

    isTeleporting = true; // TAMBAHKAN INI: Kunci input W segera setelah ditekan
    RoomManager.Instance.SwitchRoom(playerTransform, this, targetPortal);
}

// TAMBAHKAN FUNGSI BARU INI: Untuk membuka kembali kunci tombol W
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
        isTeleporting = false; // TAMBAHKAN INI: Reset otomatis jika player keluar collider
    }
}

}
