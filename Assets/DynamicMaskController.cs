using UnityEngine;
using System.Collections;

public class DynamicMaskController : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureResolution = 128; 

    [Header("Auto Clear Settings")]
    [Range(10f, 95f)]
    [Tooltip("Jika area terhapus mencapai persentase ini, reaksi pembersihan otomatis ke seluruh sprite akan terpicu.")]
    public float autoClearThresholdPercent = 80f;
    
    [Tooltip("Seberapa cepat penyebaran sisa area saat auto-clear terjadi. (Direkomendasikan diisi nilai yang sama/mirip dengan Fade Speed di Revealer).")]
    public float autoClearSpeed = 4.0f;

    private Texture2D maskTexture;
    private Material foregroundMaterial;
    private SpriteRenderer spriteRenderer;
    private Color[] currentPixels;
    private Color[] processedPixels;
    private float[] pixelExpansionTimers;
    
    private RevealerTool.MaskType activeMechanicMode = RevealerTool.MaskType.RevealOnlyWhileInside;
    private float runtimeRegrowthSpeed = 0.5f;
    private float runtimeExpansionSpeed = 2.0f;
    
    private int totalPixels;
    private bool isSystemActive = true;
    private bool isCheckingPercent = false;
    
    // Status penanda apakah fase pembersihan otomatis sedang berjalan
    private bool isAutoClearingPhase = false; 

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        foregroundMaterial = spriteRenderer.material;

        totalPixels = textureResolution * textureResolution;

        maskTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        maskTexture.filterMode = FilterMode.Point;
        maskTexture.wrapMode = TextureWrapMode.Clamp;

        currentPixels = new Color[totalPixels];
        processedPixels = new Color[totalPixels];
        pixelExpansionTimers = new float[totalPixels];
        
        ResetMaskToWhite();
        foregroundMaterial.SetTexture("_MaskTex", maskTexture);
    }

    void Update()
    {
        if (!isSystemActive) return;

        currentPixels = maskTexture.GetPixels();
        System.Array.Copy(currentPixels, processedPixels, currentPixels.Length);
        bool textureNeedsApply = false;

        // --- UPDATE TIMER PIXEL EXPANSION ---
        for (int i = 0; i < pixelExpansionTimers.Length; i++)
        {
            if (pixelExpansionTimers[i] > 0)
            {
                pixelExpansionTimers[i] -= Time.deltaTime;
            }
        }

        // --- FASE BARU: AUTO CLEAR SPREADING (PEMBERSIHAN MASSAL TANPA COLLIDER) ---
        if (isAutoClearingPhase)
        {
            float fillAmount = autoClearSpeed * Time.deltaTime;
            int fullyErasedCount = 0;

            for (int y = 1; y < textureResolution - 1; y++)
            {
                for (int x = 1; x < textureResolution - 1; x++)
                {
                    int index = y * textureResolution + x;
                    
                    if (currentPixels[index].r > 0f)
                    {
                        int r = index + 1; int l = index - 1;
                        int u = (y + 1) * textureResolution + x;
                        int d = (y - 1) * textureResolution + x;

                        // Cek apakah ada tetangga sekitar yang sudah mulai terhapus (r < 0.95f)
                        bool nearErased = currentPixels[r].r < 0.95f || currentPixels[l].r < 0.95f ||
                                          currentPixels[u].r < 0.95f || currentPixels[d].r < 0.95f;

                        if (nearErased)
                        {
                            float newV = currentPixels[index].r - fillAmount;
                            newV = Mathf.Clamp01(newV);
                            processedPixels[index] = new Color(newV, newV, newV, 1f);
                            textureNeedsApply = true;
                        }
                    }
                    else
                    {
                        fullyErasedCount++;
                    }
                }
            }

            // Jika seluruh pixel di dalam tekstur sudah berubah menjadi hitam (0) mutlak
            if (fullyErasedCount >= (textureResolution - 2) * (textureResolution - 2))
            {
                FinalizeDestroy();
            }
        }
        // --- MEKANIK STANDAR A: SHADOW REGROWTH (MENUTUP KEMBALI) ---
        else if (activeMechanicMode == RevealerTool.MaskType.RevealOnlyWhileInside)
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
        // --- MEKANIK STANDAR B: EXPANSION TRAIL NORMAL ---
        else if (activeMechanicMode == RevealerTool.MaskType.ErasePermanently)
        {
            float fillAmount = (runtimeExpansionSpeed * 0.5f) * Time.deltaTime;

            for (int y = 1; y < textureResolution - 1; y++)
            {
                for (int x = 1; x < textureResolution - 1; x++)
                {
                    int index = y * textureResolution + x;
                    
                    if (currentPixels[index].r > 0.01f)
                    {
                        int r = index + 1; int l = index - 1;
                        int u = (y + 1) * textureResolution + x;
                        int d = (y - 1) * textureResolution + x;

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

            // Pemicu cek persentase berkala (hanya aktif jika belum masuk fase auto-clear)
            if (!isCheckingPercent && !isAutoClearingPhase && activeMechanicMode == RevealerTool.MaskType.ErasePermanently)
            {
                StartCoroutine(CheckRevealPercentageRoutine());
            }
        }
    }

    IEnumerator CheckRevealPercentageRoutine()
    {
        isCheckingPercent = true;
        yield return new WaitForSeconds(0.2f); 

        int erasedPixels = 0;
        for (int i = 0; i < currentPixels.Length; i++)
        {
            if (currentPixels[i].r < 0.1f)
            {
                erasedPixels++;
            }
        }

        float currentPercent = ((float)erasedPixels / totalPixels) * 100f;

        if (currentPercent >= autoClearThresholdPercent)
        {
            // AKTIFKAN REAKSI BERANTAI PEMBERSIHAN
            isAutoClearingPhase = true; 
            Debug.Log("Target kebersihan tercapai! Memulai reaksi berantai pembersihan sisa area...");
        }

        isCheckingPercent = false;
    }

    private void FinalizeDestroy()
    {
        isSystemActive = false;
        StopAllCoroutines();
        Destroy(gameObject); 
        Debug.Log("Lapisan gelap terkikis habis secara organik! Objek dihancurkan.");
    }

    public void ApplyRevealFromCollider(Collider2D revealerCollider, RevealerTool.MaskType mechanicType, float fadeSpeed, float regrowthSpeed, float expansionSpeed, float expansionDuration)
    {
        // Jika sudah masuk fase auto-clear, abaikan semua input sentuhan dari Revealer luar
        if (!isSystemActive || isAutoClearingPhase) return;

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

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                float worldX = bounds.min.x + ((float)x / textureResolution) * bounds.size.x;
                float worldY = bounds.min.y + ((float)y / textureResolution) * bounds.size.y;
                Vector2 pixelWorldPos = new Vector2(worldX, worldY);

                if (revealerCollider.OverlapPoint(pixelWorldPos))
                {
                    int index = y * textureResolution + x;
                    
                    // Coret instan pixel sentuhan utama menjadi transparan
maskTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 1f));pixelExpansionTimers[index] = expansionDuration;changed = true;}}}if (changed){maskTexture.Apply();}}private void ResetMaskToWhite(){Color[] whitePixels = new Color[totalPixels];for (int i = 0; i < whitePixels.Length; i++){whitePixels[i] = Color.white;pixelExpansionTimers[i] = 0f;}maskTexture.SetPixels(whitePixels);maskTexture.Apply();}}