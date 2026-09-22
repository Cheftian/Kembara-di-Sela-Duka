using UnityEngine;

public class RevealerTool : MonoBehaviour
{
    public enum MaskType { ErasePermanently, RevealOnlyWhileInside }
    
    [Header("Reveal Settings")]
    public MaskType maskMechanic = MaskType.RevealOnlyWhileInside;
    
    [Range(0.1f, 10f)]
    public float fadeSpeed = 2.0f; 

    [Header("Advanced Mechanic Timings")]
    [Tooltip("Berapa cepat bayangan hitam menutup kembali (Untuk mode Reveal Only While Inside).")]
    public float shadowRegrowthSpeed = 0.5f;
    [Tooltip("Berapa cepat jejak lubang menghapus melebar sendiri (Untuk mode Erase Permanently).")]
    public float trailExpansionSpeed = 2.0f;
    [Tooltip("Berapa lama (dalam detik) jejak boleh melebar sejak pertama kali digores sebelum berhenti.")]
    public float expansionDuration = 3.0f;

    private Collider2D myCollider;
    private Vector3 lastPosition;
    private bool isMoving = false;

    void Start()
    {
        myCollider = GetComponent<Collider2D>();
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Cek apakah objek bergerak dengan membandingkan posisi saat ini dan sebelumnya
        // Menggunakan ambang batas kecil (0.001f) untuk menghindari bug micro-movement fisik
        if (Vector3.Distance(transform.position, lastPosition) > 0.001f)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }

        lastPosition = transform.position;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // JIKA OBJEK DIAM, JANGAN LAKUKAN REVEAL
        if (!isMoving) return;

        DynamicMaskController mask = other.GetComponent<DynamicMaskController>();
        
        if (mask != null && myCollider != null)
        {
            mask.ApplyRevealFromCollider(myCollider, maskMechanic, fadeSpeed, shadowRegrowthSpeed, trailExpansionSpeed, expansionDuration);
        }
    }
}
