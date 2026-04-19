using UnityEngine;

public class NPCChips : MonoBehaviour
{
    [Header("Starting Chips")]
    [SerializeField] private int startingChips = 100;
    
    private int currentChips;
    private NPCController npcController;
    
    public System.Action<NPCController, int> OnChipsChanged;
    
    void Awake()
    {
        currentChips = startingChips;
        npcController = GetComponent<NPCController>();
    }
    
    void Start()
    {
        OnChipsChanged?.Invoke(npcController, currentChips);
    }
    
    public int GetChips()
    {
        return currentChips;
    }
    
    public bool AddChips(int amount)
    {
        if (amount < 0) return false;
        currentChips += amount;
        OnChipsChanged?.Invoke(npcController, currentChips);
        return true;
    }
    
    public bool RemoveChips(int amount)
    {
        if (amount < 0) return false;
        if (currentChips < amount) return false;
        
        currentChips -= amount;
        OnChipsChanged?.Invoke(npcController, currentChips);
        return true;
    }
    
    public bool HasEnoughChips(int amount)
    {
        return currentChips >= amount;
    }
    
    public void ResetChips()
    {
        currentChips = startingChips;
        OnChipsChanged?.Invoke(npcController, currentChips);
    }
    
    public void SetStartingChips(int chips)
    {
        startingChips = chips;
        currentChips = startingChips;
        OnChipsChanged?.Invoke(npcController, currentChips);
    }
    
    public bool IsBroke()
    {
        return currentChips <= 0;
    }
}