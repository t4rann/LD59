// Card.cs
using UnityEngine;
using TMPro;

public class Card : MonoBehaviour
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
    
    [Header("Card Components")]
    public SpriteRenderer suitRenderer;
    public TextMeshPro valueText;
    public SpriteRenderer backgroundRenderer;
    
    public Suit suit { get; private set; }
    public Rank rank { get; private set; }
    
    public void SetCard(Suit newSuit, Rank newRank, Sprite suitSprite)
    {
        suit = newSuit;
        rank = newRank;
        
        if (suitRenderer != null && suitSprite != null)
            suitRenderer.sprite = suitSprite;
        
        if (valueText != null)
        {
            valueText.text = GetRankString(newRank);
            // Цвет не меняем, остается тот что задан в префабе
        }
    }
    
    private string GetRankString(Rank r)
    {
        return r switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            _ => ((int)r).ToString()
        };
    }
    
    public string GetCardName()
    {
        string suitStr = suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            Suit.Spades => "♠",
            _ => ""
        };
        return $"{GetRankString(rank)}{suitStr}";
    }
    
    public void SetSortingOrder(int order)
    {
        if (backgroundRenderer != null) backgroundRenderer.sortingOrder = order;
        if (suitRenderer != null) suitRenderer.sortingOrder = order + 1;
        if (valueText != null) valueText.sortingOrder = order + 2;
    }
}