using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BettingController
{
    private TableController table;
    private PlayerCardsController player;
    private PlayerChips playerChips;
    
    private float npcTurnDelay;
    private int anteAmount;
    private int raiseAmount = 10;
    
    public int CurrentBet { get; private set; } = 0;
    public int Pot { get; private set; } = 0;
    public PlayerAction PlayerAction { get; private set; } = PlayerAction.None;
    
    public System.Action<int> OnPotChanged;
    public System.Action<string> OnPlayerRaised;
    
    private bool canPlayerAct = false;
    private ActionButtonsController actionButtons;
    private bool waitingForNPCsAfterRaise = false;
    private bool playerIsBrokeThisRound = false;
    
    // Флаги для финального уровня
    private bool isFinalLevel = false;
    private bool deleteNPCsOnLoss = true;
    
    public BettingController(TableController table, float turnDelay, int ante)
    {
        this.table = table;
        this.player = table.GetPlayer();
        this.playerChips = table.GetPlayerChips();
        this.npcTurnDelay = turnDelay;
        this.anteAmount = ante;
    }
    
    public void ResetRoundState()
    {
        Pot = 0;
        CurrentBet = 0;
        PlayerAction = PlayerAction.None;
        waitingForNPCsAfterRaise = false;
        playerIsBrokeThisRound = false;
        OnPotChanged?.Invoke(Pot);
    }
    
    public void SetRaiseAmount(int amount)
    {
        raiseAmount = amount;
        GameDebug.LogInfo($"Высота рейза установлена: {raiseAmount}");
    }
    
    public int GetRaiseAmount()
    {
        return raiseAmount;
    }
    
    public void SetActionButtons(ActionButtonsController buttons)
    {
        actionButtons = buttons;
    }
    
    private void AddToPot(int amount)
    {
        Pot += amount;
        OnPotChanged?.Invoke(Pot);
    }
    
    #region Ante Collection
    
    public void SetAnteAmount(int amount)
    {
        anteAmount = amount;
    }
    
    public bool CollectAnte()
    {
        GameDebug.LogPhase("СБОР АНТЕ");
        
        if (playerChips == null)
        {
            GameDebug.LogError("PlayerChips не найден!");
            return false;
        }
        
        if (!playerChips.HasEnoughChips(anteAmount))
        {
            GameDebug.LogError($"У вас недостаточно фишек для анте! Нужно: {anteAmount}, есть: {playerChips.GetChips()}");
            return false;
        }
        
        playerChips.RemoveChips(anteAmount);
        AddToPot(anteAmount);
        GameDebug.LogInfo($"Вы внесли анте: {anteAmount}");
        
        AudioManager.Instance?.PlayBetSound();
        
        List<NPCController> npcsToRemove = new List<NPCController>();
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc == null) continue;
            
            NPCChips chips = npc.GetComponent<NPCChips>();
            
            if (chips == null)
            {
                GameDebug.LogError($"{npc.npcName}: нет компонента фишек!");
                npcsToRemove.Add(npc);
                continue;
            }
            
            if (!chips.HasEnoughChips(anteAmount))
            {
                GameDebug.LogWarning($"{npc.npcName}: недостаточно фишек для анте ({chips.GetChips()}/{anteAmount}), покидает стол!");
                npcsToRemove.Add(npc);
                continue;
            }
            
            chips.RemoveChips(anteAmount);
            AddToPot(anteAmount);
            GameDebug.LogInfo($"  {npc.npcName} внес анте: {anteAmount}");
        }
        
        foreach (var npc in npcsToRemove)
        {
            if (npc != null)
            {
                table.RemoveNPC(npc);
                if (npc.gameObject != null)
                    npc.gameObject.SetActive(false);
            }
        }
        
        GameDebug.LogSuccess($"Банк после сбора анте: {Pot}");
        return true;
    }
    
    #endregion
    
    #region Betting Phases
    
    public void ResetPot()
    {
        Pot = 0;
        CurrentBet = 0;
        OnPotChanged?.Invoke(Pot);
    }

    public IEnumerator BettingPhase()
    {
        GameDebug.LogPhase("ХОД NPC");
        
        foreach (var npc in table.GetActiveNPCs())
        {
            if (npc == null) continue;
            yield return ProcessNPCTurn(npc);
            yield return new WaitForSeconds(npcTurnDelay);
        }
        
        GameDebug.LogBetInfo(CurrentBet, Pot);
        GameDebug.LogPhase("ХОД ИГРОКА");
    }
    
    public IEnumerator AfterPlayerRaisePhase()
    {
        GameDebug.LogPhase("NPC ОТВЕЧАЮТ НА РЕЙЗ");
        waitingForNPCsAfterRaise = true;
        
        foreach (var npc in table.GetActiveNPCs())
        {
            if (npc == null) continue;
            yield return ProcessNPCTurnAfterRaise(npc);
            yield return new WaitForSeconds(npcTurnDelay);
        }
        
        waitingForNPCsAfterRaise = false;
        GameDebug.LogBetInfo(CurrentBet, Pot);
    }
    
    public IEnumerator PlayerPhase()
    {
        if (table.GetActivePlayersCount() == 0)
        {
            GameDebug.LogSuccess("Все NPC сфолдили, вы забираете банк!");
            if (playerChips != null)
            {
                playerChips.AddChips(Pot);
            }
            Pot = 0;
            OnPotChanged?.Invoke(Pot);
            yield break;
        }
        
        yield return ProcessPlayerTurn();
        
        if (PlayerAction == PlayerAction.Raise && table.GetActivePlayersCount() > 0)
        {
            GameDebug.LogInfo("Игрок сделал рейз! NPC отвечают...");
            yield return AfterPlayerRaisePhase();
        }
    }
    
    public void CheckPlayerLossAfterShowdown()
    {
        if (playerChips != null && playerChips.IsBroke())
        {
            GameDebug.LogError("Игрок проиграл все фишки после раунда!");
            GameManager.Instance.PlayerOutOfChips();
        }
    }
    
    #endregion
    
    #region NPC Turn Processing
    
    private IEnumerator ProcessNPCTurn(NPCController npc)
    {
        if (npc == null) yield break;
        
        NPCChips chips = npc.GetComponent<NPCChips>();
        
        if (chips == null || chips.IsBroke())
        {
            npc.DiscardCards();
            GameDebug.LogWarning($"{npc.npcName}: Нет фишек, фолд");
            yield break;
        }
        
        if (!npc.HasCardsActive)
        {
            Debug.LogWarning($"[{npc.npcName}] Нет карт, пропускаем ход");
            yield break;
        }
        
        PlayerAction action = GetValidNPCAction(npc, chips);
        EmotionType emotion = npc.GetCurrentEmotion();
        
        GameDebug.LogNPCAction(npc.npcName, emotion, action);
        
        npc.ShowAction(action);
        ExecuteNPCAction(npc, chips, action);
    }
    
    private IEnumerator ProcessNPCTurnAfterRaise(NPCController npc)
    {
        if (npc == null) yield break;
        
        NPCChips chips = npc.GetComponent<NPCChips>();
        
        if (chips == null || chips.IsBroke())
        {
            npc.DiscardCards();
            GameDebug.LogWarning($"{npc.npcName}: Нет фишек, фолд");
            yield break;
        }
        
        if (!npc.HasCardsActive)
        {
            yield break;
        }
        
        int neededAmount = CurrentBet;
        
        if (!chips.HasEnoughChips(neededAmount))
        {
            GameDebug.LogWarning($"{npc.npcName}: Не хватает фишек для колла ({chips.GetChips()}/{neededAmount}), фолд");
            npc.DiscardCards();
            npc.IncrementConsecutiveFolds();
            yield break;
        }
        
        GameDebug.LogNPCAction(npc.npcName, npc.GetCurrentEmotion(), PlayerAction.Call);
        npc.ShowAction(PlayerAction.Call);
        npc.PlayCall();
        chips.RemoveChips(neededAmount);
        AddToPot(neededAmount);
        npc.ResetConsecutiveFolds();
        
        GameDebug.LogInfo($"{npc.npcName} отвечает на рейз: CALL {neededAmount}");
    }
    
    private PlayerAction GetValidNPCAction(NPCController npc, NPCChips chips)
    {
        PlayerAction action = npc.MakeDecision();
        
        if (action == PlayerAction.Raise)
        {
            int raiseAmountValue = CurrentBet + raiseAmount;
            if (!chips.HasEnoughChips(raiseAmountValue))
            {
                action = PlayerAction.Call;
            }
        }
        
        if (action == PlayerAction.Call)
        {
            if (!chips.HasEnoughChips(CurrentBet))
            {
                action = PlayerAction.Fold;
            }
        }
        
        return action;
    }
    
    private void ExecuteNPCAction(NPCController npc, NPCChips chips, PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.Fold:
                npc.DiscardCards();
                npc.IncrementConsecutiveFolds();
                AudioManager.Instance?.PlayFoldSound();
                break;
                
            case PlayerAction.Call:
                npc.PlayCall();
                chips.RemoveChips(CurrentBet);
                AddToPot(CurrentBet);
                npc.ResetConsecutiveFolds();
                AudioManager.Instance?.PlayCallSound();
                break;
                
            case PlayerAction.Raise:
                npc.PlayRaise();
                int raiseAmountValue = CurrentBet + raiseAmount;
                chips.RemoveChips(raiseAmountValue);
                CurrentBet = raiseAmountValue;
                AddToPot(CurrentBet);
                GameDebug.LogRaise(CurrentBet);
                OnPlayerRaised?.Invoke(npc.npcName);
                npc.ResetConsecutiveFolds();
                AudioManager.Instance?.PlayRaiseSound();
                break;
        }
    }
    
    #endregion
    
    #region Player Turn Processing
    
    private IEnumerator ProcessPlayerTurn()
    {
        canPlayerAct = true;
        PlayerAction = PlayerAction.None;
        
        if (actionButtons != null)
        {
            actionButtons.UpdateButtonsState(CurrentBet);
        }
        
        GameDebug.LogPlayerTurn(CurrentBet, player != null ? player.GetHandValue() : 0, playerChips != null ? playerChips.GetChips() : 0);
        
        while (PlayerAction == PlayerAction.None && canPlayerAct)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                PlayerAction = PlayerAction.Fold;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                if (playerChips != null && playerChips.HasEnoughChips(CurrentBet))
                    PlayerAction = PlayerAction.Call;
                else
                    GameDebug.LogWarning("Недостаточно фишек для Call!");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                int raiseAmountValue = CurrentBet + raiseAmount;
                if (playerChips != null && playerChips.HasEnoughChips(raiseAmountValue))
                    PlayerAction = PlayerAction.Raise;
                else
                    GameDebug.LogWarning("Недостаточно фишек для Raise!");
            }
            
            yield return null;
        }
        
        canPlayerAct = false;
        ExecutePlayerAction();
        if (playerChips != null)
            GameDebug.LogChips(playerChips.GetChips());
    }
    
    public void SetPlayerAction(PlayerAction action)
    {
        if (canPlayerAct)
        {
            switch (action)
            {
                case PlayerAction.Fold:
                    PlayerAction = action;
                    break;
                case PlayerAction.Call:
                    if (playerChips != null && playerChips.HasEnoughChips(CurrentBet))
                        PlayerAction = action;
                    else
                        GameDebug.LogWarning("Недостаточно фишек для Call!");
                    break;
                case PlayerAction.Raise:
                    int raiseAmountValue = CurrentBet + raiseAmount;
                    if (playerChips != null && playerChips.HasEnoughChips(raiseAmountValue))
                        PlayerAction = action;
                    else
                        GameDebug.LogWarning("Недостаточно фишек для Raise!");
                    break;
            }
        }
    }
    
    private void ExecutePlayerAction()
    {
        GameDebug.LogPlayerAction(PlayerAction, CurrentBet);
        
        switch (PlayerAction)
        {
            case PlayerAction.Fold:
                if (player != null)
                    player.FoldCards();
                GameDebug.LogWarning("Вы сбросили карты");
                AudioManager.Instance?.PlayFoldSound();
                break;
                
            case PlayerAction.Call:
                if (playerChips != null)
                    playerChips.RemoveChips(CurrentBet);
                AddToPot(CurrentBet);
                AudioManager.Instance?.PlayCallSound();
                break;
                
            case PlayerAction.Raise:
                int raiseAmountValue = CurrentBet + raiseAmount;
                if (playerChips != null)
                    playerChips.RemoveChips(raiseAmountValue);
                CurrentBet = raiseAmountValue;
                AddToPot(CurrentBet);
                GameDebug.LogRaise(CurrentBet);
                OnPlayerRaised?.Invoke("Игрок");
                AudioManager.Instance?.PlayRaiseSound();
                break;
        }
    }
    
    #endregion
    
    #region NPC Management
    
    public void CheckAndRemoveBrokeNPCs()
    {
        List<NPCController> brokeNPCs = new List<NPCController>();
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc == null) continue;
            
            NPCChips chips = npc.GetComponent<NPCChips>();
            if (chips != null && chips.IsBroke())
            {
                brokeNPCs.Add(npc);
            }
        }
        
        foreach (var npc in brokeNPCs)
        {
            if (npc != null)
                GameManager.Instance.NPCOutOfChips(npc);
        }
    }

    public List<NPCController> GetActiveNPCs()
    {
        List<NPCController> active = new List<NPCController>();
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null && npc.HasCardsActive)
                active.Add(npc);
        }
        return active;
    }
    
    public bool CollectAnteFromNPCsOnly(int anteAmount)
    {
        int paidNPCs = 0;
        List<NPCController> npcsToRemove = new List<NPCController>();
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc == null) continue;
            
            NPCChips chips = npc.GetComponent<NPCChips>();
            
            if (chips == null)
            {
                npcsToRemove.Add(npc);
                continue;
            }
            
            if (!chips.HasEnoughChips(anteAmount))
            {
                npcsToRemove.Add(npc);
                continue;
            }
            
            chips.RemoveChips(anteAmount);
            AddToPot(anteAmount);
            paidNPCs++;
        }
        
        foreach (var npc in npcsToRemove)
        {
            if (npc != null)
            {
                table.RemoveNPC(npc);
                if (npc.gameObject != null)
                    npc.gameObject.SetActive(false);
            }
        }
        
        return paidNPCs > 0;
    }
    
    public void SetFinalLevel(bool isFinal)
    {
        isFinalLevel = isFinal;
        if (isFinalLevel)
        {
            GameDebug.LogWarning("ФИНАЛЬНЫЙ УРОВЕНЬ: NPC не будут удаляться при проигрыше!");
        }
    }
    
    public void SetDeleteNPCsOnLoss(bool delete)
    {
        deleteNPCsOnLoss = delete;
    }
    
    public bool RemoveBrokeNPCs()
    {
        List<NPCController> toRemove = new List<NPCController>();
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc == null) continue;
            
            NPCChips chips = npc.GetComponent<NPCChips>();
            if (chips == null) continue;
            
            if (chips.IsBroke())
            {
                if (!deleteNPCsOnLoss || isFinalLevel)
                {
                    GameDebug.LogWarning($"{npc.npcName} проиграл, но остается для финала!");
                    npc.DiscardCards();
                }
                else
                {
                    toRemove.Add(npc);
                }
            }
        }
        
        foreach (var npc in toRemove)
        {
            if (npc != null)
            {
                table.RemoveNPC(npc);
                if (npc.gameObject != null)
                    npc.gameObject.SetActive(false);
                GameDebug.LogWarning($"{npc.npcName} покинул стол!");
            }
        }
        
        int aliveCount = 0;
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null) aliveCount++;
        }
        
        return aliveCount > 0;
    }
    
    #endregion
}