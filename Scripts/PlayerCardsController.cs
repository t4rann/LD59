using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pixelplacement;

public class PlayerCardsController : MonoBehaviour
{
    [Header("Card Settings")]
    public GameObject cardPrefab;
    public DeckManager deckManager;
    public HandEvaluator handEvaluator;
    
    [Header("Positions")]
    public Transform handPosition;      // Точка где рука держит карты
    public Transform spawnPosition;     // Точка спавна карт (снизу)
    public Transform despawnPosition;   // Точка сброса карт (снизу)
    
    [Header("Hand Settings")]
    public int maxCards = 5;
    public float cardSpacing = 1.2f;
    public float fanStartAngle = -20f;
    public float fanEndAngle = 20f;
    public float verticalOffset = -0.5f;
    
    [Header("Animation")]
    public float dealDuration = 0.4f;
    public float dealDelay = 0.1f;
    public float discardDuration = 0.3f;
    public AnimationCurve dealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve discardCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Card Sprites")]
    public Sprite heartsSprite;
    public Sprite diamondsSprite;
    public Sprite clubsSprite;
    public Sprite spadesSprite;
    
    private List<Card> currentCards = new List<Card>();
    private List<CardData> currentHandData = new List<CardData>();
    private (HandRank rank, int value, string description) handEvaluation;
    private bool isFolded = false;  // Флаг фолда
    
    public System.Action<string, int> OnHandReceived;
    
    void Start()
    {
        if (handPosition == null) handPosition = transform;
        if (spawnPosition == null) spawnPosition = transform;
        if (despawnPosition == null) despawnPosition = transform;
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (handEvaluator == null) handEvaluator = FindObjectOfType<HandEvaluator>();
    }
    
    public bool IsFolded()
    {
        return isFolded;
    }
    
    public void ResetFoldState()
    {
        isFolded = false;
    }
    
    public void DealNewHand()
    {
        isFolded = false;  // Сбрасываем фолд при новой раздаче
        StartCoroutine(DealHandSequence());
    }
    
    IEnumerator DealHandSequence()
    {
        if (currentCards.Count > 0)
        {
            yield return StartCoroutine(DiscardAllCardsSequence());
        }
        
        // Берем карты из колоды
        currentHandData = deckManager.DealCards(maxCards);
        
        // Оцениваем комбинацию
        handEvaluation = handEvaluator.EvaluateHand(currentHandData);
        
        // Раздаем карты
        for (int i = 0; i < currentHandData.Count; i++)
        {
            DealCard(i, currentHandData[i]);
            yield return new WaitForSeconds(dealDelay);
        }
        
        yield return new WaitForSeconds(dealDuration);
        
        string handDescription = GetHandDescription();
        OnHandReceived?.Invoke(handDescription, handEvaluation.value);
        
        GameDebug.LogPlayerHand(handDescription, handEvaluation.value);
    }
    
    private void DealCard(int index, CardData cardData)
    {
        Vector3 targetPos = CalculateHandPosition(index);
        Quaternion targetRot = CalculateHandRotation(index);
        
        // Спавним в точке спавна
        GameObject cardObj = Instantiate(cardPrefab, spawnPosition.position, Quaternion.identity, handPosition);
        Card card = cardObj.GetComponent<Card>();
        
        if (card == null) card = cardObj.AddComponent<Card>();
        
        Sprite suitSprite = GetSuitSprite(cardData.suit);
        card.SetCard(cardData.suit, cardData.rank, suitSprite);
        card.SetSortingOrder(index * 2);
        
        currentCards.Add(card);
        
        cardObj.transform.localScale = Vector3.one * 0.5f;
        
        // Анимация к руке
        Tween.Position(cardObj.transform, targetPos, dealDuration, 0, dealCurve);
        Tween.Rotation(cardObj.transform, targetRot, dealDuration, 0, dealCurve);
        Tween.LocalScale(cardObj.transform, Vector3.one, dealDuration, 0, dealCurve);
    }
    
    private Vector3 CalculateHandPosition(int index)
    {
        float totalWidth = (maxCards - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        float x = startX + index * cardSpacing;
        
        float t = Mathf.Abs((float)index / (maxCards - 1) - 0.5f) * 2f;
        float y = verticalOffset + t * 0.1f;
        float z = -t * 0.3f;
        
        return handPosition.position + new Vector3(x, y, z);
    }
    
    private Quaternion CalculateHandRotation(int index)
    {
        float t = (float)index / (maxCards - 1);
        float angle = Mathf.Lerp(fanStartAngle, fanEndAngle, t);
        return Quaternion.Euler(0, 0, angle);
    }
    
    private Sprite GetSuitSprite(Card.Suit suit)
    {
        return suit switch
        {
            Card.Suit.Hearts => heartsSprite,
            Card.Suit.Diamonds => diamondsSprite,
            Card.Suit.Clubs => clubsSprite,
            Card.Suit.Spades => spadesSprite,
            _ => null
        };
    }
    
    public void DiscardAllCards()
    {
        StartCoroutine(DiscardAllCardsSequence());
    }
    
    IEnumerator DiscardAllCardsSequence()
    {
        // Сбрасываем справа налево
        for (int i = currentCards.Count - 1; i >= 0; i--)
        {
            if (currentCards[i] != null)
            {
                DiscardCard(currentCards[i]);
            }
            yield return new WaitForSeconds(dealDelay);
        }
        
        yield return new WaitForSeconds(discardDuration + 0.1f);
        
        // Уничтожаем карты
        foreach (var card in currentCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        
        currentCards.Clear();
        currentHandData.Clear();
        
        // Возвращаем карты в колоду
        deckManager.ResetDeck();
    }
    
    private void DiscardCard(Card card)
    {
        // Анимация к точке деспавна
        Tween.Position(card.transform, despawnPosition.position, discardDuration, 0, discardCurve);
        Tween.LocalScale(card.transform, Vector3.one * 0.5f, discardDuration, 0, discardCurve);
    }
    
    public string GetHandDescription()
    {
        if (currentCards.Count == 0) return "Нет карт";
        
        string cardsStr = "";
        foreach (var card in currentCards)
        {
            cardsStr += card.GetCardName() + " ";
        }
        return $"{cardsStr.Trim()} → {handEvaluation.description}";
    }
    
    public int GetHandValue()
    {
        return handEvaluation.value;
    }
    
    public string GetHandRankName()
    {
        return handEvaluation.description;
    }
    
    public List<CardData> GetCurrentHandData()
    {
        return currentHandData;
    }
    
    public void FoldCards()
    {
        isFolded = true;  // Устанавливаем флаг фолда
        StartCoroutine(DiscardAllCardsSequence());
    }
    
    // Сброс всех карт без установки флага фолда (используется при очистке)
    public void ResetCards()
    {
        StartCoroutine(ResetCardsSequence());
    }
    
    private IEnumerator ResetCardsSequence()
    {
        for (int i = currentCards.Count - 1; i >= 0; i--)
        {
            if (currentCards[i] != null)
            {
                DiscardCard(currentCards[i]);
            }
            yield return new WaitForSeconds(dealDelay);
        }
        
        yield return new WaitForSeconds(discardDuration + 0.1f);
        
        foreach (var card in currentCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        
        currentCards.Clear();
        currentHandData.Clear();
    }
}