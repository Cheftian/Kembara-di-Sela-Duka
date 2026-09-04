using UnityEngine;

public class RotationMinigame : BaseMinigame 
{
    [Header("Minigame Elements")]
    [SerializeField] private RotatableUI[] rotators;

    protected override void Update()
    {
        base.Update(); // Untuk deteksi ESC di BaseMinigame

        // JIKA sedang narasi atau sudah selesai, jangan cek input rotasi
        if (!canPlayPuzzle || isSolved) return;

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        bool allCorrect = true;
        foreach (RotatableUI rotator in rotators)
        {
            if (!rotator.IsCorrect())
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            WinMinigame();
        }
    }
}