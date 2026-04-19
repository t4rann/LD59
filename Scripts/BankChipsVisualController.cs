using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections.Generic;

public class BankChipsVisualController : MonoBehaviour
{
    [Header("Chips Display")]
    [SerializeField] private GameObject chipsStackPrefab;
    [SerializeField] private Transform bankChipsPoint;
    [SerializeField] private TextMeshPro bankText;
    
    [Header("Pile Settings")]
    [SerializeField] private float pileRadius = 0.5f;
    [SerializeField] private float heightStep = 0.03f;
    [SerializeField] private float chipScale = 0.4f;
    
    [Header("Animation")]
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private AnimationCurve flyCurve;
    
    private List<GameObject> bankChips = new List<GameObject>();
    private int lastPot = 0;
    
    void Start()
    {
        if (flyCurve == null || flyCurve.keys.Length == 0)
            flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        if (chipsStackPrefab == null)
            Debug.LogError("chipsStackPrefab not assigned in BankChipsVisualController!");
        
        if (bankChipsPoint == null)
            Debug.LogError("bankChipsPoint not assigned in BankChipsVisualController!");
        
        UpdateBankText(0);
    }
    
    private void UpdateBankText(int pot)
    {
        if (bankText != null)
        {
            bankText.text = $"{pot}";
        }
    }
    
    public void UpdateBankDisplay(int pot)
    {
        UpdateBankText(pot);
        
        // Если пот увеличился - добавляем новые фишки
        if (pot > lastPot)
        {
            int addedAmount = pot - lastPot;
            AddChipsToBank(addedAmount);
        }
        // Если пот уменьшился - удаляем фишки (но по логике пот не должен уменьшаться)
        
        lastPot = pot;
    }
    
    private void AddChipsToBank(int amount)
    {
        if (chipsStackPrefab == null || bankChipsPoint == null) return;
        
        // Создаем 1 фишку за каждые 10 денег
        int chipsToCreate = Mathf.Max(1, amount / 10);
        
        Debug.Log($"Adding {chipsToCreate} chips to bank for {amount} money");
        
        for (int i = 0; i < chipsToCreate; i++)
        {
            CreateNewChipInBank();
        }
    }
    
    private void CreateNewChipInBank()
    {
        if (chipsStackPrefab == null) return;
        
        GameObject chip = Instantiate(chipsStackPrefab, bankChipsPoint.position, Quaternion.identity);
        chip.transform.SetParent(bankChipsPoint);
        chip.transform.localScale = Vector3.one * chipScale;
        
        Vector3 targetPos = CalculatePilePosition(bankChips.Count);
        Tween.Position(chip.transform, targetPos, flyDuration * 0.5f, 0, flyCurve);
        
        bankChips.Add(chip);
    }
    
    public void AddChip(GameObject chip, float scale)
    {
        if (chip == null) return;
        
        chip.transform.SetParent(bankChipsPoint);
        chip.transform.localScale = Vector3.one * scale;
        bankChips.Add(chip);
        
        Vector3 targetPos = CalculatePilePosition(bankChips.Count);
        Tween.Position(chip.transform, targetPos, flyDuration * 0.5f, 0, flyCurve);
    }
    
    private Vector3 CalculatePilePosition(int index)
    {
        float angle = (index * 137.5f) * Mathf.Deg2Rad;
        float radius = Mathf.Sqrt(index + 1) / 10f * pileRadius;
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        float y = (index / 15) * heightStep;
        
        return bankChipsPoint.position + new Vector3(x, y, z);
    }
    
    public void RequestChips(int count, NPCVisualController requester)
    {
        int chipsToGive = Mathf.Min(count, bankChips.Count);
        
        for (int i = 0; i < chipsToGive; i++)
        {
            if (bankChips.Count == 0) break;
            
            int index = bankChips.Count - 1;
            GameObject chip = bankChips[index];
            bankChips.RemoveAt(index);
            
            requester.ReceiveChipFromBank(chip);
        }
    }
    
    public void RequestChipForPlayer(PlayerChipsVisualController requester)
    {
        if (bankChips.Count == 0) return;
        
        int index = bankChips.Count - 1;
        GameObject chip = bankChips[index];
        bankChips.RemoveAt(index);
        
        requester.ReceiveChipFromBank(chip);
    }
    
    public void ClearBank()
    {
        Debug.Log($"Clearing bank: removing {bankChips.Count} chips");
        
        foreach (var chip in bankChips)
        {
            if (chip != null)
                Destroy(chip);
        }
        bankChips.Clear();
        lastPot = 0;
        UpdateBankText(0);
    }
    
    public void ResetBank()
    {
        ClearBank();
    }
}