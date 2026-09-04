using UnityEngine;

public class PanelAnimationReceiver : MonoBehaviour
{
    private ObjectivesPanel parentPanel;

    void Awake()
    {
        // Mencari script ObjectivesPanel yang ada di objek induk (Parent)
        parentPanel = GetComponentInParent<ObjectivesPanel>();
    }

    // Fungsi ini yang akan dipanggil oleh Animation Event di objek Animator
    public void TriggerAnimationFinish(string type)
    {
        if (parentPanel != null)
        {
            // Teruskan sinyal ke script induk
            parentPanel.TriggerAnimationFinish(type);
        }
        else
        {
            Debug.LogError("[PanelAnimationReceiver] Tidak menemukan script ObjectivesPanel di objek Parent!", gameObject);
        }
    }
}
