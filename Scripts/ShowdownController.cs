using UnityEngine;

public class ShowdownController
{
    private TableController table;
    private PlayerCardsController player;
    private PlayerChips playerChips;
    private BettingController bettingController;
    
    [Header("Sound Settings")]
    private bool playWinSound = true;
    
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
        
        // Воспроизводим звук победы
        PlayWinSound(winner);
        
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
                if (npc != null && npc.npcName == winner)
                {
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
            if (npc != null && npc.HasCardsActive)
            {
                var evaluation = table.EvaluateNPCHand(npc);
                npc.ShowCombination(evaluation.description, evaluation.value);
            }
        }
    }
    
    private void ShowPlayerHand(PlayerAction playerAction)
    {
        if (playerAction != PlayerAction.Fold && player != null && !player.IsFolded())
        {
            GameDebug.LogPlayerHand(player.GetHandDescription(), player.GetHandValue());
        }
        else
        {
            GameDebug.LogInfo("Вы: ФОЛД");
        }
    }
    
    private (string winner, string handDesc) DetermineWinner(PlayerAction playerAction)
    {
        int bestValue = -1;  // -1 означает что нет активных участников
        string winner = "";
        string bestHandDesc = "";
        bool hasActivePlayer = false;
        int activeNPCsCount = 0;
        
        // Проверяем игрока ТОЛЬКО если он не сфолдил
        if (playerAction != PlayerAction.Fold && player != null && !player.IsFolded())
        {
            hasActivePlayer = true;
            bestValue = player.GetHandValue();
            winner = "Вы";
            bestHandDesc = player.GetHandDescription();
            GameDebug.LogInfo($"Сила вашей руки: {bestValue} ({bestHandDesc})");
        }
        else
        {
            GameDebug.LogInfo("Вы сфолдили - не участвуете в определении победителя");
        }
        
        // Проверяем всех активных NPC
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc == null) continue;
            
            if (npc.HasCardsActive)
            {
                activeNPCsCount++;
                var evaluation = table.EvaluateNPCHand(npc);
                GameDebug.LogNPCHand(npc.npcName, table.GetNPCHand(npc), evaluation.description, evaluation.value);
                
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
        
        // Если все сфолдили (включая игрока), то победителя нет
        if (activeNPCsCount == 0 && !hasActivePlayer)
        {
            GameDebug.LogWarning("Все участники сфолдили! Банк возвращается?");
            winner = "";
            bestHandDesc = "НЕТ ПОБЕДИТЕЛЯ";
        }
        
        // Если только игрок активен (все NPC сфолдили)
        if (activeNPCsCount == 0 && hasActivePlayer)
        {
            GameDebug.LogSuccess("Все NPC сфолдили! Вы побеждаете автоматически!");
            winner = "Вы";
            bestHandDesc = player.GetHandDescription();
        }
        
        return (winner, bestHandDesc);
    }
    
    private void PayWinner(string winner, int pot, string bestHandDesc)
    {
        if (string.IsNullOrEmpty(winner))
        {
            GameDebug.LogWarning("Нет победителя! Банк не выплачен.");
            return;
        }
        
        if (winner == "Вы")
        {
            if (playerChips != null)
            {
                playerChips.AddChips(pot);
                GameDebug.LogWinner(winner, pot, bestHandDesc);
            }
        }
        else if (!string.IsNullOrEmpty(winner))
        {
            foreach (var npc in table.GetAllNPCs())
            {
                if (npc != null && npc.npcName == winner)
                {
                    npc.GetComponent<NPCChips>()?.AddChips(pot);
                    GameDebug.LogWinner(winner, pot, bestHandDesc);
                    break;
                }
            }
        }
    }
    
    #region Sound Methods
    
    private void PlayWinSound(string winner)
    {
        if (!playWinSound) return;
        
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager.Instance is null, cannot play win sound");
            return;
        }
        
        // Воспроизводим разные звуки в зависимости от победителя
        if (winner == "Вы")
        {
            // Звук победы игрока (триумфальный)
            AudioManager.Instance.PlayWinSound();
            Debug.Log("Playing player win sound!");
        }
        else if (!string.IsNullOrEmpty(winner))
        {
            // Звук победы NPC (более спокойный или грустный)
            AudioManager.Instance.PlayNPCWinSound();
            Debug.Log($"Playing NPC win sound for {winner}");
        }
    }
    
    #endregion
}