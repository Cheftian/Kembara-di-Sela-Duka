using UnityEngine;

public class UniqueID : MonoBehaviour
{
    [Header("Identitas Objek")]
    [Tooltip("Tuliskan ID unik secara spesifik (Contoh: Laci_Buku_01)")]
    [SerializeField] private string id = "";

    public string ID => id;

    // Tombol opsional jika suatu saat malas mengetik manual
    [ContextMenu("Generate Random ID")]
    private void GenerateRandomID()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }
    }
}