// GameDebug.cs
using System.Collections.Generic;
using UnityEngine;

public static class GameDebug
{
    // Цвета
    private const string COLOR_YELLOW = "#FFD700";
    private const string COLOR_WHITE = "#FFFFFF";
    private const string COLOR_CYAN = "#00FFFF";
    private const string COLOR_ORANGE = "#FFA500";
    private const string COLOR_GREEN = "#51CF66";
    private const string COLOR_RED = "#FF6B6B";
    private const string COLOR_GRAY = "#888888";
    private const string COLOR_PURPLE = "#C586C0";
    private const string COLOR_BLUE = "#4A90E2";
    
    #region Headers and Dividers
    
    public static void LogHeader(string text)
    {
        Debug.Log($"<color={COLOR_YELLOW}><b>=== {text} ===</b></color>");
    }
    
    public static void LogRound(int round, int maxRounds)
    {
        Debug.Log($"<color={COLOR_YELLOW}><b>--- РАУНД {round}/{maxRounds} ---</b></color>");
    }
    
    public static void LogPhase(string phase)
    {
        Debug.Log($"<color={COLOR_BLUE}><b>--- {phase} ---</b></color>");
    }
    
    public static void LogDivider()
    {
        Debug.Log($"<color=#444444>================================</color>");
    }
    
    #endregion
    
    #region Info Messages
    
    public static void LogInfo(string text)
    {
        Debug.Log($"<color={COLOR_WHITE}>{text}</color>");
    }
    
    public static void LogSuccess(string text)
    {
        Debug.Log($"<color={COLOR_GREEN}>✓ {text}</color>");
    }
    
    public static void LogWarning(string text)
    {
        Debug.Log($"<color={COLOR_ORANGE}>⚠ {text}</color>");
    }
    
    public static void LogError(string text)
    {
        Debug.Log($"<color={COLOR_RED}><b>✗ {text}</b></color>");
    }
    
    #endregion
    
    #region NPC Logs
    
    public static void LogNPCEmotion(string npcName, EmotionType emotion)
    {
        string icon = GetEmotionIcon(emotion);
        Debug.Log($"  <color={COLOR_WHITE}>{npcName}: {icon} {emotion}</color>");
    }
    
    public static void LogNPCAction(string npcName, EmotionType emotion, PlayerAction action)
    {
        string icon = GetEmotionIcon(emotion);
        string actionText = GetActionShortText(action);
        Debug.Log($"<color={COLOR_ORANGE}>  {npcName}: {icon} → {actionText}</color>");
    }
    
    public static void LogNPCHand(string npcName, List<CardData> cards, string handDesc, int handValue)
    {
        string cardsStr = GetCardsString(cards);
        Debug.Log($"  <color={COLOR_WHITE}>{npcName}: {cardsStr} → {handDesc} (Сила: {handValue})</color>");
    }
    
    #endregion
    
    #region Player Logs
    
    public static void LogPlayerHand(string handDesc, int handValue)
    {
        Debug.Log($"  <color={COLOR_CYAN}>Вы: {handDesc} (Сила: {handValue})</color>");
    }
    
    public static void LogPlayerTurn(int currentBet, int handValue, int chips)
    {
        Debug.Log($"<color={COLOR_CYAN}><b>Ваш ход!</b> Ставка: {currentBet} | Ваши фишки: {chips}</color>");
        Debug.Log($"<color={COLOR_CYAN}>1 - FOLD | 2 - CALL ({currentBet}) | 3 - RAISE (+10)</color>");
        
        string advice = handValue >= 300 ? "💪 Сильная рука! Рекомендуется Raise или Call" :
                        handValue >= 100 ? "✋ Средняя рука. Можно Call, осторожно с Raise" :
                        "🤚 Слабая рука. Лучше Fold, если только не блефуешь";
        
        Debug.Log($"<color={COLOR_GRAY}>{advice}</color>");
    }
    
    public static void LogPlayerAction(PlayerAction action, int currentBet)
    {
        string actionText = action switch
        {
            PlayerAction.Fold => "🏃 FOLD",
            PlayerAction.Call => $"✅ CALL ({currentBet})",
            PlayerAction.Raise => $"⬆️ RAISE ({currentBet + 10})",
            _ => "???"
        };
        
        Debug.Log($"<color={COLOR_CYAN}><b>Вы выбрали: {actionText}</b></color>");
    }
    
    #endregion
    
    #region Betting Logs
    
    public static void LogRaise(int newBet)
    {
        Debug.Log($"  <color={COLOR_YELLOW}>⬆️ Ставка повышена до {newBet}!</color>");
    }
    
    public static void LogBetInfo(int currentBet, int pot)
    {
        Debug.Log($"<color={COLOR_WHITE}><b>Текущая ставка: {currentBet} | Банк: {pot}</b></color>");
    }
    
    public static void LogPot(int pot)
    {
        Debug.Log($"<color={COLOR_WHITE}><b>💰 Банк: {pot}</b></color>");
    }
    
    public static void LogChips(int chips)
    {
        Debug.Log($"<color={COLOR_WHITE}>💰 Осталось фишек: {chips}</color>");
    }
    
    #endregion
    
    #region Winner Logs
    
    public static void LogWinner(string winner, int pot, string handDesc)
    {
        if (winner == "Вы")
        {
            Debug.Log($"<color={COLOR_GREEN}><b>🏆 ВЫ ВЫИГРАЛИ {pot} фишек! ({handDesc})</b></color>");
        }
        else if (!string.IsNullOrEmpty(winner))
        {
            Debug.Log($"<color={COLOR_PURPLE}><b>🏆 {winner} выиграл {pot} фишек! ({handDesc})</b></color>");
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private static string GetEmotionIcon(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Happy => "😊",
            EmotionType.Neutral => "😐",
            EmotionType.Angry => "😠",
            _ => "❓"
        };
    }
    
    private static string GetActionShortText(PlayerAction action)
    {
        return action switch
        {
            PlayerAction.Fold => "🏃 FOLD",
            PlayerAction.Call => "✅ CALL",
            PlayerAction.Raise => "⬆️ RAISE",
            _ => "???"
        };
    }
    
    private static string GetCardsString(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0) return "";
        
        string result = "";
        foreach (var card in cards)
        {
            string suitStr = card.suit switch
            {
                Card.Suit.Hearts => "♥",
                Card.Suit.Diamonds => "♦",
                Card.Suit.Clubs => "♣",
                Card.Suit.Spades => "♠",
                _ => ""
            };
            
            string rankStr = card.rank switch
            {
                Card.Rank.Ace => "A",
                Card.Rank.King => "K",
                Card.Rank.Queen => "Q",
                Card.Rank.Jack => "J",
                _ => ((int)card.rank).ToString()
            };
            
            // Цвет масти
            string color = (card.suit == Card.Suit.Hearts || card.suit == Card.Suit.Diamonds) ? "#FF6B6B" : "#FFFFFF";
            result += $"<color={color}>{rankStr}{suitStr}</color> ";
        }
        return result.Trim();
    }
    
    #endregion
}