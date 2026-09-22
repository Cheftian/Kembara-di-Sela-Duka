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

    private Vector3 startPosition;
    private float randomOffset;
    private float adjustedSpeed;

    void Start()
    {
        // Store the exact position where you placed the island in the scene
        startPosition = transform.position;

        // Generate a random time offset so islands don't start at the exact same point in the wave
        randomOffset = Random.Range(0f, 100f);

        // Slightly randomize the speed for this specific island
        adjustedSpeed = hoverSpeed + Random.Range(-speedRandomizationRange, speedRandomizationRange);
    }

    void Update()
    {
        // Calculate the new Y position using a Sine wave
        // Multiplying Time.time by adjustedSpeed changes the pace, + randomOffset desynchronizes it
        float newY = startPosition.y + Mathf.Sin((Time.time * adjustedSpeed) + randomOffset) * hoverAmplitude;

        // Apply the new position while keeping X and Z exactly the same
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}
