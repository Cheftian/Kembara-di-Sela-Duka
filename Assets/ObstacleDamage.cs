using UnityEngine;

public class ObstacleDamage : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private bool hasKilled = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Cek jika yang menyentuh kabut adalah player dan belum memicu kematian
        if (!hasKilled && collision.CompareTag(playerTag))
        {
            hasKilled = true;
            TriggerPlayerDeath(collision.transform);
        }
    }

    private void TriggerPlayerDeath(Transform playerTransform)
    {
        Debug.Log("Player tereliminasi oleh kabut! Memulai proses reset ruangan...");
        
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.RespawnPlayerInRoom(playerTransform);
        }
    }
    private void OnEnable()
    {
        // Membuka kembali status lock kematian saat room di-reset
        hasKilled = false; 
    }
}
