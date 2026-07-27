using UnityEngine;

public class ParallaxAutoScroll : MonoBehaviour
{
    // Membuat pilihan arah di Inspector Unity
    public enum ScrollDirection { Left, Right }

    [Header("Movement Settings")]
    [Tooltip("Choose the scroll direction of the background.")]
    public ScrollDirection direction = ScrollDirection.Left;

    [Tooltip("Horizontal movement speed of the background layer.")]
    public float scrollSpeed = 2f;

    [Tooltip("The total width of a single background image.")]
    public float imageWidth;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;

        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && imageWidth == 0)
        {
            imageWidth = sprite.bounds.size.x;
        }
    }

    void Update()
    {
        // Menentukan vektor arah berdasarkan pilihan user
        Vector3 moveDirection = (direction == ScrollDirection.Left) ? Vector3.left : Vector3.right;

        // Menggerakkan background sesuai arah yang dipilih
        transform.Translate(moveDirection * scrollSpeed * Time.deltaTime);

        // Logika Reset Posisi berdasarkan arah gerak
        if (direction == ScrollDirection.Left)
        {
            // Jika bergerak ke kiri, cek apakah sudah melewati batas kiri
            if (transform.position.x <= startPosition.x - imageWidth)
            {
                transform.Translate(Vector3.right * imageWidth);
            }
        }
        else
        {
            // Jika bergerak ke kanan, cek apakah sudah melewati batas kanan
            if (transform.position.x >= startPosition.x + imageWidth)
            {
                transform.Translate(Vector3.left * imageWidth);
            }
        }
    }
}
