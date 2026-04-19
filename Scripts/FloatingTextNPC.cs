using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingTextNPC : MonoBehaviour
{
    [Header("TextMeshPro Reference")]
    [SerializeField] private TextMeshPro floatingText;
    
    [Header("Settings")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float fadeStartTime = 0.8f;
    
    [Header("Colors")]
    [SerializeField] private Color goodHandColor = Color.green;
    [SerializeField] private Color mediumHandColor = Color.yellow;
    [SerializeField] private Color badHandColor = Color.red;
    [SerializeField] private Color winnerColor = Color.yellow;
    
    private Vector3 startPosition;
    private Color startColor;
    private Coroutine currentCoroutine;
    
    void Awake()
    {
        if (floatingText == null)
            floatingText = GetComponentInChildren<TextMeshPro>();
        
        if (floatingText != null)
        {
            floatingText.gameObject.SetActive(false);
            startColor = floatingText.color;
        }
    }
    
    public void ShowCombination(string handDescription, int handValue)
    {
        if (floatingText == null) return;
        
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        
        string shortDesc = ShortenDescription(handDescription);
        floatingText.text = shortDesc;
        floatingText.color = GetColorForHandValue(handValue);
        startColor = floatingText.color;
        
        startPosition = transform.position + new Vector3(0, 1.5f, 0);
        floatingText.transform.position = startPosition;
        
        floatingText.gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(AnimateText());
    }
    
    public void ShowWinnerMessage(string handDescription, int potAmount)
    {
        if (floatingText == null) return;
        
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        
        // Только комбинация и выигрыш, без имени
        string message = $"{ShortenDescription(handDescription)}\n+{potAmount}";
        floatingText.text = message;
        floatingText.color = winnerColor;
        startColor = winnerColor;
        
        startPosition = transform.position + new Vector3(0, 2f, 0);
        floatingText.transform.position = startPosition;
        
        floatingText.gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(AnimateWinnerText());
    }
    
    private IEnumerator AnimateText()
    {
        float elapsed = 0f;
        
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            
            Vector3 newPos = startPosition + Vector3.up * (t * floatSpeed);
            floatingText.transform.position = newPos;
            
            if (t >= fadeStartTime)
            {
                float fadeT = (t - fadeStartTime) / (1f - fadeStartTime);
                Color c = floatingText.color;
                c.a = Mathf.Lerp(startColor.a, 0f, fadeT);
                floatingText.color = c;
            }
            
            yield return null;
        }
        
        floatingText.gameObject.SetActive(false);
        floatingText.color = startColor;
        currentCoroutine = null;
    }
    
    private IEnumerator AnimateWinnerText()
    {
        float elapsed = 0f;
        float winnerLifetime = 2f;
        
        while (elapsed < winnerLifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / winnerLifetime;
            
            Vector3 newPos = startPosition + Vector3.up * (t * floatSpeed);
            floatingText.transform.position = newPos;
            
            if (t >= 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                Color c = floatingText.color;
                c.a = Mathf.Lerp(startColor.a, 0f, fadeT);
                floatingText.color = c;
            }
            
            yield return null;
        }
        
        floatingText.gameObject.SetActive(false);
        floatingText.color = startColor;
        currentCoroutine = null;
    }
    
    private Color GetColorForHandValue(int handValue)
    {
        if (handValue >= 300) return goodHandColor;
        if (handValue >= 100) return mediumHandColor;
        return badHandColor;
    }
    
    private string ShortenDescription(string fullDesc)
    {
        if (string.IsNullOrEmpty(fullDesc)) return "";
        
        if (fullDesc.Contains("Флеш-рояль")) return "ФЛЕШ-РОЯЛЬ!";
        if (fullDesc.Contains("Стрит-флеш")) return "СТРИТ-ФЛЕШ!";
        if (fullDesc.Contains("Каре")) return "КАРЕ!";
        if (fullDesc.Contains("Фулл хаус")) return "ФУЛЛ ХАУС!";
        if (fullDesc.Contains("Флеш")) return "ФЛЕШ!";
        if (fullDesc.Contains("Стрит")) return "СТРИТ!";
        if (fullDesc.Contains("Сет") || fullDesc.Contains("Трипс")) return "СЕТ!";
        if (fullDesc.Contains("Две пары")) return "ДВЕ ПАРЫ";
        if (fullDesc.Contains("Пара")) return "ПАРА";
        if (fullDesc.Contains("Старшая")) return fullDesc;
        
        return fullDesc.Length > 15 ? fullDesc.Substring(0, 12) + "..." : fullDesc;
    }
}