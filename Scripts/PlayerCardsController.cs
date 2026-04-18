// PlayerCardsController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCardsController : MonoBehaviour
{
    [Header("Card Settings")]
    public GameObject cardPrefab;
    public Transform cardSpawnPoint;
    public Transform cardTargetPoint;
    public Transform cardDiscardPoint;
    
    [Header("Hand Settings")]
    public int maxCards = 5;
    public float cardSpacing = 1.5f;
    public float cardLiftHeight = 2f;
    public float moveDuration = 0.5f;
    public float discardDuration = 0.3f;
    
    [Header("Card Sprites")]
    public List<Sprite> cardSprites; // Спрайты карт
    
    private List<Card> currentCards = new List<Card>();
    private HandStrength currentHandStrength;
    
    // Событие получения новой руки
    public System.Action<HandStrength, string> OnHandReceived;
    
    void Start()
    {
        if (cardSpawnPoint == null)
            cardSpawnPoint = transform;
        if (cardTargetPoint == null)
            cardTargetPoint = transform;
        if (cardDiscardPoint == null)
            cardDiscardPoint = transform;
    }
    
    public void DealNewHand()
    {
        StartCoroutine(DealHandSequence());
    }
    
    IEnumerator DealHandSequence()
    {
        // Сначала сбрасываем старые карты
        if (currentCards.Count > 0)
        {
            yield return StartCoroutine(DiscardAllCardsSequence());
        }
        
        // Генерируем новую руку
        GenerateRandomHand();
        
        // Раздаем новые карты
        for (int i = 0; i < maxCards; i++)
        {
            CreateCard(i);
            yield return new WaitForSeconds(0.1f);
        }
        
        // Определяем силу руки
        currentHandStrength = EvaluateHandStrength();
        
        // Уведомляем о новой руке
        string handDescription = GetHandDescription();
        OnHandReceived?.Invoke(currentHandStrength, handDescription);
        
        Debug.Log($"<color=cyan>Игрок получил: {handDescription}</color>");
    }
    
    private void GenerateRandomHand()
    {
        // Простая генерация случайных карт
        // В будущем можно заменить на реальную колоду
    }
    
    private void CreateCard(int index)
    {
        Vector3 spawnPos = cardSpawnPoint.position + Vector3.down * cardLiftHeight;
        Vector3 targetPos = CalculateCardPosition(index, maxCards);
        
        GameObject cardObj = Instantiate(cardPrefab, spawnPos, Quaternion.identity, transform);
        Card card = cardObj.GetComponent<Card>();
        
        if (card == null)
        {
            card = cardObj.AddComponent<Card>();
        }
        
        // Установка случайной карты
        Card.Suit randomSuit = (Card.Suit)Random.Range(0, 4);
        Card.Rank randomRank = (Card.Rank)Random.Range(2, 15);
        
        Sprite cardSprite = GetCardSprite(randomSuit, randomRank);
        card.SetCard(randomSuit, randomRank, cardSprite);
        card.SetSortingOrder(index);
        
        currentCards.Add(card);
        
        // Анимация подъема
        card.MoveTo(targetPos, moveDuration);
    }
    
    private Vector3 CalculateCardPosition(int index, int totalCards)
    {
        float startX = cardTargetPoint.position.x - (totalCards - 1) * cardSpacing / 2f;
        float x = startX + index * cardSpacing;
        float y = cardTargetPoint.position.y;
        
        // Небольшой веер по rotation
        return new Vector3(x, y, 0);
    }
    
    private Sprite GetCardSprite(Card.Suit suit, Card.Rank rank)
    {
        // Заглушка - в реальном проекте спрайты карт
        if (cardSprites.Count > 0)
        {
            int index = ((int)suit * 13 + (int)rank - 2) % cardSprites.Count;
            return cardSprites[index];
        }
        return null;
    }
    
    public void DiscardAllCards()
    {
        StartCoroutine(DiscardAllCardsSequence());
    }
    
    IEnumerator DiscardAllCardsSequence()
    {
        foreach (var card in currentCards)
        {
            if (card != null)
            {
                Vector3 discardPos = cardDiscardPoint.position + Vector3.down * cardLiftHeight;
                card.MoveTo(discardPos, discardDuration);
            }
        }
        
        yield return new WaitForSeconds(discardDuration);
        
        // Уничтожаем карты
        foreach (var card in currentCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        
        currentCards.Clear();
    }
    
    private HandStrength EvaluateHandStrength()
    {
        // Простая оценка по сумме значений
        int totalValue = 0;
        foreach (var card in currentCards)
        {
            totalValue += card.GetValue();
        }
        
        if (totalValue > 45) return HandStrength.Strong;
        if (totalValue > 30) return HandStrength.Medium;
        return HandStrength.Weak;
    }
    
    public HandStrength GetCurrentHandStrength()
    {
        return currentHandStrength;
    }
    
    public string GetHandDescription()
    {
        if (currentCards.Count == 0) return "Нет карт";
        
        string desc = "";
        foreach (var card in currentCards)
        {
            desc += GetCardShortName(card) + " ";
        }
        
        return $"{desc.Trim()} | {GetStrengthText(currentHandStrength)}";
    }
    
    private string GetCardShortName(Card card)
    {
        string rankStr = card.rank switch
        {
            Card.Rank.Ace => "A",
            Card.Rank.King => "K",
            Card.Rank.Queen => "Q",
            Card.Rank.Jack => "J",
            _ => ((int)card.rank).ToString()
        };
        
        string suitStr = card.suit switch
        {
            Card.Suit.Hearts => "♥",
            Card.Suit.Diamonds => "♦",
            Card.Suit.Clubs => "♣",
            Card.Suit.Spades => "♠",
            _ => ""
        };
        
        return rankStr + suitStr;
    }
    
    private string GetStrengthText(HandStrength strength)
    {
        return strength switch
        {
            HandStrength.Weak => "СЛАБАЯ",
            HandStrength.Medium => "СРЕДНЯЯ",
            HandStrength.Strong => "СИЛЬНАЯ",
            _ => "???"
        };
    }
    
    public string GetStrengthText()
    {
        return GetStrengthText(currentHandStrength);
    }
    
    public void FoldCards()
    {
        StartCoroutine(DiscardAllCardsSequence());
    }
}