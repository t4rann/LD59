using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Signal Table/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;
    public int roundsCount = 5;
    public int anteAmount = 10;
    public int chipsForLevel = 100;      // Фишки игрока на уровень
    public int npcStartingChips = 100;   // Стартовые фишки NPC на уровень
    public int raiseAmount = 10;
    public bool isFinalLevel = false;     // Финальный уровень (босс)
    public bool isPlayerInvolved = true;  // Участвует ли игрок в уровне
    
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