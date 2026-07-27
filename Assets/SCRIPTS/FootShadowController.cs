using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FootShadowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D characterRigidbody;

    [Header("Dimension Settings")]
    [Tooltip("The base scale of the shadow when the character is idle (n).")]
    [SerializeField] private Vector3 idleScale = new Vector3(1f, 0.3f, 1f);
    
    [Tooltip("The target scale for the X axis when the character is walking.")]
    [SerializeField] private float walkingScaleX = 1.5f;
    
    [Tooltip("How fast the shadow transitions between idle and walking scales.")]
    [SerializeField] private float transitionSpeed = 12f;

    private Vector3 targetScale;

    private void Awake()
    {
        transform.localScale = idleScale;
        targetScale = idleScale;
    }

    private void Update()
    {
        if (characterRigidbody == null) return;

        // Verify if the character is currently moving horizontally
        bool isMoving = Mathf.Abs(characterRigidbody.linearVelocity.x) > 0.1f;

        if (isMoving)
        {
            // Stretch the X scale while keeping Y and Z at their idle values
            targetScale = new Vector3(walkingScaleX, idleScale.y, idleScale.z);
        }
        else
        {
            // Return to the base idle scale
            targetScale = idleScale;
        }

        // Smoothly interpolate to the target scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
    }
}