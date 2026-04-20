using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections;
using System.Collections.Generic;

public class NPCVisualController : MonoBehaviour
{
    [Header("Chips Display")]
    [SerializeField] private TextMeshPro chipsText;
    [SerializeField] private GameObject chipsStackPrefab10;
    [SerializeField] private GameObject chipsStackPrefab100;
    [SerializeField] private Transform chipsSpawnPoint;
    [SerializeField] private Transform bankTarget;
    
    [Header("Scale Settings")]
    [SerializeField] private float npcChipScale = 0.4f;
    [SerializeField] private float bankChipScale = 0.4f;
    
    [Header("Animation")]
    [SerializeField] private float stackSpacing = 0.1f;
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private AnimationCurve flyCurve;
    
    [Header("Sound Settings")]
    [SerializeField] private bool playChipSound = true;
    
    private NPCChips npcChips;
    private NPCController npcController;
    private int lastChipsCount;
    private List<GameObject> activeChips = new List<GameObject>();
    private bool isInitialized = false;
    private Dictionary<int, int> pendingChipsRequest = new Dictionary<int, int>();
    
    void Start()
    {
        npcChips = GetComponent<NPCChips>();
        npcController = GetComponent<NPCController>();
        
        if (flyCurve == null || flyCurve.keys.Length == 0)
            flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        if (npcChips != null)
        {
            npcChips.OnChipsChanged += UpdateChipsDisplay;
            StartCoroutine(DelayedInitialization());
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] NPCChips не найден!");
        }
        
        if (bankTarget == null)
        {
            BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
            if (bank != null)
                bankTarget = bank.transform;
        }
        
        if (chipsStackPrefab10 == null)
            Debug.LogError($"[{gameObject.name}] chipsStackPrefab10 not assigned!");
        if (chipsStackPrefab100 == null)
            Debug.LogError($"[{gameObject.name}] chipsStackPrefab100 not assigned!");
        if (chipsSpawnPoint == null)
            Debug.LogError($"[{gameObject.name}] chipsSpawnPoint not assigned!");
    }
    
    private IEnumerator DelayedInitialization()
    {
        yield return null;
        yield return null;
        
        InitializeChips();
    }
    
    private void InitializeChips()
    {
        if (npcChips == null)
        {
            npcChips = GetComponent<NPCChips>();
            if (npcChips == null) return;
        }
        
        int currentChips = npcChips.GetChips();
        lastChipsCount = currentChips;
        
        ClearAllChips();
        RebuildStackInstantly(currentChips);
        UpdateChipsText(currentChips);
        
        isInitialized = true;
        
        Debug.Log($"[NPCVisual] {gameObject.name} инициализирован с {currentChips} фишками");
    }
    
private void ClearAllChips()
{
    foreach (var chip in activeChips)
    {
        if (chip != null)
        {
            // 🔑 ИСПРАВЛЕНО: используем Stop с ID объекта
            Tween.Stop(chip.transform.GetInstanceID());
            Destroy(chip);
        }
    }
    activeChips.Clear();
}
    
    private void RebuildStackInstantly(int chips)
    {
        if (chips <= 0) return;
        if (chipsSpawnPoint == null) return;
        
        int chips100 = chips / 100;
        int chips10 = (chips % 100) / 10;
        int chips1 = chips % 10;
        
        int index = 0;
        
        // Создаем фишки номиналом 100
        if (chipsStackPrefab100 != null)
        {
            for (int i = 0; i < chips100; i++)
            {
                CreateChipAtIndex(chipsStackPrefab100, index);
                index++;
            }
        }
        
        // Создаем фишки номиналом 10
        if (chipsStackPrefab10 != null)
        {
            for (int i = 0; i < chips10; i++)
            {
                CreateChipAtIndex(chipsStackPrefab10, index);
                index++;
            }
        }
        
        // Остаток
        if (chips1 > 0 && chipsStackPrefab10 != null)
        {
            for (int i = 0; i < chips1; i++)
            {
                CreateChipAtIndex(chipsStackPrefab10, index);
                index++;
            }
        }
    }
    
    private void CreateChipAtIndex(GameObject prefab, int index)
    {
        if (prefab == null || chipsSpawnPoint == null) return;
        
        Vector3 pos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
        GameObject chip = Instantiate(prefab, pos, Quaternion.identity, chipsSpawnPoint);
        chip.transform.localScale = Vector3.one * npcChipScale;
        
        // Добавляем идентификатор номинала
        ChipIdentifier identifier = chip.GetComponent<ChipIdentifier>();
        if (identifier == null)
        {
            identifier = chip.AddComponent<ChipIdentifier>();
        }
        
        if (prefab == chipsStackPrefab100)
            identifier.chipValue = 100;
        else if (prefab == chipsStackPrefab10)
            identifier.chipValue = 10;
        
        activeChips.Add(chip);
    }
    
    private void UpdateChipsText(int chips)
    {
        if (chipsText != null)
        {
            chipsText.text = $"{chips}";
        }
    }
    
    private void UpdateChipsDisplay(NPCController npc, int chips)
    {
        if (!isInitialized)
        {
            InitializeChips();
            return;
        }
        
        UpdateChipsText(chips);
        
        if (chipsStackPrefab10 == null || chipsSpawnPoint == null) return;
        
        Debug.Log($"[NPCVisual] {gameObject.name} chips changed: {lastChipsCount} -> {chips}");
        
        if (chips < lastChipsCount)
        {
            int lost = lastChipsCount - chips;
            FlyChipsToBank(lost);
        }
        else if (chips > lastChipsCount)
        {
            int won = chips - lastChipsCount;
            FlyChipsFromBank(won);
        }
        
        lastChipsCount = chips;
    }
    
private void FlyChipsToBank(int amount)
{
    if (bankTarget == null)
    {
        RebuildStackInstantly(lastChipsCount - amount);
        return;
    }
    
    PlayChipSound();
    
    int remainingToRemove = amount;
    List<GameObject> chipsToFly = new List<GameObject>();
    
    // Собираем фишки с конца стека
    for (int i = activeChips.Count - 1; i >= 0 && remainingToRemove > 0; i--)
    {
        GameObject chip = activeChips[i];
        int chipValue = GetChipValue(chip);
        
        if (chipValue <= remainingToRemove)
        {
            chipsToFly.Add(chip);
            remainingToRemove -= chipValue;
        }
    }
    
    // Удаляем из активного списка
    foreach (var chip in chipsToFly)
    {
        activeChips.Remove(chip);
    }
    
    // Анимируем полет
    for (int i = 0; i < chipsToFly.Count; i++)
    {
        GameObject chip = chipsToFly[i];
        
        // 🔑 ИСПРАВЛЕНО: StopAll без аргументов или Stop с ID
        Tween.Stop(chip.transform.GetInstanceID());
        
        chip.transform.SetParent(null);
        
        Tween.Position(chip.transform, bankTarget.position, flyDuration, 0, flyCurve);
        Tween.LocalScale(chip.transform, Vector3.one * bankChipScale, flyDuration, 0, flyCurve, 
            Tween.LoopType.None, null, () =>
            {
                BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
                if (bank != null)
                    bank.AddChip(chip, bankChipScale);
                else
                    Destroy(chip);
            });
    }
    
    RepositionRemainingChips();
}
    
    private void FlyChipsFromBank(int amount)
    {
        if (bankTarget == null)
        {
            RebuildStackInstantly(lastChipsCount + amount);
            return;
        }
        
        PlayChipSound();
        
        int chips100 = amount / 100;
        int chips10 = (amount % 100) / 10;
        int chips1 = amount % 10;
        int totalChips = chips100 + chips10 + chips1;
        
        if (totalChips == 0) totalChips = 1;
        
        BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
        if (bank != null)
        {
            pendingChipsRequest.Clear();
            
            if (chips100 > 0) pendingChipsRequest[100] = chips100;
            if (chips10 > 0) pendingChipsRequest[10] = chips10;
            if (chips1 > 0) pendingChipsRequest[10] = pendingChipsRequest.ContainsKey(10) ? 
                pendingChipsRequest[10] + chips1 : chips1;
            
            // Запрашиваем общее количество фишек
            bank.RequestChips(totalChips, this);
        }
        else
        {
            RebuildStackInstantly(lastChipsCount + amount);
        }
    }
    
    private int GetChipValue(GameObject chip)
    {
        if (chip == null) return 10;
        
        ChipIdentifier identifier = chip.GetComponent<ChipIdentifier>();
        if (identifier != null)
            return identifier.chipValue;
        
        if (chipsStackPrefab100 != null && chip.name.Contains(chipsStackPrefab100.name))
            return 100;
        if (chipsStackPrefab10 != null && chip.name.Contains(chipsStackPrefab10.name))
            return 10;
            
        return 10;
    }
    
    private void RepositionRemainingChips()
    {
        for (int i = 0; i < activeChips.Count; i++)
        {
            GameObject chip = activeChips[i];
            if (chip == null) continue;
            
            Vector3 targetPos = chipsSpawnPoint.position + Vector3.up * (i * stackSpacing);
            
            if (Vector3.Distance(chip.transform.position, targetPos) > 0.01f)
            {
                Tween.Position(chip.transform, targetPos, 0.2f, 0, flyCurve);
            }
        }
    }
    
    public void ReceiveChipFromBank(GameObject chip)
    {
        if (chip == null) return;
        if (chipsSpawnPoint == null) return;
        
        chip.transform.SetParent(chipsSpawnPoint);
        
        ChipIdentifier identifier = chip.GetComponent<ChipIdentifier>();
        if (identifier == null)
        {
            identifier = chip.AddComponent<ChipIdentifier>();
            identifier.chipValue = 10;
        }
        
        // Устанавливаем номинал из очереди
        if (pendingChipsRequest.Count > 0)
        {
            foreach (var kvp in pendingChipsRequest)
            {
                if (kvp.Value > 0)
                {
                    identifier.chipValue = kvp.Key;
                    pendingChipsRequest[kvp.Key]--;
                    if (pendingChipsRequest[kvp.Key] <= 0)
                        pendingChipsRequest.Remove(kvp.Key);
                    break;
                }
            }
        }
        
        chip.transform.localScale = Vector3.one * npcChipScale;
        activeChips.Add(chip);
        
        int index = activeChips.Count - 1;
        Vector3 targetPos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
        
        Tween.Position(chip.transform, targetPos, flyDuration, 0, flyCurve);
    }
    
    public void ForceUpdateVisuals()
    {
        if (npcChips != null)
        {
            int currentChips = npcChips.GetChips();
            lastChipsCount = currentChips;
            
            ClearAllChips();
            RebuildStackInstantly(currentChips);
            UpdateChipsText(currentChips);
            
            isInitialized = true;
        }
    }
    
    public void ResetForNewLevel()
    {
        isInitialized = false;
        pendingChipsRequest.Clear();
        ClearAllChips();
        
        if (npcChips != null)
        {
            int currentChips = npcChips.GetChips();
            lastChipsCount = currentChips;
            RebuildStackInstantly(currentChips);
            UpdateChipsText(currentChips);
            isInitialized = true;
        }
    }
    
    private void PlayChipSound()
    {
        if (!playChipSound) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayChipAddSound();
        }
    }
    
void OnDestroy()
{
    // 🔑 ИСПРАВЛЕНО: StopAll без аргументов останавливает ВСЕ анимации в сцене
    Tween.StopAll();
    
    if (npcChips != null)
        npcChips.OnChipsChanged -= UpdateChipsDisplay;
    
    ClearAllChips();
}
}