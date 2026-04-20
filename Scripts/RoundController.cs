using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundController
{
    private TableController table;
    private Dictionary<NPCController, bool> npcTakeCardsFinished;
    private bool allTakeCardsFinished = false;
    
    private float dealAnimationDelay;
    private float emotionsPhaseDelay;
    
    public RoundController(TableController table, float dealDelay, float emotionsDelay)
    {
        this.table = table;
        this.dealAnimationDelay = dealDelay;
        this.emotionsPhaseDelay = emotionsDelay;
    }
    
    public IEnumerator DealPhase()
    {
        GameDebug.LogPhase("РАЗДАЧА КАРТ");
        
        // Воспроизводим звук раздачи карт
        AudioManager.Instance?.PlayDealCardsSound();
        
        // Подписываемся на события завершения получения карт NPC
        SubscribeToTakeCardsFinished();
        allTakeCardsFinished = false;
        npcTakeCardsFinished = new Dictionary<NPCController, bool>();
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null)
                npcTakeCardsFinished[npc] = false;
        }
        
        // Начинаем раздачу
        table.DealNewRound();
        
        // Ждем пока все NPC получат карты
        if (npcTakeCardsFinished.Count > 0)
        {
            yield return new WaitUntil(() => allTakeCardsFinished);
        }
        
        UnsubscribeFromTakeCardsFinished();
        
        // Даем время на анимацию раздачи карт игроку
        yield return new WaitForSeconds(dealAnimationDelay);
        
        GameDebug.LogInfo("Все взяли карты");
    }
    
    public IEnumerator EmotionsPhase()
    {
        GameDebug.LogPhase("ЭМОЦИИ");
        
        foreach (var npc in table.GetActiveNPCs())
        {
            if (npc != null)
            {
                npc.ShowEmotion();
                GameDebug.LogNPCEmotion(npc.npcName, npc.GetCurrentEmotion());
            }
        }
        
        yield return new WaitForSeconds(emotionsPhaseDelay);
    }
    
    public void CleanupPhase()
    {
        table.ResetAllEmotions();
    }
    
    private void SubscribeToTakeCardsFinished()
    {
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null)
                npc.OnTakeCardsFinished += OnNPCTakeCardsFinished;
        }
    }
    
    private void UnsubscribeFromTakeCardsFinished()
    {
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null)
                npc.OnTakeCardsFinished -= OnNPCTakeCardsFinished;
        }
    }
    
    private void OnNPCTakeCardsFinished(NPCController npc)
    {
        if (npcTakeCardsFinished == null) return;
        if (npc == null) return;
        
        if (npcTakeCardsFinished.ContainsKey(npc))
        {
            npcTakeCardsFinished[npc] = true;
        }
        
        // Проверяем, все ли NPC получили карты
        foreach (var finished in npcTakeCardsFinished.Values)
        {
            if (!finished) return;
        }
        
        allTakeCardsFinished = true;
        Debug.Log("[RoundController] Все NPC получили карты");
    }
    
    public void SetDelays(float dealDelay, float emotionsDelay)
    {
        dealAnimationDelay = dealDelay;
        emotionsPhaseDelay = emotionsDelay;
    }
}