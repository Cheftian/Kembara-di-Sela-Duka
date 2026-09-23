using UnityEngine;

public class NotificationTrigger : MonoBehaviour
{
    [Header("Referensi Notifikasi")]
    [Tooltip("NotificationPopup dengan jenis ini akan dicari otomatis dari Player dan seluruh child-nya")]
    [SerializeField] private NotificationPopup.NotificationType notificationType = NotificationPopup.NotificationType.W;
    private NotificationPopup notification;

    [Header("Pengaturan Tag")]
    [Tooltip("Tag dari GameObject Player")]
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ganti ke OnTriggerEnter jika Anda menggunakan game 3D
        Transform playerTransform = FindPlayerTransform(collision.transform);
        if (playerTransform != null)
        {
            notification = FindNotificationPopup(playerTransform);
            if (notification != null)
            {
                notification.Show();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Ganti ke OnTriggerExit jika Anda menggunakan game 3D
        if (FindPlayerTransform(collision.transform) != null && notification != null)
        {
            notification.Hide();
        }
    }

    private Transform FindPlayerTransform(Transform currentTransform)
    {
        while (currentTransform != null)
        {
            if (currentTransform.CompareTag(playerTag))
            {
                return currentTransform;
            }

            currentTransform = currentTransform.parent;
        }

        return null;
    }

    private NotificationPopup FindNotificationPopup(Transform playerTransform)
    {
        NotificationPopup[] notifications = playerTransform.GetComponentsInChildren<NotificationPopup>(true);

        foreach (NotificationPopup popup in notifications)
        {
            if (popup.Type == notificationType)
            {
                return popup;
            }
        }

        return null;
    }

    public void ShowNotification()
    {
        if (notification != null)
        {
            notification.Show();
        }
    }

    public void HideNotification()
    {
        if (notification != null)
        {
            notification.Hide();
        }
    }
}
