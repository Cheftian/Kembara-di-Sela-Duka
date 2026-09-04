using UnityEngine;

public class WipingMinigame : BaseMinigame
{
    [Header("Wiping References")]
    [SerializeField] private WipeableSurface surface;
    
    // Properti agar WipeableSurface tahu kapan boleh menyeka
    public bool CanPlay => canPlayPuzzle;

    protected override void Update()
    {
        base.Update(); 

        if (isSolved || !canPlayPuzzle) return;

        if (surface != null)
        {
            float currentProgress = surface.GetProgress();
            if (currentProgress >= surface.WinThreshold)
            {
                CompleteWiping();
            }
        }
    }

    private void CompleteWiping()
    {
        isSolved = true;
        surface.FinalizeWipe(); // Perintah ke surface untuk bersih total
        WinMinigame(); // Fungsi dari BaseMinigame untuk tutup panel & balik ke Play
    }
}