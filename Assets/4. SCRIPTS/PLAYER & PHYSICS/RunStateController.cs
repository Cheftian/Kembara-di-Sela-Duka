using UnityEngine;

public class RunStateController : StateMachineBehaviour
{
    private PlayerController player;

    // OnStateEnter dipanggil saat transisi MASUK ke State tempat skrip ini berada
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null)
        {
            // Mencari komponen PlayerController di objek induk utama
            player = animator.GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            // Beritahu PlayerController bahwa animasi Run-Pre sudah selesai dan sekarang berada di state Run
            player.SetIsFullyRunning(true);
        }
    }

    // OnStateExit dipanggil saat KELUAR dari State tempat skrip ini berada (misal saat ngerem ke Run-Post)
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player != null)
        {
            player.SetIsFullyRunning(false);
        }
    }
}
