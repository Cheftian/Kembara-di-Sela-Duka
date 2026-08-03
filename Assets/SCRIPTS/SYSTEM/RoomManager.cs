using UnityEngine;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    // Struktur data khusus untuk memberikan flag pada setiap ruangan di Inspector
    [System.Serializable]
    public struct RoomData
    {
        [Tooltip("Nama/Label ruangan untuk mempermudah identifikasi")]
        public string roomName;
        
        [Tooltip("GameObject utama dari ruangan ini")]
        public GameObject roomObject;
        
        [Tooltip("Centang jika ruangan ini adalah tempat game dimulai")]
        public bool isStartingRoom;
    }

    public static RoomManager Instance;

    [Header("Pengaturan Ruangan")]
    [Tooltip("Daftar seluruh ruangan di scene beserta flag statusnya")]
    [SerializeField] private RoomData[] allRooms;

    [Header("Pengaturan Transisi UI")]
    [Tooltip("Masukkan komponen Animator dari TransitionPanel di sini")]
    public Animator transitionAnimator;
    
    [Tooltip("Waktu yang dibutuhkan animasi Fade In untuk menutup layar penuh (jangan diubah saat animasi berjalan)")]
    public float transitionDelay = 0.35f;

    [Tooltip("Waktu tunggu layar tetap hitam pekat SETELAH ruangan berubah, sebelum Fade Out dimulai")]
    public float holdDelay = 0.5f;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Jalankan inisialisasi ruangan saat game dimulai berdasarkan flag
        InitializeRooms();
    }

    /// <summary>
    /// Memeriksa flag isStartingRoom untuk mengaktifkan ruangan awal dan mematikan sisanya
    /// </summary>
    private void InitializeRooms()
    {
        if (allRooms == null || allRooms.Length == 0)
        {
            Debug.LogWarning("Daftar allRooms masih kosong! Harap masukkan data ruangan di Inspector.");
            return;
        }

        int startingRoomCount = 0;

        foreach (RoomData data in allRooms)
        {
            if (data.roomObject != null)
            {
                // Aktifkan jika mencentang isStartingRoom, matikan jika tidak
                data.roomObject.SetActive(data.isStartingRoom);

                if (data.isStartingRoom)
                {
                    startingRoomCount++;
                }
            }
        }

        // Validasi pengingat di Console jika Anda lupa mencentang atau mencentang lebih dari satu
        if (startingRoomCount == 0)
        {
            Debug.LogError("Waduh! Tidak ada ruangan yang dicentang sebagai 'Is Starting Room' di Inspector.");
        }
        else if (startingRoomCount > 1)
        {
            Debug.LogWarning("Peringatan: Ada lebih dari 1 ruangan yang dicentang sebagai 'Is Starting Room'. Keduanya akan aktif bersamaan.");
        }
    }

    public void SwitchRoom(Transform player, RoomPortal currentPortal, RoomPortal destinationPortal)
    {
        StartCoroutine(ExecuteRoomSwitch(player, currentPortal, destinationPortal));
    }

    private IEnumerator ExecuteRoomSwitch(Transform player, RoomPortal currentPortal, RoomPortal destinationPortal)
    {
        // ------------------------------------------------------------
        // TAHAP 1: MEMULAI TRANSISI (Layar mulai menutup)
        // ------------------------------------------------------------
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        yield return new WaitForSeconds(transitionDelay);

        // ------------------------------------------------------------
        // TAHAP 2: LAYAR SUDAH TERTUTUP TOTAL (Proses Ubah Ruangan)
        // ------------------------------------------------------------
        if (currentPortal.currentRoomParent != null)
        {
            currentPortal.currentRoomParent.SetActive(false);
        }

        if (destinationPortal.currentRoomParent != null)
        {
            destinationPortal.currentRoomParent.SetActive(true);
        }

        // Pindahkan posisi Player ke koordinat tujuan
        Vector3 targetPosition = destinationPortal.transform.position;
        targetPosition.x += destinationPortal.spawnOffsetX;
        player.position = targetPosition;
        
        // ------------------------------------------------------------
        // TAHAP 3: JEDA TRANSISI (Layar menahan warna hitam)
        // ------------------------------------------------------------
        yield return new WaitForSeconds(holdDelay);

        // ------------------------------------------------------------
        // TAHAP 4: MEMBUKA LAYAR (Fade Out)
        // ------------------------------------------------------------
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("EndTransition");
        }
    }
}
