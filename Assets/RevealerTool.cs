using UnityEngine;

public class RevealerTool : MonoBehaviour
{
    public enum MaskType { ErasePermanently, RevealOnlyWhileInside }
    
    [Header("Reveal Settings")]
    public MaskType maskMechanic = MaskType.RevealOnlyWhileInside;
    
    [Range(0.1f, 10f)]
    public float fadeSpeed = 2.0f; 

    [Header("Advanced Mechanic Timings")]
    public float shadowRegrowthSpeed = 0.5f;
    public float trailExpansionSpeed = 2.0f;
    public float expansionDuration = 3.0f;

    [Header("Camera Shake Settings")]
    [Tooltip("Kekuatan guncangan kamera saat sedang mengikis lapisan gelap.")]
    [Range(0.01f, 0.5f)]
    public float shakeMagnitude = 0.03f;


    private Collider2D myCollider;
    private Vector3 lastPosition;
    private bool isMoving = false;

    void Start()
    {
        myCollider = GetComponent<Collider2D>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Deteksi pergerakan di Update agar lebih presisi menangkap input pergeseran posisi
        if (transform.position != lastPosition)
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
        if (!isMoving) return;

        DynamicMaskController mask = other.GetComponent<DynamicMaskController>();
        
        if (mask != null && myCollider != null)
        {
            mask.ApplyRevealFromCollider(myCollider, maskMechanic, fadeSpeed, shadowRegrowthSpeed, trailExpansionSpeed, expansionDuration);
            
            // --- SEKARANG MEMANGGIL INSTANCE CAMERA CONTROLLER ---
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.05f, shakeMagnitude);
            }
        }
    }


}
