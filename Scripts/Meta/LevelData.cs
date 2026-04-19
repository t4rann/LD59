using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Signal Table/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;
    public int roundsCount = 5;
    public int anteAmount = 10;
    
    [Header("NPCs in Level")]
    public List<NPCSetup> npcSetups = new List<NPCSetup>();
}

[System.Serializable]
public class NPCSetup
{
    public GameObject npcPrefab;
    public NPCBehaviour npcBrain;
    public TransformPoint spawnPoint;
}