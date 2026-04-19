// GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private bool isPlayerBroke = false;
    
    public System.Action OnPlayerBroke;
    public System.Action<NPCController> OnNPCBroke;
    public System.Action OnGameOver;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
    
    public void PlayerOutOfChips()
    {
        if (isGameOver) return;
        
        isPlayerBroke = true;
        isGameOver = true;
        
        OnPlayerBroke?.Invoke();
        OnGameOver?.Invoke();
    }
    
    public void NPCOutOfChips(NPCController npc)
    {
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
    
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}