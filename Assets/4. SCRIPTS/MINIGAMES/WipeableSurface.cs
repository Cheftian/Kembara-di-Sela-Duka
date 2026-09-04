using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class WipeableSurface : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Settings")]
    public int brushRadius = 20;
    // Nilai Range sudah disesuaikan agar tidak membatasi 10% lagi
    [Range(0f, 0.1f)] public float WinThreshold = 0.09f;

    [Header("Visuals")]
    [SerializeField] private RectTransform brushVisual;

    private RawImage rawImage;
    private Texture2D wipeTexture;
    private int totalPixels;
    private int clearedPixels = 0;
    private bool isDone = false;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        InitializeTexture();
    }

    private void Start()
    {
        // Pastikan kuas mati di awal
        if (brushVisual != null) brushVisual.gameObject.SetActive(false);
    }

    private void InitializeTexture()
    {
        Texture2D originalTex = (Texture2D)rawImage.texture;
        if (originalTex == null) return;

        wipeTexture = new Texture2D(originalTex.width, originalTex.height, originalTex.format, false);
        wipeTexture.SetPixels(originalTex.GetPixels());
        wipeTexture.Apply();

        rawImage.texture = wipeTexture;
        totalPixels = wipeTexture.width * wipeTexture.height;
    }

    // --- LOGIKA VISUAL KUAS (TANPA PERUBAHAN UKURAN) ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (brushVisual != null && !isDone) brushVisual.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (brushVisual != null) brushVisual.gameObject.SetActive(false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateBrushPosition(eventData);
    }

    private void UpdateBrushPosition(PointerEventData eventData)
    {
        if (brushVisual != null && brushVisual.gameObject.activeSelf)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)brushVisual.parent, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPoint);
                
            brushVisual.localPosition = localPoint;
        }
    }

    // --- LOGIKA PENGHAPUSAN ---

    public float GetProgress()
    {
        if (totalPixels == 0) return 0;
        return (float)clearedPixels / totalPixels;
    }

    public void FinalizeWipe()
    {
        isDone = true;
        if (brushVisual != null) brushVisual.gameObject.SetActive(false);

        Color[] clearColors = new Color[totalPixels];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
        wipeTexture.SetPixels(clearColors);
        wipeTexture.Apply();
    }

    public void OnPointerDown(PointerEventData eventData) 
    {
        UpdateBrushPosition(eventData);
        HandleWipe(eventData);
    }

    public void OnDrag(PointerEventData eventData) 
    {
        UpdateBrushPosition(eventData);
        HandleWipe(eventData);
    }

    private void HandleWipe(PointerEventData eventData)
    {
        WipingMinigame parent = GetComponentInParent<WipingMinigame>();
        if (parent != null && !parent.CanPlay) return;

        RectTransform rectTransform = rawImage.rectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            Vector2 pivot = rectTransform.pivot;
            Vector2 normalizedPoint = new Vector2((localPoint.x / rectTransform.rect.width) + pivot.x, (localPoint.y / rectTransform.rect.height) + pivot.y);
            ErasePixels(Mathf.RoundToInt(normalizedPoint.x * wipeTexture.width), Mathf.RoundToInt(normalizedPoint.y * wipeTexture.height));
        }
    }

    private void ErasePixels(int x, int y)
    {
        bool changed = false;
        for (int i = -brushRadius; i <= brushRadius; i++)
        {
            for (int j = -brushRadius; j <= brushRadius; j++)
            {
                if (i * i + j * j <= brushRadius * brushRadius)
                {
                    int px = x + i; int py = y + j;
                    if (px >= 0 && px < wipeTexture.width && py >= 0 && py < wipeTexture.height)
                    {
                        if (wipeTexture.GetPixel(px, py).a > 0.1f)
                        {
                            wipeTexture.SetPixel(px, py, Color.clear);
                            clearedPixels++;
                            changed = true;
                        }
                    }
                }
            }
        }
        if (changed) wipeTexture.Apply();
    }
}