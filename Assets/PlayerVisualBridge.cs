using UnityEngine;

public class PlayerVisualBridge : MonoBehaviour
{
    private PlayerController playerController;

    private void Awake()
    {
        // Mengambil referensi script utama yang berada di objek induk (Parent)
        playerController = GetComponentInParent<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("PlayerController tidak ditemukan di Parent objek ini!", this);
        }
    }

    // Fungsi ini yang disasar oleh Animation Event di frame terakhir animasi Flip Anda
    public void OnFlipAnimationComplete()
    {
        if (playerController != null)
        {
            playerController.OnFlipAnimationComplete();
        }
    }
}
