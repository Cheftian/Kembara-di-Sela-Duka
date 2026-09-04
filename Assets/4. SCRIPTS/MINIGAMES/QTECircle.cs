using UnityEngine;
using UnityEngine.EventSystems;

public class QTECircle : MonoBehaviour, IPointerDownHandler
{
    [Header("Visual References")]
    [Tooltip("Lingkaran luar yang akan menyusut")]
    [SerializeField] private RectTransform approachRing;
    
    [Header("QTE Settings")]
    [Tooltip("Waktu yang dibutuhkan cincin untuk menyusut sempurna (detik)")]
    public float approachTime = 1.5f;
    [Tooltip("Toleransi waktu untuk dianggap tepat sasaran (detik)")]
    public float hitTolerance = 0.2f;

    private float timer = 0f;
    private bool isActive = false;
    private QTEMinigame manager;

    // Dipanggil oleh QTEMinigame saat lingkaran di-spawn
    public void Setup(QTEMinigame qteManager)
    {
        manager = qteManager;
        timer = 0f;
        isActive = true;
        
        // Memulai ukuran lingkaran luar menjadi 3x lipat lebih besar
        approachRing.localScale = Vector3.one * 3f;
    }

    private void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        float progress = timer / approachTime;

        // Menyusutkan ukuran dari 3x menjadi 1x (seukuran lingkaran dalam)
        float currentScale = Mathf.Lerp(3f, 1f, progress);
        approachRing.localScale = Vector3.one * currentScale;

        // Jika waktu habis dan tidak ditekan, maka dianggap Meleset (Miss)
        if (progress > 1f + hitTolerance)
        {
            Miss();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;

        // Mengecek seberapa dekat waktu saat ini dengan target waktu (approachTime)
        float progress = timer / approachTime;
        float difference = Mathf.Abs(1f - progress);

        if (difference <= hitTolerance)
        {
            Hit(); // Tepat sasaran
        }
        else
        {
            Miss(); // Terlalu cepat menekan
        }
    }

    private void Hit()
    {
        isActive = false;
        manager.ReportHit(true);
        Destroy(gameObject); // Hapus lingkaran dari layar
    }

    private void Miss()
    {
        isActive = false;
        manager.ReportHit(false);
        Destroy(gameObject); // Hapus lingkaran dari layar
    }
}