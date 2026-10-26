using UnityEngine;
using System.Linq; // Dibutuhkan untuk pengecekan array dengan mudah

public class RandomChildScaler : MonoBehaviour
{
    [Header("Scale Settings")]
    [Tooltip("Batas minimum ukuran scale")]
    public float minScale = 0.8f;
    
    [Tooltip("Batas maksimum ukuran scale")]
    public float maxScale = 1.5f;

    [Tooltip("Gunakan scale yang sama untuk X, Y, dan Z agar proporsional")]
    public bool uniformScale = true;

    [Header("Exclusions")]
    [Tooltip("Masukkan child yang TIDAK ingin diubah ukurannya di sini")]
    public Transform[] excludedChildren;

    void Start()
    {
        RandomizeChildScales();
    }

    [ContextMenu("Randomize Scales Now")]
    public void RandomizeChildScales()
    {
        // Melooping setiap child langsung di bawah object ini
        foreach (Transform child in transform)
        {
            // Pengecekan: Jika child ada di dalam array pengecualian, lewati (skip)
            if (excludedChildren != null && excludedChildren.Contains(child))
            {
                continue; 
            }

            if (uniformScale)
            {
                // Mengacak satu nilai untuk semua sumbu (proporsional)
                float randomScale = Random.Range(minScale, maxScale);
                child.localScale = new Vector3(randomScale, randomScale, randomScale);
            }
            else
            {
                // Mengacak nilai yang berbeda untuk tiap sumbu X, Y, dan Z
                float randomX = Random.Range(minScale, maxScale);
                float randomY = Random.Range(minScale, maxScale);
                float randomZ = Random.Range(minScale, maxScale);
                child.localScale = new Vector3(randomX, randomY, randomZ);
            }
        }
    }
}
