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
    
    private bool isLoadingLevel = false;
    private bool isRestartingLevel = false;
    
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
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRestartCurrentLevel += RestartCurrentLevel;
        }
        
        StartCoroutine(MainLoop());
    }
    
    void OnDestroy()
    {
        if (bettingController != null)
            bettingController.OnPotChanged -= OnPotChanged;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRestartCurrentLevel -= RestartCurrentLevel;
        }
    }
    
    private void OnPotChanged(int pot)
    {
        if (bankVisual != null)
            bankVisual.UpdateBankDisplay(pot);
    }
    
// В методе RestartCurrentLevel убедитесь, что порядок правильный:
private void RestartCurrentLevel()
{
    if (isRestartingLevel) return;
    
    isRestartingLevel = true;
    
    GameDebug.LogHeader($"ПЕРЕЗАПУСК УРОВНЯ {currentLevelIndex + 1}...");
    
    StopAllCoroutines();
    
    // 1. Останавливаем все процессы
    if (bettingController != null)
    {
        bettingController.ForceStop();
        bettingController.ResetRoundState();
    }
    
    // 2. Очищаем визуал банка
    if (bankVisual != null)
    {
        bankVisual.ClearBank();
        bankVisual.ResetBank();
    }
    
    // 3. Очищаем стол ОДИН раз
    if (table != null)
    {
        table.FullCleanup();
        table.ClearAllNPCs(); // Это очистит список NPC
    }
    
    currentRound = 1;
    
    if (levels != null && currentLevelIndex < levels.Length)
    {
        LevelData currentLevel = levels[currentLevelIndex];
        
        if (playerChips != null)
        {
            playerChips.SetChipsForLevel(currentLevel.chipsForLevel);
            GameDebug.LogInfo($"Игроку выдано {currentLevel.chipsForLevel} фишек для перезапуска уровня");
        }
        
        // Загружаем уровень (без повторной очистки)
        if (levelController != null)
        {
            levelController.ReloadCurrentLevel(table);
        }
        
        if (bettingController != null)
        {
            bettingController.SetRaiseAmount(currentLevel.raiseAmount);
            bettingController.SetAnteAmount(currentLevel.anteAmount);
            bettingController.SetFinalLevel(currentLevel.isFinalLevel);
        }
        
        roundController = new RoundController(table, dealAnimationDelay, emotionsPhaseDelay);
    }
    
    isRestartingLevel = false;
    
    StartCoroutine(MainLoop());
}
    
IEnumerator MainLoop()
{
    GameDebug.LogHeader("SIGNAL TABLE");
    
    // Очищаем стол перед началом игры
    if (table != null)
    {
        table.FullCleanup();
        table.ClearAllNPCs();
    }
    
    if (levels == null || levels.Length == 0)
    {
        yield return StartCoroutine(PlayWithoutLevels());
        yield break;
    }

        
        for (currentLevelIndex = 0; currentLevelIndex < levels.Length; currentLevelIndex++)
        {
            if (isRestartingLevel) yield break;
            
            LevelData level = levels[currentLevelIndex];
            
            if (level.isFinalLevel)
            {
                yield return StartCoroutine(PlayFinalLevel(level));
                
                if (GameManager.Instance != null && !GameManager.Instance.IsGameOver())
                {
                    yield return StartCoroutine(PlayFinalScene());
                }
                break;
            }
            
            if (currentLevelIndex == 0)
            {
                LoadNewLevel(level);
            }
            else
            {
                if (transitionEffects != null)
                {
                    yield return StartCoroutine(transitionEffects.StartTransition());
                    if (isRestartingLevel) yield break;
                    LoadNewLevel(level);
                    yield return StartCoroutine(transitionEffects.EndTransition());
                    yield return new WaitForSeconds(levelTransitionDelay);
                }
                else
                {
                    LoadNewLevel(level);
                }
            }
            
            if (isRestartingLevel) yield break;
            
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
            
            bool levelFailed = false;
            
            for (int round = 1; round <= level.roundsCount; round++)
            {
                if (isRestartingLevel) yield break;
                
                if (level.isPlayerInvolved)
                {
                    yield return PlayRound(round, level);
                }
                else
                {
                    yield return PlayNPCRound(round, level);
                }
                
                if (bettingController != null)
                {
                    bettingController.CheckAndRemoveBrokeNPCs();
                }
                
                if (playerChips != null && level.isPlayerInvolved && playerChips.IsBroke())
                {
                    GameDebug.LogError("Игрок проиграл все фишки! Перезапуск уровня...");
                    levelFailed = true;
                    break;
                }
            }
            
            if (levelFailed)
            {
                RestartCurrentLevel();
                yield break;
            }
            
            if (isRestartingLevel) yield break;
            
            if (currentLevelIndex < levels.Length - 1 && !levels[currentLevelIndex + 1].isFinalLevel)
            {
                GameDebug.LogSuccess($"Уровень {currentLevelIndex + 1} пройден!");
                
                if (transitionEffects != null)
                {
                    yield return StartCoroutine(transitionEffects.PlayLevelComplete());
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        GameDebug.LogHeader("ИГРА ПРОЙДЕНА ПОЛНОСТЬЮ!");
    }
    
    private IEnumerator PlayWithoutLevels()
    {
        GameDebug.LogInfo($"Управление: Кнопки внизу экрана | Анте: {anteAmount}");
        GameDebug.LogDivider();
        
        for (int round = 1; round <= maxRounds; round++)
        {
            if (isRestartingLevel) yield break;
            
            yield return PlayRound(round);
            
            if (bettingController != null)
                bettingController.CheckAndRemoveBrokeNPCs();
            
            if (playerChips != null && playerChips.IsBroke())
            {
                GameDebug.LogError("Игрок проиграл все фишки! Игра окончена.");
                GameDebug.LogInfo("Нажми R для рестарта");
                yield break;
            }
        }
        
        table.FullCleanup();
        GameDebug.LogHeader("ИГРА ЗАВЕРШЕНА");
        GameDebug.LogInfo("Нажми R для рестарта");
    }
    
private void LoadNewLevel(LevelData level)
{
    if (isLoadingLevel) return;
    isLoadingLevel = true;
    
    try
    {
        // Убираем очистку NPC и карт - это уже сделано до вызова LoadNewLevel
        // if (table != null)
        // {
        //     table.ClearAllNPCs();
        //     table.DiscardAllCards();
        // }
        
        if (bankVisual != null)
        {
            bankVisual.ClearBank();
            bankVisual.ResetBank();
        }
        
        if (levelController != null)
        {
            levelController.LoadLevel(level, table);
        }
        
        if (playerChips != null && level.isPlayerInvolved)
        {
            playerChips.SetChipsForLevel(level.chipsForLevel);
            GameDebug.LogInfo($"Игроку выдано {level.chipsForLevel} фишек");
        }
        
        if (bettingController != null)
        {
            bettingController.SetRaiseAmount(level.raiseAmount);
            bettingController.SetAnteAmount(level.anteAmount);
            bettingController.ResetRoundState();
            bettingController.SetFinalLevel(level.isFinalLevel);
        }
        
        maxRounds = level.roundsCount;
        anteAmount = level.anteAmount;
        
        roundController = new RoundController(table, dealAnimationDelay, emotionsPhaseDelay);
        
    }
    catch (System.Exception e)
    {
        GameDebug.LogError($"Ошибка загрузки уровня: {e.Message}");
    }
    finally
    {
        isLoadingLevel = false;
    }
}
    
    private IEnumerator PlayFinalLevel(LevelData level)
    {
        finalLevelNPCs.Clear();
        
        if (bettingController != null)
        {
            bettingController.SetFinalLevel(true);
            bettingController.SetDeleteNPCsOnLoss(false);
        }
        
        LoadNewLevel(level);
        
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
        GameDebug.LogDivider();
        
        bool levelFailed = false;
        
        for (int round = 1; round <= level.roundsCount; round++)
        {
            if (isRestartingLevel) yield break;
            
            if (level.isPlayerInvolved)
            {
                yield return PlayRound(round, level);
            }
            else
            {
                yield return PlayNPCRound(round, level);
            }
            
            if (bettingController != null)
                bettingController.CheckAndRemoveBrokeNPCs();
            
            if (playerChips != null && level.isPlayerInvolved && playerChips.IsBroke())
            {
                GameDebug.LogError("Игрок проиграл все фишки!");
                levelFailed = true;
                break;
            }
        }
        
        if (levelFailed)
        {
            RestartCurrentLevel();
        }
    }
    
    private IEnumerator PlayFinalScene()
    {
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
    }
    
    IEnumerator PlayRound(int roundNumber)
    {
        if (bettingController == null) yield break;
        
        currentRound = roundNumber;
        bettingController.ResetRoundState();
        
        if (bankVisual != null)
            bankVisual.ClearBank();
        
        GameDebug.LogRound(roundNumber, maxRounds);
        
        PlayerChips playerChipsRef = table.GetPlayerChips();
        if (playerChipsRef == null || playerChipsRef.IsBroke())
        {
            GameDebug.LogError("У игрока нет фишек для игры!");
            if (GameManager.Instance != null)
                GameManager.Instance.PlayerOutOfChips();
            yield break;
        }
        
        if (!bettingController.RemoveBrokeNPCs())
        {
            GameDebug.LogWarning("Все NPC покинули стол!");
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver?.Invoke();
            yield break;
        }
        
        if (!bettingController.CollectAnte())
        {
            GameDebug.LogError("Не удалось собрать анте!");
            if (GameManager.Instance != null)
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
        
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
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
        if (bettingController == null) yield break;
        
        currentRound = roundNumber;
        bettingController.ResetRoundState();
        
        if (bankVisual != null)
            bankVisual.ClearBank();
        
        GameDebug.LogRound(roundNumber, level.roundsCount);
        
        PlayerChips playerChipsRef = table.GetPlayerChips();
        if (playerChipsRef == null || playerChipsRef.IsBroke())
        {
            GameDebug.LogError("У игрока нет фишек для игры!");
            if (GameManager.Instance != null)
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
            if (GameManager.Instance != null)
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
        
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
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
    
    IEnumerator PlayNPCRound(int roundNumber, LevelData level)
    {
        if (bettingController == null) yield break;
        
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
        
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
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
    
    public void SetMaxRounds(int rounds) => maxRounds = rounds;
    public void SetAnteAmount(int ante) => anteAmount = ante;
}