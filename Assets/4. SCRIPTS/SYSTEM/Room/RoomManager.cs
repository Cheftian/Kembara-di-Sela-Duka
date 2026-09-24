using UnityEngine;
using System.Collections;

public class RoomManager : MonoBehaviour
{
[System.Serializable]
    public struct RoomData
    {
        [Tooltip("Nama/Label ruangan untuk mempermudah identifikasi")]
        public string roomName;
        
        [Tooltip("GameObject aktif yang ada di hirarki Scene saat ini")]
        public GameObject roomObject;

        [Tooltip("SERET ASSET PREFAB ASLI DARI FOLDER PROJECT KE SINI (Untuk sistem Reset saat Player mati)")]
        public GameObject roomPrefab;
        
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
    
    [Tooltip("Waktu yang dibutuhkan animasi Fade In untuk menutup layar penuh")]
    public float transitionDelay = 0.35f;

    [Tooltip("Waktu tunggu layar tetap hitam pekat SETELAH ruangan berubah, sebelum Fade Out dimulai")]
    public float holdDelay = 0.5f;

    [Header("Glitch Exit Transition")]
    [SerializeField] private string glitchFlashInName = "FlashIn";
    [SerializeField] private string glitchFlashOutName = "FlashOut";
    [Tooltip("Jeda minimal saat menggunakan tombol S sebelum room tujuan diaktifkan")]
    [SerializeField] private float glitchRoomDelay = 5f;

    // BARU: Tempat menyimpan referensi portal terakhir tempat player masuk
    private RoomPortal lastEnteredPortal;

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

        // Daftarkan referensi prefab asli ke setiap room saat awal game dimulai
        if (allRooms != null)
        {
            foreach (RoomData data in allRooms)
            {
                if (data.roomObject != null && data.roomPrefab != null)
                {
                    RoomIdentity identity = data.roomObject.GetComponent(typeof(RoomIdentity)) as RoomIdentity;
                    if (identity == null)
                    {
                        // PERBAIKAN MUTLAK: Menggunakan typeof() untuk menghindari error AddComponent compiler
                        identity = data.roomObject.AddComponent(typeof(RoomIdentity)) as RoomIdentity;
                    }
                    // Menggunakan data.roomPrefab (Aset Project aman) bukan data.roomObject (Scene instance)
                    identity.originalPrefabReference = data.roomPrefab; 
                }
            }
        }
    }
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

    // MODIFIKASI: SwitchRoom diperbarui untuk mencatat portal tujuan sebagai tempat respawn lokal
    public void SwitchRoom(Transform player, RoomPortal currentPortal, RoomPortal destinationPortal)
    {
        lastEnteredPortal = destinationPortal; // Catat portal masuk terakhir
        StartCoroutine(ExecuteRoomSwitch(player, currentPortal, destinationPortal, false, false, false, false));
    }

    public void SwitchRoomFromGlitch(Transform player, RoomPortal currentPortal, RoomPortal destinationPortal)
    {
        lastEnteredPortal = destinationPortal; // Catat portal masuk terakhir
        StartCoroutine(ExecuteRoomSwitch(player, currentPortal, destinationPortal, true, true, false, true));
    }

    public void SwitchRoomWithFlash(Transform player, RoomPortal currentPortal, RoomPortal destinationPortal)
    {
        lastEnteredPortal = destinationPortal; // Catat portal masuk terakhir
        StartCoroutine(ExecuteRoomSwitch(player, currentPortal, destinationPortal, true, false, true, true));
    }

    // =================================================================================
    // FUNGSI BARU: MEMICU PROSES LOCAL RESPAWN & DUPLIKASI ULANG RUANGAN SEGAR
    // =================================================================================
    public void RespawnPlayerInRoom(Transform player)
    {
        if (lastEnteredPortal == null)
        {
            Debug.LogError("Player mati, tetapi data portal masuk terakhir tidak ditemukan!");
            return;
        }
        StartCoroutine(ExecuteRoomResetAndRespawn(player));
    }

private IEnumerator ExecuteRoomResetAndRespawn(Transform player)
{
    PlayerController playerCtrl = player.GetComponent(typeof(PlayerController)) as PlayerController;
    if (playerCtrl != null)
    {
        playerCtrl.BlockInput = true;
        playerCtrl.ResetToIdleState(); 
    }

    // 1. TRANSISI: Mulai memutar animasi layar menutup (menuju hitam)
    if (SceneController.Instance != null)
    {
        SceneController.Instance.PlayTransitionByName("FadeOut");
    }
    
    // 2. BERHENTI DI FRAME TERAKHIR: Tunggu hingga animasi menutup selesai sepenuhnya dan layar menjadi hitam pekat
    yield return new WaitForSeconds(transitionDelay*2);

    // 3. DELAY: Menahan layar tetap hitam pekat selama beberapa detik (Jeda mati sebelum dunia di-reset)
    // Kamu bisa menggunakan variabel 'holdDelay' bawaan RoomManager atau menggantinya dengan angka langsung (misal: 1.5f)
    yield return new WaitForSeconds(holdDelay);

    // 4. RESTART: Proses mengembalikan status objek ruangan dan memindahkan player ke posisi aman
    GameObject sceneRoom = lastEnteredPortal.currentRoomParent;
    RoomIdentity roomIdent = sceneRoom != null ? sceneRoom.GetComponent(typeof(RoomIdentity)) as RoomIdentity : null;

    if (sceneRoom != null && roomIdent != null && roomIdent.originalPrefabReference != null)
    {
        GameObject prefabMaster = roomIdent.originalPrefabReference;

        Transform[] sceneTransforms = sceneRoom.GetComponentsInChildren<Transform>(true);
        Transform[] prefabTransforms = prefabMaster.GetComponentsInChildren<Transform>(true);

        // Loop 1: Mengembalikan semua posisi, rotasi, skala, dan status aktif objek ke semula
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            if (i < prefabTransforms.Length && sceneTransforms[i].gameObject.name == prefabTransforms[i].gameObject.name)
            {
                sceneTransforms[i].localPosition = prefabTransforms[i].localPosition;
                sceneTransforms[i].localRotation = prefabTransforms[i].localRotation;
                sceneTransforms[i].localScale = prefabTransforms[i].localScale;

                sceneTransforms[i].gameObject.SetActive(prefabTransforms[i].gameObject.activeSelf);

                Rigidbody2D rb2d = sceneTransforms[i].GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.linearVelocity = Vector2.zero;
                    rb2d.angularVelocity = 0f;
                }
                
                ChaseObstacle obstacleScript = sceneTransforms[i].GetComponent<ChaseObstacle>();
                if (obstacleScript != null)
                {
                    obstacleScript.enabled = false;
                    obstacleScript.enabled = true;
                }

                ObstacleTrigger triggerScript = sceneTransforms[i].GetComponent<ObstacleTrigger>();
                if (triggerScript != null)
                {
                    triggerScript.enabled = false;
                    triggerScript.enabled = true;
                }
            }
        }

        // Pindahkan posisi Player ke koordinat portal masuk
        Vector3 spawnPos = lastEnteredPortal.transform.position;
        spawnPos.x += lastEnteredPortal.spawnOffsetX;
        player.position = spawnPos;

        if (playerCtrl != null)
        {
            playerCtrl.SetFacingDirection(lastEnteredPortal.faceDirectionOnSpawn == RoomPortal.FaceDirection.Right);
        }

        // Sinkronisasi engine physics setelah perpindahan instan
        yield return new WaitForFixedUpdate();

        // Loop 2: Mengaktifkan kembali sistem bahaya kabut
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            if (sceneTransforms[i] != null)
            {
                ObstacleDamage damageScript = sceneTransforms[i].GetComponent<ObstacleDamage>();
                if (damageScript != null)
                {
                    damageScript.enabled = false;
                    damageScript.enabled = true; 
                }
            }
        }
    }
    else
    {
        Debug.LogError("Gagal mereset status room! Pastikan 'Room Prefab' di Inspector RoomManager sudah diisi.");
    }

    // Berikan jeda super singkat (1 frame) setelah restart agar visual kamera stabil membidik posisi baru player sebelum layar dibuka
    yield return null;

    // 5. TRANSISI: Memutar animasi layar membuka kembali (dari hitam menuju normal) menggunakan SceneController
    if (SceneController.Instance != null)
    {
        SceneController.Instance.PlayTransitionByName("FadeIn");
    }

    if (playerCtrl != null)
    {
        // Karakter dipaksa memainkan animasi "Sit" dan kode ditahan sampai animasinya selesai penuh [9]
        yield return StartCoroutine(playerCtrl.PlayAnimationAndWait("Sit"));

        // Karakter terdiam sejenak dalam posisi duduk sesuai variabel sitDuration di skrip player [9]
        // Kamu bisa mengambil variabel sitDuration lewat class player (jika public/accessible) atau diisi angka manual (misal: 1.5f)
        yield return new WaitForSeconds(1.5f);

        // Karakter memainkan animasi "Stand" hingga berdiri kembali secara normal [9]
        yield return StartCoroutine(playerCtrl.PlayAnimationAndWait("Stand"));

        // Mengembalikan visual ke state default/Idle agar pergerakan berjalan lancar [9]
        playerCtrl.ResetToIdleState();

        // Buka kembali kunci kontrol input player
        playerCtrl.BlockInput = false;
    }
}

    private IEnumerator ExecuteRoomSwitch(Transform player, RoomPortal currentPortal, RoomPortal destinationPortal, bool useFlashTransition, bool isGlitchExit, bool delayBeforeFlash, bool standAfterFlash)
    {
        string transitionOut = useFlashTransition ? glitchFlashOutName : "Room_FadeIn";

        if (!useFlashTransition)
        {
            SceneController.Instance.PlayTransitionByName("Room_FadeOut");
            yield return new WaitForSeconds(transitionDelay);
        }
        else
        {
            if (delayBeforeFlash)
            {
                float minimumFlashRoomDelay = Mathf.Max(5f, glitchRoomDelay);
                yield return new WaitForSeconds(minimumFlashRoomDelay);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
                }

                PlayerController playerController = player.GetComponent(typeof(PlayerController)) as PlayerController;
                if (playerController != null)
                {
                    yield return StartCoroutine(playerController.PlayAnimationAndWait("Sit"));
                }
            }

            SceneController.Instance.PlayTransitionByName(glitchFlashInName);
            yield return new WaitForSeconds(transitionDelay);
        }

        if (currentPortal.currentRoomParent != null)
        {
            currentPortal.currentRoomParent.SetActive(false);
        }

        if (destinationPortal.currentRoomParent != null)
        {
            destinationPortal.currentRoomParent.SetActive(true);
        }

        Vector3 targetPosition = destinationPortal.transform.position;
        targetPosition.x += destinationPortal.spawnOffsetX;
        player.position = targetPosition;

        PlayerController playerCtrl = player.GetComponent(typeof(PlayerController)) as PlayerController;
        if (playerCtrl != null)
        {
            bool faceRight = destinationPortal.faceDirectionOnSpawn == RoomPortal.FaceDirection.Right;
            playerCtrl.SetFacingDirection(faceRight);
        }
        else
        {
            Debug.LogWarning("PlayerController tidak ditemukan pada objek Player saat pergantian ruangan!");
        }

        if (useFlashTransition)
        {
            SceneController.Instance.PlayTransitionByName(transitionOut);
            yield return new WaitForSeconds(transitionDelay);

            if (standAfterFlash && playerCtrl != null)
            {
                playerCtrl.CompleteGlitchExit();
            }
        }
        else
        {
            yield return new WaitForSeconds(holdDelay);
            SceneController.Instance.PlayTransitionByName(transitionOut);
        }

        currentPortal.ResetTeleportStatus();
        destinationPortal.ResetTeleportStatus();
    }
}