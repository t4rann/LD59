using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class GameLoop : MonoBehaviour
{
    [Header("Table Reference")]
    [SerializeField] private TableController table;
    
    [Header("Round Settings")]
    [SerializeField] private int maxRounds = 5;
    [SerializeField] private float roundDelay = 2f;
    [SerializeField] private int anteAmount = 10;
    
    [Header("Phase Delays")]
    [SerializeField] private float dealAnimationDelay = 1.5f;
    [SerializeField] private float emotionsPhaseDelay = 0.5f;
    [SerializeField] private float npcTurnDelay = 1.0f;
    
    [Header("Visual Controllers")]
    [SerializeField] private BankChipsVisualController bankVisual;
    
    [Header("Action Buttons")]
    [SerializeField] private ActionButtonsController actionButtons;
    
    [Header("Level System")]
    [SerializeField] private LevelData[] levels;
    [SerializeField] private LevelController levelController;
    
    [Header("Level Transition Effects")]
    [SerializeField] private LevelTransitionEffects transitionEffects;
    [SerializeField] private float levelTransitionDelay = 0.5f;
    
    [Header("Final Scene")]
    [SerializeField] private FinalSceneController finalSceneController;
    [SerializeField] private float finalSceneDelay = 1f;
    
    private RoundController roundController;
    private BettingController bettingController;
    private ShowdownController showdownController;
    private PlayerChips playerChips;
    
    private int currentRound = 1;
    private int currentLevelIndex = 0;
    private List<NPCController> finalLevelNPCs = new List<NPCController>();
    
    void Start()
    {
        if (table == null)
            table = FindFirstObjectByType<TableController>();
        
        if (transitionEffects == null)
            transitionEffects = FindFirstObjectByType<LevelTransitionEffects>();
        
        playerChips = table.GetPlayerChips();
        
        roundController = new RoundController(table, dealAnimationDelay, emotionsPhaseDelay);
        bettingController = new BettingController(table, npcTurnDelay, anteAmount);
        showdownController = new ShowdownController(table, bettingController);
        
        if (bettingController != null)
        {
            bettingController.OnPotChanged += OnPotChanged;
            
            if (actionButtons != null)
            {
                bettingController.SetActionButtons(actionButtons);
                actionButtons.SetBettingController(bettingController);
                Debug.Log("GameLoop: ActionButtons connected to BettingController");
            }
            else
            {
                Debug.LogWarning("ActionButtons is null! Make sure it's assigned in inspector.");
            }
        }
        
        if (actionButtons != null)
            actionButtons.ShowButtons(false);
        
        StartCoroutine(MainLoop());
    }
    
    private void OnPotChanged(int pot)
    {
        if (bankVisual != null)
            bankVisual.UpdateBankDisplay(pot);
    }
    
    IEnumerator MainLoop()
    {
        GameDebug.LogHeader("SIGNAL TABLE");
        
        if (levels == null || levels.Length == 0)
        {
            GameDebug.LogInfo($"Управление: Кнопки внизу экрана | Анте: {anteAmount}");
            GameDebug.LogDivider();
            
            for (int round = 1; round <= maxRounds; round++)
            {
                if (GameManager.Instance.IsGameOver())
                    yield break;
                
                yield return PlayRound(round);
                bettingController.CheckAndRemoveBrokeNPCs();
            }
            
            table.FullCleanup();
            GameDebug.LogHeader("ИГРА ЗАВЕРШЕНА");
            GameDebug.LogInfo("Нажми R для рестарта");
            StartCoroutine(RestartWaiter());
            yield break;
        }
        
        for (currentLevelIndex = 0; currentLevelIndex < levels.Length; currentLevelIndex++)
        {
            LevelData level = levels[currentLevelIndex];
            
            if (level.isFinalLevel)
            {
                GameDebug.LogHeader($"ФИНАЛЬНЫЙ УРОВЕНЬ {currentLevelIndex + 1}: {level.levelName}");
                GameDebug.LogInfo($"БОСС-БИТВА! Раундов: {level.roundsCount}");
                
                yield return StartCoroutine(PlayFinalLevel(level));
                
                if (!GameManager.Instance.IsGameOver())
                {
                    yield return StartCoroutine(PlayFinalScene());
                }
                break;
            }
            
            if (currentLevelIndex == 0)
            {
                if (playerChips != null && level.isPlayerInvolved)
                {
                    playerChips.SetChipsForLevel(level.chipsForLevel);
                }
                LoadNewLevel(level);
            }
            else
            {
                if (transitionEffects != null)
                {
                    yield return StartCoroutine(transitionEffects.StartTransition());
                    LoadNewLevel(level);
                    yield return StartCoroutine(transitionEffects.EndTransition());
                    yield return new WaitForSeconds(levelTransitionDelay);
                }
                else
                {
                    LoadNewLevel(level);
                }
            }
            
            GameDebug.LogHeader($"УРОВЕНЬ {currentLevelIndex + 1}: {level.levelName}");
            if (level.isPlayerInvolved)
            {
                GameDebug.LogInfo($"Раундов: {level.roundsCount} | Анте: {level.anteAmount} | Фишек игрока: {level.chipsForLevel} | Фишек NPC: {level.npcStartingChips} | Рейз: {level.raiseAmount}");
            }
            else
            {
                GameDebug.LogInfo($"Раундов: {level.roundsCount} | Анте: {level.anteAmount} | Фишек NPC: {level.npcStartingChips} | Рейз: {level.raiseAmount} | Режим: NPC vs NPC");
            }
            GameDebug.LogDivider();
            
            for (int round = 1; round <= level.roundsCount; round++)
            {
                if (GameManager.Instance.IsGameOver())
                    yield break;
                
                if (level.isPlayerInvolved)
                {
                    yield return PlayRound(round, level);
                }
                else
                {
                    yield return PlayNPCRound(round, level);
                }
                
                bettingController.CheckAndRemoveBrokeNPCs();
            }
            
            if (playerChips != null && level.isPlayerInvolved && playerChips.IsBroke())
            {
                GameDebug.LogError("Игрок проиграл все фишки на уровне!");
                GameManager.Instance.PlayerOutOfChips();
                yield break;
            }
            
            if (currentLevelIndex < levels.Length - 1 && !levels[currentLevelIndex + 1].isFinalLevel)
            {
                GameDebug.LogSuccess($"Уровень {currentLevelIndex + 1} пройден!");
                
                if (transitionEffects != null)
                    yield return StartCoroutine(transitionEffects.PlayLevelComplete());
                
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    private IEnumerator PlayFinalLevel(LevelData level)
    {

    finalLevelNPCs.Clear();
    
    // Устанавливаем флаг финального уровня (не удаляем NPC)
    if (bettingController != null)
    {
        bettingController.SetFinalLevel(true);
        bettingController.SetDeleteNPCsOnLoss(false);
    }

        
        if (currentLevelIndex == 0)
        {
            if (playerChips != null && level.isPlayerInvolved)
            {
                playerChips.SetChipsForLevel(level.chipsForLevel);
            }
            LoadNewLevel(level);
        }
        else
        {
            if (transitionEffects != null)
            {
                yield return StartCoroutine(transitionEffects.StartTransition());
                LoadNewLevel(level);
                yield return StartCoroutine(transitionEffects.EndTransition());
                yield return new WaitForSeconds(levelTransitionDelay);
            }
            else
            {
                LoadNewLevel(level);
            }
        }
        
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null)
            {
                finalLevelNPCs.Add(npc);
            }
        }
        
        if (finalSceneController != null)
        {
            finalSceneController.SetFinalNPCs(finalLevelNPCs.ToArray());
        }
        
        GameDebug.LogHeader($"ФИНАЛЬНЫЙ УРОВЕНЬ {currentLevelIndex + 1}: {level.levelName}");
        if (level.isPlayerInvolved)
        {
            GameDebug.LogInfo($"Раундов: {level.roundsCount} | Анте: {level.anteAmount} | Фишки игрока: {level.chipsForLevel} | Фишки NPC: {level.npcStartingChips} | Рейз: {level.raiseAmount}");
        }
        else
        {
            GameDebug.LogInfo($"Раундов: {level.roundsCount} | Анте: {level.anteAmount} | Фишки NPC: {level.npcStartingChips} | Рейз: {level.raiseAmount} | БИТВА БОССОВ!");
        }
        GameDebug.LogDivider();
        
        for (int round = 1; round <= level.roundsCount; round++)
        {
            if (GameManager.Instance.IsGameOver())
                yield break;
            
            if (level.isPlayerInvolved)
            {
                yield return PlayRound(round, level);
            }
            else
            {
                yield return PlayNPCRound(round, level);
            }
            
            bettingController.CheckAndRemoveBrokeNPCs();
        }
        
        if (playerChips != null && level.isPlayerInvolved && playerChips.IsBroke())
        {
            GameDebug.LogError("Игрок проиграл все фишки!");
            GameManager.Instance.PlayerOutOfChips();
            yield break;
        }
    }
    
private IEnumerator PlayFinalScene()
{
    // Сброс флагов
    if (bettingController != null)
    {
        bettingController.SetFinalLevel(false);
        bettingController.SetDeleteNPCsOnLoss(true);
    }
    
        GameDebug.LogHeader("ФИНАЛЬНАЯ СЦЕНА");
        
        if (finalSceneController != null)
        {
            if (finalLevelNPCs.Count > 0)
            {
                finalSceneController.SetFinalNPCs(finalLevelNPCs.ToArray());
            }
            
            yield return new WaitForSeconds(finalSceneDelay);
            yield return StartCoroutine(finalSceneController.PlayFinalScene());
        }
        else
        {
            if (transitionEffects != null)
            {
                yield return StartCoroutine(transitionEffects.StartTransition());
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(transitionEffects.EndTransition());
            }
        }
        
        table.FullCleanup();
        GameDebug.LogHeader("ИГРА ПРОЙДЕНА! ПОБЕДА!");
        GameDebug.LogInfo("Нажми R для рестарта");
        StartCoroutine(RestartWaiter());
    }
    
    IEnumerator PlayNPCRound(int roundNumber, LevelData level)
    {
        currentRound = roundNumber;
        bettingController.ResetRoundState();
        
        if (bankVisual != null)
            bankVisual.ClearBank();
        
        GameDebug.LogRound(roundNumber, level.roundsCount);
        
        int aliveNPCs = 0;
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null)
            {
                NPCChips chips = npc.GetComponent<NPCChips>();
                if (chips != null && !chips.IsBroke())
                    aliveNPCs++;
            }
        }
        
        if (aliveNPCs <= 1)
        {
            GameDebug.LogSuccess("Остался только один NPC! Битва окончена!");
            yield break;
        }
        
        if (!bettingController.RemoveBrokeNPCs())
        {
            GameDebug.LogSuccess("Все NPC покинули стол!");
            yield break;
        }
        
        bool anteCollected = bettingController.CollectAnteFromNPCsOnly(level.anteAmount);
        if (!anteCollected)
        {
            GameDebug.LogError("Не удалось собрать анте с NPC!");
            yield break;
        }
        
        yield return roundController.DealPhase();
        yield return roundController.EmotionsPhase();
        
        yield return bettingController.BettingPhase();
        
        if (GameManager.Instance.IsGameOver())
            yield break;
        
        showdownController.ShowdownPhase(PlayerAction.None);
        
        roundController.CleanupPhase();
        table.DiscardAllCards();
        
        yield return new WaitForSeconds(3f);
        
        table.HideAllNPCCards();
        table.ShowAllNPCCards();
        
        yield return new WaitForSeconds(roundDelay);
        Debug.Log("");
    }
    
    private void LoadNewLevel(LevelData level)
    {
        if (table != null)
        {
            table.ClearAllNPCs();
        }
        
        if (playerChips != null && level.isPlayerInvolved)
        {
            playerChips.SetChipsForLevel(level.chipsForLevel);
            Debug.Log($"Игроку выдано {level.chipsForLevel} фишек");
        }
        
        if (bettingController != null)
        {
            bettingController.SetRaiseAmount(level.raiseAmount);
        }
        
        if (levelController != null)
        {
            levelController.LoadLevel(level, table);
        }
        
        maxRounds = level.roundsCount;
        anteAmount = level.anteAmount;
        
        if (bettingController != null)
        {
            bettingController.SetAnteAmount(anteAmount);
            bettingController.ResetRoundState();
        }
        
    }
    
    IEnumerator PlayRound(int roundNumber)
    {
        currentRound = roundNumber;
        bettingController.ResetRoundState();
        
        if (bankVisual != null)
            bankVisual.ClearBank();
        
        GameDebug.LogRound(roundNumber, maxRounds);
        
        PlayerChips playerChipsRef = table.GetPlayerChips();
        if (playerChipsRef == null || playerChipsRef.IsBroke())
        {
            GameDebug.LogError("У игрока нет фишек для игры!");
            GameManager.Instance.PlayerOutOfChips();
            yield break;
        }
        
        if (!bettingController.RemoveBrokeNPCs())
        {
            GameDebug.LogWarning("Все NPC покинули стол!");
            GameManager.Instance.OnGameOver?.Invoke();
            yield break;
        }
        
        if (!bettingController.CollectAnte())
        {
            GameDebug.LogError("Не удалось собрать анте!");
            GameManager.Instance.OnGameOver?.Invoke();
            yield break;
        }
        
        yield return roundController.DealPhase();
        yield return roundController.EmotionsPhase();
        
        if (actionButtons != null)
            actionButtons.ShowButtons(false);
        
        yield return bettingController.BettingPhase();
        
        if (actionButtons != null)
            actionButtons.ShowButtons(true);
        
        yield return bettingController.PlayerPhase();
        
        if (actionButtons != null)
            actionButtons.ShowButtons(false);
        
        if (GameManager.Instance.IsGameOver())
            yield break;
        
        showdownController.ShowdownPhase(bettingController.PlayerAction);
        
        bettingController.CheckPlayerLossAfterShowdown();
        
        roundController.CleanupPhase();
        table.DiscardAllCards();
        
        yield return new WaitForSeconds(3f);
        
        table.HideAllNPCCards();
        table.ShowAllNPCCards();
        
        yield return new WaitForSeconds(roundDelay);
        Debug.Log("");
    }
    
    IEnumerator PlayRound(int roundNumber, LevelData level)
    {
        currentRound = roundNumber;
        bettingController.ResetRoundState();
        
        if (bankVisual != null)
            bankVisual.ClearBank();
        
        GameDebug.LogRound(roundNumber, level.roundsCount);
        
        PlayerChips playerChipsRef = table.GetPlayerChips();
        if (playerChipsRef == null || playerChipsRef.IsBroke())
        {
            GameDebug.LogError("У игрока нет фишек для игры!");
            GameManager.Instance.PlayerOutOfChips();
            yield break;
        }
        
        int aliveNPCs = 0;
        foreach (var npc in table.GetAllNPCs())
        {
            if (npc != null)
            {
                NPCChips chips = npc.GetComponent<NPCChips>();
                if (chips != null && !chips.IsBroke())
                    aliveNPCs++;
            }
        }
        
        if (aliveNPCs == 0)
        {
            GameDebug.LogSuccess("Все NPC разорились! Вы победили уровень!");
            yield break;
        }
        
        if (!bettingController.RemoveBrokeNPCs())
        {
            GameDebug.LogSuccess("Все NPC покинули стол! Вы победили!");
            yield break;
        }
        
        if (!bettingController.CollectAnte())
        {
            int remainingNPCs = 0;
            foreach (var npc in table.GetAllNPCs())
            {
                if (npc != null) remainingNPCs++;
            }
            
            if (remainingNPCs == 0)
            {
                GameDebug.LogSuccess("Нет NPC для игры! Уровень пройден!");
                yield break;
            }
            
            GameDebug.LogError("Не удалось собрать анте!");
            GameManager.Instance.OnGameOver?.Invoke();
            yield break;
        }
        
        yield return roundController.DealPhase();
        yield return roundController.EmotionsPhase();
        
        if (actionButtons != null)
            actionButtons.ShowButtons(false);
        
        yield return bettingController.BettingPhase();
        
        if (actionButtons != null)
            actionButtons.ShowButtons(true);
        
        yield return bettingController.PlayerPhase();
        
        if (actionButtons != null)
            actionButtons.ShowButtons(false);
        
        if (GameManager.Instance.IsGameOver())
            yield break;
        
        showdownController.ShowdownPhase(bettingController.PlayerAction);
        
        bettingController.CheckPlayerLossAfterShowdown();
        
        roundController.CleanupPhase();
        table.DiscardAllCards();
        
        yield return new WaitForSeconds(3f);
        
        table.HideAllNPCCards();
        table.ShowAllNPCCards();
        
        yield return new WaitForSeconds(roundDelay);
        Debug.Log("");
    }
    
    IEnumerator RestartWaiter()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.R))
                GameManager.Instance.RestartGame();
            yield return null;
        }
    }
    
    public void SetMaxRounds(int rounds) => maxRounds = rounds;
    public void SetAnteAmount(int ante) => anteAmount = ante;
    
    void OnDestroy()
    {
        if (bettingController != null)
            bettingController.OnPotChanged -= OnPotChanged;
    }
}