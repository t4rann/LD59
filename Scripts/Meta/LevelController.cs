using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField] private List<TransformPoint> spawnPoints = new List<TransformPoint>();
    [SerializeField] private Transform npcContainer;
    
    private List<NPCController> spawnedNPCs = new List<NPCController>();
    
    public List<NPCController> LoadLevel(LevelData levelData, TableController table)
    {
        ClearCurrentNPCs(table);
        
        if (levelData.npcSetups.Count > spawnPoints.Count)
        {
            Debug.LogError($"Не хватает точек спавна! Нужно {levelData.npcSetups.Count}, есть {spawnPoints.Count}");
            return null;
        }
        
        // Применяем настройки уровня
        GameLoop gameLoop = FindFirstObjectByType<GameLoop>();
        if (gameLoop != null)
        {
            gameLoop.SetMaxRounds(levelData.roundsCount);
            gameLoop.SetAnteAmount(levelData.anteAmount);
        }
        
        // Спавним NPC
        for (int i = 0; i < levelData.npcSetups.Count; i++)
        {
            NPCSetup setup = levelData.npcSetups[i];
            TransformPoint point = spawnPoints[i];
            
            if (point.isOccupied) continue;
            
            GameObject npcObj = Instantiate(setup.npcPrefab, point.transform.position, 
                                           point.transform.rotation, npcContainer);
            
            NPCController npc = npcObj.GetComponent<NPCController>();
            if (npc != null)
            {
                // Применяем поведение через публичное поле
                var field = typeof(NPCController).GetField("brain", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(npc, setup.npcBrain);
                }
                
                // Убеждаемся, что у NPC есть фишки
                NPCChips chips = npc.GetComponent<NPCChips>();
                if (chips == null)
                {
                    chips = npc.gameObject.AddComponent<NPCChips>();
                    Debug.Log($"Добавлен компонент NPCChips для {npc.npcName}");
                }
                
                spawnedNPCs.Add(npc);
                point.isOccupied = true;
                table.AddNPC(npc);
            }
        }
        
        Debug.Log($"Загружено NPC: {spawnedNPCs.Count}");
        return spawnedNPCs;
    }
    
    public void ClearCurrentNPCs(TableController table = null)
    {
        // Очищаем TableController
        if (table != null)
        {
            // Получаем всех NPC из таблицы и удаляем их
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
        
        // Уничтожаем спавненные объекты
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null && npc.gameObject != null)
                Destroy(npc.gameObject);
        }
        spawnedNPCs.Clear();
        
        // Освобождаем точки спавна
        foreach (var point in spawnPoints)
        {
            point.isOccupied = false;
        }
        
        Debug.Log("Все NPC очищены");
    }
}