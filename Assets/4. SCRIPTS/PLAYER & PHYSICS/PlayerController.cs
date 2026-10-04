using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling,
        Dizzy,
        Flipping,
        DizzyRecovery,
        Narration,
        GlitchExit,
        InputBlocked
    }

    public enum DizzyRecoveryPhase
    {
        None,
        Sit,
        Waiting,
        Stand
    }

    public bool BlockInput { get; set; } = false;

    #region Movement Settings
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
    #endregion

    private float shiftPressedTimer = 0f; // Menghitung durasi tombol Shift ditahan

    
    #region Visual & Animation References
    [Header("Visual & Animation Setup")]
    [Tooltip("Seret GameObject Child yang memiliki komponen Animator dan SpriteRenderer ke sini")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer; 
    #endregion

    #region Recovery Settings
    [Header("Dizzy Recovery Settings")]
    [Tooltip("Durasi waktu karakter terdiam dalam posisi Duduk (Sit) sebelum berdiri kembali")]
    [SerializeField] private float sitDuration = 2.0f;
    [Tooltip("Durasi player tetap duduk setelah teleport sebelum menjalankan Stand")]
    [SerializeField] private float sitDelayAfterTeleport = 1.5f;
    [SerializeField] private string glitchFlashInName = "FlashIn";
    #endregion

    #region Jump Settings
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [Tooltip("Pengali kecepatan naik saat tombol lompat dilepas lebih awal")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpReleaseMultiplier = 0.35f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float minimumGroundContactNormalY = 0.2f;
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
    #endregion

    #region Runtime State
    private Collider2D[] slopeColliders;
    private readonly HashSet<Collider2D> supportingGroundColliders = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> supportingPlatformColliders = new HashSet<Collider2D>();
    

    public bool isGrounded = true;
    public bool isPlatforming = false;
    private bool isJumping = false;
    private bool isFallingFromPlatform = false;
    private bool fallAnimationStarted = false;
    private bool isInJumpPreOrPost = false; // Flag pengunci input horizontal
    private float jumpCooldownTimer = 0f;
    private bool jumpInputHeld = false;
    private bool jumpInputConsumed = false;
    private bool jumpInputBlockedDuringFlip = false;
    private bool jumpCutApplied = false;

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
    private readonly int runPostHash = Animator.StringToHash("RunPost");
    private readonly int isDizzyHash = Animator.StringToHash("IsDizzy"); 
    private readonly int flipHash = Animator.StringToHash("Flip");
    private readonly int sitHash = Animator.StringToHash("Sit");
    private readonly int standHash = Animator.StringToHash("Stand");
    
    private readonly string defaultStateName = "Idle"; 

    private bool wasDizzyFromLeftWalk = false;
    private bool isGlitchExiting = false;
    private bool isNarrationSitSequenceActive = false;
    private PlayerState currentState = PlayerState.Idle;
    private bool dizzyPendingAfterFlip = false;
    private bool dizzyPendingAfterRecovery = false;
    private Coroutine dizzyRecoveryCoroutine;
    private DizzyRecoveryPhase dizzyRecoveryPhase = DizzyRecoveryPhase.None;

    public bool IsDizzy => isDizzy;
    public PlayerState CurrentState => currentState;
    public bool IsDizzyRecovering => dizzyRecoveryPhase != DizzyRecoveryPhase.None;
    public DizzyRecoveryPhase CurrentDizzyRecoveryPhase => dizzyRecoveryPhase;
    public bool IsNarrationSitSequenceActive => isNarrationSitSequenceActive;
    public bool IsWalking => Mathf.Abs(horizontalInput) > 0f && !isFlipping;
    public bool IsRunning => isRunning && !isDizzy; // Lari hanya valid jika tidak pusing

    private bool isFullyRunning = false;
    private bool runPostPending = false;
    private float currentVelocityX = 0f;

    [Header("Runtime Speed Information")]
    [Tooltip("Kecepatan horizontal saat ini sebelum arah gerak diterapkan.")]
    public float currentSpeed;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalMoveSpeed = moveSpeed; 

        if (platformLayer.value == 0)
        {
            platformLayer = LayerMask.GetMask("Platform");
        }

        UpdateSlopePlatformsList();

        if (visualTransform == null && transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
        if (visualTransform != null)
        {
            if (animator == null) animator = visualTransform.GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = visualTransform.GetComponent<SpriteRenderer>(); 
        }
        HandleFlip();
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
        ManageSlopePlatforms();
        UpdatePlayerState();

        if (BlockInput)
        {
            horizontalInput = 0;
            isRunning = false;
            if (!isNarrationSitSequenceActive)
            {
                UpdateAnimation();
            }
            return;
        }

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
        UpdatePlayerState();
        UpdateAnimation();
    }
    #endregion

    #region State & Dizzy Logic
    private void UpdatePlayerState()
    {
        if (isGlitchExiting)
        {
            currentState = PlayerState.GlitchExit;
        }
        else if (isNarrationSitSequenceActive)
        {
            currentState = PlayerState.Narration;
        }
        else if (dizzyRecoveryPhase != DizzyRecoveryPhase.None)
        {
            currentState = PlayerState.DizzyRecovery;
        }
        else if (BlockInput || (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play))
        {
            currentState = PlayerState.InputBlocked;
        }
        else if (isFlipping)
        {
            currentState = PlayerState.Flipping;
        }
        else if (!isGrounded)
        {
            currentState = isJumping ? PlayerState.Jumping : PlayerState.Falling;
        }
        else if (isDizzy)
        {
            currentState = PlayerState.Dizzy;
        }
        else if (isRunning)
        {
            currentState = PlayerState.Running;
        }
        else if (Mathf.Abs(horizontalInput) > 0f)
        {
            currentState = PlayerState.Walking;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
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
            RequestDizzyFromLeft();
        }
    }

    public void RequestDizzyFromLeft()
    {
        wasDizzyFromLeftWalk = true;

        if (isDizzy || isFlipping) return;

        if (dizzyRecoveryCoroutine != null)
        {
            dizzyPendingAfterRecovery = true;
        }
        else if (isFacingRight)
        {
            dizzyPendingAfterFlip = true;
        }
        else
        {
            SetDizzyStatus(true);
        }
    }
    #endregion

    #region Ground Detection & Physics
    private void CheckGroundStatus()
    {
        if (groundCheckPoint != null)
        {
            bool wasGroundedBefore = isGrounded;
            bool isOnGroundLayer = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer) ||
                HasSupportingGroundContact();
            bool isOnPlatformLayer = HasSupportingPlatformContact();

            isPlatforming = !isOnGroundLayer && isOnPlatformLayer;
            isGrounded = isOnGroundLayer || isOnPlatformLayer;

            if (wasGroundedBefore && !isGrounded && !isJumping && !isInJumpPreOrPost)
            {
                isFallingFromPlatform = true;
                fallAnimationStarted = false;
            }

            if (!wasGroundedBefore && isGrounded)
            {
                ResetMovementStateAfterLanding();

                bool landedAfterFall = isFallingFromPlatform;
                if ((isJumping || landedAfterFall) && !isInJumpPreOrPost)
                {
                    if (landedAfterFall && !fallAnimationStarted && animator != null)
                    {
                        animator.Play("Jump-Post", 0, 0f);
                    }

                    isFallingFromPlatform = false;
                    fallAnimationStarted = false;
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

    private bool HasSupportingPlatformContact()
    {
        foreach (Collider2D platformCollider in supportingPlatformColliders)
        {
            if (platformCollider != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSupportingGroundContact()
    {
        foreach (Collider2D groundCollider in supportingGroundColliders)
        {
            if (groundCollider != null)
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Collider2D surfaceCollider = collision.collider;
        if (surfaceCollider == null ||
            rb.linearVelocity.y > 0.1f)
        {
            return;
        }

        int surfaceLayer = 1 << surfaceCollider.gameObject.layer;
        HashSet<Collider2D> supportingColliders = null;
        if ((platformLayer.value & surfaceLayer) != 0)
        {
            supportingColliders = supportingPlatformColliders;
        }
        else if ((groundLayer.value & surfaceLayer) != 0)
        {
            supportingColliders = supportingGroundColliders;
        }

        if (supportingColliders == null)
        {
            return;
        }

        Collider2D playerCollider = collision.otherCollider;
        if (playerCollider == null)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y >= minimumGroundContactNormalY &&
                contact.point.y <= playerCollider.bounds.min.y + groundCheckRadius)
            {
                supportingColliders.Add(surfaceCollider);
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        supportingGroundColliders.Remove(collision.collider);
        supportingPlatformColliders.Remove(collision.collider);
    }

    private void ResetMovementStateAfterLanding()
    {
        horizontalInput = 0f;
        isRunning = false;
        shiftPressedTimer = 0f;
        currentVelocityX = 0f;
        currentSpeed = 0f;
        jumpInputHeld = false;
        jumpInputConsumed = false;
        isFlipping = false;
        isFullyRunning = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void FixedUpdate()
    {
        if (isJumping && !jumpCutApplied && rb.linearVelocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpReleaseMultiplier);
            jumpCutApplied = true;
        }

       if (BlockInput)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
    
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        ApplyMovement();
    }
    #endregion

    #region Input & Facing
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

        if (runPostPending && !IsRunTransitionPlaying())
        {
            runPostPending = false;
        }

        if (jumpInputBlockedDuringFlip)
        {
            jumpInputHeld = jumpKeyHeld;
            jumpInputConsumed = jumpKeyHeld;

            if (!jumpKeyHeld)
            {
                jumpInputBlockedDuringFlip = false;
                jumpInputConsumed = false;
            }
        }
        else if (jumpKeyHeld)
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
        if (!isFlipping && canRun && Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(horizontalInput) > 0f && !isDizzy)
        {
            isRunning = true;
            // Akumulasikan waktu selama tombol Shift ditekan secara terus-menerus
            shiftPressedTimer += Time.deltaTime;
        }
        else
        {
            if (isRunning && animator != null)
            {
                runPostPending = true;
                if (shiftPressedTimer <= 0.5f)
                {
                    animator.SetTrigger(runPostHash);
                }
                else if (shiftPressedTimer > 0.5f)
                {
                    animator.SetTrigger(stopRunningHash);
                }
            }
            animator.SetTrigger(stopRunningHash);
            isRunning = false;
            shiftPressedTimer = 0f; // Reset timer saat tombol dilepas atau karakter berhenti
        }

        // Logika Input Lompat
        if (canJump && jumpKeyPressed && isGrounded && !isJumping && !isDizzy && !isInJumpPreOrPost && !isFlipping && jumpCooldownTimer <= 0f && !jumpInputConsumed)
        {
            jumpInputConsumed = true;
            StartCoroutine(JumpPreSequence());
        }
    }

    private void HandleFlip()
    {
        if (isFlipping || isInJumpPreOrPost || isJumping) return;
        if (!isGrounded) return;

        if (isDizzy)
        {
            if (horizontalInput > 0f)
            {
                SetDizzyStatus(false);
            }

            return;
        }

        if (horizontalInput > 0 && !isFacingRight)
        {
            wasDizzyFromLeftWalk = false; 
            StartFlip();
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            StartFlip();
        }
    }

    private bool IsRunTransitionPlaying()
    {
        if (animator == null) return false;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName("Run-Pre") || currentState.IsName("Run-Post"))
        {
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            return nextState.IsName("Run-Pre") || nextState.IsName("Run-Post");
        }

        return false;
    }
    #endregion

    #region Flip Animation
    private void StartFlip()
    {
        if (visualTransform == null || animator == null || spriteRenderer == null) return;

        isFlipping = true;                 
        isRunning = false;
        isFullyRunning = false;
        runPostPending = false;
        shiftPressedTimer = 0f;
        jumpInputBlockedDuringFlip = true;
        jumpInputHeld = false;
        jumpInputConsumed = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
        animator.SetBool(isRunningHash, false);
        animator.ResetTrigger(runPostHash);
        animator.ResetTrigger(stopRunningHash);

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

    public void Flip(int flipCount)
    {
        if (flipCount > 0)
        {
            StartCoroutine(FlipSequence(flipCount));
        }
    }

    private IEnumerator FlipSequence(int flipCount)
    {
        for (int i = 0; i < flipCount; i++)
        {
            StartFlip();

            while (isFlipping)
            {
                yield return null;
            }

            yield return null;
        }
    }

    public void OnFlipAnimationComplete()
    {
        if (visualTransform == null || animator == null || spriteRenderer == null) return;

        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !isFacingRight;

        animator.Play(defaultStateName, 0, 0f); 

        isFlipping = false;                

        if (dizzyPendingAfterFlip)
        {
            dizzyPendingAfterFlip = false;
            SetDizzyStatus(true);
        }

        GetPlayerInput();
        UpdateAnimation(); 
    }
    #endregion

    #region Movement & Animation
    private void ApplyMovement()
    {
        float targetSpeed = 0f;

        switch (currentState)
        {
            case PlayerState.Jumping:
            case PlayerState.Falling:
                if (!isInJumpPreOrPost)
                {
                    targetSpeed = jumpMovementSpeed;
                }
                break;
            case PlayerState.Walking:
                targetSpeed = moveSpeed;
                break;
            case PlayerState.Running:
                targetSpeed = shiftPressedTimer >= durationBeforeSprint ? maxSprintSpeed : runSpeed;
                break;
            case PlayerState.Dizzy:
                targetSpeed = horizontalInput == 0f ? 0f : dizzyMoveSpeed;
                break;
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

        if (isFallingFromPlatform && !fallAnimationStarted && verticalVel <= -0.5f)
        {
            animator.Play("Jump-Down", 0, 0f);
            fallAnimationStarted = true;
        }

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

                if (isWalkingDizzy && spriteRenderer != null)
                {
                    spriteRenderer.flipX = true;
                }

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
    #endregion

    #region Dizzy Recovery & Cutscene Sequences
    public void SetDizzyStatus(bool status)
    {
        if (isDizzy && !status)
        {
            isDizzy = false;
            isRunning = false; // Matikan status lari saat pemulihan pusing
            moveSpeed = originalMoveSpeed;

            if (dizzyRecoveryCoroutine == null)
            {
                dizzyRecoveryCoroutine = StartCoroutine(DizzyRecoverySequence());
            }

            return;
        }

        if (status && dizzyRecoveryCoroutine != null)
        {
            dizzyPendingAfterRecovery = true;
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

    public void BeginGlitchExit(RoomPortal destinationPortal)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || isGlitchExiting || destinationPortal == null) return;

        isGlitchExiting = true;
        isDizzy = false;
        isRunning = false;
        moveSpeed = originalMoveSpeed;
        StartCoroutine(GlitchExitSequence(destinationPortal));
    }

    public void PlayNarrationWithSit(NarrationData narrationData)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || narrationData == null || isNarrationSitSequenceActive)
            return;

        StartCoroutine(SitThenPlayNarration(narrationData));
    }

    private IEnumerator SitThenPlayNarration(NarrationData narrationData)
    {
        dizzyRecoveryPhase = DizzyRecoveryPhase.Sit;
        isNarrationSitSequenceActive = true;
        BlockInput = true;
        isDizzy = false;
        isRunning = false;
        horizontalInput = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = 1f;
        }

        yield return StartCoroutine(PlayAnimationAndWait("Sit"));
        if (animator != null)
        {
            animator.Play("Sit", 0, 1f);
            animator.Update(0f);
            animator.speed = 0f;
        }

        if (NarrationManager.Instance == null)
        {
            yield return StartCoroutine(StandAfterNarration());
            yield break;
        }

        dizzyRecoveryPhase = DizzyRecoveryPhase.Waiting;
        NarrationManager.Instance.NarrationFinished += OnSitNarrationFinished;
        NarrationManager.Instance.PlayNarration(
            narrationData,
            GameManager.GameState.Cutscene,
            GameManager.GameState.Play);
    }

    private void OnSitNarrationFinished()
    {
        if (NarrationManager.Instance != null)
        {
            NarrationManager.Instance.NarrationFinished -= OnSitNarrationFinished;
        }

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            StartCoroutine(StandAfterNarration());
        }
    }

    private IEnumerator StandAfterNarration()
    {
        dizzyRecoveryPhase = DizzyRecoveryPhase.Stand;

        if (animator != null)
        {
            animator.speed = 1f;
        }

        yield return StartCoroutine(PlayStandAnimationAndWait());

        isDizzy = false;
        isRunning = false;
        horizontalInput = 0f;
        isNarrationSitSequenceActive = false;
        BlockInput = false;
        ResetToIdleState();

        if (animator != null)
        {
            animator.Play(defaultStateName, 0, 0f);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }

        dizzyRecoveryPhase = DizzyRecoveryPhase.None;
    }

    private IEnumerator PlayStandAnimationAndWait()
    {
        if (animator == null) yield break;

        animator.ResetTrigger("Sit");
        animator.ResetTrigger("Stand");
        animator.Play("Stand", 0, 0f);
        animator.Update(0f);

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float elapsed = 0f;
        while (elapsed < stateInfo.length)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator GlitchExitSequence(RoomPortal destinationPortal)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);
        }

        horizontalInput = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = 1f;
        }

        if (SceneController.Instance != null && !string.IsNullOrEmpty(glitchFlashInName))
        {
            SceneController.Instance.PlayTransitionByName(glitchFlashInName);
        }

        yield return StartCoroutine(PlayAnimationAndWait("Sit"));
        yield return new WaitForSeconds(sitDuration);
        bool teleportStarted = destinationPortal.TeleportPlayerFromGlitch(transform);

        if (!teleportStarted)
        {
            CompleteGlitchExit();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.Play);
            }
        }
    }

    public void CompleteGlitchExit()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        StartCoroutine(CompleteGlitchExitSequence());
    }

    private IEnumerator CompleteGlitchExitSequence()
    {
        yield return new WaitForSeconds(sitDelayAfterTeleport);
        yield return StartCoroutine(PlayAnimationAndWait("Stand"));

        isGlitchExiting = false;
        ResetToIdleState();

        if (animator != null)
        {
            animator.Play(defaultStateName, 0, 0f);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    private IEnumerator DizzyRecoverySequence()
    {
        dizzyRecoveryPhase = DizzyRecoveryPhase.Sit;

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

    dizzyRecoveryPhase = DizzyRecoveryPhase.Waiting;
        yield return new WaitForSeconds(sitDuration);

    dizzyRecoveryPhase = DizzyRecoveryPhase.Stand;
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

        if (dizzyPendingAfterRecovery)
        {
            dizzyPendingAfterRecovery = false;
            wasDizzyFromLeftWalk = true;
        }

        dizzyRecoveryCoroutine = null;
        dizzyRecoveryPhase = DizzyRecoveryPhase.None;
    }
    #endregion

    #region Public Controls & Animation Helpers
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
        // Cutscene dapat memotong animasi flip sebelum event penyelesaiannya dipanggil.
        isFlipping = false;
        horizontalInput = 0f;
        isRunning = false;
        shiftPressedTimer = 0f;
        currentVelocityX = 0f;
        currentSpeed = 0f;

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
    #endregion

    #region Jump Sequences
    private IEnumerator JumpPreSequence()
    {
        jumpCooldownTimer = jumpCooldownTime;
        isInJumpPreOrPost = true; // Kunci gerakan horizontal manual (input A/D)
        horizontalInput = 0f;
        isRunning = false;
        isFullyRunning = false;
        runPostPending = false;
        shiftPressedTimer = 0f;

        if (animator != null)
        {
            animator.SetBool(isRunningHash, false);
            animator.ResetTrigger(runPostHash);
            animator.ResetTrigger(stopRunningHash);
            animator.ResetTrigger(jumpTriggerHash);
            animator.Play("Jump-Pre", 0, 0f);
        }

        // Tunggu 1 frame agar Animator memperbarui state Jump-Pre.
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

        jumpCutApplied = false;
        isJumping = true;
        isInJumpPreOrPost = false; // Buka kunci gerakan agar pemain bisa mengendalikan arah saat di udara
        currentVelocityX = jumpMovementSpeed;

        // KUNCI BARU: Panggil partikel melompat tepat saat karakter melesat ke atas!
        PlayerParticleController particleController = GetComponent<PlayerParticleController>();
        if (particleController != null)
        {
            particleController.PlayJumpParticles();
        }

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
    #endregion

    #region Gizmos & Slope Platforms
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

    public void UpdateSlopePlatformsList()
    {
        // Mencari semua objek dengan tag, lalu mengambil komponen Collider2D-nya
        GameObject[] slopeObjects = GameObject.FindGameObjectsWithTag("SlopePlatform");
        slopeColliders = new Collider2D[slopeObjects.Length];
        
        for (int i = 0; i < slopeObjects.Length; i++)
        {
            slopeColliders[i] = slopeObjects[i].GetComponent<Collider2D>();
        }
    }

    private void ManageSlopePlatforms()
    {
        if (slopeColliders == null || slopeColliders.Length == 0) return;

        bool shouldEnableSlope = !isGrounded || isPlatforming;

        foreach (Collider2D collider in slopeColliders)
        {
            if (collider != null)
            {
                collider.enabled = shouldEnableSlope;
            }
        }
    }
    #endregion

}
