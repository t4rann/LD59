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
    private Dictionary<NPCController, string> npcHandDescriptions = new Dictionary<NPCController, string>();
    
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
            
            var evaluation = handEvaluator.EvaluateHand(hand);
            npcHandValues[npc] = evaluation.value;
            npcHandDescriptions[npc] = evaluation.description;
            
            Debug.Log($"[Table] {npc.npcName} получил: {evaluation.description} (сила: {evaluation.value})");
            
            npc.ReceiveNewHand(evaluation.value);
        }
        
        // Раздача игроку
        if (playerCards != null)
        {
            playerCards.DealNewHand();
        }
    }
    
    // 👈 НОВЫЙ МЕТОД: помечает NPC как сфолдившего (очищает его карты)
    public void MarkNPCAsFolded(NPCController npc)
    {
        if (npcHandValues.ContainsKey(npc))
        {
            npcHandValues[npc] = 0;
            npcHandDescriptions[npc] = "FOLDED";
        }
        Debug.Log($"[Table] {npc.npcName} помечен как сфолдивший");
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
        if (playerCards != null)
        {
            playerCards.DiscardAllCards();
        }
        
        foreach (var npc in allNPCs)
        {
            if (npc.HasCardsActive)
            {
                npc.DiscardCards();
            }
        }
        
        npcHands.Clear();
        npcHandValues.Clear();
        npcHandDescriptions.Clear();
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
            // 👈 ИСПРАВЛЕНО: проверяем HasCardsActive И значение > 0
            if (npc.HasCardsActive && npcHandValues.ContainsKey(npc) && npcHandValues[npc] > 0)
            {
                active.Add(npc);
                Debug.Log($"[Active] {npc.npcName} активен, сила: {npcHandValues[npc]}");
            }
            else if (npc.HasCardsActive && (!npcHandValues.ContainsKey(npc) || npcHandValues[npc] <= 0))
            {
                Debug.LogWarning($"[Active] {npc.npcName} имеет HasCardsActive=true но нет данных в словаре!");
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
    
    public string GetNPCHandDescription(NPCController npc)
    {
        if (npcHandDescriptions.ContainsKey(npc))
            return npcHandDescriptions[npc];
        return "HIGH CARD";
    }
    
    // 👈 ИСПРАВЛЕННЫЙ МЕТОД: возвращает корректную комбинацию
    public (int value, string description) EvaluateNPCHand(NPCController npc)
    {
        if (npc == null)
            return (0, "HIGH CARD");
        
        // Если NPC сфолдил - возвращаем 0
        if (!npc.HasCardsActive)
        {
            return (0, "FOLDED");
        }
        
        // Проверяем сохраненные значения
        if (npcHandValues.ContainsKey(npc) && npcHandValues[npc] > 0)
        {
            string desc = npcHandDescriptions.ContainsKey(npc) ? npcHandDescriptions[npc] : "HIGH CARD";
            return (npcHandValues[npc], desc);
        }
        
        // Если нет сохраненного значения, но NPC активен - вычисляем заново
        if (npc.HasCardsActive && npcHands.ContainsKey(npc) && npcHands[npc] != null && npcHands[npc].Count > 0)
        {
            var evaluation = handEvaluator.EvaluateHand(npcHands[npc]);
            return (evaluation.value, evaluation.description);
        }
        
        return (0, "FOLDED");
    }
    
    public NPCChips GetNPCChips(NPCController npc)
    {
        return npc.GetComponent<NPCChips>();
    }
    
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