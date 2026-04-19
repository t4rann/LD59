// NPCController.cs
using System.Collections;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("NPC Identity")]
    public string npcName = "NPC";
    public NPCBehaviour brain;
    
    [Header("Animators")]
    public Animator faceAnimator;
    public Animator handsAnimator;
    
    [Header("Current State")]
    [SerializeField] private int currentHandValue;
    [SerializeField] private EmotionType currentEmotion;
        
    [Header("Floating Text")]
    [SerializeField] private FloatingTextNPC floatingText;

    public int CurrentHandValue => currentHandValue;
    public EmotionType CurrentEmotion => currentEmotion;
    public bool HasCardsActive { get; private set; } = false;
    
    // События
    public System.Action<NPCController> OnTakeCardsFinished;
    public System.Action<NPCController> OnTakeCardsStarted;
    public System.Action<NPCController> OnCardsDiscarded;
    public System.Action<NPCController> OnEmotionShown;
    
    // Хеши
    private static readonly int EmotionHash = Animator.StringToHash("Emotion");
    private static readonly int TakeCardsTrigger = Animator.StringToHash("TakeCards");
    private static readonly int CallTrigger = Animator.StringToHash("Call");
    private static readonly int RaiseTrigger = Animator.StringToHash("Raise");
    private static readonly int FoldTrigger = Animator.StringToHash("Fold");
    
    private Coroutine emotionCoroutine;
    private Coroutine takeCardsCoroutine;
    
    void Start()
    {
        if (string.IsNullOrEmpty(npcName))
            npcName = gameObject.name;
            
        if (faceAnimator == null)
            faceAnimator = GetComponent<Animator>();
            
        if (faceAnimator != null)
            faceAnimator.SetFloat(EmotionHash, 0f);
            
        currentEmotion = EmotionType.Neutral;
        HasCardsActive = false;
    }
    
    // Добавьте метод для показа комбинации
    public void ShowCombination(string handDescription, int handValue)
    {
        if (floatingText != null)
            floatingText.ShowCombination(handDescription, handValue);
    }
        public void ReceiveNewHand(int handValue)
    {
        currentHandValue = handValue;
        HasCardsActive = false;
        
        OnTakeCardsStarted?.Invoke(this);
        
        if (handsAnimator != null)
        {
            handsAnimator.SetTrigger(TakeCardsTrigger);
        }
        
        if (takeCardsCoroutine != null)
            StopCoroutine(takeCardsCoroutine);
            
        takeCardsCoroutine = StartCoroutine(TakeCardsSequence());
    }

public void ShowAction(PlayerAction action)
{
    string actionText = action switch
    {
        PlayerAction.Fold => "FOLD",
        PlayerAction.Call => "CALL",
        PlayerAction.Raise => "RAISE",
        _ => ""
    };
    
    if (!string.IsNullOrEmpty(actionText) && floatingText != null)
        floatingText.ShowAction(actionText);
}

    private IEnumerator TakeCardsSequence()
    {
        yield return null;
        
        float animationLength = 0.8f;
        if (handsAnimator != null)
        {
            AnimatorStateInfo stateInfo = handsAnimator.GetCurrentAnimatorStateInfo(0);
            animationLength = stateInfo.length;
        }
        
        yield return new WaitForSeconds(animationLength);
        
        HasCardsActive = true;
        OnTakeCardsFinished?.Invoke(this);
        takeCardsCoroutine = null;
    }
    
    public void ShowEmotion()
    {
        if (brain == null)
        {
            Debug.LogError($"[{npcName}] Brain не назначен!");
            return;
        }
        
        currentEmotion = brain.GetDisplayedEmotion(currentHandValue);
        
        float targetValue = currentEmotion switch
        {
            EmotionType.Happy => 1f,
            EmotionType.Angry => -1f,
            _ => 0f
        };
        
        StartSmoothChange(targetValue);
        OnEmotionShown?.Invoke(this);
    }
    
    public EmotionType GetCurrentEmotion()
    {
        return currentEmotion;
    }
    
    public void ResetToNeutral()
    {
        currentEmotion = EmotionType.Neutral;
        StartSmoothChange(0f);
    }
    
    private void StartSmoothChange(float targetValue)
    {
        if (emotionCoroutine != null)
            StopCoroutine(emotionCoroutine);
            
        emotionCoroutine = StartCoroutine(SmoothChange(targetValue));
    }
    
    private IEnumerator SmoothChange(float targetValue)
    {
        if (faceAnimator == null) yield break;
        
        float startValue = faceAnimator.GetFloat(EmotionHash);
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentValue = Mathf.Lerp(startValue, targetValue, t);
            
            faceAnimator.SetFloat(EmotionHash, currentValue);
            yield return null;
        }
        
        faceAnimator.SetFloat(EmotionHash, targetValue);
        emotionCoroutine = null;
    }
    
    public void PlayCall()
    {
        if (handsAnimator != null)
            handsAnimator.SetTrigger(CallTrigger);
    }
    
    public void PlayRaise()
    {
        if (handsAnimator != null)
            handsAnimator.SetTrigger(RaiseTrigger);
    }
    
    public void DiscardCards()
    {
        if (!HasCardsActive) return;
        
        HasCardsActive = false;
        
        if (handsAnimator != null)
        {
            handsAnimator.SetTrigger(FoldTrigger);
        }
        
        // НЕ сбрасываем эмоции здесь
        // ResetToNeutral();
        
        OnCardsDiscarded?.Invoke(this);
    }
    
    public void FullReset()
    {
        HasCardsActive = false;
        ResetToNeutral();
        OnCardsDiscarded?.Invoke(this);
    }
    
    public PlayerAction MakeDecision()
    {
        if (brain == null) return PlayerAction.Fold;
        
        float seed = Random.value;
        return brain.GetAction(currentHandValue, seed);
    }
    
    public string GetStrengthText()
    {
        if (currentHandValue >= 300) return "СИЛЬНАЯ";
        if (currentHandValue >= 100) return "СРЕДНЯЯ";
        return "СЛАБАЯ";
    }
    
public void ShowWinnerMessage(string handDescription, int potAmount)
{
    if (floatingText != null)
        floatingText.ShowWinnerMessage(handDescription, potAmount);
}

    void OnDestroy()
    {
        if (takeCardsCoroutine != null)
            StopCoroutine(takeCardsCoroutine);
        if (emotionCoroutine != null)
            StopCoroutine(emotionCoroutine);
    }
}