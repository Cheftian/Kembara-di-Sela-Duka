using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpriteLayerDistributor : MonoBehaviour
{
    [Header("Sorting Layer Settings")]
    [Tooltip("Nama Sorting Layer target (kosongkan jika ingin memakai Default)")]
    public string sortingLayerName = "Default";

    [Header("Order Range")]
    [Tooltip("Nilai Sorting Order terkecil untuk child pertama")]
    public int minOrder = 10;

    [Tooltip("Nilai Sorting Order terbesar untuk child terakhir")]
    public int maxOrder = 100;

    [Header("Behavior")]
    [Tooltip("Jika dicentang, proses pengurutan juga berlaku untuk cucu (child didalam child)")]
    public bool includeSubChildren = true;

    /// <summary>
    /// Fungsi utama untuk mendistribusikan ulang Sorting Order ke semua child
    /// </summary>
    public void DistributeLayers()
    {
        // 1. Ambil semua komponen SpriteRenderer dari objek child
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeSubChildren);

        // Validasi jika tidak ada sprite di objek child
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"Tidak ditemukan SpriteRenderer pada child objek {gameObject.name}");
            return;
        }

        // Jika hanya ada 1 sprite, langsung beri nilai minOrder
        if (renderers.Length == 1)
        {
            renderers[0].sortingOrder = minOrder;
            if (!string.IsNullOrEmpty(sortingLayerName)) renderers[0].sortingLayerName = sortingLayerName;
            return;
        }

        // 2. Hitung jarak (step) antar order agar terbagi rata dalam range
        // Menggunakan float agar pembagian presisi sebelum di-convert ke int
        float step = (float)(maxOrder - minOrder) / (renderers.Length - 1);

        // 3. Terapkan nilai unik ke masing-masing SpriteRenderer
        for (int i = 0; i < renderers.Length; i++)
        {
            #if UNITY_EDITOR
            // Mencatat aksi ke Undo system Unity agar bisa di-Ctrl+Z jika salah
            Undo.RecordObject(renderers[i], "Distribute Sprite Layers");
            #endif

            // Set nama Sorting Layer jika diisi
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                renderers[i].sortingLayerName = sortingLayerName;
            }

            // Hitung order unik untuk index ini dan bulatkan ke integer terdekat
            int uniqueOrder = Mathf.RoundToInt(minOrder + (step * i));
            renderers[i].sortingOrder = uniqueOrder;
        }

        Debug.Log($"Sukses mengatur {renderers.Length} SpriteRenderer dengan rentang Order [{minOrder} sampai {maxOrder}].");
    }
}

// ============================================================================
// EDITOR CODE: Membuat tombol "Distribute Layers" muncul di Inspector Unity
// ============================================================================
#if UNITY_EDITOR
[CustomEditor(typeof(SpriteLayerDistributor))]
public class SpriteLayerDistributorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Gambar properti bawaan script terlebih dahulu
        DrawDefaultInspector();

        SpriteLayerDistributor script = (SpriteLayerDistributor)target;

        GUILayout.Space(15);
        
        // Membuat tombol kustom di bawah variabel inspector
        if (GUILayout.Button("Distribute Layers Now", GUILayout.Height(35)))
        {
            script.DistributeLayers();
        }
    }
}
#endif
