using UnityEngine;
using UnityEngine.UI;

public class ObjectInteraction : BaseMinigame
{
    // Awake dan Update tidak perlu di-override lagi.
    // Kita memanfaatkan alur ESC dan tombol Close yang sudah ada di BaseMinigame.

    public override void CloseMinigame()
    {
        // Jika objek belum ditandai selesai (pemain sedang menginspeksi)
        if (!isSolved)
        {
            WinMinigame(); // Eksekusi penyelesaian (termasuk narasi outro jika ada)

            // Jika tidak ada narasi outro, langsung teruskan penutupan 
            // agar pemain tidak perlu menekan tombol ESC dua kali.
            if (outroNarration == null)
            {
                base.CloseMinigame();
            }
        }
        else
        {
            // Jika sudah selesai (contoh: narasi outro sudah beres dan pemain baru menekan ESC),
            // tutup panel secara normal.
            base.CloseMinigame();
        }
    }
}