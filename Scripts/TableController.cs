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
    private Dictionary<NPCController, string> npcHandDescriptions = new Dictionary<NPCController, string>();
    private bool isDealing = false;
    
    void Start()
    {
        if (deckManager == null) deckManager = FindFirstObjectByType<DeckManager>();
        if (handEvaluator == null) handEvaluator = FindFirstObjectByType<HandEvaluator>();
        if (playerChips == null) playerChips = FindFirstObjectByType<PlayerChips>();
    }
    
    public void AddNPC(NPCController npc)
    {
        if (npc != null && !allNPCs.Contains(npc))
        {
            allNPCs.Add(npc);
            Debug.Log($"[Table] Добавлен NPC: {npc.npcName}");
        }
    }

    public void DealNewRound()
    {   
        if (isDealing)
        {
            Debug.LogWarning("Раздача уже идет, пропускаем");
            return;
        }
        
        isDealing = true;
        
        Debug.Log("[Table] Начинаем раздачу нового раунда");
        
        // Сначала раздаем карты NPC
        foreach (var npc in allNPCs)
        {
            if (npc == null) continue;
            
            List<CardData> hand = deckManager.DealCards(5);
            npcHands[npc] = hand;
            
            var evaluation = handEvaluator.EvaluateHand(hand);
            npcHandValues[npc] = evaluation.value;
            npcHandDescriptions[npc] = evaluation.description;
            
            Debug.Log($"[Table] {npc.npcName} получил: {evaluation.description} (сила: {evaluation.value})");
            
            npc.ReceiveNewHand(evaluation.value);
        }
        
        // Потом раздаем карты игроку
        if (playerCards != null)
        {
            Debug.Log("[Table] Раздаем карты игроку");
            playerCards.DealNewHand();
        }
        
        isDealing = false;
        Debug.Log("[Table] Раздача завершена");
    }
    
    public void MarkNPCAsFolded(NPCController npc)
    {
        if (npc != null && npcHandValues.ContainsKey(npc))
        {
            npcHandValues[npc] = 0;
            npcHandDescriptions[npc] = "FOLDED";
        }
        Debug.Log($"[Table] {npc?.npcName} помечен как сфолдивший");
    }
    
    public void ResetAllEmotions()
    {
        foreach (var npc in allNPCs)
        {
            if (npc != null)
                npc.ResetToNeutral();
        }
    }
    
public void FullCleanup()
{
    if (isDealing)
    {
        Debug.LogWarning("[Table] Нельзя очищать во время раздачи!");
        return;
    }
    
    Debug.Log("[Table] Полная очистка");
    DiscardAllCards();
    ResetAllEmotions();
}

public void DiscardAllCards()
{
    if (isDealing)
    {
        Debug.LogWarning("[Table] Нельзя сбрасывать карты во время раздачи!");
        return;
    }
    
    Debug.Log("[Table] Сброс всех карт");
    
    if (playerCards != null)
    {
        playerCards.DiscardAllCards();
    }
    
    foreach (var npc in allNPCs)
    {
        if (npc != null && npc.HasCardsActive)
        {
            npc.DiscardCards();
        }
    }
    
    npcHands.Clear();
    npcHandValues.Clear();
    npcHandDescriptions.Clear();
}
    
    public List<NPCController> GetAllNPCs()
    {
        allNPCs.RemoveAll(npc => npc == null);
        return allNPCs;
    }
    
    public List<NPCController> GetActiveNPCs()
    {
        List<NPCController> active = new List<NPCController>();
        foreach (var npc in GetAllNPCs())
        {
            if (npc != null && npc.HasCardsActive && npcHandValues.ContainsKey(npc) && npcHandValues[npc] > 0)
            {
                active.Add(npc);
            }
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
        if (npc != null && npcHands.ContainsKey(npc))
            return npcHands[npc];
        return new List<CardData>();
    }
    
    public int GetNPCHandValue(NPCController npc)
    {
        if (npc != null && npcHandValues.ContainsKey(npc))
            return npcHandValues[npc];
        return 0;
    }
    
    public string GetNPCHandDescription(NPCController npc)
    {
        if (npc != null && npcHandDescriptions.ContainsKey(npc))
            return npcHandDescriptions[npc];
        return "HIGH CARD";
    }
    
    public (int value, string description) EvaluateNPCHand(NPCController npc)
    {
        if (npc == null)
            return (0, "HIGH CARD");
        
        if (!npc.HasCardsActive)
        {
            return (0, "FOLDED");
        }
        
        if (npcHandValues.ContainsKey(npc) && npcHandValues[npc] > 0)
        {
            string desc = npcHandDescriptions.ContainsKey(npc) ? npcHandDescriptions[npc] : "HIGH CARD";
            return (npcHandValues[npc], desc);
        }
        
        if (npc.HasCardsActive && npcHands.ContainsKey(npc) && npcHands[npc] != null && npcHands[npc].Count > 0)
        {
            var evaluation = handEvaluator.EvaluateHand(npcHands[npc]);
            return (evaluation.value, evaluation.description);
        }
        
        return (0, "FOLDED");
    }
    
    public NPCChips GetNPCChips(NPCController npc)
    {
        if (npc != null)
            return npc.GetComponent<NPCChips>();
        return null;
    }
    
    public void RemoveNPC(NPCController npc)
    {
        if (npc != null && allNPCs.Contains(npc))
        {
            if (npcHands.ContainsKey(npc))
                npcHands.Remove(npc);
            if (npcHandValues.ContainsKey(npc))
                npcHandValues.Remove(npc);
            if (npcHandDescriptions.ContainsKey(npc))
                npcHandDescriptions.Remove(npc);
            
            allNPCs.Remove(npc);
            Debug.Log($"[Table] NPC удален: {npc.npcName}");
        }
    }
    
    public void ClearAllNPCs()
    {
        Debug.Log("[Table] Очистка всех NPC");
        
        npcHands.Clear();
        npcHandValues.Clear();
        npcHandDescriptions.Clear();
        
        for (int i = allNPCs.Count - 1; i >= 0; i--)
        {
            var npc = allNPCs[i];
            if (npc != null)
            {
                RemoveNPC(npc);
            }
        }
        allNPCs.Clear();
        
        Debug.Log("[Table] Все NPC и их данные очищены");
    }
    
    public void ShowAllNPCCards()
    {
        foreach (var npc in allNPCs)
        {
            if (npc != null)
                npc.GetComponent<NPCCardsVisual>()?.ShowCards();
        }
    }
    
    public void HideAllNPCCards()
    {
        foreach (var npc in allNPCs)
        {
            if (npc != null)
                npc.GetComponent<NPCCardsVisual>()?.HideCards();
        }
    }
    
    public bool IsDealing()
    {
        return isDealing;
    }
}