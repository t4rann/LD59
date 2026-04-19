using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections;
using System.Collections.Generic;

public class PlayerChipsVisualController : MonoBehaviour
{
    [Header("Chips Display")]
    [SerializeField] private TextMeshPro chipsText;
    [SerializeField] private GameObject chipsStackPrefab;
    [SerializeField] private Transform chipsSpawnPoint;
    [SerializeField] private Transform bankTarget;
    
    [Header("Scale Settings")]
    [SerializeField] private float playerChipScale = 1.0f;
    [SerializeField] private float bankChipScale = 0.4f;
    
    [Header("Animation")]
    [SerializeField] private float stackSpacing = 0.1f;
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private AnimationCurve flyCurve;
    
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
            // Ждем один кадр, чтобы PlayerChips успел инициализироваться
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
        
        if (chipsStackPrefab == null)
            Debug.LogError("chipsStackPrefab not assigned in PlayerChipsVisualController!");
        
        if (chipsSpawnPoint == null)
            Debug.LogError("chipsSpawnPoint not assigned in PlayerChipsVisualController!");
    }
    
    private IEnumerator DelayedInitialization()
    {
        yield return null; // Ждем один кадр
        
        int initialChips = playerChips.GetChips();
        lastChipsCount = initialChips;
        ShowInitialChips(initialChips);
        UpdateChipsText(initialChips);
        
        Debug.Log($"Initialized with {initialChips} chips");
    }
    
    private void ShowInitialChips(int chips)
    {
        UpdateChipsText(chips);
        
        if (chipsStackPrefab == null || chipsSpawnPoint == null) return;
        
        if (chips <= 0)
        {
            Debug.Log("No chips to display");
            return;
        }
        
        // Показываем 1 фишку за каждые 10 фишек, но минимум 1
        int chipsToShow = Mathf.Max(1, chips / 10);
        
        Debug.Log($"Creating {chipsToShow} 3D chips for {chips} total chips");
        
        // Очищаем старые фишки
        foreach (var chip in activeChips)
        {
            if (chip != null) Destroy(chip);
        }
        activeChips.Clear();
        
        for (int i = 0; i < chipsToShow; i++)
        {
            Vector3 pos = chipsSpawnPoint.position + Vector3.up * (i * stackSpacing);
            GameObject chip = Instantiate(chipsStackPrefab, pos, Quaternion.identity);
            chip.transform.SetParent(chipsSpawnPoint);
            chip.transform.localScale = Vector3.one * playerChipScale;
            activeChips.Add(chip);
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
        
        if (chipsStackPrefab == null || chipsSpawnPoint == null) return;
        
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
        
        int chipsCount = Mathf.Max(1, amount / 10);
        int chipsToRemove = Mathf.Min(chipsCount, activeChips.Count);
        
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
        
        int chipsCount = Mathf.Max(1, amount / 10);
        
        Debug.Log($"Requesting {chipsCount} chips from bank (won {amount})");
        
        BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
        if (bank != null)
        {
            for (int i = 0; i < chipsCount; i++)
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
    
    void OnDestroy()
    {
        if (playerChips != null)
            playerChips.OnChipsChanged -= UpdateChipsDisplay;
    }
}