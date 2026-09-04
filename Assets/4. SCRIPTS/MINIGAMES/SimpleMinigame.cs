using UnityEngine;
using System.Collections.Generic;

public class SimpleMinigame : BaseMinigame
{
    [Header("Required Items")]
    [Tooltip("Masukkan semua ClickableItem yang wajib ditekan untuk menyelesaikan minigame")]
    [SerializeField] private List<ClickableItem> requiredItems;

    public bool CanPlay => canPlayPuzzle;

    public override void SetupMinigame(InteractableObject source)
    {
        base.SetupMinigame(source);
        
        // Memastikan semua item kembali ke state awal saat minigame baru dibuka
        foreach (ClickableItem item in requiredItems)
        {
            if (item != null)
            {
                item.ResetItem();
            }
        }
    }

    protected override void Update()
    {
        base.Update(); // Menjaga fungsi tombol ESC tetap berjalan
    }

    public void CheckWinCondition()
    {
        if (isSolved) return;

        bool allClicked = true;
        foreach (ClickableItem item in requiredItems)
        {
            // Jika ada satu saja yang belum ditekan, hentikan pengecekan
            if (item != null && !item.IsResolved)
            {
                allClicked = false;
                break;
            }
        }

        if (allClicked)
        {
            WinMinigame();
        }
    }
}