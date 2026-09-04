using UnityEngine;

public class QTEMinigame : BaseMinigame
{
    [Header("QTE Settings")]
    [SerializeField] private QTECircle circlePrefab;
    [SerializeField] private RectTransform spawnArea;
    
    [Tooltip("Jumlah klik sukses berurutan untuk menang")]
    [SerializeField] private int requiredCombo = 5;
    [Tooltip("Jarak waktu antar kemunculan lingkaran (detik)")]
    [SerializeField] private float spawnInterval = 1.2f;

    private int currentCombo = 0;
    private float spawnTimer = 0f;

    public override void SetupMinigame(InteractableObject source)
    {
        base.SetupMinigame(source);
        currentCombo = 0;
        spawnTimer = 0f;
        
        // Membersihkan lingkaran yang mungkin tersisa dari permainan sebelumnya
        foreach (Transform child in spawnArea)
        {
            Destroy(child.gameObject);
        }
    }

    protected override void Update()
    {
        base.Update(); 
        
        // Cegah lingkaran muncul saat transisi narasi atau jika sudah menang
        if (isSolved || !canPlayPuzzle) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnCircle();
            spawnTimer = 0f;
        }
    }

    private void SpawnCircle()
    {
        QTECircle newCircle = Instantiate(circlePrefab, spawnArea);
        
        // Memberikan margin agar lingkaran tidak muncul terpotong di pinggir layar
        float margin = 75f; 
        float x = Random.Range((-spawnArea.rect.width / 2) + margin, (spawnArea.rect.width / 2) - margin);
        float y = Random.Range((-spawnArea.rect.height / 2) + margin, (spawnArea.rect.height / 2) - margin);
        
        newCircle.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        newCircle.Setup(this);
    }

    // Menerima laporan dari QTECircle yang ditekan/terlewat
    public void ReportHit(bool success)
    {
        if (isSolved) return;

        if (success)
        {
            currentCombo++;
            Debug.Log($"<color=green>HIT!</color> Combo: {currentCombo}/{requiredCombo}");

            if (currentCombo >= requiredCombo)
            {
                WinMinigame();
            }
        }
        else
        {
            currentCombo = 0; // Reset dari nol jika meleset
            Debug.Log("<color=red>MISS!</color> Combo kembali ke 0.");
        }
    }
}