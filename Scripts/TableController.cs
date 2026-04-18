// TableController.cs
using System.Collections.Generic;
using UnityEngine;

public class TableController : MonoBehaviour
{
    [Header("Participants")]
    [SerializeField] private PlayerCardsController playerCards;
    [SerializeField] private List<NPCController> allNPCs = new List<NPCController>();
    
    public void DealNewRound()
    {
        // Раздача игроку
        if (playerCards != null)
        {
            playerCards.DealNewHand();
        }
        
        // Раздача NPC
        foreach (var npc in allNPCs)
        {
            npc.ReceiveNewHand(GetRandomHandStrength());
        }
    }
    
    private HandStrength GetRandomHandStrength()
    {
        float val = Random.value;
        if (val < 0.33f) return HandStrength.Weak;
        if (val < 0.66f) return HandStrength.Medium;
        return HandStrength.Strong;
    }
    
    public void ResetAllEmotions()
    {
        foreach (var npc in allNPCs)
        {
            npc.ResetToNeutral();
        }
    }
    
    public void DiscardAllCards()
    {
        // Сброс карт игрока
        if (playerCards != null)
        {
            playerCards.DiscardAllCards();
        }
        
        // Сброс карт NPC
        foreach (var npc in allNPCs)
        {
            npc.DiscardCards();
        }
    }
    
    public List<NPCController> GetAllNPCs()
    {
        return allNPCs;
    }
    
    public List<NPCController> GetActiveNPCs()
    {
        List<NPCController> active = new List<NPCController>();
        foreach (var npc in allNPCs)
        {
            if (npc.HasCardsActive)
                active.Add(npc);
        }
        return active;
    }
    
    public int GetActivePlayersCount()
    {
        return GetActiveNPCs().Count;
    }
    
    public PlayerCardsController GetPlayer()
    {
        return playerCards;
    }
}