using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCBrain", menuName = "Signal Table/NPC Behaviour")]
public class NPCBehaviour : ScriptableObject
{
    public string behaviourName;
    public NPCArchetype archetype;
    public RiskTolerance riskTolerance;

    [Header("Hand Strength Thresholds")]
    public int weakThreshold = 100;   // < 100 = слабая
    public int strongThreshold = 300; // > 300 = сильная

    // Archetype влияет ТОЛЬКО на эмоции!
    public EmotionType GetDisplayedEmotion(int handValue)
    {
        bool isStrong = handValue >= strongThreshold;
        bool isWeak = handValue < weakThreshold;
        
        if (archetype == NPCArchetype.Sigma)
        {
            return EmotionType.Sigma;
        }
        
        if (archetype == NPCArchetype.Panicker)
        {
            if (isStrong) return EmotionType.Neutral;
            if (isWeak) return EmotionType.Scared;
            return EmotionType.Worried;
        }
        
        if (archetype == NPCArchetype.Villain)
        {
            if (isStrong) return EmotionType.Evil;
            if (isWeak) return EmotionType.Angry;
            return EmotionType.Neutral;
        }
        
        if (archetype == NPCArchetype.Truth)
        {
            if (isStrong) return EmotionType.Happy;
            if (isWeak) return EmotionType.Angry;
            return EmotionType.Neutral;
        }
        else // Bluff
        {
            if (isWeak) return EmotionType.Happy;
            if (isStrong) return EmotionType.Angry;
            return EmotionType.Neutral;
        }
    }

    public PlayerAction GetAction(int handValue, float randomSeed, int consecutiveFolds = 0)
    {
        bool isStrong = handValue >= strongThreshold;
        bool isWeak = handValue < weakThreshold;
        float random = randomSeed;
        
        // Если до этого был сброс (consecutiveFolds > 0), то следующий ход 100% колл или рейз
        bool wasFoldBefore = consecutiveFolds > 0;
        
        // ========== СИЛЬНАЯ РУКА (≥300) ==========
        if (isStrong)
        {
            if (wasFoldBefore)
            {
                // Если был сброс - обязательно рейз
                return PlayerAction.Raise;
            }
            
            switch (riskTolerance)
            {
                case RiskTolerance.Aggressive:
                    return PlayerAction.Raise;
                    
                case RiskTolerance.Normal:
                    return random < 0.8f ? PlayerAction.Raise : PlayerAction.Call;
                    
                case RiskTolerance.Safe:
                    return random < 0.4f ? PlayerAction.Raise : PlayerAction.Call;
                    
                default:
                    return PlayerAction.Call;
            }
        }

        // ========== СРЕДНЯЯ РУКА (100-300) ==========
        if (!isStrong && !isWeak)
        {
            if (wasFoldBefore)
            {
                // Если был сброс - обязательно играем (рейз или колл)
                return random < 0.7f ? PlayerAction.Raise : PlayerAction.Call;
            }
            
            switch (riskTolerance)
            {
                case RiskTolerance.Aggressive:
                    return random < 0.7f ? PlayerAction.Raise : PlayerAction.Call;
                    
                case RiskTolerance.Normal:
                    if (random < 0.2f)
                        return PlayerAction.Fold;
                    else
                        return random < 0.6f ? PlayerAction.Call : PlayerAction.Raise;
                    
                case RiskTolerance.Safe:
                    if (random < 0.3f)
                        return PlayerAction.Fold;
                    else
                        return random < 0.6f ? PlayerAction.Call : PlayerAction.Raise;
                    
                default:
                    return PlayerAction.Call;
            }
        }

        // ========== СЛАБАЯ РУКА (<100) ==========
        if (isWeak)
        {
            if (wasFoldBefore)
            {
                // Если был сброс - обязательно играем (рейз или колл)
                return random < 0.5f ? PlayerAction.Raise : PlayerAction.Call;
            }
            
            switch (riskTolerance)
            {
                case RiskTolerance.Aggressive:
                    return random < 0.5f ? PlayerAction.Raise : PlayerAction.Call;
                    
                case RiskTolerance.Normal:
                    if (random < 0.3f)
                        return PlayerAction.Fold;
                    else
                        return random < 0.5f ? PlayerAction.Call : PlayerAction.Raise;
                    
                case RiskTolerance.Safe:
                    if (random < 0.4f)
                        return PlayerAction.Fold;
                    else
                        return random < 0.6f ? PlayerAction.Call : PlayerAction.Raise;
                    
                default:
                    return PlayerAction.Fold;
            }
        }

        return PlayerAction.Fold;
    }
}