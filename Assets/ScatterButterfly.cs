using UnityEngine;

public class ScatterButterfly : MonoBehaviour
{
    public enum ButterflyState { Idle, Scattering, GoingToTarget }

    [Header("Flight Speed")]
    public float minSpeed = 3f;
    public float maxSpeed = 6f;

    [Header("Distance & Fade (Scatter Mode Only)")]
    public float minFlightDistance = 2f;
    public float maxFlightDistance = 5f;
    public float fadeSpeed = 2f;

    [Header("Go To Target Settings")]
    [Tooltip("Seberapa acak kepakan/belokan kupu-kupu saat menuju target.")]
    public float randomnessIntensity = 1.5f;
    [Tooltip("Jarak toleransi ke target sebelum kupu-kupu dianggap tiba.")]
    public float arrivalDistanceThreshold = 0.2f;

    [Header("Idle Movement (After Arrival / Drift)")]
    [Tooltip("Radius batas terbang acak lokal di sekitar objek induk saat idle.")]
    public float idleRadius = 2.0f;
    [Tooltip("Kecepatan terbang santai saat melakukan drift acak.")]
    public float idleSpeed = 2.0f;

    [Header("Visual Orientation")]
    public bool spriteFacesRight = true;

    // Variabel Internal Pergerakan
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private ButterflySwarmManager manager;
    
    private ButterflyState currentState = ButterflyState.Idle;
    private Vector3 flightDirection;
    private float targetDistance;
    private float distanceTraveled = 0f;
    private float currentSpeed;

    // SISTEM NAVIGASI FIXED: Menggunakan localPosition untuk Idle agar sinkron dengan parent
    private Vector3 globalTargetPosition; 
    private Vector3 localIdleTargetPosition;
    private bool arrived = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        manager = GetComponentInParent<ButterflySwarmManager>();

        RevealerTool tool = GetComponent<RevealerTool>();
        if (tool != null) tool.enabled = false;

        if (animator != null) animator.Update(Random.Range(0f, 5f));

        // Mengeset posisi target lokal acak pertama kali saat game dimulai
        PickNewLocalIdleTarget();
    }

    public void StartScatterFlight()
    {
        RevealerTool tool = GetComponent<RevealerTool>();
        if (tool != null) tool.enabled = true;

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.useFullKinematicContacts = false;
        }

        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
        
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        flightDirection = direction * currentSpeed;
        targetDistance = Random.Range(minFlightDistance, maxFlightDistance);

        HandleFlipScatter(direction.x);
        currentState = ButterflyState.Scattering;
    }

    public void StartGoToTargetFlight(Vector3 targetPos)
    {
        globalTargetPosition = targetPos;
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        arrived = false;

        Vector3 dirToTarget = (globalTargetPosition - transform.position).normalized;
        HandleFlipScatter(dirToTarget.x);

        currentState = ButterflyState.GoingToTarget;
    }

    public bool HasArrivedAtTarget()
    {
        return arrived;
    }

    public void AttachToParentAndNormalize(Transform parentTransform)
    {
        // Menjaga posisi global visual saat ini agar tidak teleportasi saat dipasang ke parent baru
        transform.SetParent(parentTransform, true);
    }

    void Update()
    {
        // STATE 1: IDLE DRIFT BEHAVIOUR (Mekanik adaptasi stabil berbasis koordinat Lokal)
        if (currentState == ButterflyState.Idle)
        {
            // Bergerak halus ke target posisi lokal menggunakan MoveTowards
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition, 
                localIdleTargetPosition, 
                idleSpeed * Time.deltaTime
            );

            // PERBAIKAN UTAMA: Menggunakan Jarak Toleransi Fleksibel agar tidak macet/terkunci
            if (Vector3.Distance(transform.localPosition, localIdleTargetPosition) < 0.05f)
            {
                PickNewLocalIdleTarget();
            }
            return;
        }

        // STATE 2: SCATTERING MODE
        if (currentState == ButterflyState.Scattering)
        {
            Vector3 movement = flightDirection * Time.deltaTime;
            transform.position += movement;
            distanceTraveled += movement.magnitude;

            if (distanceTraveled >= targetDistance) FadeAndDestroy();
            return;
        }

        // STATE 3: GO TO TARGET MODE
        if (currentState == ButterflyState.GoingToTarget)
        {
            Vector3 baseDirection = (globalTargetPosition - transform.position).normalized;

            float noiseX = Mathf.Sin(Time.time * currentSpeed + Random.value) * randomnessIntensity;
            float noiseY = Vector3.Cross(baseDirection, Vector3.forward).y * Mathf.Cos(Time.time * currentSpeed) * randomnessIntensity;
            Vector3 randomOffset = new Vector3(noiseX, noiseY, 0f) * 0.3f;

            Vector3 finalDirection = (baseDirection + randomOffset).normalized;
            transform.position += finalDirection * currentSpeed * Time.deltaTime;

            HandleFlipScatter(finalDirection.x);

            // Deteksi ketibaan di koordinat target global
            if (Vector3.Distance(transform.position, globalTargetPosition) <= arrivalDistanceThreshold)
            {
                arrived = true;
                currentState = ButterflyState.Idle;
                
                // Pindahkan parent secara global tanpa merusak posisi visual (mencegah lompatan visual)
                if (manager != null && manager.GetCurrentTargetTransform() != null)
                {
                    AttachToParentAndNormalize(manager.GetCurrentTargetTransform());
                }
                
                // Langsung tentukan titik acak baru berbasis koordinat lokal di parent yang baru
                PickNewLocalIdleTarget();

                if (manager != null)
                {
                    manager.NotifySwarmArrival();
                }
            }
        }
    }

    void PickNewLocalIdleTarget()
    {
        // Membuat titik target acak baru di dalam lingkaran lokal radius
        Vector2 randomCirclePoint = Random.insideUnitCircle * idleRadius;
        localIdleTargetPosition = new Vector3(randomCirclePoint.x, randomCirclePoint.y, 0f);

        // Menentukan arah hadap sprite berdasarkan posisi lokal
        Vector3 scale = transform.localScale;
        if (localIdleTargetPosition.x > transform.localPosition.x) // Bergerak ke Kanan
        {
            scale.x = spriteFacesRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        }
        else // Bergerak ke Kiri
        {
            scale.x = spriteFacesRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        }
        transform.localScale = scale;
    }

    void HandleFlipScatter(float directionX)
    {
        Vector3 scale = transform.localScale;
        if (directionX > 0.01f)
        {
            scale.x = spriteFacesRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        }
        else if (directionX < -0.01f)
        {
            scale.x = spriteFacesRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        }
        transform.localScale = scale;
    }

    void FadeAndDestroy()
    {
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a -= fadeSpeed * Time.deltaTime;
            spriteRenderer.color = currentColor;
            if (currentColor.a <= 0f) Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
