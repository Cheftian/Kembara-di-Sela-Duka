using UnityEngine;
using UnityEngine.UI;

public class ParallaxAutoScroll : MonoBehaviour
{
    public enum ScrollDirection { Left, Right }

    [Header("Movement Settings")]
    [Tooltip("Pilih arah pergerakan background.")]
    public ScrollDirection direction = ScrollDirection.Left;

    [Tooltip("Kecepatan gerak horizontal background.")]
    public float scrollSpeed = 2f;

    [Tooltip("Lebar gambar. Jika diisi 0, script akan mendeteksi ukurannya secara otomatis.")]
    public float imageWidth;

    // Referensi komponen untuk UI
    private RectTransform rectTransform;
    private bool isUI = false;

    // Variabel posisi awal
    private Vector2 startAnchoredPosition; // Untuk UI
    private Vector3 startWorldPosition;    // Untuk Sprite 2D

    void Start()
    {
        // 1. Cek apakah objek ini adalah komponen UI Canvas
        rectTransform = GetComponent<RectTransform>();
        CanvasRenderer canvasRenderer = GetComponent<CanvasRenderer>();

        if (rectTransform != null && canvasRenderer != null)
        {
            isUI = true;
            startAnchoredPosition = rectTransform.anchoredPosition;
            
            // Ambil lebar otomatis dari RectTransform jika nilainya 0
            if (imageWidth == 0)
            {
                imageWidth = rectTransform.rect.width;
            }
        }
        else
        {
            // 2. Jika bukan UI, maka treated sebagai Sprite Renderer biasa
            isUI = false;
            startWorldPosition = transform.position;

            // Ambil lebar otomatis dari SpriteRenderer jika nilainya 0
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null && imageWidth == 0)
            {
                imageWidth = sprite.bounds.size.x;
            }
        }

        if (imageWidth == 0)
        {
            Debug.LogWarning($"Lebar gambar di {gameObject.name} terdeteksi 0! Silakan isi manual di Inspector jika gambar tidak bergerak dengan benar.");
        }
    }

    void Update()
    {
        // Menentukan arah gerak (-1 untuk kiri, 1 untuk kanan)
        float moveSign = (direction == ScrollDirection.Left) ? -1f : 1f;
        float movement = moveSign * scrollSpeed * Time.deltaTime;

        if (isUI)
        {
            // LOGIKA UNTUK UI CANVAS
            rectTransform.anchoredPosition += new Vector2(movement, 0);

            if (direction == ScrollDirection.Left)
            {
                if (rectTransform.anchoredPosition.x <= startAnchoredPosition.x - imageWidth)
                {
                    rectTransform.anchoredPosition += new Vector2(imageWidth, 0);
                }
            }
            else
            {
                if (rectTransform.anchoredPosition.x >= startAnchoredPosition.x + imageWidth)
                {
                    rectTransform.anchoredPosition -= new Vector2(imageWidth, 0);
                }
            }
        }
        else
        {
            // LOGIKA UNTUK SPRITE RENDERER (WORLD SPACE)
            transform.Translate(new Vector3(movement, 0, 0));

            if (direction == ScrollDirection.Left)
            {
                if (transform.position.x <= startWorldPosition.x - imageWidth)
                {
                    transform.Translate(Vector3.right * imageWidth);
                }
            }
            else
            {
                if (transform.position.x >= startWorldPosition.x + imageWidth)
                {
                    transform.Translate(Vector3.left * imageWidth);
                }
            }
        }
    }
}
