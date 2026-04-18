// GameLoop.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    [Header("Table Reference")]
    public TableController table;
    
    [Header("Settings")]
    public float npcTurnDelay = 1.0f;
    public float roundDelay = 2f;
    
    private int currentRound = 1;
    private int maxRounds = 5;
    private int currentBet = 0;
    private int pot = 0;
    
    private PlayerAction playerAction = PlayerAction.None;
    private bool canPlayerAct = false;
    
    private PlayerCardsController player;
    
    private Dictionary<NPCController, bool> npcTakeCardsFinished;
    private bool allTakeCardsFinished = false;
    
    void Start()
    {
        if (table == null)
            table = FindObjectOfType<TableController>();
        
        player = table.GetPlayer();
        StartCoroutine(MainLoop());
    }
    
    IEnumerator MainLoop()
    {
        Debug.Log("<color=yellow>=== SIGNAL TABLE ===</color>");
        Debug.Log("Управление: 1-Fold, 2-Call, 3-Raise");
        Debug.Log("================================\n");
        
        for (int round = 1; round <= maxRounds; round++)
        {
            yield return StartCoroutine(PlayRound(round));
        }
        
        Debug.Log("<color=yellow>=== ИГРА ЗАВЕРШЕНА ===</color>");
        Debug.Log("<color=cyan>Нажми R для рестарта</color>");
        
        WaitForRestart();
    }
    
    IEnumerator PlayRound(int roundNumber)
    {
        currentRound = roundNumber;
        pot = 0;
        currentBet = 0;
        
        Debug.Log($"<color=yellow>--- РАУНД {roundNumber}/{maxRounds} ---</color>");
        
        // 1. Все берут карты (одновременно)
        yield return StartCoroutine(TakeCardsPhase());
        
        // 2. Все показывают эмоции (одновременно)
        ShowEmotionsPhase();
        yield return new WaitForSeconds(0.5f);
        
        // 3. Ходят по очереди с интервалом
        yield return StartCoroutine(NPCBetsPhase());
        
        // 4. Ход игрока
        if (table.GetActivePlayersCount() > 0)
        {
            yield return StartCoroutine(PlayerTurnPhase());
        }
        else
        {
            Debug.Log("<color=cyan>Все NPC сфолдили, вы забираете банк!</color>");
        }
        
        // 5. Вскрытие
        ShowdownPhase();
        
        // 6. Все сбрасывают карты
        table.DiscardAllCards();
        table.ResetAllEmotions();
        
        yield return new WaitForSeconds(roundDelay);
        Debug.Log("");
    }
    
    IEnumerator TakeCardsPhase()
    {
        Debug.Log("<color=white>--- РАЗДАЧА КАРТ ---</color>");
        
        // Подписка на окончание анимаций NPC
        SubscribeToTakeCardsFinished();
        allTakeCardsFinished = false;
        npcTakeCardsFinished = new Dictionary<NPCController, bool>();
        foreach (var npc in table.GetAllNPCs())
        {
            npcTakeCardsFinished[npc] = false;
        }
        
        // Раздача всем
        table.DealNewRound();
        
        // Ждем пока все NPC возьмут карты
        yield return new WaitUntil(() => allTakeCardsFinished);
        UnsubscribeFromTakeCardsFinished();
        
        // Ждем пока игрок получит карты (анимация подъема)
        yield return new WaitForSeconds(1.5f);
        
        Debug.Log("<color=white>Все взяли карты</color>");
    }
    
    private void SubscribeToTakeCardsFinished()
    {
        foreach (var npc in table.GetAllNPCs())
        {
            npc.OnTakeCardsFinished += OnNPCTakeCardsFinished;
        }
    }
    
    private void UnsubscribeFromTakeCardsFinished()
    {
        foreach (var npc in table.GetAllNPCs())
        {
            npc.OnTakeCardsFinished -= OnNPCTakeCardsFinished;
        }
    }
    
    private void OnNPCTakeCardsFinished(NPCController npc)
    {
        npcTakeCardsFinished[npc] = true;
        
        foreach (var finished in npcTakeCardsFinished.Values)
        {
            if (!finished) return;
        }
        
        allTakeCardsFinished = true;
    }
    
    private void ShowEmotionsPhase()
    {
        Debug.Log("<color=white>--- ЭМОЦИИ ---</color>");
        
        // Все NPC показывают эмоции одновременно
        foreach (var npc in table.GetActiveNPCs())
        {
            npc.ShowEmotion();
            EmotionType emotion = npc.GetCurrentEmotion();
            string icon = GetEmotionIcon(emotion);
            Debug.Log($"  {npc.npcName}: {icon} {emotion}");
        }
    }
    
    IEnumerator NPCBetsPhase()
    {
        Debug.Log("<color=white>--- ХОД NPC ---</color>");
        
        foreach (var npc in table.GetActiveNPCs())
        {
            // NPC принимает решение
            PlayerAction npcAction = npc.MakeDecision();
            
            string actionText = GetActionShortText(npcAction);
            EmotionType emotion = npc.GetCurrentEmotion();
            string icon = GetEmotionIcon(emotion);
            
            Debug.Log($"<color=#FFA500>{npc.npcName}: {icon} → {actionText}</color>");
            
            switch (npcAction)
            {
                case PlayerAction.Fold:
                    npc.DiscardCards();
                    break;
                    
                case PlayerAction.Call:
                    npc.PlayCall();
                    pot += currentBet;
                    break;
                    
                case PlayerAction.Raise:
                    npc.PlayRaise();
                    currentBet += 10;
                    pot += currentBet;
                    Debug.Log($"  <color=yellow>Ставка повышена до {currentBet}!</color>");
                    break;
            }
            
            // Интервал между ходами
            yield return new WaitForSeconds(npcTurnDelay);
        }
        
        Debug.Log($"<color=white>Текущая ставка: {currentBet} | Банк: {pot}</color>");
        Debug.Log("<color=white>--- ХОД ИГРОКА ---</color>");
    }
    
    IEnumerator PlayerTurnPhase()
    {
        canPlayerAct = true;
        playerAction = PlayerAction.None;
        
        Debug.Log($"<color=cyan>Ваш ход! Ставка: {currentBet} | 1-Fold, 2-Call ({currentBet}), 3-Raise (+10)</color>");
        
        while (playerAction == PlayerAction.None && canPlayerAct)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                playerAction = PlayerAction.Fold;
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                playerAction = PlayerAction.Call;
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                playerAction = PlayerAction.Raise;
            
            yield return null;
        }
        
        canPlayerAct = false;
        
        string actionText = playerAction switch
        {
            PlayerAction.Fold => "FOLD",
            PlayerAction.Call => $"CALL ({currentBet})",
            PlayerAction.Raise => $"RAISE ({currentBet + 10})",
            _ => "???"
        };
        
        Debug.Log($"<color=cyan>Вы выбрали: {actionText}</color>");
        
        switch (playerAction)
        {
            case PlayerAction.Fold:
                player.FoldCards();
                Debug.Log("<color=red>Вы сбросили карты</color>");
                break;
                
            case PlayerAction.Call:
                pot += currentBet;
                break;
                
            case PlayerAction.Raise:
                currentBet += 10;
                pot += currentBet;
                Debug.Log($"<color=yellow>Вы повысили ставку до {currentBet}!</color>");
                break;
        }
    }
    
    private void ShowdownPhase()
    {
        Debug.Log("<color=white>--- ВСКРЫТИЕ ---</color>");
        
        if (playerAction != PlayerAction.Fold)
            Debug.Log($"  Вы: {player.GetHandDescription()}");
        else
            Debug.Log($"  Вы: ФОЛД");
        
        int strongNPCs = 0;
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc.HasCardsActive)
            {
                string strengthText = npc.GetStrengthText();
                Debug.Log($"  {npc.npcName}: {strengthText}");
                
                if (npc.TrueHandStrength == HandStrength.Strong)
                    strongNPCs++;
            }
            else
            {
                Debug.Log($"  {npc.npcName}: ФОЛД");
            }
        }
        
        Debug.Log($"<color=white>Банк: {pot}</color>");
        
        if (playerAction != PlayerAction.Fold)
        {
            HandStrength playerStrength = player.GetCurrentHandStrength();
            
            if (playerStrength == HandStrength.Strong)
                Debug.Log("<color=green>✓ У вас сильная рука!</color>");
            else if (playerStrength == HandStrength.Weak)
                Debug.Log("<color=red>✗ У вас слабая рука</color>");
        }
        
        Debug.Log($"<color=grey>Сильных рук у NPC: {strongNPCs}/{table.GetAllNPCs().Count}</color>");
    }
    
    void WaitForRestart()
    {
        StartCoroutine(RestartWaiter());
    }
    
    IEnumerator RestartWaiter()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            yield return null;
        }
    }
    
    private string GetEmotionIcon(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => "😊",
            EmotionType.Neutral => "😐",
            EmotionType.Angry => "😠",
            _ => "❓"
        };
    }
    
    private string GetActionShortText(PlayerAction action)
    {
        return action switch
        {
            PlayerAction.Fold => "🏃 FOLD",
            PlayerAction.Call => "✅ CALL",
            PlayerAction.Raise => "⬆️ RAISE",
            _ => "???"
        };
    }
}