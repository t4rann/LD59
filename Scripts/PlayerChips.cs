// PlayerChips.cs
using UnityEngine;
using TMPro;
using Pixelplacement;

public class PlayerChips : MonoBehaviour
{
    [Header("Starting Chips")]
    [SerializeField] private int startingChips = 100;
    
    [Header("Visuals")]
    [SerializeField] private GameObject chipsVisual;      // Спрайт фишки
    [SerializeField] private TextMeshPro chipsText;       // TextMeshPro в мире
    
    [Header("Animation")]
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private int currentChips;
    private Vector3 originalScale;
    
    public System.Action<int> OnChipsChanged;
    
    void Start()
    {
        if (chipsVisual != null)
            originalScale = chipsVisual.transform.localScale;
            
        currentChips = startingChips;
        UpdateChipsDisplay();
        OnChipsChanged?.Invoke(currentChips);
    }
    
    private void UpdateChipsDisplay()
    {
        if (chipsText != null)
        {
            chipsText.text = $"{currentChips}";
        }
    }
    
    private void AnimateChipsChange()
    {
        if (chipsVisual == null) return;
        
        // Анимация увеличения спрайта фишки
        Tween.LocalScale(chipsVisual.transform, 
            originalScale * punchScale, 
            animationDuration * 0.5f, 
            0, 
            punchCurve, 
            Tween.LoopType.None, 
            null, 
            () => 
            {
                // Возвращаем к исходному размеру
                Tween.LocalScale(chipsVisual.transform, 
                    originalScale, 
                    animationDuration * 0.5f, 
                    0, 
                    punchCurve);
            });
    }
    
    public int GetChips()
    {
        return currentChips;
    }
    
    public bool AddChips(int amount)
    {
        if (amount < 0) return false;
        currentChips += amount;
        UpdateChipsDisplay();
        AnimateChipsChange();
        OnChipsChanged?.Invoke(currentChips);
        return true;
    }
    
    public bool RemoveChips(int amount)
    {
        if (amount < 0) return false;
        if (currentChips < amount) return false;
        
        currentChips -= amount;
        UpdateChipsDisplay();
        AnimateChipsChange();
        OnChipsChanged?.Invoke(currentChips);
        return true;
    }
    
    public bool HasEnoughChips(int amount)
    {
        return currentChips >= amount;
    }
    
    public bool IsBroke()
    {
        return currentChips <= 0;
    }
    
    public void ResetChips()
    {
        currentChips = startingChips;
        UpdateChipsDisplay();
        OnChipsChanged?.Invoke(currentChips);
    }
}