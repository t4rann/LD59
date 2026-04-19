// NPCBehaviour.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCBrain", menuName = "Signal Table/NPC Behaviour")]
public class NPCBehaviour : ScriptableObject
{
    public string behaviourName;
    public NPCArchetype archetype;
    public RiskTolerance riskTolerance;

    [Header("Bluff Settings")]
    [Range(0f, 1f)]
    public float irrationalAggressionChance = 0.2f;
    
    [Header("Hand Strength Thresholds")]
    public int weakThreshold = 100;   // < 100 = слабая
    public int strongThreshold = 300; // > 300 = сильная

    public EmotionType GetDisplayedEmotion(int handValue)
    {
        bool isStrong = handValue >= strongThreshold;
        bool isWeak = handValue < weakThreshold;
        
        if (archetype == NPCArchetype.Truth)
        {
            if (isStrong) return EmotionType.Happy;
            if (isWeak) return EmotionType.Angry;
            return EmotionType.Neutral;
        }
        else // Bluff
        {
            if (isWeak) return EmotionType.Happy;    // Блефует силой
            if (isStrong) return EmotionType.Angry;   // Прикидывается слабым
            return EmotionType.Neutral;
        }
    }

    public PlayerAction GetAction(int handValue, float randomSeed)
    {
        bool isStrong = handValue >= strongThreshold;
        bool isWeak = handValue < weakThreshold;
        
        if (isStrong)
        {
            if (riskTolerance == RiskTolerance.Safe) return PlayerAction.Call;
            return PlayerAction.Raise;
        }

        if (!isStrong && !isWeak) // Medium
        {
            if (riskTolerance == RiskTolerance.Aggressive) return PlayerAction.Raise;
            if (riskTolerance == RiskTolerance.Normal) return PlayerAction.Call;
            return (randomSeed > 0.5f) ? PlayerAction.Call : PlayerAction.Fold;
        }

        if (isWeak)
        {
            if (archetype == NPCArchetype.Bluff && randomSeed < irrationalAggressionChance)
            {
                return PlayerAction.Raise;
            }
            return PlayerAction.Fold;
        }

        return PlayerAction.Fold;
    }
}