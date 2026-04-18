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

    public EmotionType GetDisplayedEmotion(HandStrength trueStrength)
    {
        if (archetype == NPCArchetype.Truth)
        {
            return StrengthToEmotion(trueStrength);
        }
        else // Bluff
        {
            if (trueStrength == HandStrength.Weak) return EmotionType.Happy;
            if (trueStrength == HandStrength.Strong) return EmotionType.Angry;
            return EmotionType.Neutral;
        }
    }

    public PlayerAction GetAction(HandStrength trueStrength, float randomSeed)
    {
        if (trueStrength == HandStrength.Strong)
        {
            if (riskTolerance == RiskTolerance.Safe) return PlayerAction.Call;
            return PlayerAction.Raise;
        }

        if (trueStrength == HandStrength.Medium)
        {
            if (riskTolerance == RiskTolerance.Aggressive) return PlayerAction.Raise;
            if (riskTolerance == RiskTolerance.Normal) return PlayerAction.Call;
            return (randomSeed > 0.5f) ? PlayerAction.Call : PlayerAction.Fold;
        }

        if (trueStrength == HandStrength.Weak)
        {
            if (archetype == NPCArchetype.Bluff && randomSeed < irrationalAggressionChance)
            {
                return PlayerAction.Raise;
            }
            return PlayerAction.Fold;
        }

        return PlayerAction.Fold;
    }

    private EmotionType StrengthToEmotion(HandStrength strength)
    {
        switch (strength)
        {
            case HandStrength.Strong: return EmotionType.Happy;
            case HandStrength.Weak: return EmotionType.Angry;
            default: return EmotionType.Neutral;
        }
    }
}