using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    public enum TriggerMode { OnTriggerEnter, OnObjectEnable }

    [Header("Activation Settings")]
    [SerializeField] private TriggerMode activationMode = TriggerMode.OnTriggerEnter;

    [Header("Camera Settings to Apply")]
    [SerializeField] private float targetCameraSize = 5f;
    [SerializeField] private bool enableManualControl = false;
    [SerializeField] private Transform focusTarget; // Isi jika ingin kamera pindah fokus dari player ke objek lain
    
    private CameraController cameraCtrl;

    private void Awake()
    {
        cameraCtrl = Camera.main.GetComponent<CameraController>();
        if (GetComponent<BoxCollider2D>()) GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        if (activationMode == TriggerMode.OnObjectEnable) ExecuteTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activationMode == TriggerMode.OnTriggerEnter && other.CompareTag("Player"))
        {
            ExecuteTrigger();
        }
    }

    private void ExecuteTrigger()
    {
        if (cameraCtrl == null) return;

        // Jika focusTarget kosong, tetap ikuti target kamera sebelumnya (biasanya player)
        Transform finalTarget = (focusTarget != null) ? focusTarget : null; 
        
        if (finalTarget != null)
            cameraCtrl.SetTarget(finalTarget);
            
        cameraCtrl.SetCameraSize(targetCameraSize);
        cameraCtrl.SetManualControl(enableManualControl);
    }
}