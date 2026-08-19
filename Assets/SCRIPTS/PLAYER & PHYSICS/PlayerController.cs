using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Kecepatan gerak saat karakter dalam kondisi pusing (Dizzy)")]
    [SerializeField] private float dizzyMoveSpeed = 2f; 

    [Header("Advanced Run Settings")]
    [SerializeField] private float runSpeed = 8f; 
    [Tooltip("Kecepatan ekstra saat lari ditahan dalam waktu lama (Sprint/Boost)")]
    [SerializeField] private float maxSprintSpeed = 11f; 
    [Tooltip("Berapa detik waktu yang dibutuhkan sebelum efek Sprint/Boost aktif setelah mulai lari")]
    [SerializeField] private float durationBeforeSprint = 2f; 
    [Tooltip("Seberapa cepat akselerasi bertambah (Nilai tinggi = akselerasi lebih cepat)")]
    [SerializeField] private float runAcceleration = 6f; 
    [SerializeField] private bool canRun = true;

    private float shiftPressedTimer = 0f; // Menghitung durasi tombol Shift ditahan

    
    [Header("Visual & Animation Setup")]
    [Tooltip("Seret GameObject Child yang memiliki komponen Animator dan SpriteRenderer ke sini")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer; 

    [Header("Dizzy Recovery Settings")]
    [Tooltip("Durasi waktu karakter terdiam dalam posisi Duduk (Sit) sebelum berdiri kembali")]
    [SerializeField] private float sitDuration = 2.0f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [Tooltip("Waktu jeda sebelum pemain bisa lompat lagi setelah melompat")]
    [SerializeField] private float jumpCooldownTime = 0.25f;
    [Tooltip("Kecepatan gerak horizontal khusus saat karakter melompat atau berada di udara")]
    [FormerlySerializedAs("airMoveSpeed")]
    [SerializeField] private float jumpMovementSpeed = 7f;
    [Tooltip("Sudut maksimal tilt sprite saat karakter berada di udara dan menekan A/D")]
    [SerializeField] private float airTiltAngle = 25f;
    [Tooltip("Kecepatan merespons tilt visual saat di udara")]
    [SerializeField] private float airTiltLerpSpeed = 6f;
    [SerializeField] private bool canJump = true;

    private bool isGrounded = true;
    private bool isJumping = false;
    private bool isInJumpPreOrPost = false; // Flag pengunci input horizontal
    private float jumpCooldownTimer = 0f;
    private bool jumpInputHeld = false;
    private bool jumpInputConsumed = false;

    // Parameter Animator baru
    private readonly int jumpTriggerHash = Animator.StringToHash("JumpTrigger");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int verticalVelocityHash = Animator.StringToHash("VerticalVelocity");


    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;
    
    private bool isFlipping = false;
    private bool isDizzy = false; 
    private bool isRunning = false; // Status internal berlari
    private float originalMoveSpeed; 

    private readonly int isWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int isRunningHash = Animator.StringToHash("IsRunning");
    private readonly int stopRunningHash = Animator.StringToHash("StopRunning");
    private readonly int isDizzyHash = Animator.StringToHash("IsDizzy"); 
    private readonly int flipHash = Animator.StringToHash("Flip");
    private readonly int sitHash = Animator.StringToHash("Sit");
    private readonly int standHash = Animator.StringToHash("Stand");
    
    private readonly string defaultStateName = "Idle"; 

    private bool wasDizzyFromLeftWalk = false;

    public bool IsDizzy => isDizzy;
    public bool IsWalking => Mathf.Abs(horizontalInput) > 0f && !isFlipping;
    public bool IsRunning => isRunning && !isDizzy; // Lari hanya valid jika tidak pusing

    private bool isFullyRunning = false;
    private float currentVelocityX = 0f;

    [Header("Runtime Speed Information")]
    [Tooltip("Kecepatan horizontal saat ini sebelum arah gerak diterapkan.")]
    public float currentSpeed;

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
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
            if (jumpCooldownTimer < 0f)
            {
                jumpCooldownTimer = 0f;
            }
        }

        // KUNCI UTAMA: Panggil fungsi deteksi tanah di sini agar berjalan setiap frame!
        CheckGroundStatus();

        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
        {
            horizontalInput = 0;
            isRunning = false;
            UpdateAnimation();
            return;
        }

        if (isFlipping)
        {
            horizontalInput = 0;
            isRunning = false;
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
        if (isDizzy && horizontalInput == 0f)
        {
            wasDizzyFromLeftWalk = true; 
            SetDizzyStatus(false);       
        }
        else if (!isDizzy && horizontalInput < 0f && wasDizzyFromLeftWalk)
        {
            SetDizzyStatus(true);        
        }
    }

    private void CheckGroundStatus()
    {
        if (groundCheckPoint != null)
        {
            bool wasGroundedBefore = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

            if (!wasGroundedBefore && isGrounded)
            {
                currentVelocityX = moveSpeed;

                if (isJumping && !isInJumpPreOrPost)
                {
                    StartCoroutine(JumpPostSequence());
                }
            }

            // DEBUG 1: Mencetak status deteksi tanah setiap kali terjadi perubahan (Grounded <-> Airborne)
            // if (wasGroundedBefore != isGrounded)
            // {
            //     Debug.Log($"[Ground Check] Status Berubah! IsGrounded Sekarang: {isGrounded}. " +
            //             $"Kecepatan Vertikal Y saat ini: {rb.linearVelocity.y}");
            // }

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

        // KUNCI 1: Input gerakan horizontal mati total saat berada di fase Jump-Pre atau Jump-Post
        if (!isInJumpPreOrPost)
        {
            if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
            else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
        }

        bool jumpKeyHeld = Input.GetKey(KeyCode.Space);
        bool jumpKeyPressed = Input.GetKeyDown(KeyCode.Space);

        if (jumpKeyHeld)
        {
            if (!jumpInputHeld)
            {
                jumpInputHeld = true;
                jumpInputConsumed = false;
            }
        }
        else
        {
            jumpInputHeld = false;
            jumpInputConsumed = false;
        }

      // Logika Input Lari
        if (canRun && Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(horizontalInput) > 0f && !isDizzy)
        {
            isRunning = true;
            // Akumulasikan waktu selama tombol Shift ditekan secara terus-menerus
            shiftPressedTimer += Time.deltaTime;
        }
        else
        {
            if (isRunning && animator != null)
            {
                animator.SetTrigger(stopRunningHash);
            }
            isRunning = false;
            shiftPressedTimer = 0f; // Reset timer saat tombol dilepas atau karakter berhenti
        }

        // Logika Input Lompat
        if (canJump && jumpKeyPressed && isGrounded && !isDizzy && !isInJumpPreOrPost && !isFlipping && jumpCooldownTimer <= 0f && !jumpInputConsumed)
        {
            jumpInputConsumed = true;
            StartCoroutine(JumpPreSequence());
        }
    }

    private void HandleFlip()
    {
        if (isFlipping) return;
        if (!isGrounded) return;

        if (horizontalInput > 0 && !isFacingRight)
        {
            if (isDizzy)
            {
                SetDizzyStatus(false);
            }
            wasDizzyFromLeftWalk = false; 
            StartFlip();
        }
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
        float targetSpeed = 0f;

        if (!isGrounded && !isInJumpPreOrPost)
        {
            targetSpeed = jumpMovementSpeed;
        }
        else if (horizontalInput == 0f)
        {
            targetSpeed = 0f;
        }
        else if (isDizzy)
        {
            targetSpeed = dizzyMoveSpeed;
        }
        else if (isRunning)
        {
            if (shiftPressedTimer >= durationBeforeSprint)
            {
                targetSpeed = maxSprintSpeed;
            }
            else
            {
                targetSpeed = runSpeed;
            }
        }
        else
        {
            targetSpeed = moveSpeed;
        }

        float accelRate = runAcceleration;

        if (!isGrounded)
        {
            accelRate = runAcceleration * 1.2f;
        }
        else if (!isRunning && currentVelocityX > moveSpeed)
        {
            accelRate = runAcceleration * 1.5f;
        }

        currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetSpeed, accelRate * Time.fixedDeltaTime);

        float desiredHorizontalVelocity = horizontalInput * currentVelocityX;
        rb.linearVelocity = new Vector2(desiredHorizontalVelocity, rb.linearVelocity.y);
        currentSpeed = Mathf.Abs(rb.linearVelocity.x);
    }

    private void UpdateAnimation()
    {
    if (animator == null) return;

        float verticalVel = rb.linearVelocity.y;
        
        // Memberikan batas toleransi getaran angka kecil bawaan physics mesin Unity
        if (Mathf.Abs(verticalVel) < 0.1f) verticalVel = 0f;

        // // DEBUG 3: Cek angka velocity yang dikirim ke animator saat karakter sedang melompat/turun
        // if (!isGrounded)
        // {
        //     Debug.Log($"[Animator Feed] IsGrounded: {isGrounded} | " +
        //             $"Velocity Y Asli: {rb.linearVelocity.y} | Velocity Y Terfilter: {verticalVel}");
        // }

        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetFloat(verticalVelocityHash, verticalVel);
        {
            if (visualTransform != null)
            {
                float targetZ = 0f;

                if (!isGrounded)
                {
                    if (rb.linearVelocity.y < 0f)
                    {
                        targetZ = 0f;
                    }
                    else if (horizontalInput > 0f)
                    {
                        targetZ = -airTiltAngle;
                    }
                    else if (horizontalInput < 0f)
                    {
                        targetZ = airTiltAngle;
                    }
                }

                Vector3 currentEuler = visualTransform.localEulerAngles;
                currentEuler.z = Mathf.LerpAngle(currentEuler.z, targetZ, airTiltLerpSpeed * Time.deltaTime);
                visualTransform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z);
            }

            if (isDizzy)
            {
                // --- KONDISI PUSING (Metode Lama Anda) ---
                // Kembalikan skala Container ke normal (1)
                if (visualTransform != null) visualTransform.localScale = Vector3.one;

                bool isWalkingDizzy = Input.GetKey(KeyCode.A);
                animator.SetBool(isWalkingHash, isWalkingDizzy);
                animator.SetBool(isRunningHash, false); 
                animator.SetBool(isDizzyHash, true);

                if (!isWalkingDizzy && !isFlipping) animator.speed = 0f;
                else animator.speed = 1f;
            }
            else
            {
                // Default kecepatan animasi adalah normal (1f)
                animator.speed = 1f;

                bool isWalking = Mathf.Abs(horizontalInput) > 0f && !isFlipping;

                if (isRunning && !isFlipping)
                {
                    if (visualTransform != null)
                    {
                        Vector3 scale = visualTransform.localScale;
                        scale.x = isFacingRight ? 1f : -1f;
                        visualTransform.localScale = scale;

                        if (spriteRenderer != null) spriteRenderer.flipX = false;
                    }

                    // TAMBAHAN VISUAL SPRINT: Jika sudah masuk fase Sprint, 
                    // percepat animasi kaki berlari menjadi 1.4 kali lebih cepat (atau sesuaikan nilainya)
                    if (shiftPressedTimer >= durationBeforeSprint)
                    {
                        animator.speed = 1.4f; 
                    }
                }
                else
                {
                    if (visualTransform != null) visualTransform.localScale = Vector3.one;

                    if (spriteRenderer != null && !isFlipping)
                    {
                        spriteRenderer.flipX = !isFacingRight;
                    }
                }

                animator.SetBool(isWalkingHash, isWalking);
                animator.SetBool(isRunningHash, isRunning); 
                animator.SetBool(isDizzyHash, false); 
            }

        }
    }

    public void SetDizzyStatus(bool status)
    {
        if (isDizzy && !status)
        {
            isDizzy = false;
            isRunning = false; // Matikan status lari saat pemulihan pusing
            moveSpeed = originalMoveSpeed;
            
            StartCoroutine(DizzyRecoverySequence());
            return;
        }

        isDizzy = status;

        if (isDizzy)
        {
            isRunning = false; // Matikan paksa jika tiba-tiba pusing saat berlari
            moveSpeed = dizzyMoveSpeed; 
        }
        else
        {
            moveSpeed = originalMoveSpeed; 
        }
    }

    private IEnumerator DizzyRecoverySequence()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        }

        horizontalInput = 0f;
        isRunning = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = 1f;
        }

        yield return StartCoroutine(PlayAnimationAndWait("Sit"));

        yield return new WaitForSeconds(sitDuration);

        yield return StartCoroutine(PlayAnimationAndWait("Stand"));

        if (animator != null)
        {
            animator.Play(defaultStateName, 0, 0f); 
        }

        isFlipping = false;
        horizontalInput = 0f;

        ResetToIdleState();

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

        animator.ResetTrigger("Sit");
        animator.ResetTrigger("Stand");

        animator.SetTrigger(triggerName);

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

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
            animator.SetBool(isRunningHash, false); 
            animator.SetBool(isDizzyHash, false); 
            isInJumpPreOrPost = false;
            isJumping = false;
            animator.speed = 1f; 
        }
        isFullyRunning = false; // Pastikan flag lari penuh ikut dibersihkan
    }

    public void SetFacingDirection(bool lookRight)
    {
        isFacingRight = lookRight;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isFacingRight;
        }
        
        isFlipping = false;
        UpdateAnimation();
    }

    public void ClearDizzyMemory()
    {
        wasDizzyFromLeftWalk = false;
    }

    public void SetIsFullyRunning(bool status)
    {
        isFullyRunning = status;
    }

    private IEnumerator JumpPreSequence()
    {
        jumpCooldownTimer = jumpCooldownTime;
        isInJumpPreOrPost = true; // Kunci gerakan horizontal manual (input A/D)
        horizontalInput = 0f;
        isRunning = false;

        if (animator != null)
        {
            animator.SetTrigger(jumpTriggerHash);
        }

        // Tunggu 1 frame agar animator berpindah state secara penuh ke Jump-Pre
        yield return null; 
        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump-Pre"))
            {
                // Menahan kode sampai visual persiapan melompat selesai diputar
                yield return new WaitForSeconds(stateInfo.length);
            }
        }

        isJumping = true;
        isInJumpPreOrPost = false; // Buka kunci gerakan agar pemain bisa mengendalikan arah saat di udara
        currentVelocityX = jumpMovementSpeed;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private IEnumerator JumpPostSequence()
    {
        isJumping = false;
        isInJumpPreOrPost = true; // Kunci kembali gerakan horizontal saat mendarat di tanah (meredam benturan)
        horizontalInput = 0f;
        currentVelocityX = 0f; // Matikan instan sisa momentum kecepatan lari sebelumnya jika ada

        yield return null; // Tunggu transisi ke Jump-Post aktif di Animator

        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump-Post"))
            {
                // Menahan kontrol player sampai visual animasi mendarat selesai diputar penuh
                yield return new WaitForSeconds(stateInfo.length);
            }
        }

        isInJumpPreOrPost = false; // Buka kembali kontrol penuh gerakan player secara normal
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            // Bola Hijau = Di Tanah, Bola Merah = Melayang di Udara
            Gizmos.color = isGrounded ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            
            // Menggambar bola padat transparan
            Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius);
            
            // Memberikan garis tepi lingkaran luar berwarna hitam tegas
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
