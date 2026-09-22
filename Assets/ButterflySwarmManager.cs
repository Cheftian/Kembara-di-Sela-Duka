using UnityEngine;

public class ButterflySwarmManager : MonoBehaviour
{
    [Header("Activation Control")]
    [Tooltip("Tombol keyboard untuk memicu kupu-kupu terbang.")]
    public KeyCode interactionKey = KeyCode.S;
    [Tooltip("Tag dari GameObject Player untuk validasi tabrakan.")]
    public string playerTag = "Player";

    [Header("Scatter Setup")]
    [Tooltip("Radius acak posisi awal kupu-kupu saat menyemprot agar tidak menumpuk di satu titik.")]
    public float spawnRandomRadius = 0.5f;

    private ScatterButterfly[] butterflies;
    private bool hasTriggered = false;
    private bool isPlayerInside = false;

    void Start()
    {
        // Mengambil semua script kupu-kupu yang menjadi child dari object ini
        butterflies = GetComponentsInChildren<ScatterButterfly>(true);
    }

    void Update()
    {
        // Tombol S hanya bekerja jika belum pernah dipicu DAN player sedang berada di dalam collider
        if (!hasTriggered && isPlayerInside && Input.GetKeyDown(interactionKey))
        {
            TriggerSwarmScatter();
        }
    }

    void TriggerSwarmScatter()
    {
        hasTriggered = true;

        foreach (ScatterButterfly butterfly in butterflies)
        {
            if (butterfly != null)
            {
                // Mengacak sedikit posisi awal lokal child agar terlihat menyebar natural
                Vector2 randomOffset = Random.insideUnitCircle * spawnRandomRadius;
                butterfly.transform.localPosition += new Vector3(randomOffset.x, randomOffset.y, 0f);

                // Perintahkan kupu-kupu untuk mulai terbang menyemprot
                butterfly.StartScatterFlight();
            }
        }
    }

    // Mendeteksi saat Player masuk ke dalam area radius kupu-kupu
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            isPlayerInside = true;
        }
    }

    // Mendeteksi saat Player keluar dari area radius kupu-kupu
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            isPlayerInside = false;
        }
    }
}
