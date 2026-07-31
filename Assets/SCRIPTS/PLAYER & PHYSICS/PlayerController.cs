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

        // Izinkan pembacaan input A dan D secara murni tanpa hambatan status dizzy di sini
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
    }

    private void HandleFlip()
    {
        // JIKA SEDANG FLIPPING, JANGAN PROSES FLIP BARU
        if (isFlipping) return;

        // KONDISI 1: Menghadap Kiri, lalu menekan Kanan (D)
        if (horizontalInput > 0 && !isFacingRight)
        {
            // Matikan status dizzy SEBELUM memulai flip agar tidak terjadi konflik loop dengan OnTriggerStay
            if (isDizzy)
            {
                SetDizzyStatus(false);
            }
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
            // Pastikan kecepatan animator selalu normal (1f) agar tidak ada animasi yang membeku
            animator.speed = 1f;

            if (isDizzy)
            {
                // Saat dizzy, status jalan ditentukan dari apakah tombol A sedang ditekan
                bool isWalkingDizzy = Input.GetKey(KeyCode.A);
                
                animator.SetBool(isWalkingHash, isWalkingDizzy);
                animator.SetBool(isDizzyHash, true);
            }
            else
            {
                // Kondisi normal atau saat berjalan ke arah kanan (D)
                bool isWalking = Mathf.Abs(horizontalInput) > 0f && !isFlipping;
                animator.SetBool(isWalkingHash, isWalking);
                animator.SetBool(isDizzyHash, false); 
            }
        }
    }


    public void SetDizzyStatus(bool status)
    {
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

    /// <summary>
    /// Mengembalikan paksa status animasi ke mode diam setelah animasi berdiri selesai.
    /// </summary>
    public void ResetToIdleState()
    {
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, false);
        }
    }
}
