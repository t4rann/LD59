using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private List<TransformPoint> spawnPoints = new List<TransformPoint>();
    [SerializeField] private Transform npcContainer;
    
    private List<NPCController> spawnedNPCs = new List<NPCController>();
    private int currentNPCChipsAmount = 100;
    private LevelData currentLevelData;
    private int currentLevelIndex = 0;
    private List<LevelData> allLevels = new List<LevelData>();
    
    public void SetAllLevels(List<LevelData> levels)
    {
        allLevels = levels;
        Debug.Log($"[LevelController] Установлено {levels.Count} уровней");
    }
    
    public void SetAllLevels(LevelData[] levels)
    {
        allLevels.Clear();
        allLevels.AddRange(levels);
        Debug.Log($"[LevelController] Установлено {levels.Length} уровней из массива");
    }
    
    public int GetLevelsCount()
    {
        return allLevels.Count;
    }
    
    public bool IsLastLevel()
    {
        bool isLast = currentLevelIndex >= allLevels.Count - 1;
        Debug.Log($"[LevelController] Проверка последнего уровня: {isLast} (индекс {currentLevelIndex} из {allLevels.Count})");
        return isLast;
    }
    
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }
    
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }
    
    public LevelData GetLevelData(int index)
    {
        if (index >= 0 && index < allLevels.Count)
            return allLevels[index];
        return null;
    }
    
    public List<NPCController> LoadLevelByIndex(int index, TableController table)
    {
        if (index < 0 || index >= allLevels.Count)
        {
            Debug.LogError($"[LevelController] Неверный индекс уровня: {index} (всего {allLevels.Count})");
            return null;
        }
        
        currentLevelIndex = index;
        currentLevelData = allLevels[index];
        
        Debug.Log($"[LevelController] Загрузка уровня {index + 1}/{allLevels.Count}: {currentLevelData.levelName}");
        
        // Сбрасываем фишки игрока при загрузке нового уровня
        ResetPlayerChips();
        
        return LoadLevel(currentLevelData, table);
    }
    
private void ResetPlayerChips()
{
    PlayerChipsVisualController playerChipsVisual = FindFirstObjectByType<PlayerChipsVisualController>();
    if (playerChipsVisual != null)
    {
        playerChipsVisual.ResetForNewLevel();
        Debug.Log("[LevelController] Сброшен визуал фишек игрока");
        
        // Дополнительно принудительно обновляем через кадр
        StartCoroutine(ForceUpdateChipsNextFrame(playerChipsVisual));
    }
    
    // Также сбрасываем сами фишки игрока
    PlayerChips playerChips = FindFirstObjectByType<PlayerChips>();
    if (playerChips != null && currentLevelData != null)
    {
        playerChips.SetChipsForLevel(currentLevelData.chipsForLevel);
        Debug.Log($"[LevelController] Установлено {currentLevelData.chipsForLevel} фишек игроку");
    }
}

private IEnumerator ForceUpdateChipsNextFrame(PlayerChipsVisualController visual)
{
    yield return null;
    yield return null;
    
    if (visual != null)
    {
        visual.ForceUpdateVisuals();
    }
}
    
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
                // Установка мозга NPC
                if (setup.npcBrain != null)
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
                
                // Установка фишек
                NPCChips chips = npc.GetComponent<NPCChips>();
                if (chips == null)
                {
                    chips = npc.gameObject.AddComponent<NPCChips>();
                }
                chips.SetStartingChips(currentNPCChipsAmount);
                
                npc.ResetToNeutral();
                npc.ResetConsecutiveFolds();
                
                spawnedNPCs.Add(npc);
                point.isOccupied = true;
                
                if (table != null)
                {
                    table.AddNPC(npc);
                }
                
                Debug.Log($"[LevelController] Спавн NPC: {npc.npcName} с {currentNPCChipsAmount} фишками");
            }
            else
            {
                Debug.LogError($"Префаб {setup.npcPrefab.name} не содержит NPCController!");
                Destroy(npcObj);
            }
        }
        
        Debug.Log($"[LevelController] Загружено {spawnedNPCs.Count} NPC для уровня {currentLevelIndex + 1}");
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
        
        Debug.Log("[LevelController] Все NPC очищены");
    }
    
    public void ReloadCurrentLevel(TableController table)
    {
        if (currentLevelData != null)
        {
            Debug.Log($"[LevelController] Перезагрузка уровня {currentLevelIndex + 1}");
            
            // Очищаем NPC
            ClearCurrentNPCs(table);
            
            // Очищаем стол
            if (table != null)
            {
                table.FullCleanup();
            }
            
            // Сбрасываем фишки игрока
            ResetPlayerChips();
            
            // Загружаем уровень заново
            LoadLevel(currentLevelData, table);
        }
        else
        {
            Debug.LogWarning("[LevelController] Нет данных о текущем уровне");
        }
    }
    
    public List<NPCController> GetSpawnedNPCs()
    {
        return spawnedNPCs;
    }
}