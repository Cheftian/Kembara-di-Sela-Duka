using UnityEngine;

[CreateAssetMenu(fileName = "NewNarration", menuName = "ZebaStudio/NarrationData")]
public class NarrationData : ScriptableObject
{
    [System.Serializable]
    public struct DialogueStep
    {
        public string characterName; // Harus pas dengan nama di NarrationManager
        public string expressionName; // Harus pas dengan nama ekspresi di list
        [TextArea(3, 10)] public string dialogueText;
    }

    [Tooltip("Daftar urutan percakapan dan karakternya")]
    public DialogueStep[] dialogueSteps;
}
