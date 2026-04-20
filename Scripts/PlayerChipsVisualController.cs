using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections;
using System.Collections.Generic;

public class PlayerChipsVisualController : MonoBehaviour
{
    [Header("Chips Display")]
    [SerializeField] private TextMeshPro chipsText;
    [SerializeField] private GameObject chipsStackPrefab10;
    [SerializeField] private GameObject chipsStackPrefab100;
    [SerializeField] private Transform chipsSpawnPoint;
    [SerializeField] private Transform bankTarget;
    
    [Header("Scale Settings")]
    [SerializeField] private float playerChipScale = 1.0f;
    [SerializeField] private float bankChipScale = 0.4f;
    
    [Header("Animation")]
    [SerializeField] private float stackSpacing = 0.1f;
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private AnimationCurve flyCurve;
    
    [Header("Sound Settings")]
    [SerializeField] private bool playChipSound = true;
    
    private PlayerChips playerChips;
    private int lastChipsCount;
    private List<GameObject> activeChips = new List<GameObject>();
    private bool isInitialized = false;
    
    // Словарь для отслеживания запрошенных фишек по номиналам
    private Dictionary<int, int> pendingChipsRequest = new Dictionary<int, int>();
    
    void Start()
    {
        playerChips = GetComponent<PlayerChips>();
        
        if (flyCurve == null || flyCurve.keys.Length == 0)
            flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        if (playerChips != null)
        {
            playerChips.OnChipsChanged += UpdateChipsDisplay;
            StartCoroutine(DelayedInitialization());
        }
        else
        {
            Debug.LogError("PlayerChips component not found!");
        }
        
        if (bankTarget == null)
        {
            BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
            if (bank != null)
                bankTarget = bank.transform;
        }
        
        if (chipsStackPrefab10 == null)
            Debug.LogError("chipsStackPrefab10 not assigned in PlayerChipsVisualController!");
        if (chipsStackPrefab100 == null)
            Debug.LogError("chipsStackPrefab100 not assigned in PlayerChipsVisualController!");
        
        if (chipsSpawnPoint == null)
            Debug.LogError("chipsSpawnPoint not assigned in PlayerChipsVisualController!");
    }
    
    private IEnumerator DelayedInitialization()
    {
        yield return null;
        yield return null; // Двойная задержка для уверенности
        
        InitializeChips();
    }
    
    // Публичный метод для принудительной переинициализации (вызывать при перезапуске уровня)
    public void ForceReinitialize()
    {
        Debug.Log("[PlayerChips] Принудительная переинициализация");
        InitializeChips();
    }
    
    private void InitializeChips()
    {
        if (playerChips == null)
        {
            playerChips = GetComponent<PlayerChips>();
            if (playerChips == null) return;
        }
        
        int currentChips = playerChips.GetChips();
        lastChipsCount = currentChips;
        
        // Полностью очищаем и перестраиваем стек
        ClearAllChips();
        RebuildStackInstantly(currentChips);
        UpdateChipsText(currentChips);
        
        isInitialized = true;
        
        Debug.Log($"[PlayerChips] Инициализировано с {currentChips} фишками, создано {activeChips.Count} объектов");
    }


// В PlayerChipsVisualController добавь метод:
public void ForceUpdateVisuals()
{
    Debug.Log($"[PlayerChips] Принудительное обновление визуала. Фишек: {playerChips?.GetChips()}");
    
    if (playerChips == null)
        playerChips = GetComponent<PlayerChips>();
    
    if (playerChips != null)
    {
        int currentChips = playerChips.GetChips();
        lastChipsCount = currentChips;
        
        ClearAllChips();
        RebuildStackInstantly(currentChips);
        UpdateChipsText(currentChips);
        
        isInitialized = true;
    }
}

// И исправь метод ResetForNewLevel:
public void ResetForNewLevel()
{
    Debug.Log("[PlayerChips] Сброс для нового уровня");
    isInitialized = false;
    pendingChipsRequest.Clear();
    ClearAllChips();
    
    // Принудительно обновляем сразу, а не через корутину
    if (playerChips != null)
    {
        int currentChips = playerChips.GetChips();
        lastChipsCount = currentChips;
        RebuildStackInstantly(currentChips);
        UpdateChipsText(currentChips);
        isInitialized = true;
        
        Debug.Log($"[PlayerChips] Создано {activeChips.Count} фишек после сброса");
    }
    else
    {
        StartCoroutine(DelayedInitialization());
    }
}

private void ClearAllChips()
{
    foreach (var chip in activeChips)
    {
        if (chip != null) 
        {
            // Останавливаем все анимации на этом объекте
            Tween.Stop(chip.transform.GetInstanceID());
            
            // Или альтернативный вариант - использовать StopAll для конкретного объекта
            // Tween.StopAll(chip.transform);
            
            Destroy(chip);
        }
    }
    activeChips.Clear();
}
    
    private void RebuildStackInstantly(int chips)
    {
        if (chips <= 0) 
        {
            Debug.Log("[PlayerChips] Нет фишек для отображения");
            return;
        }
        
        if (chipsSpawnPoint == null) return;
        
        // Конвертируем фишки в номиналы
        int chips100 = chips / 100;
        int chips10 = (chips % 100) / 10;
        int chips1 = chips % 10; // Для остатка
        
        Debug.Log($"[PlayerChips] Создание стека: {chips100}x100 + {chips10}x10 + {chips1}x1 = {chips}");
        
        int index = 0;
        
        // Создаем фишки номиналом 100 (снизу стека)
        if (chipsStackPrefab100 != null)
        {
            for (int i = 0; i < chips100; i++)
            {
                CreateChipAtPosition(chipsStackPrefab100, index);
                index++;
            }
        }
        
        // Создаем фишки номиналом 10
        if (chipsStackPrefab10 != null)
        {
            for (int i = 0; i < chips10; i++)
            {
                CreateChipAtPosition(chipsStackPrefab10, index);
                index++;
            }
        }
        
        // Если есть остаток, создаем фишки по 10 (или можно добавить префаб для 1)
        if (chips1 > 0 && chipsStackPrefab10 != null)
        {
            for (int i = 0; i < chips1; i++)
            {
                CreateChipAtPosition(chipsStackPrefab10, index);
                index++;
            }
        }
    }
    
    private void CreateChipAtPosition(GameObject prefab, int index)
    {
        if (prefab == null || chipsSpawnPoint == null) return;
        
        Vector3 pos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
        GameObject chip = Instantiate(prefab, pos, Quaternion.identity, chipsSpawnPoint);
        chip.transform.localScale = Vector3.one * playerChipScale;
        
        // Добавляем компонент для идентификации номинала
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
    
    private void UpdateChipsDisplay(int chips)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[PlayerChips] Попытка обновления до инициализации, выполняем инициализацию");
            InitializeChips();
            return;
        }
        
        UpdateChipsText(chips);
        
        if (chipsStackPrefab10 == null || chipsSpawnPoint == null) return;
        
        Debug.Log($"[PlayerChips] Изменение фишек: {lastChipsCount} -> {chips}");
        
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
            Debug.LogWarning("[PlayerChips] Bank target is null, перестраиваем стек локально");
            RebuildStackInstantly(lastChipsCount - amount);
            return;
        }
        
        PlayChipSound();
        
        int remainingToRemove = amount;
        List<GameObject> chipsToFly = new List<GameObject>();
        
        // Идем с конца списка (верхние фишки) и собираем нужную сумму
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
        
        // Если не набрали нужную сумму, берем оставшиеся фишки
        if (remainingToRemove > 0 && activeChips.Count > 0)
        {
            for (int i = activeChips.Count - 1; i >= 0 && chipsToFly.Count < 10; i--)
            {
                GameObject chip = activeChips[i];
                if (!chipsToFly.Contains(chip))
                {
                    chipsToFly.Add(chip);
                }
            }
        }
        
        // Удаляем фишки из активного списка
        foreach (var chip in chipsToFly)
        {
            activeChips.Remove(chip);
        }
        
        Debug.Log($"[PlayerChips] Отправка {chipsToFly.Count} фишек в банк (потеряно {amount})");
        
        // Анимируем полет
        for (int i = 0; i < chipsToFly.Count; i++)
        {
            GameObject chip = chipsToFly[i];
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
        
        // Перестраиваем позиции оставшихся фишек
        RepositionRemainingChips();
    }
    
    private void FlyChipsFromBank(int amount)
    {
        if (bankTarget == null) 
        {
            Debug.LogWarning("[PlayerChips] Bank target is null, перестраиваем стек локально");
            RebuildStackInstantly(lastChipsCount + amount);
            return;
        }
        
        PlayChipSound();
        
        // Рассчитываем сколько фишек нужно
        int chips100 = amount / 100;
        int chips10 = (amount % 100) / 10;
        int chips1 = amount % 10;
        int totalChips = chips100 + chips10 + chips1;
        
        if (totalChips == 0) totalChips = 1;
        
        Debug.Log($"[PlayerChips] Запрос {totalChips} фишек из банка (выиграно {amount})");
        
        BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
        if (bank != null)
        {
            // Очищаем предыдущие ожидающие запросы
            pendingChipsRequest.Clear();
            
            // Сохраняем информацию о запрошенных номиналах
            if (chips100 > 0) pendingChipsRequest[100] = chips100;
            if (chips10 > 0) pendingChipsRequest[10] = chips10;
            if (chips1 > 0) pendingChipsRequest[10] = pendingChipsRequest.ContainsKey(10) ? 
                pendingChipsRequest[10] + chips1 : chips1;
            
            // Запрашиваем общее количество фишек (будем получать их через ReceiveChipFromBank)
            for (int i = 0; i < totalChips; i++)
            {
                bank.RequestChipForPlayer(this);
            }
        }
        else
        {
            Debug.LogWarning("[PlayerChips] BankChipsVisualController not found, создаем локально");
            RebuildStackInstantly(lastChipsCount + amount);
        }
    }
    
    private int GetChipValue(GameObject chip)
    {
        if (chip == null) return 10;
        
        ChipIdentifier identifier = chip.GetComponent<ChipIdentifier>();
        if (identifier != null)
            return identifier.chipValue;
        
        // Fallback по имени префаба
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
        
        // Убеждаемся, что у фишки есть идентификатор
        ChipIdentifier identifier = chip.GetComponent<ChipIdentifier>();
        if (identifier == null)
        {
            identifier = chip.AddComponent<ChipIdentifier>();
            identifier.chipValue = 10; // По умолчанию
        }
        
        // Пытаемся установить правильный номинал из ожидающих запросов
        if (pendingChipsRequest.Count > 0)
        {
            // Ищем первый доступный номинал
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
        
        // Устанавливаем правильный масштаб и позицию
        chip.transform.localScale = Vector3.one * playerChipScale;
        
        activeChips.Add(chip);
        
        int index = activeChips.Count - 1;
        Vector3 targetPos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
        
        Tween.Position(chip.transform, targetPos, flyDuration, 0, flyCurve);
        
        Debug.Log($"[PlayerChips] Получена фишка из банка номиналом {identifier.chipValue}, всего в стеке: {activeChips.Count}");
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
        Tween.StopAll();
        
        if (playerChips != null)
            playerChips.OnChipsChanged -= UpdateChipsDisplay;
            
        ClearAllChips();
    }
    
public int GetChipsCount()
{
    return activeChips.Count;
}
}

// Вспомогательный компонент для идентификации номинала фишки
public class ChipIdentifier : MonoBehaviour
{
    public int chipValue = 10;
}