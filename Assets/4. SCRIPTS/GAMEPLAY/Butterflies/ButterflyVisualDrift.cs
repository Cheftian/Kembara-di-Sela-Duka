using UnityEngine;

public class ButterflyLightweight : MonoBehaviour
{
    [Header("Movement Settings")]
    public float radius = 2.0f;
    public float speed = 2.0f;

    [Header("Visual Orientation")]
    [Tooltip("Check this if your default sprite naturally faces right.")]
    public bool spriteFacesRight = true;

    private Vector3 targetLocalPosition;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (animator != null)
        {
            // Fast-forwards the animation by a random time at startup.
            // This desynchronizes multiple butterflies instantly with zero lag.
            animator.Update(Random.Range(0f, 5f));
        }

        PickNewTarget();
    }

    void Update()
    {
        // Move smoothly to the target position using low-cost MoveTowards
        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition, 
            targetLocalPosition, 
            speed * Time.deltaTime
        );

        // Check if the butterfly reached its destination
        if (transform.localPosition == targetLocalPosition)
        {
            PickNewTarget();
        }
    }

    void PickNewTarget()
    {
        // Generate a fast, lightweight random point inside a circle
        Vector2 randomCirclePoint = Random.insideUnitCircle * radius;
        targetLocalPosition = new Vector3(randomCirclePoint.x, randomCirclePoint.y, 0f);

        // Handle sprite flipping based on target direction
        Vector3 scale = transform.localScale;

        if (targetLocalPosition.x > transform.localPosition.x) // Moving Right
        {
            scale.x = spriteFacesRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        }
        else // Moving Left
        {
            scale.x = spriteFacesRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        }

        transform.localScale = scale;
    }
}
