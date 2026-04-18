// Card.cs
using UnityEngine;

public class Card : MonoBehaviour
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
    
    public Suit suit;
    public Rank rank;
    public int value;
    public bool isFaceUp = true;
    
    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition;
    private bool isMoving = false;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.position;
    }
    
    public void SetCard(Suit newSuit, Rank newRank, Sprite faceSprite)
    {
        suit = newSuit;
        rank = newRank;
        value = (int)newRank;
        
        if (spriteRenderer != null && faceSprite != null)
        {
            spriteRenderer.sprite = faceSprite;
        }
    }
    
    public int GetValue()
    {
        return value;
    }
    
    public string GetCardName()
    {
        return $"{rank} of {suit}";
    }
    
    public void MoveTo(Vector3 targetPosition, float duration)
    {
        if (!isMoving)
        {
            StartCoroutine(MoveCoroutine(targetPosition, duration));
        }
    }
    
    private System.Collections.IEnumerator MoveCoroutine(Vector3 target, float duration)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        
        transform.position = target;
        isMoving = false;
    }
    
    public void SetSortingOrder(int order)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
}