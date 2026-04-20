using System.Collections;
using UnityEngine;
using Pixelplacement;

public class LevelTransitionEffects : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem transitionParticles;
    [SerializeField] private ParticleSystem levelCompleteParticles;
    [SerializeField] private float particleStartDelay = 0.2f;
    
    [Header("Visual Elements")]
    [SerializeField] private GameObject[] additionalVisualElements;
    
    [Header("Sound Settings")]
    [SerializeField] private bool playSounds = true;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip transitionStartSound;
    [SerializeField] private AudioClip transitionEndSound;
    [SerializeField] private float transitionSoundVolume = 0.7f;
    [SerializeField] private float levelCompleteSoundVolume = 0.8f;
    
    private Camera mainCamera;
    private GameObject fadePanel;
    private CanvasGroup fadeCanvasGroup;
    private bool isTransitioning = false;
    private float transitionTimeout = 5.0f; // Таймаут для защиты от зависания
    
    void Awake()
    {
        mainCamera = Camera.main;
        StopAllParticles();
    }
    
    void Start()
    {
        StopAllParticles();
    }
    
    private void StopAllParticles()
    {
        if (transitionParticles != null)
        {
            transitionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticles.Clear();
        }
        
        if (levelCompleteParticles != null)
        {
            levelCompleteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            levelCompleteParticles.Clear();
        }
    }
    
    public IEnumerator PlayLevelComplete()
    {
        if (isTransitioning) 
        {
            Debug.LogWarning("[Transition] Эффект завершения уровня уже воспроизводится");
            yield break;
        }
        
        isTransitioning = true;
        float startTime = Time.time;
        
        Debug.Log("[Transition] Воспроизведение эффекта завершения уровня");
        
        PlayLevelCompleteSound();
        
        if (levelCompleteParticles != null)
        {
            levelCompleteParticles.Clear();
            levelCompleteParticles.Play();
        }
        
        yield return new WaitForSeconds(0.5f);
        
        if (levelCompleteParticles != null)
        {
            levelCompleteParticles.Stop();
            levelCompleteParticles.Clear();
        }
        
        isTransitioning = false;
        Debug.Log($"[Transition] Эффект завершения уровня закончен за {Time.time - startTime} сек");
    }
    
    public IEnumerator StartTransition()
    {
        // Защита от зависания
        if (isTransitioning)
        {
            Debug.LogWarning("[Transition] Переход уже выполняется, принудительный сброс");
            ForceCleanup();
        }
        
        isTransitioning = true;
        float startTime = Time.time;
        
        Debug.Log("[Transition] Начало перехода (затемнение)");
        
        PlayTransitionStartSound();
        
        // Очищаем партиклы перед запуском
        if (transitionParticles != null)
        {
            transitionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticles.Clear();
            yield return new WaitForSeconds(0.05f);
            transitionParticles.Play();
        }
        
        if (particleStartDelay > 0)
        {
            yield return new WaitForSeconds(particleStartDelay);
        }
        
        yield return StartCoroutine(FadeToBlack());
        DisableVisualElements();
        
        Debug.Log($"[Transition] Затемнение завершено за {Time.time - startTime} сек");
    }
    
    public IEnumerator EndTransition()
    {
        float startTime = Time.time;
        Debug.Log("[Transition] Начало завершения перехода (осветление)");
        
        EnableVisualElements();
        yield return StartCoroutine(FadeFromBlack());
        
        PlayTransitionEndSound();
        
        if (transitionParticles != null)
        {
            transitionParticles.Stop();
            transitionParticles.Clear();
        }
        
        isTransitioning = false;
        
        Debug.Log($"[Transition] Осветление завершено за {Time.time - startTime} сек");
    }
    
    private IEnumerator FadeToBlack()
    {
        CreateFadePanel();
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, fadeCurve.Evaluate(t));
            yield return null;
        }
        
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 1;
    }
    
    private IEnumerator FadeFromBlack()
    {
        if (fadePanel == null) 
        {
            Debug.LogWarning("[Transition] Нет панели для осветления");
            yield break;
        }
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, fadeCurve.Evaluate(t));
            yield return null;
        }
        
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0;
        
        if (fadePanel != null)
            Destroy(fadePanel);
        
        fadePanel = null;
        fadeCanvasGroup = null;
    }
    
    private void DisableVisualElements()
    {
        // Отключаем NPC
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc != null)
            {
                var renderers = npc.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = false;
                }
            }
        }
        
        // Отключаем игрока
        PlayerCardsController player = FindFirstObjectByType<PlayerCardsController>();
        if (player != null)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
        
        // Отключаем банк
        BankChipsVisualController bank = FindFirstObjectByType<BankChipsVisualController>();
        if (bank != null)
        {
            var renderers = bank.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
        
        // Отключаем кнопки
        ActionButtonsController buttons = FindFirstObjectByType<ActionButtonsController>();
        if (buttons != null)
        {
            buttons.ShowButtons(false);
        }
        
        // Дополнительные элементы
        foreach (var element in additionalVisualElements)
        {
            if (element != null)
            {
                var renderers = element.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = false;
                }
            }
        }
    }
    
    private void EnableVisualElements()
    {
        // Включаем NPC
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc != null)
            {
                var renderers = npc.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
            }
        }
        
        // Включаем игрока
        PlayerCardsController player = FindFirstObjectByType<PlayerCardsController>();
        if (player != null)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
        
        // Включаем банк
        BankChipsVisualController bank = FindFirstObjectByType<BankChipsVisualController>();
        if (bank != null)
        {
            var renderers = bank.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
        
        // Включаем дополнительные элементы
        foreach (var element in additionalVisualElements)
        {
            if (element != null)
            {
                var renderers = element.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
            }
        }
    }
    
    private void CreateFadePanel()
    {
        if (fadePanel != null) return;
        if (mainCamera == null) mainCamera = Camera.main;
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
    
    public void ForceCleanup()
    {
        Debug.LogWarning("[Transition] Принудительная очистка");
        
        StopAllCoroutines();
        
        if (fadePanel != null)
            Destroy(fadePanel);
        
        fadePanel = null;
        fadeCanvasGroup = null;
        
        StopAllParticles();
        EnableVisualElements();
        isTransitioning = false;
    }
    
    private void PlayLevelCompleteSound()
    {
        if (!playSounds) return;
        
        if (AudioManager.Instance != null && levelCompleteSound != null)
        {
            AudioManager.Instance.PlaySFX(levelCompleteSound, levelCompleteSoundVolume);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWinSound();
        }
    }
    
    private void PlayTransitionStartSound()
    {
        if (!playSounds) return;
        
        if (AudioManager.Instance != null && transitionStartSound != null)
        {
            AudioManager.Instance.PlaySFX(transitionStartSound, transitionSoundVolume);
        }
    }
    
    private void PlayTransitionEndSound()
    {
        if (!playSounds) return;
        
        if (AudioManager.Instance != null && transitionEndSound != null)
        {
            AudioManager.Instance.PlaySFX(transitionEndSound, transitionSoundVolume);
        }
    }
    
    void OnDestroy()
    {
        ForceCleanup();
    }
}