// PlayerChipsVisualController.cs
using UnityEngine;
using TMPro;
using Pixelplacement;
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
            lastChipsCount = playerChips.GetChips();
            ShowInitialChips(lastChipsCount);
        }
        
        if (bankTarget == null)
        {
            BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
            if (bank != null)
                bankTarget = bank.transform;
        }
    }
    
    private void ShowInitialChips(int chips)
    {
        UpdateChipsText(chips);
        
        if (chipsStackPrefab != null && chipsSpawnPoint != null)
        {
            int chipsToShow = chips / 10;
            for (int i = 0; i < chipsToShow; i++)
            {
                Vector3 pos = chipsSpawnPoint.position + Vector3.up * (i * stackSpacing);
                GameObject chip = Instantiate(chipsStackPrefab, pos, Quaternion.identity);
                chip.transform.SetParent(chipsSpawnPoint);
                chip.transform.localScale = Vector3.one * playerChipScale;
                activeChips.Add(chip);
            }
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
        if (bankTarget == null) return;
        
        int chipsCount = amount / 10;
        int chipsToRemove = Mathf.Min(chipsCount, activeChips.Count);
        
        for (int i = 0; i < chipsToRemove; i++)
        {
            int index = activeChips.Count - 1 - i;
            if (index < 0) break;
            
            GameObject chip = activeChips[index];
            activeChips.RemoveAt(index);
            
            // Анимация полета с уменьшением
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
        
        int chipsCount = amount / 10;
        
        BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
        if (bank != null)
        {
            for (int i = 0; i < chipsCount; i++)
            {
                bank.RequestChipForPlayer(this);
            }
        }
    }
    
    public void ReceiveChipFromBank(GameObject chip)
    {
        if (chip == null) return;
        
        chip.transform.SetParent(chipsSpawnPoint);
        activeChips.Add(chip);
        
        Vector3 targetPos = chipsSpawnPoint.position + Vector3.up * ((activeChips.Count - 1) * stackSpacing);
        
        // Анимация полета с увеличением
        Tween.Position(chip.transform, targetPos, flyDuration, 0, flyCurve);
        Tween.LocalScale(chip.transform, Vector3.one * playerChipScale, flyDuration, 0, flyCurve);
    }
    
    void OnDestroy()
    {
        if (playerChips != null)
            playerChips.OnChipsChanged -= UpdateChipsDisplay;
    }
}