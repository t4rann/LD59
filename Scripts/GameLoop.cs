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
    [SerializeField] private float levelTransitionDelay = 1.5f;
    
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
            
            // Эффект перехода на новый уровень (кроме первого)
            if (currentLevelIndex > 0 && transitionEffects != null)
            {
                yield return StartCoroutine(transitionEffects.PlayLevelTransition());
                yield return new WaitForSeconds(levelTransitionDelay);
            }
            
            // Очищаем старых NPC перед загрузкой нового уровня
            if (table != null)
            {
                table.ClearAllNPCs();
            }
            
            if (levelController != null)
            {
                levelController.LoadLevel(level, table);
            }
            
            maxRounds = level.roundsCount;
            anteAmount = level.anteAmount;
            bettingController.SetAnteAmount(anteAmount);
            
            // Сбрасываем состояние BettingController
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
                
                if (transitionEffects != null)
                    yield return StartCoroutine(transitionEffects.PlayLevelComplete());
                
                yield return new WaitForSeconds(levelTransitionDelay);
            }
        }
        
        // Финальная победа
        if (transitionEffects != null)
            yield return StartCoroutine(transitionEffects.PlayGameComplete());
        
        table.FullCleanup();
        GameDebug.LogHeader("ВСЕ УРОВНИ ПРОЙДЕНЫ! ПОБЕДА!");
        GameDebug.LogInfo("Нажми R для рестарта");
        StartCoroutine(RestartWaiter());
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