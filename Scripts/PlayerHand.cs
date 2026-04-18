// PlayerHand.cs
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [SerializeField] private HandStrength currentStrength;
    
    public HandStrength CurrentStrength => currentStrength;
    
    public void ReceiveNewHand(HandStrength strength)
    {
        currentStrength = strength;
        Debug.Log($"<color=cyan>Вам раздали: {GetStrengthName(strength)}</color>");
    }
    
    private string GetStrengthName(HandStrength strength)
    {
        switch (strength)
        {
            case HandStrength.Weak: return "🤚 СЛАБАЯ РУКА";
            case HandStrength.Medium: return "✋ СРЕДНЯЯ РУКА";
            case HandStrength.Strong: return "💪 СИЛЬНАЯ РУКА";
            default: return "???";
        }
    }
    
    public string GetStrengthText()
    {
        return currentStrength switch
        {
            HandStrength.Weak => "СЛАБАЯ",
            HandStrength.Medium => "СРЕДНЯЯ",
            HandStrength.Strong => "СИЛЬНАЯ",
            _ => "???"
        };
    }
}