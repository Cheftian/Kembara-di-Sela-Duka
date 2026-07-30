using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Kecepatan gerak saat karakter dalam kondisi pusing (Dizzy)")]
    [SerializeField] private float dizzyMoveSpeed = 2f; // Tambahan: Kecepatan saat dizzy
    
    [Header("Visual & Animation Setup")]
    [Tooltip("Seret GameObject Child yang memiliki komponen Animator dan SpriteRenderer ke sini")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer; 
    
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;
    
    private bool isFlipping = false;
    private bool isDizzy = false; 
    private float originalMoveSpeed; // Tambahan: Untuk menyimpan nilai speed asli

    private readonly int isWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int isDizzyHash = Animator.StringToHash("IsDizzy"); 
    private readonly int flipHash = Animator.StringToHash("Flip");
    
    private readonly string defaultStateName = "Idle"; 

    public bool IsDizzy => isDizzy;
    public bool IsWalking => Mathf.Abs(horizontalInput) > 0f && !isFlipping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalMoveSpeed = moveSpeed; // Tambahan: Simpan nilai awal moveSpeed saat game mulai
        
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
        HandleFlip();
        UpdateAnimation();
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
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
    }

    private void HandleFlip()
    {
        if (horizontalInput > 0 && !isFacingRight) StartFlip();
        else if (horizontalInput < 0 && isFacingRight) StartFlip();
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
            bool isWalking = Mathf.Abs(horizontalInput) > 0f;
            animator.SetBool(isWalkingHash, isWalking);
            animator.SetBool(isDizzyHash, isDizzy); 
        }
    }

    // Perubahan pada Fungsi ini
    public void SetDizzyStatus(bool status)
    {
        isDizzy = status;

        if (isDizzy)
        {
            moveSpeed = dizzyMoveSpeed; // Ganti ke kecepatan lambat saat pusing
        }
        else
        {
            moveSpeed = originalMoveSpeed; // Kembalikan ke kecepatan normal
        }
    }

    public void UpdateMoveSpeed(float newSpeed)
    {
        originalMoveSpeed = newSpeed; // Update nilai dasar jika ada power-up kecepatan
        if (!isDizzy) moveSpeed = newSpeed;
    }
}
