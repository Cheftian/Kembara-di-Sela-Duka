using System.Collections;
using UnityEngine;

public class ObstacleTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Masukkan CameraController spesifik yang mengontrol kamera area ini")]
    [SerializeField] private CameraController targetCameraController;
    [SerializeField] private ChaseObstacle obstacle;
    [SerializeField] private string playerTag = "Player";

    [Header("Cutscene Settings")]
    [Tooltip("Berapa lama kamera menetap di Obstacle sebelum kembali ke Player")]
    [SerializeField] private float durationFocusOnObstacle = 2f;
    [Tooltip("Jeda waktu setelah kamera kembali ke player sebelum obstacle mulai bergerak")]
    [SerializeField] private float delayBeforeObstacleMoves = 0.5f;

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isTriggered && collision.CompareTag(playerTag))
        {
            isTriggered = true;
            StartCoroutine(CameraCutsceneSequence(collision.transform));
        }
    }

    private IEnumerator CameraCutsceneSequence(Transform playerTransform)
    {
        if (targetCameraController == null || obstacle == null) yield break;

        // 1. Kunci input kontrol Player secara mandiri
        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        if (playerCtrl != null)
        {
            playerCtrl.BlockInput = true;
            playerCtrl.ResetToIdleState(); // Mengembalikan pose ke animasi diam
        }

        // 2. Hubungkan obstacle dengan CameraController
        obstacle.AssignCamera(targetCameraController);

        // 3. Ganti target kamera ke Obstacle
        targetCameraController.SetTarget(obstacle.transform);

        // 4. Aktifkan mode guncang khusus di Obstacle
        obstacle.EnableCameraShakeOnly(true);

        // Jeda selama kamera fokus melihat ke Obstacle
        yield return new WaitForSeconds(durationFocusOnObstacle);

        // 5. Matikan guncangan sebelum kamera kembali
        obstacle.EnableCameraShakeOnly(false);

        // 6. Kembalikan target kamera ke Player
        targetCameraController.SetTarget(playerTransform);

        // Jeda sedikit agar transisi kamera kembali selesai secara mulus
        yield return new WaitForSeconds(delayBeforeObstacleMoves);

        // 7. Buka kembali input kontrol Player
        if (playerCtrl != null)
        {
            playerCtrl.BlockInput = false;
        }

        // 8. Jalankan Obstacle secara horizontal
        obstacle.StartChase();
    }
    private void OnEnable()
    {
        // Otomatis berjalan saat script dimatikan lalu dinyalakan kembali oleh RoomManager
        isTriggered = false;
    }

}
