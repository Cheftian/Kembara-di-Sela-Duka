using UnityEngine;

public class NotificationTrigger : MonoBehaviour
{
    [Header("Referensi Notifikasi")]
    [Tooltip("Masukkan GameObject notifikasi yang memiliki script NotificationPopup")]
    public NotificationPopup notification;

    [Header("Pengaturan Tag")]
    [Tooltip("Tag dari GameObject Player")]
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ganti ke OnTriggerEnter jika Anda menggunakan game 3D
        if (collision.CompareTag(playerTag) && notification != null)
        {
            notification.Show();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Ganti ke OnTriggerExit jika Anda menggunakan game 3D
        if (collision.CompareTag(playerTag) && notification != null)
        {
            notification.Hide();
        }
    }
}
