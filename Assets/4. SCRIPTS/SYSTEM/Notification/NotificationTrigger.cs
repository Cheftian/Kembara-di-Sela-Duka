using UnityEngine;
using System.Collections;

public class NotificationTrigger : MonoBehaviour
{
    [Header("Referensi Notifikasi")]
    [Tooltip("NotificationPopup dengan jenis ini akan dicari otomatis dari Player dan seluruh child-nya")]
    [SerializeField] private NotificationPopup.NotificationType notificationType = NotificationPopup.NotificationType.W;
    private NotificationPopup notification;

    private void OnEnable()
    {
        StartCoroutine(CheckPlayerAlreadyInside());
    }

    private IEnumerator CheckPlayerAlreadyInside()
    {
        yield return new WaitForFixedUpdate();

        Collider2D triggerCollider = GetComponent<Collider2D>();
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (triggerCollider == null || player == null) yield break;

        if (IsPlayerInside(player.transform))
        {
            notification = FindNotificationPopup(player.transform);
            if (notification != null)
            {
                notification.Show();
            }
        }
    }

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
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (FindPlayerTransform(collision.transform) != null)
        {
            StartCoroutine(HandlePlayerExit(collision.transform));
        }
    }

    private IEnumerator HandlePlayerExit(Transform playerTransform)
    {
        yield return new WaitForFixedUpdate();

        Transform player = FindPlayerTransform(playerTransform);
        if (player == null) yield break;

        notification = FindNotificationPopup(player);
        if (notification == null) yield break;

        if (IsPlayerInside(player))
        {
            notification.Show();
        }
        else
        {
            notification.Hide();
        }
    }

    private bool IsPlayerInside(Transform playerTransform)
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null) return false;

        Collider2D[] playerColliders = playerTransform.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null &&
                (triggerCollider.IsTouching(playerCollider) || triggerCollider.bounds.Intersects(playerCollider.bounds)))
            {
                return true;
            }
        }

        return false;
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
