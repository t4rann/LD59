using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private List<TransformPoint> spawnPoints = new List<TransformPoint>();
    [SerializeField] private Transform npcContainer;
    
    private List<NPCController> spawnedNPCs = new List<NPCController>();
    private int currentNPCChipsAmount = 100;
    private LevelData currentLevelData;
    
    public List<NPCController> LoadLevel(LevelData levelData, TableController table)
    {
        ClearCurrentNPCs(table);
        
        if (levelData.npcSetups.Count > spawnPoints.Count)
        {
            Debug.LogError($"Не хватает точек спавна! Нужно {levelData.npcSetups.Count}, есть {spawnPoints.Count}");
            return null;
        }
        
        currentLevelData = levelData;
        currentNPCChipsAmount = levelData.npcStartingChips;
        
        GameLoop gameLoop = FindFirstObjectByType<GameLoop>();
        if (gameLoop != null)
        {
            gameLoop.SetMaxRounds(levelData.roundsCount);
            gameLoop.SetAnteAmount(levelData.anteAmount);
        }
        
        for (int i = 0; i < levelData.npcSetups.Count; i++)
        {
            NPCSetup setup = levelData.npcSetups[i];
            TransformPoint point = spawnPoints[i];
            
            if (point.isOccupied) 
            {
                Debug.LogWarning($"Точка спавна {i} уже занята, пропускаем");
                continue;
            }
            
            GameObject npcObj = Instantiate(setup.npcPrefab, point.transform.position, 
                                           point.transform.rotation, npcContainer);
            
            NPCController npc = npcObj.GetComponent<NPCController>();
            if (npc != null)
            {
                if (setup.npcBrain != null)
                {
                    var method = typeof(NPCController).GetMethod("SetBrain", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(npc, new object[] { setup.npcBrain });
                    }
                    else
                    {
                        var field = typeof(NPCController).GetField("brain", 
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(npc, setup.npcBrain);
                        }
                    }
                }
                
                NPCChips chips = npc.GetComponent<NPCChips>();
                if (chips == null)
                {
                    chips = npc.gameObject.AddComponent<NPCChips>();
                }
                
                chips.SetStartingChips(currentNPCChipsAmount);
                
                npc.ResetToNeutral();
                npc.ResetConsecutiveFolds();
                
                var visualController = npc.GetComponent<NPCVisualController>();
                if (visualController != null)
                {
                    var method = typeof(NPCVisualController).GetMethod("UpdateChipsDisplay");
                    if (method != null)
                    {
                        method.Invoke(visualController, new object[] { npc, currentNPCChipsAmount });
                    }
                }
                
                spawnedNPCs.Add(npc);
                point.isOccupied = true;
                
                if (table != null)
                {
                    table.AddNPC(npc);
                }
                
                Debug.Log($"Спавн NPC: {npc.npcName} с {currentNPCChipsAmount} фишками");
            }
            else
            {
                Debug.LogError($"Префаб {setup.npcPrefab.name} не содержит компонент NPCController!");
                Destroy(npcObj);
            }
        }
        
        Debug.Log($"Загружено NPC: {spawnedNPCs.Count}");
        return spawnedNPCs;
    }
    
    public void ClearCurrentNPCs(TableController table = null)
    {
        if (table != null)
        {
            var allNPCs = table.GetAllNPCs();
            for (int i = allNPCs.Count - 1; i >= 0; i--)
            {
                var npc = allNPCs[i];
                if (npc != null)
                {
                    table.RemoveNPC(npc);
                }
            }
        }
        
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null && npc.gameObject != null)
            {
                Destroy(npc.gameObject);
            }
        }
        spawnedNPCs.Clear();
        
        foreach (var point in spawnPoints)
        {
            point.isOccupied = false;
        }
        
        Debug.Log("Все NPC очищены");
    }
    
    public void ReloadCurrentLevel(TableController table)
    {
        if (currentLevelData != null)
        {
            Debug.Log("Перезагрузка текущего уровня...");
            ClearCurrentNPCs(table);
            LoadLevel(currentLevelData, table);
        }
        else
        {
            Debug.LogWarning("Нет данных о текущем уровне для перезагрузки");
        }
    }
    
    public int GetCurrentNPCChipsAmount()
    {
        return currentNPCChipsAmount;
    }
    
    public void SetCurrentNPCChipsAmount(int amount)
    {
        currentNPCChipsAmount = amount;
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null)
            {
                var chips = npc.GetComponent<NPCChips>();
                if (chips != null)
                {
                    chips.SetStartingChips(currentNPCChipsAmount);
                }
            }
        }
    }
    
    public List<NPCController> GetSpawnedNPCs()
    {
        return spawnedNPCs;
    }
}