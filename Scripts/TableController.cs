// TableController.cs
using System.Collections.Generic;
using UnityEngine;

public class TableController : MonoBehaviour
{
    [Header("Participants")]
    [SerializeField] private PlayerCardsController playerCards;
    [SerializeField] private PlayerChips playerChips;
    [SerializeField] private List<NPCController> allNPCs = new List<NPCController>();
    
    [Header("References")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandEvaluator handEvaluator;
    
    private Dictionary<NPCController, List<CardData>> npcHands = new Dictionary<NPCController, List<CardData>>();
    private Dictionary<NPCController, int> npcHandValues = new Dictionary<NPCController, int>();
    
    void Start()
    {
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (handEvaluator == null) handEvaluator = FindObjectOfType<HandEvaluator>();
        if (playerChips == null) playerChips = FindObjectOfType<PlayerChips>();
    }
    
    public void DealNewRound()
    {   
        // Раздача NPC
        foreach (var npc in allNPCs)
        {
            List<CardData> hand = deckManager.DealCards(5);
            npcHands[npc] = hand;
            int handValue = handEvaluator.GetHandStrengthValue(hand);
            npcHandValues[npc] = handValue;
            
            npc.ReceiveNewHand(handValue);
        }
        
        // Раздача игроку
        if (playerCards != null)
        {
            playerCards.DealNewHand();
        }
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
    
    // Сброс карт NPC - только сброс карт, без сброса эмоций
    foreach (var npc in allNPCs)
    {
        if (npc.HasCardsActive)
        {
            npc.DiscardCards();
        }
    }
    
    npcHands.Clear();
    npcHandValues.Clear();
}

public void FullCleanup()
{
    DiscardAllCards();
    ResetAllEmotions();
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
    
    public PlayerChips GetPlayerChips()
    {
        return playerChips;
    }
    
    public List<CardData> GetNPCHand(NPCController npc)
    {
        if (npcHands.ContainsKey(npc))
            return npcHands[npc];
        return new List<CardData>();
    }
    
    public int GetNPCHandValue(NPCController npc)
    {
        if (npcHandValues.ContainsKey(npc))
            return npcHandValues[npc];
        return 0;
    }
    
    public Dictionary<NPCController, List<CardData>> GetAllNPCHands()
    {
        return npcHands;
    }
    
    public (HandRank rank, int value, string description) EvaluateNPCHand(NPCController npc)
    {
        if (npcHands.ContainsKey(npc))
            return handEvaluator.EvaluateHand(npcHands[npc]);
        return (HandRank.HighCard, 0, "Нет карт");
    }
    
    // Получить компонент фишек NPC
    public NPCChips GetNPCChips(NPCController npc)
    {
        return npc.GetComponent<NPCChips>();
    }
    
    // Удалить NPC из-за стола
    public void RemoveNPC(NPCController npc)
    {
        if (allNPCs.Contains(npc))
        {
            allNPCs.Remove(npc);
        }
    }

public void ShowAllNPCCards()
{
    foreach (var npc in allNPCs)
    {
        npc.GetComponent<NPCCardsVisual>()?.ShowCards();
    }
}

public void HideAllNPCCards()
{
    foreach (var npc in allNPCs)
    {
        npc.GetComponent<NPCCardsVisual>()?.HideCards();
    }
}
}