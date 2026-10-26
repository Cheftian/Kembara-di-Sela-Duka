using UnityEngine;
using UnityEngine.EventSystems;

public class RotatableUI : MonoBehaviour, IDragHandler
{
    [Header("Rotation Target")]
    public float targetAngle = 90f;
    public float tolerance = 15f;
    
    [Header("Sprite Offset")]
    [Tooltip("Penyesuaian sudut jika arah atas sprite tidak mengarah ke Y positif (biasanya -90)")]
    [SerializeField] private float spriteAngleOffset = -90f;

    private RectTransform rectTransform;
    private float initialAngle;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialAngle = rectTransform.localEulerAngles.z;
    }

    private void OnEnable()
    {
        // Reset posisi rotasi setiap kali minigame dibuka
        rectTransform.localEulerAngles = new Vector3(0, 0, initialAngle);
    }

    public void OnDrag(PointerEventData eventData)
    {
        BaseMinigame parentMinigame = GetComponentInParent<BaseMinigame>();
        
        // Hanya izinkan rotasi jika canPlayPuzzle bernilai true
        // (Kita perlu membuat properti publik atau mengubah canPlayPuzzle menjadi protected di Base)
        if (parentMinigame != null && !parentMinigame.GetType().GetField("canPlayPuzzle", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(parentMinigame).Equals(true))
        {
            return;
        }
        // Kalkulasi sudut berdasarkan posisi kursor dan titik pusat objek
        Vector2 direction = eventData.position - (Vector2)rectTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        rectTransform.rotation = Quaternion.Euler(0, 0, angle + spriteAngleOffset);
    }

    public bool IsCorrect()
    {
        float currentZ = rectTransform.localEulerAngles.z;
        
        // Menghitung selisih sudut absolut (0-360)
        float difference = Mathf.DeltaAngle(currentZ, targetAngle);
        return Mathf.Abs(difference) <= tolerance;
    }
}