// HandEvaluator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HandRank
{
    HighCard = 0,
    Pair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8,
    RoyalFlush = 9
}

public class HandEvaluator : MonoBehaviour
{
    public static HandEvaluator Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public (HandRank rank, int value, string description) EvaluateHand(List<CardData> cards)
    {
        if (cards.Count < 5) return (HandRank.HighCard, 0, "High Card");
        
        var sortedCards = cards.OrderByDescending(c => (int)c.rank).ToList();
        
        // Проверяем комбинации от сильной к слабой
        if (IsRoyalFlush(sortedCards)) return (HandRank.RoyalFlush, 900, "Royal Flush!");
        if (IsStraightFlush(sortedCards, out int sfHigh)) return (HandRank.StraightFlush, 800 + sfHigh, "Straight Flush");
        if (IsFourOfAKind(sortedCards, out int fHigh)) return (HandRank.FourOfAKind, 700 + fHigh, "Four of a Kind");
        if (IsFullHouse(sortedCards, out int fhHigh)) return (HandRank.FullHouse, 600 + fhHigh, "Full House");
        if (IsFlush(sortedCards, out int flHigh)) return (HandRank.Flush, 500 + flHigh, "Flush");
        if (IsStraight(sortedCards, out int sHigh)) return (HandRank.Straight, 400 + sHigh, "Straight");
        if (IsThreeOfAKind(sortedCards, out int tHigh)) return (HandRank.ThreeOfAKind, 300 + tHigh, "Three of a Kind");
        if (IsTwoPair(sortedCards, out int tpHigh)) return (HandRank.TwoPair, 200 + tpHigh, "Two Pair");
        if (IsPair(sortedCards, out int pHigh)) return (HandRank.Pair, 100 + pHigh, "Pair");
        
        int highCard = (int)sortedCards[0].rank;
        return (HandRank.HighCard, highCard, $"{sortedCards[0].rank} High");
    }
    
    private bool IsRoyalFlush(List<CardData> cards)
    {
        if (!IsFlush(cards, out _)) return false;
        var ranks = cards.Select(c => (int)c.rank).OrderBy(r => r).ToList();
        return ranks.SequenceEqual(new List<int> { 10, 11, 12, 13, 14 });
    }
    
    private bool IsStraightFlush(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        if (!IsFlush(cards, out _)) return false;
        return IsStraight(cards, out highCard);
    }
    
    private bool IsFourOfAKind(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var groups = cards.GroupBy(c => c.rank);
        foreach (var g in groups)
        {
            if (g.Count() == 4)
            {
                highCard = (int)g.Key;
                return true;
            }
        }
        return false;
    }
    
    private bool IsFullHouse(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var groups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ToList();
        if (groups.Count == 2 && groups[0].Count() == 3 && groups[1].Count() == 2)
        {
            highCard = (int)groups[0].Key;
            return true;
        }
        return false;
    }
    
    private bool IsFlush(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var suit = cards[0].suit;
        if (cards.All(c => c.suit == suit))
        {
            highCard = (int)cards.Max(c => c.rank);
            return true;
        }
        return false;
    }
    
    private bool IsStraight(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var ranks = cards.Select(c => (int)c.rank).OrderBy(r => r).ToList();
        
        // Проверка на стрит
        bool isStraight = true;
        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] != ranks[i - 1] + 1)
            {
                isStraight = false;
                break;
            }
        }
        
        // Проверка на A-2-3-4-5
        if (!isStraight && ranks.SequenceEqual(new List<int> { 2, 3, 4, 5, 14 }))
        {
            highCard = 5;
            return true;
        }
        
        if (isStraight)
        {
            highCard = ranks.Max();
            return true;
        }
        
        return false;
    }
    
    private bool IsThreeOfAKind(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var groups = cards.GroupBy(c => c.rank);
        foreach (var g in groups)
        {
            if (g.Count() == 3)
            {
                highCard = (int)g.Key;
                return true;
            }
        }
        return false;
    }
    
    private bool IsTwoPair(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var pairs = cards.GroupBy(c => c.rank).Where(g => g.Count() == 2).ToList();
        if (pairs.Count >= 2)
        {
            highCard = pairs.Max(p => (int)p.Key);
            return true;
        }
        return false;
    }
    
    private bool IsPair(List<CardData> cards, out int highCard)
    {
        highCard = 0;
        var groups = cards.GroupBy(c => c.rank);
        foreach (var g in groups)
        {
            if (g.Count() == 2)
            {
                highCard = (int)g.Key;
                return true;
            }
        }
        return false;
    }
    
    public int GetHandStrengthValue(List<CardData> cards)
    {
        var result = EvaluateHand(cards);
        return result.value;
    }
}