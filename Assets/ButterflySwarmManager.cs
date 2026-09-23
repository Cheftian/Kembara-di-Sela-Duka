using UnityEngine;
using System.Linq; // TAMBAHKAN INI: Untuk penyaringan array yang aman dan instan

public class ButterflySwarmManager : MonoBehaviour
{
    [System.Serializable]
    public struct RoutePoint
    {
        [Tooltip("Target tujuan terbang untuk sisa kupu-kupu yang tidak melakukan Scatter.")]
        public Transform targetTransform;
        [Tooltip("Centang jika ingin memicu efek Scatter langsung di titik awal saat interaksi rute ini dimulai.")]
        public bool triggerScatterOnArrival;
        [Tooltip("Jumlah kupu-kupu yang akan langsung Scatter di tempat asal. Jika 0 atau melebihi jumlah yang ada, semua kupu-kupu akan Scatter.")]
        public int scatterCount;
    }

    [Header("Activation Control")]
    public KeyCode interactionKey = KeyCode.S;
    public string playerTag = "Player";

    [Header("Go To Target Route Settings")]
    [Tooltip("Daftar rute perjalanan swarm beserta pengaturan event scatter di setiap titik.")]
    public RoutePoint[] routeTargets;

    private ScatterButterfly[] butterflies;
    private int currentRouteIndex = 0;
    private bool isPlayerInside = false;
    private bool isSwarmFlying = false;
    private bool routeCompleted = false;

    void Start()
    {
        UpdateButterflyArray();
    }

    void Update()
    {
        if (routeCompleted || isSwarmFlying) return;

        if (isPlayerInside && Input.GetKeyDown(interactionKey))
        {
            TriggerSwarmAction();
        }
    }

    private void UpdateButterflyArray()
    {
        // Hanya mengambil kupu-kupu yang masih ada/belum dihancurkan
        butterflies = GetComponentsInChildren<ScatterButterfly>(true);
    }

    public Transform GetCurrentTargetTransform()
    {
        if (routeTargets != null && currentRouteIndex < routeTargets.Length)
        {
            return routeTargets[currentRouteIndex].targetTransform;
        }
        return null;
    }

    void TriggerSwarmAction()
    {
        if (routeTargets == null || routeTargets.Length == 0 || currentRouteIndex >= routeTargets.Length)
        {
            routeCompleted = true;
            return;
        }

        RoutePoint currentRoute = routeTargets[currentRouteIndex];
        UpdateButterflyArray(); 

        if (butterflies.Length == 0)
        {
            AdvanceRoute();
            return;
        }

        int countToScatter = 0;

        // 1. EKSEKUSI PEMISAHAN SWARM
        if (currentRoute.triggerScatterOnArrival)
        {
            countToScatter = currentRoute.scatterCount;
            if (countToScatter <= 0 || countToScatter > butterflies.Length)
            {
                countToScatter = butterflies.Length;
            }

            for (int i = 0; i < countToScatter; i++)
            {
                if (butterflies[i] != null)
                {
                    // Lepas dari hierarki manager agar tidak terhitung di rute selanjutnya
                    butterflies[i].transform.SetParent(null); 
                    butterflies[i].StartScatterFlight();
                }
            }
        }

        // 2. TERBANGKAN SISA KUPU-KUPU KE TARGET BARU
        if (currentRoute.targetTransform != null)
        {
            if (countToScatter < butterflies.Length)
            {
                isSwarmFlying = true;

                for (int i = countToScatter; i < butterflies.Length; i++)
                {
                    if (butterflies[i] != null)
                    {
                        butterflies[i].transform.SetParent(null);
                        butterflies[i].StartGoToTargetFlight(currentRoute.targetTransform.position);
                    }
                }
            }
            else
            {
                AdvanceRoute();
            }
        }
        else
        {
            AdvanceRoute();
        }

        // PERBAIKAN: Langsung bersihkan array saat ini juga menggunakan LINQ tanpa jeda Invoke
        butterflies = butterflies.Where(b => b != null && b.transform.parent == null && !b.HasArrivedAtTarget()).ToArray();
    }

    public void NotifySwarmArrival()
    {
        // Bersihkan data null terlebih dahulu secara aman sebelum dicek
        butterflies = butterflies.Where(b => b != null).ToArray();

        // Cek apakah ada kupu-kupu terbang yang BELUM sampai
        foreach (ScatterButterfly butterfly in butterflies)
        {
            if (!butterfly.HasArrivedAtTarget())
            {
                return; 
            }
        }

        RoutePoint currentRoute = routeTargets[currentRouteIndex];
        
        if (currentRoute.targetTransform != null)
        {
            transform.position = currentRoute.targetTransform.position;
        }

        // Tarik kembali sisa kupu-kupu yang selamat ke dalam parent manager
        foreach (ScatterButterfly butterfly in butterflies)
        {
            butterfly.AttachToParentAndNormalize(this.transform);
        }

        AdvanceRoute();
    }

    private void AdvanceRoute()
    {
        currentRouteIndex++;
        isSwarmFlying = false;

        if (currentRouteIndex >= routeTargets.Length)
        {
            routeCompleted = true;
            Debug.Log("Swarm telah mencapai rute terakhir.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag)) isPlayerInside = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag)) isPlayerInside = false;
    }
}
