using System.Collections;
using UnityEngine;
using Pixelplacement;

public class FinalSceneController : MonoBehaviour
{
    [Header("NPC References")]
    [SerializeField] private NPCController[] finalNPCs;
    
    [Header("Animation Settings")]
    [SerializeField] private float defeatAnimationDelay = 0.5f;
    [SerializeField] private float defeatAnimationDuration = 1.5f;  // Длительность анимации поражения
    [SerializeField] private float victoryAnimationDuration = 2f;   // Длительность анимации победы
    
    [Header("Audio")]
    [SerializeField] private AudioClip gunshotSound;
    [SerializeField] private float gunshotVolume = 1f;
    
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraShakeDuration = 0.3f;
    [SerializeField] private float cameraShakeMagnitude = 0.5f;
    
    [Header("Fade")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector3 originalCameraPos;
    private GameObject fadePanel;
    private CanvasGroup fadeCanvasGroup;
    private NPCController winnerNPC;
    private NPCController[] cachedNPCs;
    
    public System.Action OnFinalSceneComplete;
    
    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        originalCameraPos = mainCamera.transform.position;
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    public void SetFinalNPCs(NPCController[] npcs)
    {
        cachedNPCs = npcs;
        finalNPCs = npcs;
        Debug.Log($"Получено {npcs.Length} NPC для финальной сцены");
    }
    
    public void SetWinnerNPC(NPCController winner)
    {
        winnerNPC = winner;
    }
    
    private Animator GetHandsAnimator(NPCController npc)
    {
        if (npc == null) return null;
        
        // Пытаемся получить handsAnimator через рефлексию
        var handsAnimatorField = typeof(NPCController).GetField("handsAnimator", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (handsAnimatorField != null)
        {
            Animator animator = handsAnimatorField.GetValue(npc) as Animator;
            if (animator != null)
                return animator;
        }
        
        // Если не нашли, ищем в детях
        Animator foundAnimator = npc.GetComponentInChildren<Animator>();
        if (foundAnimator != null)
            return foundAnimator;
        
        return npc.GetComponent<Animator>();
    }
    
    public IEnumerator PlayFinalScene()
    {
        Debug.Log("=== НАЧАЛО ФИНАЛЬНОЙ СЦЕНЫ ===");
        
        if (cachedNPCs != null && cachedNPCs.Length > 0)
        {
            finalNPCs = cachedNPCs;
        }
        
        if (finalNPCs == null || finalNPCs.Length == 0)
        {
            finalNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        }
        
        if (finalNPCs == null || finalNPCs.Length == 0)
        {
            Debug.LogWarning("Нет NPC для финальной сцены!");
            yield break;
        }
        
        Debug.Log($"Найдено NPC: {finalNPCs.Length}");
        
        // Находим победителя
        winnerNPC = null;
        foreach (var npc in finalNPCs)
        {
            if (npc != null && npc.gameObject.activeSelf)
            {
                NPCChips chips = npc.GetComponent<NPCChips>();
                if (chips != null && chips.GetChips() > 0)
                {
                    winnerNPC = npc;
                    Debug.Log($"Победитель: {winnerNPC.npcName}");
                    break;
                }
            }
        }
        
        if (winnerNPC == null && finalNPCs.Length > 0)
        {
            winnerNPC = finalNPCs[0];
            Debug.Log($"Победитель (по умолчанию): {winnerNPC.npcName}");
        }
        
        // Запускаем анимации
        if (winnerNPC != null)
        {
            // Анимация победы для победителя
            Animator winnerAnimator = GetHandsAnimator(winnerNPC);
            if (winnerAnimator != null)
            {
                winnerAnimator.SetTrigger("Victory");
                Debug.Log($"{winnerNPC.npcName} - Victory анимация");
            }
            
            // Анимация поражения для остальных
            foreach (var npc in finalNPCs)
            {
                if (npc != null && npc != winnerNPC && npc.gameObject.activeSelf)
                {
                    Animator animator = GetHandsAnimator(npc);
                    if (animator != null)
                    {
                        animator.SetTrigger("Defeat");
                        Debug.Log($"{npc.npcName} - Defeat анимация");
                        yield return new WaitForSeconds(defeatAnimationDelay);
                    }
                }
            }
            
            // Ждем пока анимация победы проиграется
            Debug.Log($"Ожидание анимации победы ({victoryAnimationDuration} сек)");
            yield return new WaitForSeconds(victoryAnimationDuration);
        }
        else
        {
            // Все проигрывают
            foreach (var npc in finalNPCs)
            {
                if (npc != null && npc.gameObject.activeSelf)
                {
                    Animator animator = GetHandsAnimator(npc);
                    if (animator != null)
                    {
                        animator.SetTrigger("Defeat");
                        Debug.Log($"{npc.npcName} - Defeat анимация");
                        yield return new WaitForSeconds(defeatAnimationDelay);
                    }
                }
            }
            
            // Ждем пока анимация поражения проиграется
            Debug.Log($"Ожидание анимации поражения ({defeatAnimationDuration} сек)");
            yield return new WaitForSeconds(defeatAnimationDuration);
        }
        
        Debug.Log("Выстрел!");
        if (gunshotSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gunshotSound, gunshotVolume);
        }
        
        yield return StartCoroutine(CameraShake());
        yield return StartCoroutine(FadeToBlack());
        
        Debug.Log("Финальная сцена завершена");
        OnFinalSceneComplete?.Invoke();
    }
    
    private IEnumerator CameraShake()
    {
        float elapsed = 0f;
        
        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * cameraShakeMagnitude;
            float y = Random.Range(-1f, 1f) * cameraShakeMagnitude;
            
            if (mainCamera != null)
                mainCamera.transform.position = originalCameraPos + new Vector3(x, y, 0);
            
            yield return null;
        }
        
        if (mainCamera != null)
            mainCamera.transform.position = originalCameraPos;
    }
    
    private IEnumerator FadeToBlack()
    {
        CreateFadePanel();
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, fadeCurve.Evaluate(t));
            yield return null;
        }
        
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 1;
    }
    
    private void CreateFadePanel()
    {
        if (fadePanel != null) return;
        if (mainCamera == null) return;
        
        fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(mainCamera.transform);
        fadePanel.transform.localPosition = new Vector3(0, 0, 10);
        fadePanel.transform.localScale = new Vector3(100, 100, 1);
        
        Canvas canvas = fadePanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
        
        fadeCanvasGroup = fadePanel.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0;
        
        var image = fadePanel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
    }
    
    public void Cleanup()
    {
        if (fadePanel != null)
            Destroy(fadePanel);
        
        if (mainCamera != null)
            mainCamera.transform.position = originalCameraPos;
        
        fadeCanvasGroup = null;
    }
}