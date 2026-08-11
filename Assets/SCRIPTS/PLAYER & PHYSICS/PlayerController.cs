using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Kecepatan gerak saat karakter dalam kondisi pusing (Dizzy)")]
    [SerializeField] private float dizzyMoveSpeed = 2f; 
    
    [Header("Visual & Animation Setup")]
    [Tooltip("Seret GameObject Child yang memiliki komponen Animator dan SpriteRenderer ke sini")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer; 

    [Header("Dizzy Recovery Settings")]
    [Tooltip("Durasi waktu karakter terdiam dalam posisi Duduk (Sit) sebelum berdiri kembali")]
    [SerializeField] private float sitDuration = 2.0f;

    
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;
    
    private bool isFlipping = false;
    private bool isDizzy = false; 
    private float originalMoveSpeed; 

    private readonly int isWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int isDizzyHash = Animator.StringToHash("IsDizzy"); 
    private readonly int flipHash = Animator.StringToHash("Flip");
    private readonly int sitHash = Animator.StringToHash("Sit");
    private readonly int standHash = Animator.StringToHash("Stand");
    
    private readonly string defaultStateName = "Idle"; 

    private bool wasDizzyFromLeftWalk = false;

    // Properti Publik untuk Kamera
    public bool IsDizzy => isDizzy;
    public bool IsWalking => Mathf.Abs(horizontalInput) > 0f && !isFlipping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalMoveSpeed = moveSpeed; 
        
        if (visualTransform == null && transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
        if (visualTransform != null)
        {
            if (animator == null) animator = visualTransform.GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = visualTransform.GetComponent<SpriteRenderer>(); 
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
        {
            horizontalInput = 0;
            UpdateAnimation();
            return;
        }

        if (isFlipping)
        {
            horizontalInput = 0;
            UpdateAnimation(); 
            return;
        }

        GetPlayerInput();
        HandleDizzyLogic();
        HandleFlip();
        UpdateAnimation();
    }

    private void HandleDizzyLogic()
    {
        // Jika player diam (idle) saat sedang pusing, matikan isDizzy (memicu Sit & Stand)
        if (isDizzy && horizontalInput == 0f)
        {
            wasDizzyFromLeftWalk = true; 
            SetDizzyStatus(false);       
        }
        // Jika player berjalan ke kiri lagi (A) setelah sempat pulih, buat pusing kembali
        else if (!isDizzy && horizontalInput < 0f && wasDizzyFromLeftWalk)
        {
            SetDizzyStatus(true);        
        }
    }



    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        ApplyMovement();
    }

       private void GetPlayerInput()
    {
        horizontalInput = 0;

        // Izinkan pembacaan input A dan D secara murni tanpa hambatan status dizzy di sini
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
    }

private void HandleFlip()
{
    if (isFlipping) return;

    // KONDISI 1: Menghadap Kiri, lalu menekan Kanan (D)
    if (horizontalInput > 0 && !isFacingRight)
    {
        if (isDizzy)
        {
            SetDizzyStatus(false);
        }
        wasDizzyFromLeftWalk = false; // <--- TAMBAHKAN INI (Reset ingatan pusing)
        StartFlip();
    }
    // KONDISI 2: Menghadap Kanan, lalu menekan Kiri (A)
    else if (horizontalInput < 0 && isFacingRight)
    {
        StartFlip();
    }
}



    private void StartFlip()
    {
        if (visualTransform == null || animator == null || spriteRenderer == null) return;

        isFlipping = true;                 
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 

        if (!isFacingRight)
        {
            spriteRenderer.flipX = true; 
        }
        else
        {
            spriteRenderer.flipX = false; 
        }

        animator.SetTrigger(flipHash);     
    }

    public void OnFlipAnimationComplete()
    {
        if (visualTransform == null || animator == null || spriteRenderer == null) return;

        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !isFacingRight;

        animator.Play(defaultStateName, 0, 0f); 

        isFlipping = false;                

        GetPlayerInput();
        UpdateAnimation(); 
    }

    private void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }
    
    
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            if (isDizzy)
            {
                // Saat dizzy, status jalan ditentukan dari apakah tombol A sedang ditekan
                bool isWalkingDizzy = Input.GetKey(KeyCode.A);
                
                animator.SetBool(isWalkingHash, isWalkingDizzy);
                animator.SetBool(isDizzyHash, true);

                // BARU: Jika sedang pusing (Dizzy) dan TIDAK sedang berjalan (Idle)
                if (!isWalkingDizzy && !isFlipping)
                {
                    animator.speed = 0f; // Bekukan animasi di tempat (menahan sprite frame terakhir)
                }
                else
                {
                    animator.speed = 1f; // Jalankan kembali animasi saat player bergerak/membalik
                }
            }
            else
            {
                // BARU: Pastikan kecepatan animator kembali normal saat kondisi tidak pusing
                animator.speed = 1f;

                // Kondisi normal atau saat berjalan ke arah kanan (D)
                bool isWalking = Mathf.Abs(horizontalInput) > 0f && !isFlipping;
                animator.SetBool(isWalkingHash, isWalking);
                animator.SetBool(isDizzyHash, false); 
            }
        }
    }


    public void SetDizzyStatus(bool status)
    {
        // Deteksi jika sebelumnya pusing (true), lalu diubah menjadi tidak pusing (false)
        if (isDizzy && !status)
        {
            isDizzy = false;
            moveSpeed = originalMoveSpeed;
            
            // Jalankan urutan animasi pemulihan otomatis
            StartCoroutine(DizzyRecoverySequence());
            return;
        }

        // Logika dasar bawaan Anda sebelumnya
        isDizzy = status;

        if (isDizzy)
        {
            moveSpeed = dizzyMoveSpeed; 
        }
        else
        {
            moveSpeed = originalMoveSpeed; 
        }
    }

    private IEnumerator DizzyRecoverySequence()
    {
        // 1. Kunci total kontrol player dengan mengubah GameState menjadi Cutscene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        }

        // Hentikan pergerakan physics Rigidbody seketika agar tidak meluncur
        horizontalInput = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // 2. Pastikan animator.speed normal (1f) agar animasi tidak membeku
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // 3. Jalankan animasi "Sit" dan tunggu sampai klip selesai
        yield return StartCoroutine(PlayAnimationAndWait("Sit"));

        // 4. Jeda beberapa waktu dalam kondisi duduk diam
        yield return new WaitForSeconds(sitDuration);

        // 5. Jalankan animasi "Stand" dan tunggu sampai klip berdiri selesai
        yield return StartCoroutine(PlayAnimationAndWait("Stand"));

        // BARU: Kembalikan paksa visual animator ke state Idle default Anda
        if (animator != null)
        {
            animator.Play(defaultStateName, 0, 0f); 
        }

        // BARU: Pastikan semua flag pengunci input di bawah ini bersih total
        isFlipping = false;
        horizontalInput = 0f;

        // 6. Reset status parameter animasi kembali bersih ke mode normal
        ResetToIdleState();

        // 7. Kembalikan kontrol penuh permainan kepada Player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }



    public void UpdateMoveSpeed(float newSpeed)
    {
        originalMoveSpeed = newSpeed; 
        if (!isDizzy) moveSpeed = newSpeed;
    }

    public IEnumerator PlayAnimationAndWait(string triggerName)
    {
        if (animator == null) yield break;

        // Bersihkan trigger lama untuk mencegah double-triggering berkali-kali
        animator.ResetTrigger("Sit");
        animator.ResetTrigger("Stand");

        // Picu parameter trigger sesuai string yang dimasukkan ("Sit" atau "Stand")
        animator.SetTrigger(triggerName);

        // Tunggu 1 frame agar animator melakukan transisi penuh ke state animasi baru
        yield return null;

        // Mengambil data klip animasi yang sedang aktif di Layer 0 (Base Layer)
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Lakukan looping penahanan kode selama durasi waktu klip tersebut berjalan
        float elapsed = 0f;
        while (elapsed < stateInfo.length)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void ResetToIdleState()
    {
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, false);
            animator.SetBool(isDizzyHash, false); // BARU: Pastikan parameter pusing di animator mati total
            animator.speed = 1f; // BARU: Pastikan kecepatan animator kembali normal
        }
    }

       public void SetFacingDirection(bool lookRight)
    {
        isFacingRight = lookRight;
        
        if (spriteRenderer != null)
        {
            // Jika lookRight true (Kanan), flipX harus false (Normal)
            // Jika lookRight false (Kiri), flipX harus true (Flipped)
            spriteRenderer.flipX = !isFacingRight;
        }
        
        // Reset status membalik jika sedang terjadi transisi ditengah jalan
        isFlipping = false;
        
        // Perbarui animasi agar state berjalan/diam sinkron dengan arah baru
        UpdateAnimation();
    }

    public void ClearDizzyMemory()
    {
        wasDizzyFromLeftWalk = false;
    }

}
