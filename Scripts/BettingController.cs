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
    
    public int CurrentBet { get; private set; } = 0;
    public int Pot { get; private set; } = 0;
    public PlayerAction PlayerAction { get; private set; } = PlayerAction.None;
    
    public System.Action<int> OnPotChanged;
    public System.Action<string> OnPlayerRaised;
    
    private bool canPlayerAct = false;
    private ActionButtonsController actionButtons;
    
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
        OnPotChanged?.Invoke(Pot);
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
        
        if (playerChips != null && playerChips.IsBroke())
        {
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
    
    private PlayerAction GetValidNPCAction(NPCController npc, NPCChips chips)
    {
        PlayerAction action = npc.MakeDecision();
        
        if (action == PlayerAction.Raise)
        {
            int raiseAmount = CurrentBet + 10;
            if (!chips.HasEnoughChips(raiseAmount))
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
                break;
                
            case PlayerAction.Call:
                npc.PlayCall();
                chips.RemoveChips(CurrentBet);
                AddToPot(CurrentBet);
                break;
                
            case PlayerAction.Raise:
                npc.PlayRaise();
                int raiseAmount = CurrentBet + 10;
                chips.RemoveChips(raiseAmount);
                CurrentBet = raiseAmount;
                AddToPot(CurrentBet);
                GameDebug.LogRaise(CurrentBet);
                OnPlayerRaised?.Invoke(npc.npcName);
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
                int raiseAmount = CurrentBet + 10;
                if (playerChips != null && playerChips.HasEnoughChips(raiseAmount))
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
                    int raiseAmount = CurrentBet + 10;
                    if (playerChips != null && playerChips.HasEnoughChips(raiseAmount))
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
                break;
                
            case PlayerAction.Call:
                if (playerChips != null)
                    playerChips.RemoveChips(CurrentBet);
                AddToPot(CurrentBet);
                break;
                
            case PlayerAction.Raise:
                int raiseAmount = CurrentBet + 10;
                if (playerChips != null)
                    playerChips.RemoveChips(raiseAmount);
                CurrentBet = raiseAmount;
                AddToPot(CurrentBet);
                GameDebug.LogRaise(CurrentBet);
                OnPlayerRaised?.Invoke("Игрок");
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
                toRemove.Add(npc);
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
    
    #region Utility
    
    public void SetNPCTurnDelay(float delay)
    {
        npcTurnDelay = delay;
    }
    
    public void SetActionButtons(ActionButtonsController buttons)
    {
        actionButtons = buttons;
    }
    
    #endregion
}