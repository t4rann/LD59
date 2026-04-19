using System.Collections;
using UnityEngine;

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
    
    private RoundController roundController;
    private BettingController bettingController;
    private ShowdownController showdownController;
    
    private int currentRound = 1;
    private int currentLevelIndex = 0;
    
    void Start()
    {
        if (table == null)
            table = FindFirstObjectByType<TableController>();
        
        if (transitionEffects == null)
            transitionEffects = FindFirstObjectByType<LevelTransitionEffects>();
        
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
            
            // Для первого уровня - без перехода
            if (currentLevelIndex == 0)
            {
                LoadNewLevel(level);
            }
            else
            {
                // Для следующих уровней - с переходом
                if (transitionEffects != null)
                {
                    GameDebug.LogInfo($"Переход на уровень {currentLevelIndex + 1}...");
                    
                    // 1. ЗАПУСКАЕМ ПАРТИКЛ И ЗАТЕМНЕНИЕ (параллельно)
                    yield return StartCoroutine(transitionEffects.StartTransition());
                    
                    // 2. В ТЕМНОТЕ загружаем новый уровень
                    LoadNewLevel(level);
                    
                    // 3. ОСВЕТЛЕНИЕ - новый уровень уже на месте
                    yield return StartCoroutine(transitionEffects.EndTransition());
                    yield return new WaitForSeconds(levelTransitionDelay);
                }
                else
                {
                    LoadNewLevel(level);
                }
            }
            
            // Обновляем настройки BettingController
            bettingController.SetAnteAmount(anteAmount);
            bettingController.ResetRoundState();
            
            GameDebug.LogHeader($"УРОВЕНЬ {currentLevelIndex + 1}: {level.levelName}");
            GameDebug.LogInfo($"Раундов: {maxRounds} | Анте: {anteAmount} | Управление: Кнопки внизу экрана");
            GameDebug.LogDivider();
            
            for (int round = 1; round <= maxRounds; round++)
            {
                if (GameManager.Instance.IsGameOver())
                    yield break;
                
                yield return PlayRound(round);
                bettingController.CheckAndRemoveBrokeNPCs();
            }
            
            if (GameManager.Instance.IsPlayerBroke())
            {
                GameDebug.LogError("Игрок проиграл все фишки!");
                yield break;
            }
            
            if (currentLevelIndex < levels.Length - 1)
            {
                GameDebug.LogSuccess($"Уровень {currentLevelIndex + 1} пройден!");
                yield return new WaitForSeconds(1f);
            }
        }
        
        // Финальная победа
        if (transitionEffects != null)
        {
            yield return StartCoroutine(transitionEffects.StartTransition());
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(transitionEffects.EndTransition());
        }
        
        table.FullCleanup();
        GameDebug.LogHeader("ВСЕ УРОВНИ ПРОЙДЕНЫ! ПОБЕДА!");
        GameDebug.LogInfo("Нажми R для рестарта");
        StartCoroutine(RestartWaiter());
    }
    
    private void LoadNewLevel(LevelData level)
    {
        // Очищаем старых NPC
        if (table != null)
        {
            table.ClearAllNPCs();
        }
        
        // Загружаем новый уровень (НОВЫЕ NPC ПОЯВЛЯЮТСЯ ЗДЕСЬ, в темноте)
        if (levelController != null)
        {
            levelController.LoadLevel(level, table);
        }
        
        // Обновляем настройки
        maxRounds = level.roundsCount;
        anteAmount = level.anteAmount;
        
    }
    
    IEnumerator PlayRound(int roundNumber)
    {
        currentRound = roundNumber;
        bettingController.ResetRoundState();
        
        if (bankVisual != null)
            bankVisual.ClearBank();
        
        GameDebug.LogRound(roundNumber, maxRounds);
        
        // Проверяем, есть ли у игрока фишки
        PlayerChips playerChips = table.GetPlayerChips();
        if (playerChips == null || playerChips.IsBroke())
        {
            GameDebug.LogError("У игрока нет фишек для игры!");
            GameManager.Instance.OnGameOver?.Invoke();
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
        if (transitionEffects != null)
            transitionEffects.Cleanup();
    }
}