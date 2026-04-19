// ShowdownController.cs
using UnityEngine;

public class ShowdownController
{
    private TableController table;
    private PlayerCardsController player;
    private PlayerChips playerChips;
    private BettingController bettingController;
    
    public ShowdownController(TableController table, BettingController bettingController)
    {
        this.table = table;
        this.bettingController = bettingController;
        this.player = table.GetPlayer();
        this.playerChips = table.GetPlayerChips();
    }
    
    public void ShowdownPhase(PlayerAction playerAction)
    {
        GameDebug.LogPhase("ВСКРЫТИЕ");
        
        ShowPlayerHand(playerAction);
        var (winner, bestHandDesc) = DetermineWinner(playerAction);
        
        int pot = bettingController.Pot;
        GameDebug.LogPot(pot);
        PayWinner(winner, pot, bestHandDesc, playerAction);
        GameDebug.LogDivider();
    }
    
    private void ShowPlayerHand(PlayerAction playerAction)
    {
        if (playerAction != PlayerAction.Fold)
            GameDebug.LogPlayerHand(player.GetHandDescription(), player.GetHandValue());
        else
            GameDebug.LogInfo("Вы: ФОЛД");
    }
    
    private (string winner, string handDesc) DetermineWinner(PlayerAction playerAction)
    {
        int bestValue = -1; // Начинаем с -1, чтобы даже 0 мог выиграть
        string winner = "";
        string bestHandDesc = "";
        
        // Проверяем игрока только если он НЕ сфолдил
        if (playerAction != PlayerAction.Fold)
        {
            bestValue = player.GetHandValue();
            winner = "Вы";
            bestHandDesc = player.GetHandDescription();
        }
        
        // Проверяем только АКТИВНЫХ NPC (которые НЕ сфолдили)
        foreach (var npc in table.GetActiveNPCs())
        {
            // HasCardsActive уже проверяется в GetActiveNPCs()
            var hand = table.GetNPCHand(npc);
            var evaluation = table.EvaluateNPCHand(npc);
            GameDebug.LogNPCHand(npc.npcName, hand, evaluation.description, evaluation.value);
            
            if (evaluation.value > bestValue)
            {
                bestValue = evaluation.value;
                winner = npc.npcName;
                bestHandDesc = evaluation.description;
            }
        }
        
        // Логируем сфолдивших отдельно
        foreach (var npc in table.GetAllNPCs())
        {
            if (!npc.HasCardsActive)
            {
                GameDebug.LogInfo($"{npc.npcName}: ФОЛД");
            }
        }
        
        return (winner, bestHandDesc);
    }
    
    private void PayWinner(string winner, int pot, string bestHandDesc, PlayerAction playerAction)
    {
        if (string.IsNullOrEmpty(winner))
        {
            GameDebug.LogWarning("Нет победителя! Банк остается.");
            return;
        }
        
        if (winner == "Вы")
        {
            playerChips.AddChips(pot);
            GameDebug.LogWinner(winner, pot, bestHandDesc);
        }
        else
        {
            foreach (var npc in table.GetAllNPCs())
            {
                if (npc.npcName == winner && npc.HasCardsActive)
                {
                    NPCChips chips = npc.GetComponent<NPCChips>();
                    if (chips != null)
                    {
                        chips.AddChips(pot);
                    }
                    break;
                }
            }
            GameDebug.LogWinner(winner, pot, bestHandDesc);
        }
    }
}