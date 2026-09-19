using UnityEngine;

public class LookAtPlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Animator animator;

    [Header("Animation State Names")]
    [SerializeField] private string idleRightState = "Idle-Right";
    [SerializeField] private string idleLeftState = "Idle-Left";

    private string currentState = "";

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        
        // Cari player secara otomatis jika belum dimasukkan di Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player tidak ditemukan! Pastikan objek Player sudah diberi Tag 'Player'.");
            }
        }
    }

    void Update()
    {
        if (playerTransform == null || animator == null) return;

        // Logika sesuai permintaan:
        // Jika X Player < X Objek -> Idle-Right
        // Jika X Player >= X Objek -> Idle-Left
        string targetState = (playerTransform.position.x < transform.position.x) ? idleRightState : idleLeftState;

        // Hanya ganti jika state berbeda
        if (currentState != targetState)
        {
            ChangeAnimationKeepFrame(targetState);
        }
    }

    void ChangeAnimationKeepFrame(string newState)
    {
        // Ambil info state yang sedang berjalan di layer 0
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // PENTING: Gunakan Mathf.Repeat agar nilainya selalu di antara 0 dan 1 (mengatasi masalah loop waktu)
        float currentNormalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);

        // Mainkan animasi baru dari frame/waktu yang sama
        animator.Play(newState, 0, currentNormalizedTime);
        
        currentState = newState;
        Debug.Log("Animasi berganti ke: " + newState + " pada frame normalized: " + currentNormalizedTime);
    }
}
