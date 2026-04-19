using UnityEngine;
using TMPro;
using Pixelplacement;
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
    
    void Start()
    {
        npcChips = GetComponent<NPCChips>();
        npcController = GetComponent<NPCController>();
        
        if (flyCurve == null || flyCurve.keys.Length == 0)
            flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        if (npcChips != null)
        {
            npcChips.OnChipsChanged += UpdateChipsDisplay;
            lastChipsCount = npcChips.GetChips();
            ShowInitialChips(lastChipsCount);
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
    }
    
    private void ShowInitialChips(int chips)
    {
        UpdateChipsText(chips);
        
        if (chipsStackPrefab10 != null && chipsSpawnPoint != null)
        {
            if (chips <= 0) return;
            
            int chips100 = chips / 100;
            int chips10 = (chips % 100) / 10;
            
            int index = 0;
            
            for (int i = 0; i < chips100; i++)
            {
                Vector3 pos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
                GameObject chip = Instantiate(chipsStackPrefab100, pos, Quaternion.identity);
                chip.transform.SetParent(chipsSpawnPoint);
                chip.transform.localScale = Vector3.one * npcChipScale;
                activeChips.Add(chip);
                index++;
            }
            
            for (int i = 0; i < chips10; i++)
            {
                Vector3 pos = chipsSpawnPoint.position + Vector3.up * (index * stackSpacing);
                GameObject chip = Instantiate(chipsStackPrefab10, pos, Quaternion.identity);
                chip.transform.SetParent(chipsSpawnPoint);
                chip.transform.localScale = Vector3.one * npcChipScale;
                activeChips.Add(chip);
                index++;
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
    
    private void UpdateChipsDisplay(NPCController npc, int chips)
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
        if (bankTarget == null || chipsStackPrefab10 == null) return;
        
        // Воспроизводим ОДИН звук проигрыша фишек
        PlayChipSound();
        
        int chips100 = amount / 100;
        int chips10 = (amount % 100) / 10;
        int totalChips = chips100 + chips10;
        
        if (totalChips == 0) totalChips = 1;
        
        int chipsToRemove = Mathf.Min(totalChips, activeChips.Count);
        
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
        
        BankChipsVisualController bank = FindObjectOfType<BankChipsVisualController>();
        if (bank != null)
        {
            bank.RequestChips(totalChips, this);
        }
    }
    
    public void ReceiveChipFromBank(GameObject chip)
    {
        if (chip == null) return;
        
        chip.transform.SetParent(chipsSpawnPoint);
        activeChips.Add(chip);
        
        Vector3 targetPos = chipsSpawnPoint.position + Vector3.up * ((activeChips.Count - 1) * stackSpacing);
        
        Tween.Position(chip.transform, targetPos, flyDuration, 0, flyCurve);
        Tween.LocalScale(chip.transform, Vector3.one * npcChipScale, flyDuration, 0, flyCurve);
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
        if (npcChips != null)
            npcChips.OnChipsChanged -= UpdateChipsDisplay;
    }
}