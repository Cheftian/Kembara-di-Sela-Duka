using UnityEngine;

public class SceneTriggerButton : MonoBehaviour
{
    [SerializeField] private string transitionToUse;
    [SerializeField] private string targetSceneName;

    // Fungsi ini hanya memiliki 0 parameter, sehingga PASTI muncul di Button OnClick()
    public void TriggerChangeScene()
    {
        if (SceneController.Instance != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM(); 
            }
            SceneController.Instance.ChangeSceneWithLoading(transitionToUse, targetSceneName);
        }
    }
}
