using UnityEngine;
using TMPro;
using Pixelplacement;
using System.Collections.Generic;

public class BankChipsVisualController : MonoBehaviour
{
    [Header("Chips Display")]
    [SerializeField] private GameObject chipsStackPrefab10;
    [SerializeField] private GameObject chipsStackPrefab100;
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
        
        if (chipsStackPrefab10 == null)
            Debug.LogError("chipsStackPrefab10 not assigned in BankChipsVisualController!");
        
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
        
        if (pot > lastPot)
        {
            int addedAmount = pot - lastPot;
            AddChipsToBank(addedAmount);
        }
        
        lastPot = pot;
    }
    
    private void AddChipsToBank(int amount)
    {
        if (chipsStackPrefab10 == null || bankChipsPoint == null) return;
        
        int chips100 = amount / 100;
        int chips10 = (amount % 100) / 10;
        
        Debug.Log($"Adding {chips100}x100 and {chips10}x10 chips to bank for {amount} money");
        
        for (int i = 0; i < chips100; i++)
        {
            CreateNewChipInBank(chipsStackPrefab100);
        }
        
        for (int i = 0; i < chips10; i++)
        {
            CreateNewChipInBank(chipsStackPrefab10);
        }
        
        if (chips100 == 0 && chips10 == 0 && amount > 0)
        {
            CreateNewChipInBank(chipsStackPrefab10);
        }
    }
    
    private void CreateNewChipInBank(GameObject prefab)
    {
        if (prefab == null) return;
        
        GameObject chip = Instantiate(prefab, bankChipsPoint.position, Quaternion.identity);
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