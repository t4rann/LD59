// BettingController.cs
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
    
    private bool canPlayerAct = false;
    
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
    
    public bool CollectAnte()
    {
        GameDebug.LogPhase("СБОР АНТЕ");
        
        if (!playerChips.HasEnoughChips(anteAmount))
        {
            GameDebug.LogError("У вас недостаточно фишек для анте!");
            return false;
        }
        
        playerChips.RemoveChips(anteAmount);
        AddToPot(anteAmount);
        GameDebug.LogInfo($"Вы внесли анте: {anteAmount}");
        
        foreach (var npc in table.GetAllNPCs())
        {
            NPCChips chips = npc.GetComponent<NPCChips>();
            
            if (chips == null)
            {
                GameDebug.LogError($"{npc.npcName}: нет компонента фишек!");
                continue;
            }
            
            if (!chips.HasEnoughChips(anteAmount))
            {
                GameDebug.LogWarning($"{npc.npcName}: недостаточно фишек для анте, покидает стол!");
                continue;
            }
            
            chips.RemoveChips(anteAmount);
            AddToPot(anteAmount);
            GameDebug.LogInfo($"  {npc.npcName} внес анте: {anteAmount}");
        }
        
        GameDebug.LogSuccess($"Банк после сбора анте: {Pot}");
        return true;
    }
    
    #endregion
    
    #region Betting Phases
    
    public IEnumerator BettingPhase()
    {
        GameDebug.LogPhase("ХОД NPC");
        
        foreach (var npc in table.GetActiveNPCs())
        {
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
            playerChips.AddChips(Pot);
            Pot = 0;
            OnPotChanged?.Invoke(Pot);
            yield break;
        }
        
        yield return ProcessPlayerTurn();
        
        if (playerChips.IsBroke())
        {
            GameManager.Instance.PlayerOutOfChips();
        }
    }
    
    #endregion
    
    #region NPC Turn Processing
    
    private IEnumerator ProcessNPCTurn(NPCController npc)
    {
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
                break;
        }
    }
    
    #endregion
    
    #region Player Turn Processing
    
    private IEnumerator ProcessPlayerTurn()
    {
        canPlayerAct = true;
        PlayerAction = PlayerAction.None;
        
        GameDebug.LogPlayerTurn(CurrentBet, player.GetHandValue(), playerChips.GetChips());
        
        while (PlayerAction == PlayerAction.None && canPlayerAct)
        {
            PlayerAction = GetPlayerInput();
            yield return null;
        }
        
        canPlayerAct = false;
        ExecutePlayerAction();
        GameDebug.LogChips(playerChips.GetChips());
    }
    
    private PlayerAction GetPlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            return PlayerAction.Fold;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (playerChips.HasEnoughChips(CurrentBet))
                return PlayerAction.Call;
            else
                GameDebug.LogWarning("Недостаточно фишек для Call!");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            int raiseAmount = CurrentBet + 10;
            if (playerChips.HasEnoughChips(raiseAmount))
                return PlayerAction.Raise;
            else
                GameDebug.LogWarning("Недостаточно фишек для Raise!");
        }
        
        return PlayerAction.None;
    }
    
    private void ExecutePlayerAction()
    {
        GameDebug.LogPlayerAction(PlayerAction, CurrentBet);
        
        switch (PlayerAction)
        {
            case PlayerAction.Fold:
                player.FoldCards();
                GameDebug.LogWarning("Вы сбросили карты");
                break;
                
            case PlayerAction.Call:
                playerChips.RemoveChips(CurrentBet);
                AddToPot(CurrentBet);
                break;
                
            case PlayerAction.Raise:
                int raiseAmount = CurrentBet + 10;
                playerChips.RemoveChips(raiseAmount);
                CurrentBet = raiseAmount;
                AddToPot(CurrentBet);
                GameDebug.LogRaise(CurrentBet);
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
            NPCChips chips = npc.GetComponent<NPCChips>();
            if (chips != null && chips.IsBroke())
            {
                brokeNPCs.Add(npc);
            }
        }
        
        foreach (var npc in brokeNPCs)
        {
            GameManager.Instance.NPCOutOfChips(npc);
        }
    }

    public List<NPCController> GetActiveNPCs()
    {
        List<NPCController> active = new List<NPCController>();
        foreach (var npc in table.GetAllNPCs())
        {
            // Только те, у кого есть карты (не сфолдили)
            if (npc.HasCardsActive)
                active.Add(npc);
        }
        return active;
    }

    public bool RemoveBrokeNPCs()
    {
        List<NPCController> toRemove = new List<NPCController>();
        
        foreach (var npc in table.GetAllNPCs())
        {
            NPCChips chips = npc.GetComponent<NPCChips>();
            if (chips == null) continue;
            
            if (chips.IsBroke())
            {
                toRemove.Add(npc);
            }
        }
        
        foreach (var npc in toRemove)
        {
            table.RemoveNPC(npc);
            npc.gameObject.SetActive(false);
            GameDebug.LogWarning($"{npc.npcName} покинул стол!");
        }
        
        return table.GetAllNPCs().Count > 0;
    }
    
    #endregion
    
    #region Utility
    
    public void SetNPCTurnDelay(float delay)
    {
        npcTurnDelay = delay;
    }
    
    #endregion
}