using System.Collections.Generic;
using UnityEngine;

public class RevealerTool : MonoBehaviour
{
    public enum MaskType { ErasePermanently, RevealOnlyWhileInside }
    
    [Header("Reveal Settings")]
    public MaskType maskMechanic = MaskType.RevealOnlyWhileInside;
    
    [Range(0.1f, 10f)]
    public float fadeSpeed = 2.0f; 

    [Header("Advanced Mechanic Timings")]
    public float shadowRegrowthSpeed = 0.5f;
    public float trailExpansionSpeed = 2.0f;
    public float expansionDuration = 3.0f;

    [Header("Camera Shake Settings")]
    [Tooltip("Kekuatan guncangan kamera saat sedang mengikis lapisan gelap.")]
    [Range(0.01f, 0.5f)]
    public float shakeMagnitude = 0.03f;


    private Collider2D myCollider;
    private readonly List<Collider2D> overlappingColliders = new List<Collider2D>();
    private ContactFilter2D overlapFilter;
    private Vector3 lastPosition;
    private Vector3 lastScale;
    private Quaternion lastRotation;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
        {
            Debug.LogError("RevealerTool membutuhkan Collider2D pada GameObject yang sama.", this);
            enabled = false;
            return;
        }

        lastPosition = transform.position;
        lastScale = transform.lossyScale;
        lastRotation = transform.rotation;

        overlapFilter = new ContactFilter2D
        {
            useTriggers = true
        };
    }

    void LateUpdate()
    {
        if (myCollider == null || !myCollider.enabled)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 currentScale = transform.lossyScale;
        Quaternion currentRotation = transform.rotation;
        bool shapeOrPositionChanged =
            currentPosition != lastPosition ||
            currentScale != lastScale ||
            currentRotation != lastRotation;

        lastPosition = currentPosition;
        lastScale = currentScale;
        lastRotation = currentRotation;

        if (!shapeOrPositionChanged)
            return;

        // The revealer may be scaled by another Update script, so synchronize before querying overlaps.
        Physics2D.SyncTransforms();
        overlappingColliders.Clear();
        myCollider.Overlap(overlapFilter, overlappingColliders);

        foreach (Collider2D other in overlappingColliders)
        {
            DynamicMaskController mask = other.GetComponentInParent<DynamicMaskController>();
            if (mask == null)
                continue;

            mask.ApplyRevealFromCollider(
                myCollider,
                maskMechanic,
                fadeSpeed,
                shadowRegrowthSpeed,
                trailExpansionSpeed,
                expansionDuration);

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.05f, shakeMagnitude);
            }
        }
    }
}
