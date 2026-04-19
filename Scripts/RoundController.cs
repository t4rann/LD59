// RoundController.cs
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
        
        SubscribeToTakeCardsFinished();
        allTakeCardsFinished = false;
        npcTakeCardsFinished = new Dictionary<NPCController, bool>();
        
        foreach (var npc in table.GetAllNPCs())
            npcTakeCardsFinished[npc] = false;
        
        table.DealNewRound();
        
        yield return new WaitUntil(() => allTakeCardsFinished);
        UnsubscribeFromTakeCardsFinished();
        
        yield return new WaitForSeconds(dealAnimationDelay);
        GameDebug.LogInfo("Все взяли карты");
    }
    
    public IEnumerator EmotionsPhase()
    {
        GameDebug.LogPhase("ЭМОЦИИ");
        
        foreach (var npc in table.GetActiveNPCs())
        {
            npc.ShowEmotion();
            GameDebug.LogNPCEmotion(npc.npcName, npc.GetCurrentEmotion());
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
            npc.OnTakeCardsFinished += OnNPCTakeCardsFinished;
    }
    
    private void UnsubscribeFromTakeCardsFinished()
    {
        foreach (var npc in table.GetAllNPCs())
            npc.OnTakeCardsFinished -= OnNPCTakeCardsFinished;
    }
    
    private void OnNPCTakeCardsFinished(NPCController npc)
    {
        if (npcTakeCardsFinished == null) return;
        
        npcTakeCardsFinished[npc] = true;
        
        foreach (var finished in npcTakeCardsFinished.Values)
            if (!finished) return;
        
        allTakeCardsFinished = true;
    }
    
    public void SetDelays(float dealDelay, float emotionsDelay)
    {
        dealAnimationDelay = dealDelay;
        emotionsPhaseDelay = emotionsDelay;
    }
}