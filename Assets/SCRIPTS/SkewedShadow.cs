using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SkewedShadow : MonoBehaviour
{
    [Header("Shadow Setup")]
    [Tooltip("Offset posisi bayangan dari titik tengah karakter (biasanya di bawah kaki)")]
    [SerializeField] private Vector3 shadowOffset = new Vector3(0f, -0.8f, 0f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.4f);

    [Header("Transformation")]
    [Tooltip("Sudut kemiringan bayangan (Z-Axis)")]
    [SerializeField] private float shadowAngle = -60f; 
    [Tooltip("Seberapa pipih bayangan tersebut (Y-Scale)")]
    [SerializeField] private float flattenScale = 0.5f; 

    private SpriteRenderer mainRenderer;
    private SpriteRenderer shadowRenderer;
    private Transform shadowTransform;

    private void Start()
    {
        mainRenderer = GetComponent<SpriteRenderer>();

        // 1. Create a dynamic GameObject for the shadow
        GameObject shadowObj = new GameObject("Shadow_Duplicate");
        shadowTransform = shadowObj.transform;

        // 2. Parent it to the character so it follows movement automatically
        shadowTransform.SetParent(transform);
        shadowTransform.localPosition = shadowOffset;

        // 3. Apply projection transformation (Rotate to fall flat, then squash)
        shadowTransform.localRotation = Quaternion.Euler(0f, 0f, shadowAngle);
        shadowTransform.localScale = new Vector3(1f, flattenScale, 1f);

        // 4. Configure visual renderer
        shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerName = mainRenderer.sortingLayerName;
        
        // Ensure shadow renders behind the character
        shadowRenderer.sortingOrder = mainRenderer.sortingOrder - 1; 
    }

    private void LateUpdate()
    {
        // 5. Synchronize animation frames and flip state every frame
        // Placed in LateUpdate to ensure it copies the sprite AFTER the Animator updates it
        shadowRenderer.sprite = mainRenderer.sprite;
        shadowRenderer.flipX = mainRenderer.flipX;
        shadowRenderer.flipY = mainRenderer.flipY;
    }
}