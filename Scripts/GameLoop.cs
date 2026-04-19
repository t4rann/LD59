// GameLoop.cs
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
    
    private RoundController roundController;
    private BettingController bettingController;
    private ShowdownController showdownController;
    
    private int currentRound = 1;
    
    void Start()
    {
        if (table == null)
            table = FindObjectOfType<TableController>();
        
        roundController = new RoundController(table, dealAnimationDelay, emotionsPhaseDelay);
        bettingController = new BettingController(table, npcTurnDelay, anteAmount);
        showdownController = new ShowdownController(table, bettingController);
        
        if (bettingController != null)
            bettingController.OnPotChanged += OnPotChanged;
        
        StartCoroutine(MainLoop());
        
    }
    
    private void OnPotChanged(int pot)
    {
        if (bankVisual != null)
            bankVisual.UpdateBankDisplay(pot);
    }
    
// GameLoop.cs - добавить FullCleanup

IEnumerator MainLoop()
{
    GameDebug.LogHeader("SIGNAL TABLE");
    GameDebug.LogInfo($"Управление: 1-Fold, 2-Call, 3-Raise | Анте: {anteAmount}");
    GameDebug.LogDivider();
    
    for (int round = 1; round <= maxRounds; round++)
    {
        if (GameManager.Instance.IsGameOver())
            yield break;
        
        yield return PlayRound(round);
        
        bettingController.CheckAndRemoveBrokeNPCs();
    }
    
    // Полная очистка в конце игры
    table.FullCleanup();
    
    GameDebug.LogHeader("ИГРА ЗАВЕРШЕНА");
    GameDebug.LogInfo("Нажми R для рестарта");
    StartCoroutine(RestartWaiter());
}
    
// GameLoop.cs - PlayRound

IEnumerator PlayRound(int roundNumber)
{
    currentRound = roundNumber;
    bettingController.ResetRoundState();
    
    if (bankVisual != null)
        bankVisual.ClearBank();
    
    GameDebug.LogRound(roundNumber, maxRounds);
    
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
    
    // 1. Раздача - карты появляются
    yield return roundController.DealPhase();
    
    // 2. Эмоции
    yield return roundController.EmotionsPhase();
    
    // 3. Торги NPC (кто-то может сбросить карты)
    yield return bettingController.BettingPhase();
    
    // 4. Ход игрока (может сбросить)
    yield return bettingController.PlayerPhase();
    
    if (GameManager.Instance.IsGameOver())
        yield break;
    
    // 5. Вскрытие
    showdownController.ShowdownPhase(bettingController.PlayerAction);       
    
    // 6. Очистка эмоций
    roundController.CleanupPhase();
    
    // 7. Сброс карт - визуальные карты убираются
    table.DiscardAllCards();
    
    // Ждем окончания анимации сброса
    yield return new WaitForSeconds(3f);
    
// 💥 ЖЁСТКО скрываем карты
table.HideAllNPCCards();

// 🎴 СРАЗУ показываем новые карты (следующий раунд feel)
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
    
    void OnDestroy()
    {
        if (bettingController != null)
            bettingController.OnPotChanged -= OnPotChanged;
    }
}