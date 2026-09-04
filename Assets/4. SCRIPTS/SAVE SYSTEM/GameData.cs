using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    [Header("Game Progression Data")]
    public GameManager.Chapter savedChapter;
    public int savedMemories;

    [Header("Player Data")]
    public string currentScene;
    public Vector3 playerPosition;

    [Header("Player Inventory")]
    public List<string> collectedKeys;

    [Header("World State Data")]
    public List<ObjectState> savedObjects;

    public GameData()
    {
        savedChapter = GameManager.Chapter.Prologue;
        savedMemories = 0;
        
        currentScene = "Level_01"; 
        playerPosition = Vector3.zero;
        savedObjects = new List<ObjectState>();
        collectedKeys = new List<string>();
    }
}

[System.Serializable]
public struct ObjectState
{
    public string objectID;
    public bool isActive;
}