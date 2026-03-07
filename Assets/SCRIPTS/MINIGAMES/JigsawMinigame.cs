using UnityEngine;

public class JigsawMinigame : BaseMinigame
{
    [Header("Jigsaw Elements")]
    [SerializeField] private JigsawPiece[] puzzlePieces;

    // Membuka akses variabel dari BaseMinigame agar bisa dibaca oleh JigsawPiece
    public bool CanPlay => canPlayPuzzle;

    protected override void Update()
    {
        // Tetap memanggil base.Update() untuk fungsi tombol ESC
        base.Update();
    }

    // Fungsi ini dipanggil secara spesifik oleh JigsawPiece saat berhasil menempel
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
            WinMinigame();
        }
    }
}