using UnityEngine;

[CreateAssetMenu(fileName = "NewObjective", menuName = "ZebaStudio/ObjectiveData")]
public class ObjectiveData : ScriptableObject
{
    public string objectiveID; // ID unik untuk melacak objektif spesifik (misal: "misi_ambil_kunci")
    
    [TextArea(3, 5), Header("Bahasa Inggris")] 
    public string objectiveEN;

    [TextArea(3, 5), Header("Bahasa Indonesia")] 
    public string objectiveIDN;
}
