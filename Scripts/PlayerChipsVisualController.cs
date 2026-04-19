using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections;
using System.Collections.Generic;

public class PlayerChipsVisualController : MonoBehaviour
{
    [Header("Chips Display")]
    [SerializeField] private TextMeshPro chipsText;
    [SerializeField] private GameObject chipsStackPrefab10;  // Префаб фишки номиналом 10
    [SerializeField] private GameObject chipsStackPrefab100; // Префаб фишки номиналом 100
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
        
        if (chipsSpawnPoint == null)
            Debug.LogError("chipsSpawnPoint not assigned in PlayerChipsVisualController!");
    }
    
    private IEnumerator DelayedInitialization()
    {
        yield return null;
        
        int initialChips = playerChips.GetChips();
        lastChipsCount = initialChips;
        ShowInitialChips(initialChips);
        UpdateChipsText(initialChips);
        
        Debug.Log($"Initialized with {initialChips} chips");
    }
    
    private void ShowInitialChips(int chips)
    {
        UpdateChipsText(chips);
        
        if (chipsStackPrefab10 == null || chipsSpawnPoint == null) return;
        
        if (chips <= 0)
        {
            Debug.Log("No chips to display");
            return;
        }
        
        // Очищаем старые фишки
        foreach (var chip in activeChips)
        {
            if (chip != null) Destroy(chip);
        }
        activeChips.Clear();
        
        // Конвертируем фишки в номиналы
        int chips100 = chips / 100;
        int chips10 = (chips % 100) / 10;
        
        Debug.Log($"Creating {chips100}x100 and {chips10}x10 chips for {chips} total chips");
        
        int index = 0;
        
        // Создаем фишки номиналом 100
        for (int i = 0; i < chips100; i++)
        {
            Vector3 pos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
            GameObject chip = Instantiate(chipsStackPrefab100, pos, Quaternion.identity);
            chip.transform.SetParent(chipsSpawnPoint);
            chip.transform.localScale = Vector3.one * playerChipScale;
            activeChips.Add(chip);
            index++;
        }
        
        // Создаем фишки номиналом 10
        for (int i = 0; i < chips10; i++)
        {
            Vector3 pos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
            GameObject chip = Instantiate(chipsStackPrefab10, pos, Quaternion.identity);
            chip.transform.SetParent(chipsSpawnPoint);
            chip.transform.localScale = Vector3.one * playerChipScale;
            activeChips.Add(chip);
            index++;
        }
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
        UpdateChipsText(chips);
        
        if (chipsStackPrefab10 == null || chipsSpawnPoint == null) return;
        
        Debug.Log($"Chips changed from {lastChipsCount} to {chips}");
        
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
            Debug.LogWarning("Bank target is null, can't fly chips");
            return;
        }
        
        // Воспроизводим ОДИН звук проигрыша фишек
        PlayChipSound();
        
        // Определяем сколько фишек каждого номинала нужно отправить
        int chips100 = amount / 100;
        int chips10 = (amount % 100) / 10;
        int totalChipsToRemove = chips100 + chips10;
        
        if (totalChipsToRemove == 0) totalChipsToRemove = 1;
        
        int chipsToRemove = Mathf.Min(totalChipsToRemove, activeChips.Count);
        
        Debug.Log($"Flying {chipsToRemove} chips to bank (lost {amount})");
        
        for (int i = 0; i < chipsToRemove; i++)
        {
            int index = activeChips.Count - 1 - i;
            if (index < 0) break;
            
            GameObject chip = activeChips[index];
            activeChips.RemoveAt(index);
            
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
    }
    
    private void FlyChipsFromBank(int amount)
    {
        if (bankTarget == null) return;
        
        // Воспроизводим ОДИН звук выигрыша фишек
        PlayChipSound();
        
        int chips100 = amount / 100;
        int chips10 = (amount % 100) / 10;
        int totalChips = chips100 + chips10;
        
        if (totalChips == 0) totalChips = 1;
        
        Debug.Log($"Requesting {totalChips} chips from bank (won {amount})");
        
        BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
        if (bank != null)
        {
            for (int i = 0; i < totalChips; i++)
            {
                bank.RequestChipForPlayer(this);
            }
        }
        else
        {
            Debug.LogWarning("BankChipsVisualController not found!");
        }
    }
    
    public void ReceiveChipFromBank(GameObject chip)
    {
        if (chip == null) return;
        
        chip.transform.SetParent(chipsSpawnPoint);
        activeChips.Add(chip);
        
        Vector3 targetPos = chipsSpawnPoint.position + Vector3.up * ((activeChips.Count - 1) * stackSpacing);
        
        Tween.Position(chip.transform, targetPos, flyDuration, 0, flyCurve);
        Tween.LocalScale(chip.transform, Vector3.one * playerChipScale, flyDuration, 0, flyCurve);
    }
    
    #region Sound Methods
    
    private void PlayChipSound()
    {
        if (!playChipSound) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayChipAddSound();
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        if (playerChips != null)
            playerChips.OnChipsChanged -= UpdateChipsDisplay;
    }
}