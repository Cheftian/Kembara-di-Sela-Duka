using UnityEngine;

public class JigsawMinigame : BaseMinigame
{
    [Header("Jigsaw Elements")]
    [SerializeField] private JigsawPiece[] puzzlePieces;

    [Header("Randomize Settings")]
    [SerializeField] private RectTransform spawnArea; // Panel/Area batas pengacakan piece

    [Header("Win State Objects Toggle")]
    [SerializeField] private GameObject[] objectsToDisableOnWin;
    [SerializeField] private GameObject[] objectsToEnableOnWin;

    // Membuka akses variabel dari BaseMinigame agar bisa dibaca oleh JigsawPiece
    public bool CanPlay => canPlayPuzzle;

    // Mengubah ke Awake agar posisi diacak SEBELUM script JigsawPiece membaca posisi awalnya
    protected override void Awake()
    {
        base.Awake();
        RandomizePieces();
    }

    protected override void Update()
    {
        // Tetap memanggil base.Update() untuk fungsi tombol ESC
        base.Update();
    }

    private void RandomizePieces()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Spawn Area belum dimasukkan di Inspector! Pieces tidak bisa diacak.");
            return;
        }

        // Mendapatkan batas ukuran dari spawnArea berdasarkan pivot tengah (0.5, 0.5)
        Vector2 size = spawnArea.rect.size;
        float minX = -size.x / 2f;
        float maxX = size.x / 2f;
        float minY = -size.y / 2f;
        float maxY = size.y / 2f;

        foreach (JigsawPiece piece in puzzlePieces)
        {
            if (piece == null) continue;

            // Mengacak koordinat X dan Y di dalam area
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);

            RectTransform pieceRect = piece.GetComponent<RectTransform>();
            Vector2 newRandomPos = new Vector2(randomX, randomY);

            // Terapkan posisi acak ke objek
            pieceRect.anchoredPosition = newRandomPos;
            
            // PAKSA potongan puzzle untuk memperbarui data posisi awal mereka saat ini juga
            piece.SetInitialPosition(newRandomPos);
        }
    }

   public void CheckWinCondition()
    {
        if (isSolved) return;

        bool allLocked = true;
        foreach (JigsawPiece piece in puzzlePieces)
        {
            if (!piece.IsLocked)
            {
                allLocked = false;
                break;
            }
        }

        if (allLocked)
        {
            HandleWinObjectsToggle();
            WinMinigame();
        }
    }

    private void HandleWinObjectsToggle()
    {
        foreach (GameObject obj in objectsToDisableOnWin)
        {
            if (obj != null) obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToEnableOnWin)
        {
            if (obj != null) obj.SetActive(true);
        }
    }
}
