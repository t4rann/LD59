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
    [SerializeField] private HandStrength trueHandStrength;
    [SerializeField] private EmotionType currentEmotion;
    
    public HandStrength TrueHandStrength => trueHandStrength;
    public EmotionType CurrentEmotion => currentEmotion;
    public bool HasCardsActive { get; private set; } = false;
    
    // События
    public System.Action<NPCController> OnTakeCardsFinished;
    public System.Action<NPCController> OnEmotionShown;
    
    // Хеши
    private static readonly int EmotionHash = Animator.StringToHash("Emotion");
    private static readonly int HasCardsHash = Animator.StringToHash("HasCards");
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
    
    public void ReceiveNewHand(HandStrength strength)
    {
        trueHandStrength = strength;
        
        if (handsAnimator != null)
        {
            handsAnimator.SetTrigger(TakeCardsTrigger);
        }
        
        if (takeCardsCoroutine != null)
            StopCoroutine(takeCardsCoroutine);
            
        takeCardsCoroutine = StartCoroutine(TakeCardsSequence());
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
        if (handsAnimator != null)
        {
            handsAnimator.SetBool(HasCardsHash, true);
        }
        
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
        
        currentEmotion = brain.GetDisplayedEmotion(trueHandStrength);
        
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
        HasCardsActive = false;
        
        if (handsAnimator != null)
        {
            handsAnimator.SetBool(HasCardsHash, false);
            handsAnimator.SetTrigger(FoldTrigger);
        }
        
        ResetToNeutral();
    }
    
    public PlayerAction MakeDecision()
    {
        if (brain == null) return PlayerAction.Fold;
        
        float seed = Random.value;
        return brain.GetAction(trueHandStrength, seed);
    }
    
    public string GetStrengthText()
    {
        return trueHandStrength switch
        {
            HandStrength.Weak => "СЛАБАЯ",
            HandStrength.Medium => "СРЕДНЯЯ",
            HandStrength.Strong => "СИЛЬНАЯ",
            _ => "???"
        };
    }
    
    void OnDestroy()
    {
        if (takeCardsCoroutine != null)
            StopCoroutine(takeCardsCoroutine);
        if (emotionCoroutine != null)
            StopCoroutine(emotionCoroutine);
    }
}