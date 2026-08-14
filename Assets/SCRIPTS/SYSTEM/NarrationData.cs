using UnityEngine;

[CreateAssetMenu(fileName = "NewNarration", menuName = "ZebaStudio/NarrationData")]
public class NarrationData : ScriptableObject
{
    [System.Serializable]
    public struct DialogueStep
    {
        public string characterName; 
        public string expressionName; 
        
        [TextArea(3, 5), Header("Bahasa Inggris")] 
        public string dialogueEN;

        [TextArea(3, 5), Header("Bahasa Indonesia")] 
        public string dialogueID;
    }

    [Tooltip("Daftar urutan percakapan dan karakternya")]
    public DialogueStep[] dialogueSteps;
}
