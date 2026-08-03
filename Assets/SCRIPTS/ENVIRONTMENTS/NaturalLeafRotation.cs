using UnityEngine;

public class NaturalLeafRotation : MonoBehaviour
{
    [Tooltip("The maximum angle the leaf can rotate to the left or right.")]
    public float maxSwingAngle = 15f;

    [Tooltip("The base speed of the swinging motion.")]
    public float swingSpeed = 2f;

    [Tooltip("The amount of randomness applied to simulate natural wind.")]
    public float windRandomness = 0.5f;

    private float randomOffset;
    private Quaternion startRotation;

    void Start()
    {
        // Save the initial rotation of the object
        startRotation = transform.rotation;

        // Generate a random time offset so multiple leaves do not swing in perfect sync
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = Time.time;
        
        // Create a base rhythmic swinging motion between -1 and 1
        float sineWave = Mathf.Sin(time * swingSpeed + randomOffset);
        
        // Generate an organic random value between -1 and 1 for the wind effect
        float noise = Mathf.PerlinNoise(time * windRandomness, randomOffset) * 2f - 1f;

        // Combine the sine wave and noise, then apply the maximum angle limit
        float currentAngle = (sineWave + noise) * maxSwingAngle * 0.5f;

        // Apply the rotation on the Z-axis relative to the starting rotation
        transform.rotation = startRotation * Quaternion.Euler(0f, 0f, currentAngle);
    }
}