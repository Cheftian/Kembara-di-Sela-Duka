using UnityEngine;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    [Header("Pengaturan Awal")]
    [Tooltip("Parent GameObject ruangan yang aktif saat game pertama kali dimulai")]
    public GameObject startingRoom;

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
        if (startingRoom != null)
        {
            startingRoom.SetActive(true);
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

        // Kunci proses di sini. Selama menunggu detik ini, 
        // animasi Fade In sedang berjalan dan RUANGAN BELUM BERUBAH.
        yield return new WaitForSeconds(transitionDelay);

        // ------------------------------------------------------------
        // TAHAP 2: LAYAR SUDAH TERTUTUP TOTAL (Proses Ubah Ruangan)
        // ------------------------------------------------------------
        // Matikan ruangan lama saat layar sudah hitam pekat
        if (currentPortal.currentRoomParent != null)
        {
            currentPortal.currentRoomParent.SetActive(false);
        }

        // Aktifkan ruangan baru
        if (destinationPortal.currentRoomParent != null)
        {
            destinationPortal.currentRoomParent.SetActive(true);
        }

        // Pindahkan posisi Player ke koordinat tujuan
        player.position = destinationPortal.transform.position;

        // ------------------------------------------------------------
        // TAHAP 3: JEDA TRANSISI (Layar menahan warna hitam)
        // ------------------------------------------------------------
        // Berikan waktu jeda diam dalam kondisi ruangan sudah berubah & player sudah di tempat baru
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
