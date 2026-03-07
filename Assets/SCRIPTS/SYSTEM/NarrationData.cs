using UnityEngine;

[CreateAssetMenu(fileName = "NewNarration", menuName = "ZebaStudio/NarrationData")]
public class NarrationData : ScriptableObject
{
    [Tooltip("Tiap elemen adalah satu box percakapan")]
    [TextArea(3, 10)]
    public string[] dialogueLines;
}