using UnityEngine;

public class DynamicMaskController : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureResolution = 64; 

    private Texture2D maskTexture;
    private Material foregroundMaterial;
    private SpriteRenderer spriteRenderer;
    private Color[] currentPixels;
    private Color[] processedPixels;
    
    // Array mencatat sisa durasi ekspansi per pixel
    private float[] pixelExpansionTimers;
    
    private RevealerTool.MaskType activeMechanicMode = RevealerTool.MaskType.RevealOnlyWhileInside;
    private float runtimeRegrowthSpeed = 0.5f;
    private float runtimeExpansionSpeed = 2.0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        foregroundMaterial = spriteRenderer.material;

        maskTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        maskTexture.filterMode = FilterMode.Point;
        maskTexture.wrapMode = TextureWrapMode.Clamp;

        currentPixels = new Color[textureResolution * textureResolution];
        processedPixels = new Color[textureResolution * textureResolution];
        pixelExpansionTimers = new float[textureResolution * textureResolution];
        
        ResetMaskToWhite();
        foregroundMaterial.SetTexture("_MaskTex", maskTexture);
    }

    void Update()
    {
        currentPixels = maskTexture.GetPixels();
        System.Array.Copy(currentPixels, processedPixels, currentPixels.Length);
        bool textureNeedsApply = false;

        // Kurangi semua timer pixel yang aktif di frame ini
        for (int i = 0; i < pixelExpansionTimers.Length; i++)
        {
            if (pixelExpansionTimers[i] > 0)
            {
                pixelExpansionTimers[i] -= Time.deltaTime;
            }
        }

        // --- MEKANIK A: SHADOW REGROWTH (MENUTUP KEMBALI) ---
        if (activeMechanicMode == RevealerTool.MaskType.RevealOnlyWhileInside)
        {
            for (int i = 0; i < currentPixels.Length; i++)
            {
                if (currentPixels[i].r < 1f)
                {
                    float newValue = currentPixels[i].r + (runtimeRegrowthSpeed * Time.deltaTime);
                    newValue = Mathf.Clamp01(newValue);
                    processedPixels[i] = new Color(newValue, newValue, newValue, 1f);
                    textureNeedsApply = true;
                }
            }
        }
        // --- MEKANIK B: EXPANSION TRAIL PADAT DENGAN TIMER TETAP ---
        else if (activeMechanicMode == RevealerTool.MaskType.ErasePermanently)
        {
            float fillAmount = runtimeExpansionSpeed * Time.deltaTime;

            for (int y = 1; y < textureResolution - 1; y++)
            {
                for (int x = 1; x < textureResolution - 1; x++)
                {
                    int index = y * textureResolution + x;
                    
                    if (currentPixels[index].r > 0.05f)
                    {
                        int r = index + 1;
                        int l = index - 1;
                        int u = (y + 1) * textureResolution + x;
                        int d = (y - 1) * textureResolution + x;

                        // PENTING: Cek apakah tetangga sudah terhapus DAN timernya masih aktif
                        bool canExpand = 
                            (currentPixels[r].r < 0.95f && pixelExpansionTimers[r] > 0) ||
                            (currentPixels[l].r < 0.95f && pixelExpansionTimers[l] > 0) ||
                            (currentPixels[u].r < 0.95f && pixelExpansionTimers[u] > 0) ||
                            (currentPixels[d].r < 0.95f && pixelExpansionTimers[d] > 0);

                        if (canExpand)
                        {
                            float newV = currentPixels[index].r - fillAmount;
                            newV = Mathf.Clamp01(newV);
                            processedPixels[index] = new Color(newV, newV, newV, 1f);
                            
                            // Ambil sisa waktu dari tetangga terdekat, jangan membuat waktu baru/mereset durasi!
                            float maxNeighborTimer = Mathf.Max(pixelExpansionTimers[r], pixelExpansionTimers[l], pixelExpansionTimers[u], pixelExpansionTimers[d]);
                            pixelExpansionTimers[index] = maxNeighborTimer; 
                            
                            textureNeedsApply = true;
                        }
                    }
                }
            }
        }

        if (textureNeedsApply)
        {
            maskTexture.SetPixels(processedPixels);
            maskTexture.Apply();
        }
    }

    public void ApplyRevealFromCollider(Collider2D revealerCollider, RevealerTool.MaskType mechanicType, float fadeSpeed, float regrowthSpeed, float expansionSpeed, float expansionDuration)
    {
        activeMechanicMode = mechanicType;
        runtimeRegrowthSpeed = regrowthSpeed;
        runtimeExpansionSpeed = expansionSpeed;

        Bounds bounds = spriteRenderer.bounds;
        Bounds revealerBounds = revealerCollider.bounds;

        int minX = Mathf.Clamp((int)(((revealerBounds.min.x - bounds.min.x) / bounds.size.x) * textureResolution), 0, textureResolution);
        int maxX = Mathf.Clamp((int)(((revealerBounds.max.x - bounds.min.x) / bounds.size.x) * textureResolution), 0, textureResolution);
        int minY = Mathf.Clamp((int)(((revealerBounds.min.y - bounds.min.y) / bounds.size.y) * textureResolution), 0, textureResolution);
        int maxY = Mathf.Clamp((int)(((revealerBounds.max.y - bounds.min.y) / bounds.size.y) * textureResolution), 0, textureResolution);

        bool changed = false;
        int step = 2; 

        for (int y = minY; y < maxY; y += step)
        {
            for (int x = minX; x < maxX; x += step)
            {
                float worldX = bounds.min.x + ((float)x / textureResolution) * bounds.size.x;
                float worldY = bounds.min.y + ((float)y / textureResolution) * bounds.size.y;
                Vector2 pixelWorldPos = new Vector2(worldX, worldY);

                if (revealerCollider.OverlapPoint(pixelWorldPos))
                {
                    int index = y * textureResolution + x;
                    Color newColor = new Color(0f, 0f, 0f, 1f);

                    for (int sy = 0; sy < step; sy++)
                    {
                        for (int sx = 0; sx < step; sx++)
                        {
                            int targetX = x + sx;
                            int targetY = y + sy;
                            if (targetX < textureResolution && targetY < textureResolution)
                            {
                                int targetIndex = targetY * textureResolution + targetX;
                                maskTexture.SetPixel(targetX, targetY, newColor);
                                
                                // HANYA pixel yang disentuh langsung oleh collider yang berhak memicu durasi awal ekspansi
                                pixelExpansionTimers[targetIndex] = expansionDuration;
                            }
                        }
                    }
                    changed = true;
                }
            }
        }

        if (changed)
        {
            maskTexture.Apply();
        }
    }

    private void ResetMaskToWhite()
    {
        Color[] whitePixels = new Color[textureResolution * textureResolution];
        for (int i = 0; i < whitePixels.Length; i++)
        {
            whitePixels[i] = Color.white;
            pixelExpansionTimers[i] = 0f;
        }
        maskTexture.SetPixels(whitePixels);
        maskTexture.Apply();
    }
}
