using UnityEngine;
using System.Collections;

public class GhostTrail : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Transform visualTransform;

    [Header("Ghost Trail Settings")]
    [Tooltip("Kecepatan minimal karakter agar efek ghost trail mulai muncul")]
    [SerializeField] private float minSpeedToActivate = 8.1f;
    [Tooltip("Jeda waktu (detik) antar kemunculan bayangan")]
    [SerializeField] private float spawnDelay = 0.1f;
    [Tooltip("Durasi berapa lama bayangan bertahan sebelum menghilang total")]
    [SerializeField] private float ghostDuration = 0.5f;

    [Header("Visual Settings")]
    [ColorUsage(true, true)] // Mendukung warna HDR jika Anda menggunakan Post-Processing Bloom
    [SerializeField] private Color ghostColor = new Color(0f, 0.75f, 1f, 0.6f); // Biru transparan default
    [SerializeField] private Material ghostMaterial; // Opsional: Bisa diisi material khusus Sprite/Lit jika warna tidak muncul

    private Rigidbody2D rb;
    private float lastSpawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-detect komponen jika lupa di-drag di Inspector
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (rb == null || playerSpriteRenderer == null || visualTransform == null) return;

        // Hitung kecepatan horizontal mutlak saat ini
        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);

        // Efek aktif HANYA jika kecepatan melebihi batas MINIMAL dan tidak dalam kondisi pusing (Dizzy)
        if (currentSpeed >= minSpeedToActivate && !playerController.IsDizzy)
        {
            if (Time.time - lastSpawnTime >= spawnDelay)
            {
                SpawnGhost();
                lastSpawnTime = Time.time;
            }
        }
    }
    private void SpawnGhost()
    {
        // 1. Buat GameObject kosong baru untuk menampung bayangan
        GameObject ghostObj = new GameObject("GhostTrail_Instance");
        
        // Samakan posisi dan rotasi dengan objek visual agar posisi offset animasi tetap pas
        ghostObj.transform.position = visualTransform.position;
        ghostObj.transform.rotation = visualTransform.rotation;

        // 2. KOREKSI SKALA: Gunakan lossyScale objek induk (mengabaikan skala lokal objek anak)
        Vector3 parentGlobalScale = transform.lossyScale;

        // Periksa arah hadap dari PlayerController untuk menentukan arah sumbu X bayangan
        if (playerController != null)
        {
            // Jika menghadap kiri, paksa sumbu X skala global menjadi negatif
            parentGlobalScale.x = playerController.IsWalking && !Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.A) 
                ? -Mathf.Abs(parentGlobalScale.x) 
                : (playerController.IsWalking && Input.GetKey(KeyCode.D) ? Mathf.Abs(parentGlobalScale.x) : (visualTransform.localScale.x < 0 ? -Mathf.Abs(parentGlobalScale.x) : Mathf.Abs(parentGlobalScale.x)));
            
            // Cara paling aman menggunakan properti arah internal PlayerController yang sudah kita buat:
            // Kita bisa mengakses variabel isFacingRight (jika publik) atau mendeteksinya dari arah localScale anak saat ini
            parentGlobalScale.x = visualTransform.localScale.x < 0 ? -Mathf.Abs(parentGlobalScale.x) : Mathf.Abs(parentGlobalScale.x);
        }

        ghostObj.transform.localScale = parentGlobalScale;

        // 3. Tambahkan komponen SpriteRenderer pada objek bayangan
        SpriteRenderer ghostSprite = ghostObj.AddComponent<SpriteRenderer>();
        
        // Copy sprite yang sedang aktif digunakan oleh player di frame ini
        ghostSprite.sprite = playerSpriteRenderer.sprite;
        ghostSprite.sortingLayerID = playerSpriteRenderer.sortingLayerID;
        ghostSprite.sortingOrder = playerSpriteRenderer.sortingOrder - 1; // Tepat di belakang player

        if (ghostMaterial != null) ghostSprite.material = ghostMaterial;
        else ghostSprite.material = playerSpriteRenderer.material;

        // Matikan flipX agar tidak merusak kalkulasi skala global yang sudah kita balik di atas
        ghostSprite.flipX = false;
        ghostSprite.color = ghostColor;

        // 4. Jalankan Coroutine untuk memudarkan dan menghancurkan objek bayangan
        StartCoroutine(FadeAndDestroyGhost(ghostObj, ghostSprite));
    }


    private IEnumerator FadeAndDestroyGhost(GameObject ghostObj, SpriteRenderer ghostSprite)
    {
        float elapsedTime = 0f;
        Color startColor = ghostColor;

        while (elapsedTime < ghostDuration)
        {
            if (ghostSprite == null) yield break;

            elapsedTime += Time.deltaTime;
            float lerpValue = elapsedTime / ghostDuration;

            // Kurangi nilai Alpha (transparansi) secara perlahan menuju 0
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, 0f, lerpValue);
            ghostSprite.color = newColor;

            yield return null;
        }

        // Hancurkan objek dari memori setelah benar-benar pudar
        Destroy(ghostObj);
    }
}
