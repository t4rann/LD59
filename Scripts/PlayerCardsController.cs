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
    private bool isFolded = false;
    private bool isDealing = false;
    private bool isDiscarding = false;
    
    public System.Action<string, int> OnHandReceived;
    public System.Action OnDealAnimationComplete;  // НОВОЕ: событие завершения анимации
    public System.Action OnDiscardAnimationComplete; // НОВОЕ: событие завершения сброса
    
    public bool IsDealing => isDealing;
    public bool IsDiscarding => isDiscarding;
    
    void Start()
    {
        if (handPosition == null) handPosition = transform;
        if (spawnPosition == null) spawnPosition = transform;
        if (despawnPosition == null) despawnPosition = transform;
        if (deckManager == null) deckManager = FindFirstObjectByType<DeckManager>();
        if (handEvaluator == null) handEvaluator = FindFirstObjectByType<HandEvaluator>();
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
        if (isDealing)
        {
            Debug.LogWarning("[PlayerCards] Раздача уже идет!");
            return;
        }
        
        StartCoroutine(DealHandSequence());
    }
    
    IEnumerator DealHandSequence()
    {
        isDealing = true;
        isFolded = false;
        
        Debug.Log("[PlayerCards] Начало раздачи карт игроку");
        
        // Если есть старые карты - сбрасываем
        if (currentCards.Count > 0)
        {
            yield return StartCoroutine(DiscardAllCardsSequence());
        }
        
        // Берем карты из колоды
        currentHandData = deckManager.DealCards(maxCards);
        
        // Оцениваем комбинацию
        handEvaluation = handEvaluator.EvaluateHand(currentHandData);
        
        // Раздаем карты с анимацией
        List<Coroutine> animations = new List<Coroutine>();
        for (int i = 0; i < currentHandData.Count; i++)
        {
            StartCoroutine(DealCardCoroutine(i, currentHandData[i]));
            yield return new WaitForSeconds(dealDelay);
        }
        
        // Ждем завершения последней анимации
        yield return new WaitForSeconds(dealDuration + 0.2f);
        
        string handDescription = GetHandDescription();
        OnHandReceived?.Invoke(handDescription, handEvaluation.value);
        
        GameDebug.LogPlayerHand(handDescription, handEvaluation.value);
        
        isDealing = false;
        
        // ВАЖНО: Вызываем событие завершения раздачи
        Debug.Log("[PlayerCards] Раздача завершена, вызываем OnDealAnimationComplete");
        OnDealAnimationComplete?.Invoke();
    }
    
    private IEnumerator DealCardCoroutine(int index, CardData cardData)
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
        float elapsed = 0f;
        Vector3 startPos = cardObj.transform.position;
        Quaternion startRot = cardObj.transform.rotation;
        Vector3 startScale = cardObj.transform.localScale;
        
        while (elapsed < dealDuration)
        {
            elapsed += Time.deltaTime;
            float t = dealCurve.Evaluate(elapsed / dealDuration);
            
            cardObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cardObj.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            cardObj.transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
            
            yield return null;
        }
        
        cardObj.transform.position = targetPos;
        cardObj.transform.rotation = targetRot;
        cardObj.transform.localScale = Vector3.one;
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
        if (isDealing)
        {
            Debug.LogWarning("[PlayerCards] Нельзя сбросить карты во время раздачи!");
            return;
        }
        
        if (isDiscarding)
        {
            Debug.LogWarning("[PlayerCards] Сброс уже идет!");
            return;
        }
        
        StartCoroutine(DiscardAllCardsSequence());
    }
    
    IEnumerator DiscardAllCardsSequence()
    {
        if (currentCards.Count == 0)
        {
            OnDiscardAnimationComplete?.Invoke();
            yield break;
        }
        
        isDiscarding = true;
        Debug.Log("[PlayerCards] Начало сброса карт");
        
        // Сбрасываем справа налево
        for (int i = currentCards.Count - 1; i >= 0; i--)
        {
            if (currentCards[i] != null)
            {
                StartCoroutine(DiscardCardCoroutine(currentCards[i]));
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
        if (deckManager != null)
        {
            deckManager.ResetDeck();
        }
        
        isDiscarding = false;
        
        Debug.Log("[PlayerCards] Сброс завершен, вызываем OnDiscardAnimationComplete");
        OnDiscardAnimationComplete?.Invoke();
    }
    
    private IEnumerator DiscardCardCoroutine(Card card)
    {
        float elapsed = 0f;
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        Vector3 startScale = card.transform.localScale;
        Vector3 targetPos = despawnPosition.position;
        Vector3 targetScale = Vector3.one * 0.5f;
        
        while (elapsed < discardDuration)
        {
            elapsed += Time.deltaTime;
            float t = discardCurve.Evaluate(elapsed / discardDuration);
            
            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            card.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            yield return null;
        }
        
        card.transform.position = targetPos;
        card.transform.localScale = targetScale;
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
        if (isDealing)
        {
            Debug.LogWarning("[PlayerCards] Нельзя сбросить карты во время раздачи!");
            return;
        }
        
        isFolded = true;
        StartCoroutine(DiscardAllCardsSequence());
    }
    
    // Принудительная очистка без анимации (для экстренных случаев)
    public void ForceClearCards()
    {
        StopAllCoroutines();
        
        foreach (var card in currentCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        
        currentCards.Clear();
        currentHandData.Clear();
        
        isDealing = false;
        isDiscarding = false;
        isFolded = false;
        
        Debug.Log("[PlayerCards] Принудительная очистка карт");
    }
}