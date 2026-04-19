// DeckManager.cs
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    private List<CardData> deck = new List<CardData>();
    
    void Awake()
    {
        CreateDeck();
    }
    
    private void CreateDeck()
    {
        deck.Clear();
        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 2; rank <= 14; rank++)
            {
                deck.Add(new CardData((Card.Suit)suit, (Card.Rank)rank));
            }
        }
        Shuffle();
    }
    
    public void Shuffle()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
    
    public List<CardData> DealCards(int count)
    {
        if (deck.Count < count)
        {
            CreateDeck();
        }
        
        List<CardData> hand = new List<CardData>();
        for (int i = 0; i < count; i++)
        {
            hand.Add(deck[0]);
            deck.RemoveAt(0);
        }
        return hand;
    }
    
    public void ResetDeck()
    {
        CreateDeck();
    }
}

public struct CardData
{
    public Card.Suit suit;
    public Card.Rank rank;
    
    public CardData(Card.Suit s, Card.Rank r)
    {
        suit = s;
        rank = r;
    }
}