using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private bool isPlayerBroke = false;
    
    [Header("Debug Keys")]
    [SerializeField] private KeyCode instantWinKey = KeyCode.F1;
    [SerializeField] private KeyCode instantLoseKey = KeyCode.F2;
    [SerializeField] private bool enableDebugKeys = true;
    
    public System.Action OnPlayerBroke;
    public System.Action<NPCController> OnNPCBroke;
    public System.Action OnGameOver;
    public System.Action OnInstantWin;
    public System.Action OnInstantLose;
    public System.Action OnRestartCurrentLevel; // Событие перезапуска текущего уровня
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        
        if (enableDebugKeys)
        {
            if (Input.GetKeyDown(instantWinKey))
            {
                Debug.LogWarning("=== ОТЛАДКА: МГНОВЕННАЯ ПОБЕДА ===");
                OnInstantWin?.Invoke();
            }
            
            if (Input.GetKeyDown(instantLoseKey))
            {
                Debug.LogWarning("=== ОТЛАДКА: МГНОВЕННОЕ ПОРАЖЕНИЕ ===");
                OnInstantLose?.Invoke();
            }
        }
    }
    
    public void PlayerOutOfChips()
    {
        if (isGameOver) return;
        
        isPlayerBroke = true;
        isGameOver = true;
        
        GameDebug.LogError("ИГРОК ПРОИГРАЛ ВСЕ ФИШКИ!");
        
        OnPlayerBroke?.Invoke();
        OnGameOver?.Invoke();
        
        // Перезапускаем текущий уровень
        OnRestartCurrentLevel?.Invoke();
        
        // Сбрасываем флаги после перезапуска
        isGameOver = false;
        isPlayerBroke = false;
    }
    
    public void NPCOutOfChips(NPCController npc)
    {
        if (npc == null) return;
        GameDebug.LogWarning($"{npc.npcName} остался без фишек и покидает стол!");
        OnNPCBroke?.Invoke(npc);
    }
    
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    public bool IsPlayerBroke()
    {
        return isPlayerBroke;
    }
    
    public void ResetGameState()
    {
        isGameOver = false;
        isPlayerBroke = false;
    }
}