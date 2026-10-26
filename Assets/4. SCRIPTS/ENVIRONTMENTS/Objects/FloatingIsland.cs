using UnityEngine;

public class FloatingIsland : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How high and low the island can move from its starting point.")]
    public float hoverAmplitude = 0.5f;
    
    [Tooltip("How fast the island moves up and down.")]
    public float hoverSpeed = 1.0f;

    [Header("Randomization")]
    [Tooltip("Adds random variation to the movement speed so multiple islands don't move in perfect sync.")]
    public float speedRandomizationRange = 0.2f;

    [Header("Player Lift")]
    [Tooltip("When enabled, the island lifts after the player steps on top of it.")]
    public bool liftWhenPlayerSteps = false;

    [Tooltip("How far the island rises when the player steps on it.")]
    public float liftHeight = 3f;

    [Tooltip("How fast the island rises.")]
    public float liftSpeed = 1f;

    [Tooltip("How long the player must stand on the island before it starts lifting.")]
    public float standTimeBeforeLift = 1f;

    [Header("Press Effect")]
    [Tooltip("How far the island moves down while the player stands on it when lift is disabled.")]
    public float pressDepth = 0.2f;

    [Tooltip("How fast the island moves down and returns after being pressed.")]
    public float pressSpeed = 2f;

    [Tooltip("How long the island waits after the player leaves before returning.")]
    public float pressReturnDelay = 0.5f;

    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    private Vector3 startPosition;
    private float randomOffset;
    private float adjustedSpeed;
    private Rigidbody2D supportedPlayer;
    private Collider2D playerCollider;
    private Collider2D islandCollider;
    private bool liftTriggered;
    private float currentLiftHeight;
    private float supportedDuration;
    private float lastSupportTime = float.NegativeInfinity;
    private bool pressActive;
    private bool pressWaitingToReturn;
    private bool pressReturning;
    private float pressStartY;
    private float pressReleaseTime;

    void Start()
    {
        // Store the exact position where you placed the island in the scene
        startPosition = transform.position;

        // Generate a random time offset so islands don't start at the exact same point in the wave
        randomOffset = Random.Range(0f, 100f);

        // Slightly randomize the speed for this specific island
        adjustedSpeed = hoverSpeed + Random.Range(-speedRandomizationRange, speedRandomizationRange);
        islandCollider = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        bool playerIsSupported = supportedPlayer != null && playerCollider != null &&
            Time.fixedTime - lastSupportTime <= Time.fixedDeltaTime * 2.5f;
        bool playerIsAboveIsland = liftWhenPlayerSteps && IsPlayerAboveIsland();

        if (liftWhenPlayerSteps && playerIsSupported)
        {
            if (!liftTriggered)
            {
                supportedDuration += Time.fixedDeltaTime;
                liftTriggered = supportedDuration >= Mathf.Max(0f, standTimeBeforeLift);
            }
        }
        else if (liftWhenPlayerSteps)
        {
            supportedDuration = 0f;

            if (!playerIsAboveIsland)
            {
                liftTriggered = false;
                supportedPlayer = null;
                playerCollider = null;
            }
        }

        if (!liftWhenPlayerSteps)
        {
            if (playerIsSupported)
            {
                if (!pressActive && !pressWaitingToReturn && !pressReturning)
                {
                    pressStartY = transform.position.y;
                }

                pressActive = true;
                pressWaitingToReturn = false;
                pressReturning = false;
            }
            else if (pressActive)
            {
                pressActive = false;
                pressWaitingToReturn = true;
                pressReleaseTime = Time.fixedTime;
            }

            if (pressWaitingToReturn &&
                Time.fixedTime - pressReleaseTime >= Mathf.Max(0f, pressReturnDelay))
            {
                pressWaitingToReturn = false;
                pressReturning = true;
            }
        }

        float hoverOffset = Mathf.Sin((Time.time * adjustedSpeed) + randomOffset) * hoverAmplitude;
        float targetY;

        if (!liftWhenPlayerSteps && pressActive)
        {
            targetY = Mathf.MoveTowards(
                transform.position.y,
                pressStartY - Mathf.Max(0f, pressDepth),
                Mathf.Max(0f, pressSpeed) * Time.fixedDeltaTime);
        }
        else if (!liftWhenPlayerSteps && pressWaitingToReturn)
        {
            targetY = transform.position.y;
        }
        else if (!liftWhenPlayerSteps && pressReturning)
        {
            targetY = Mathf.MoveTowards(
                transform.position.y,
                pressStartY,
                Mathf.Max(0f, pressSpeed) * Time.fixedDeltaTime);

            if (Mathf.Approximately(targetY, pressStartY))
            {
                startPosition.y = pressStartY - hoverOffset;
                pressReturning = false;
            }
        }
        else
        {
            float targetLiftHeight = liftTriggered ? Mathf.Max(0f, liftHeight) : 0f;
            currentLiftHeight = Mathf.MoveTowards(
                currentLiftHeight,
                targetLiftHeight,
                Mathf.Max(0f, liftSpeed) * Time.fixedDeltaTime);
            targetY = startPosition.y + hoverOffset + currentLiftHeight;
        }

        Vector3 nextPosition = new Vector3(startPosition.x, targetY, startPosition.z);
        float verticalDelta = nextPosition.y - transform.position.y;
        transform.position = nextPosition;

        if (playerIsSupported && supportedPlayer != null && supportedPlayer.linearVelocity.y <= 0.1f)
        {
            supportedPlayer.position += Vector2.up * verticalDelta;
        }
    }

    private bool IsPlayerAboveIsland()
    {
        if (supportedPlayer == null || playerCollider == null || islandCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds islandBounds = islandCollider.bounds;
        bool overlapsHorizontally = playerBounds.max.x > islandBounds.min.x &&
            playerBounds.min.x < islandBounds.max.x;

        return overlapsHorizontally && playerBounds.min.y >= islandBounds.max.y - 0.2f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (islandCollider == null)
        {
            return;
        }

        Rigidbody2D candidate = collision.collider.attachedRigidbody;
        Collider2D candidateCollider = collision.collider;
        if (candidate == null || !candidate.CompareTag(playerTag) || candidate.linearVelocity.y > 0.1f)
        {
            return;
        }

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.point.y >= islandCollider.bounds.max.y - 0.1f &&
                candidateCollider.bounds.min.y >= islandCollider.bounds.max.y - 0.2f)
            {
                supportedPlayer = candidate;
                playerCollider = candidateCollider;
                lastSupportTime = Time.fixedTime;
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (supportedPlayer != null && collision.collider.attachedRigidbody == supportedPlayer)
        {
            lastSupportTime = float.NegativeInfinity;
            supportedDuration = 0f;

            if (!liftWhenPlayerSteps || !IsPlayerAboveIsland())
            {
                liftTriggered = false;
                supportedPlayer = null;
                playerCollider = null;
            }
        }
    }
}
