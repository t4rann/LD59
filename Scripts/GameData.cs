// GameData.cs
public enum HandStrength
{
    Weak,
    Medium,
    Strong
}

public enum EmotionType
{
    Happy,
    Neutral,
    Angry,
    Sigma,      // Сигма - надменность/безразличие
    Scared,     // Паникер - страх
    Worried,    // Паникер - беспокойство
    Evil        // Негодяй - злой/преступный
}

public enum PlayerAction
{
    None,
    Fold,
    Call,
    Raise
}

public enum NPCArchetype
{
    Truth,      // Честный - эмоция соответствует руке
    Bluff,      // Блефующий - показывает противоположное
    Sigma,      // Сигма - всегда показывает Sigma эмоцию
    Panicker,   // Паникер - боится проиграть
    Villain     // Негодяй - злой
}

public enum RiskTolerance
{
    Safe,
    Normal,
    Aggressive
}