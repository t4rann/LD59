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
        
        // Показываем комбинации NPC
        ShowNPCCombinations();
        
        ShowPlayerHand(playerAction);
        var (winner, bestHandDesc) = DetermineWinner(playerAction);
        
        int pot = bettingController.Pot;
        GameDebug.LogPot(pot);
        
        // ПОКАЗЫВАЕМ СООБЩЕНИЕ О ПОБЕДИТЕЛЕ
        ShowWinnerMessage(winner, bestHandDesc, pot);
        
        PayWinner(winner, pot, bestHandDesc);
        GameDebug.LogDivider();
    }
    
private void ShowWinnerMessage(string winner, string handDescription, int potAmount)
{
    if (winner == "Вы")
    {
        PlayerWinEffect.PlayWinEffect();
        GameDebug.LogWinner("Вы", potAmount, handDescription);
    }
    else if (!string.IsNullOrEmpty(winner))
    {
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc.npcName == winner)
            {
                // Убираем winnerName, передаем только handDescription и potAmount
                npc.ShowWinnerMessage(handDescription, potAmount);
                break;
            }
        }
    }
}
    
    private void ShowNPCCombinations()
    {
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc.HasCardsActive)
            {
                var evaluation = table.EvaluateNPCHand(npc);
                npc.ShowCombination(evaluation.description, evaluation.value);
            }
        }
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
        int bestValue = 0;
        string winner = "";
        string bestHandDesc = "";
        
        if (playerAction != PlayerAction.Fold)
        {
            bestValue = player.GetHandValue();
            winner = "Вы";
            bestHandDesc = player.GetHandDescription();
        }
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc.HasCardsActive)
            {
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
            else
            {
                GameDebug.LogInfo($"{npc.npcName}: ФОЛД");
            }
        }
        
        return (winner, bestHandDesc);
    }
    
    private void PayWinner(string winner, int pot, string bestHandDesc)
    {
        if (winner == "Вы")
        {
            playerChips.AddChips(pot);
            GameDebug.LogWinner(winner, pot, bestHandDesc);
        }
        else if (!string.IsNullOrEmpty(winner))
        {
            foreach (var npc in table.GetAllNPCs())
            {
                if (npc.npcName == winner)
                {
                    npc.GetComponent<NPCChips>()?.AddChips(pot);
                    break;
                }
            }
            GameDebug.LogWinner(winner, pot, bestHandDesc);
        }
    }
}