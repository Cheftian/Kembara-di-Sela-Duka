using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FootShadowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D characterRigidbody;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRaycastDistance = 20f;
    [SerializeField] private float groundOffset = 0.02f;

    [Header("Dimension Settings")]
    [Tooltip("The base scale of the shadow when the character is idle (n).")]
    [SerializeField] private Vector3 idleScale = new Vector3(1f, 0.3f, 1f);
    
    [Tooltip("The target scale for the X axis when the character is walking.")]
    [SerializeField] private float walkingScaleX = 1.5f;

    [Tooltip("The target scale for the X axis when the character is running.")]
    [SerializeField] private float runningScaleX = 1.8f;
    
    [Tooltip("How fast the shadow transitions between idle and walking scales.")]
    [SerializeField] private float transitionSpeed = 12f;

    [Header("Airborne Shadow Settings")]
    [Tooltip("Vertical distance at which the shadow reaches its minimum size and disappears.")]
    [SerializeField] private float maxAirborneHeight = 5f;
    [Tooltip("Shadow scale multiplier when the player reaches maxAirborneHeight.")]
    [SerializeField] private float airborneMinScale = 0.1f;
    [Tooltip("Shadow alpha multiplier when the player reaches maxAirborneHeight. Use 0 to hide it completely.")]
    [SerializeField] private float airborneMinAlpha = 0f;

    private Vector3 targetScale;
    private Color originalColor;
    private SpriteRenderer shadowRenderer;

    private void Awake()
    {
        shadowRenderer = GetComponent<SpriteRenderer>();
        originalColor = shadowRenderer.color;

        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        transform.localScale = idleScale;
        targetScale = idleScale;
    }

    private void Update()
    {
        if (characterRigidbody == null) return;

        float airborneAmount = UpdateGroundPosition();

        // Running takes priority over regular walking scale.
        bool isRunning = playerController != null && playerController.IsRunning;
        bool isMoving = Mathf.Abs(characterRigidbody.linearVelocity.x) > 0.1f;

        if (isRunning)
        {
            targetScale = new Vector3(runningScaleX, idleScale.y, idleScale.z);
        }
        else if (isMoving)
        {
            // Stretch the X scale while keeping Y and Z at their idle values
            targetScale = new Vector3(walkingScaleX, idleScale.y, idleScale.z);
        }
        else
        {
            // Return to the base idle scale
            targetScale = idleScale;
        }

        float shadowScaleMultiplier = Mathf.Lerp(1f, airborneMinScale, airborneAmount);
        targetScale *= shadowScaleMultiplier;

        // Smoothly interpolate to the target scale and airborne transparency.
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
        Color shadowColor = originalColor;
        shadowColor.a *= Mathf.Lerp(1f, airborneMinAlpha, airborneAmount);
        shadowRenderer.color = Color.Lerp(shadowRenderer.color, shadowColor, Time.deltaTime * transitionSpeed);
    }

    private float UpdateGroundPosition()
    {
        Vector2 playerPosition = characterRigidbody.position;
        RaycastHit2D groundHit = Physics2D.Raycast(
            playerPosition,
            Vector2.down,
            groundRaycastDistance,
            groundLayer);

        if (groundHit.collider == null)
        {
            return 0f;
        }

        Vector3 shadowPosition = transform.position;
        shadowPosition.x = playerPosition.x;
        shadowPosition.y = groundHit.point.y + groundOffset;
        transform.position = shadowPosition;

        float heightAboveGround = Mathf.Max(0f, playerPosition.y - groundHit.point.y);
        return maxAirborneHeight > 0f
            ? Mathf.Clamp01(heightAboveGround / maxAirborneHeight)
            : 0f;
    }
}