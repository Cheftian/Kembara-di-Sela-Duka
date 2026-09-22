using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class DynamicPolygonCollider : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polygonCollider;
    private Sprite lastSprite;

    // Cache untuk menyimpan bentuk collider dari sprite yang sudah pernah diproses
    // Ini penting agar game tidak lag saat animasi berulang
    private Dictionary<Sprite, List<Vector2[]>> colliderCache = new Dictionary<Sprite, List<Vector2[]>>();

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        
        UpdateColliderShape();
    }

    void LateUpdate()
    {
        // Jalankan pembaruan hanya jika sprite berubah di frame ini (karena animasi)
        if (spriteRenderer.sprite != lastSprite)
        {
            UpdateColliderShape();
        }
    }

    private void UpdateColliderShape()
    {
        Sprite currentSprite = spriteRenderer.sprite;
        
        if (currentSprite == null) return;

        lastSprite = currentSprite;

        // Cek apakah sprite ini sudah pernah dibuatkan bentuk collider-nya sebelumnya
        if (colliderCache.ContainsKey(currentSprite))
        {
            List<Vector2[]> paths = colliderCache[currentSprite];
            polygonCollider.pathCount = paths.Count;
            for (int i = 0; i < paths.Count; i++)
            {
                polygonCollider.SetPath(i, paths[i]);
            }
            return;
        }

        // Jika belum ada di cache, generate bentuk baru berdasarkan data fisik sprite
        List<Vector2[]> newPaths = new List<Vector2[]>();
        
        // Loop untuk mengambil semua jalur/outline dari sprite (mendukung sprite bolong/terpisah)
        for (int i = 0; i < currentSprite.GetPhysicsShapeCount(); i++)
        {
            List<Vector2> pathVertices = new List<Vector2>();
            currentSprite.GetPhysicsShape(i, pathVertices);
            newPaths.Add(pathVertices.ToArray());
        }

        // Terapkan jalur baru ke Polygon Collider 2D
        polygonCollider.pathCount = newPaths.Count;
        for (int i = 0; i < newPaths.Count; i++)
        {
            polygonCollider.SetPath(i, newPaths[i]);
        }

        // Simpan ke dalam cache memori agar frame berikutnya jauh lebih ringan
        colliderCache.Add(currentSprite, newPaths);
    }
    
    // Opsional: Membersihkan cache jika berpindah level atau memuat karakter baru
    public void ClearColliderCache()
    {
        colliderCache.Clear();
    }
}
