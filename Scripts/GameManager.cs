using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private bool isPlayerBroke = false;
    [SerializeField] private bool isRestarting = false;
    
    public System.Action OnPlayerBroke;
    public System.Action<NPCController> OnNPCBroke;
    public System.Action OnGameOver;
    
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
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
    
    public void PlayerOutOfChips()
    {
        if (isGameOver || isRestarting) return;
        
        isPlayerBroke = true;
        isGameOver = true;
        isRestarting = true;
        
        GameDebug.LogError("ИГРОК ПРОИГРАЛ ВСЕ ФИШКИ! ПЕРЕЗАПУСК...");
        
        OnPlayerBroke?.Invoke();
        OnGameOver?.Invoke();
        
        RestartGame();
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
        isRestarting = false;
    }
    
    public void RestartGame()
    {
        GameDebug.LogHeader("ПЕРЕЗАПУСК ИГРЫ...");
        ResetGameState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}