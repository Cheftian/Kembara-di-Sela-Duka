using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerControllerTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Jika dicentang, trigger ini hanya akan aktif satu kali saja.")]
    [SerializeField] private bool isOneTimeUse = true;

    [Header("Player Movement Modification")]
    [Tooltip("Apakah ingin mengubah status canJump pemain?")]
    [SerializeField] private bool changeCanJumpStatus = true;
    [Tooltip("Status canJump baru untuk pemain ketika menyentuh trigger ini.")]
    [SerializeField] private bool targetCanJump = false;

    [Space(5)]
    [Tooltip("Apakah ingin mengubah status canRun pemain?")]
    [SerializeField] private bool changeCanRunStatus = true;
    [Tooltip("Status canRun baru untuk pemain ketika menyentuh trigger ini.")]
    [SerializeField] private bool targetCanRun = false;

    [Space(5)]
    [Tooltip("Apakah ingin mengubah status BlockInput pemain?")]
    [SerializeField] private bool changeBlockInputStatus = false;
    [Tooltip("Status BlockInput baru untuk pemain ketika menyentuh trigger ini.")]
    [SerializeField] private bool targetBlockInput = false;

    [Header("Speed Modification")]
    [Tooltip("Apakah ingin mengubah kecepatan gerak (moveSpeed) dasar pemain?")]
    [SerializeField] private bool changeMoveSpeed = false;
    [Tooltip("Nilai kecepatan baru (mengubah originalMoveSpeed pada PlayerController).")]
    [SerializeField] private float newMoveSpeed = 5f;

    private bool hasBeenTriggered = false;

    private void Awake()
    {
        // Memastikan Collider2D pada objek ini diatur sebagai Trigger secara otomatis
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Validasi apakah trigger sudah terpakai
        if (isOneTimeUse && hasBeenTriggered) return;

        // Mencari komponen PlayerController pada objek yang masuk ke trigger
        PlayerController player = other.GetComponent<PlayerController>();

        // Jika tidak ditemukan di objek utama, cari di parent-nya (antisipasi jika collider ada di child)
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        // Jika objek yang menyentuh benar-benar Player
        if (player != null)
        {
            ApplyChanges(player);
        }
    }

    private void ApplyChanges(PlayerController player)
    {
        hasBeenTriggered = true;

        // 1. Mengatur flag canJump (menggunakan refleksi atau modifikasi langsung jika variabelnya public)
        // Catatan: Di skrip PlayerController Anda, canJump & canRun adalah [SerializeField] private.
        // Namun, jika Anda berniat membuatnya menjadi public atau menggunakan properti, 
        // pastikan Anda mengubah kata 'private' menjadi 'public' pada skrip PlayerController Anda 
        // untuk baris: public bool canJump dan public bool canRun.

        if (changeCanJumpStatus)
        {
            // player.canJump = targetCanJump; // Aktifkan ini setelah canJump di PlayerController diubah ke public
            SetPrivateField(player, "canJump", targetCanJump); // Mengubah via refleksi jika masih private
        }

        if (changeCanRunStatus)
        {
            // player.canRun = targetCanRun; // Aktifkan ini setelah canRun di PlayerController diubah ke public
            SetPrivateField(player, "canRun", targetCanRun); // Mengubah via refleksi jika masih private
        }

        // 2. Mengatur properti BlockInput (Sudah berupa public property berkemampuan { get; set; })
        if (changeBlockInputStatus)
        {
            player.BlockInput = targetBlockInput;
        }

        // 3. Mengatur Kecepatan Gerak melalui fungsi internal PlayerController yang sudah ada
        if (changeMoveSpeed)
        {
            player.UpdateMoveSpeed(newMoveSpeed);
        }

        Debug.Log($"[Trigger] PlayerController berhasil dimodifikasi oleh {gameObject.name}. canJump: {targetCanJump}, canRun: {targetCanRun}");

        // Jika sekali pakai, kita bisa menonaktifkan game object atau menghancurkan skrip ini
        if (isOneTimeUse)
        {
            // Pilihan 1: Hancurkan komponen triggernya saja agar hemat memori (Saran)
            Destroy(this); 
            
            // Pilihan 2: Jika ingin menghilangkan seluruh GameObject trigger dari map, ganti dengan:
            // Destroy(gameObject);
        }
    }

    /// <summary>
    /// Helper fungsi Refleksi untuk mengubah variabel private ber-attribute [SerializeField] 
    /// tanpa perlu memaksa Anda mengubah arsitektur kode PlayerController asli.
    /// </summary>
    private void SetPrivateField(PlayerController target, string fieldName, bool value)
    {
        System.Reflection.FieldInfo field = typeof(PlayerController).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"Variabel bernama '{fieldName}' tidak ditemukan di PlayerController.");
        }
    }
}
