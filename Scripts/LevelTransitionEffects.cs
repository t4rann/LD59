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
    
    void Awake()
    {
        mainCamera = Camera.main;
        
        // Полностью останавливаем партиклы при загрузке
        StopAllParticles();
    }
    
    void Start()
    {
        // Еще раз останавливаем на всякий случай
        StopAllParticles();
    }
    
    private void StopAllParticles()
    {
        // Останавливаем партикл перехода
        if (transitionParticles != null)
        {
            transitionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticles.Clear();
            // Убеждаемся что партикл не проигрывается
            transitionParticles.gameObject.SetActive(true);
            transitionParticles.Play();
            transitionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticles.Clear();
        }
        
        // Останавливаем партикл завершения уровня
        if (levelCompleteParticles != null)
        {
            levelCompleteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            levelCompleteParticles.Clear();
            levelCompleteParticles.gameObject.SetActive(true);
            levelCompleteParticles.Play();
            levelCompleteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            levelCompleteParticles.Clear();
        }
        
        Debug.Log("[Transition] Все партиклы остановлены");
    }
    
    // Эффект завершения уровня
    public IEnumerator PlayLevelComplete()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        Debug.Log("[Transition] Воспроизведение эффекта завершения уровня");
        
        // Воспроизводим звук завершения уровня
        PlayLevelCompleteSound();
        
        // Запускаем партикл завершения уровня
        if (levelCompleteParticles != null)
        {
            levelCompleteParticles.Clear();
            levelCompleteParticles.Play();
            Debug.Log("[Transition] LevelComplete партикл запущен");
        }
        
        // Небольшая задержка
        yield return new WaitForSeconds(0.5f);
        
        // Останавливаем партикл
        if (levelCompleteParticles != null)
        {
            levelCompleteParticles.Stop();
            levelCompleteParticles.Clear();
            Debug.Log("[Transition] LevelComplete партикл остановлен");
        }
        
        isTransitioning = false;
    }
    
    // Запуск перехода - затемнение
    public IEnumerator StartTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        // Воспроизводим звук начала перехода
        PlayTransitionStartSound();
        
        // Принудительно останавливаем и очищаем партикл перед запуском
        if (transitionParticles != null)
        {
            transitionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticles.Clear();
            
            // Небольшая задержка перед запуском
            yield return new WaitForSeconds(0.05f);
            
            transitionParticles.Play();
            Debug.Log("[Transition] Партикл запущен");
        }
        
        if (particleStartDelay > 0)
        {
            yield return new WaitForSeconds(particleStartDelay);
        }
        
        yield return StartCoroutine(FadeToBlack());
        DisableVisualElements();
    }
    
    // Завершение перехода - осветление
    public IEnumerator EndTransition()
    {
        yield return StartCoroutine(FadeFromBlack());
        
        // Воспроизводим звук окончания перехода
        PlayTransitionEndSound();
        
        if (transitionParticles != null)
        {
            transitionParticles.Stop();
            transitionParticles.Clear();
            Debug.Log("[Transition] Партикл остановлен");
        }
        
        isTransitioning = false;
    }
    
    private IEnumerator FadeToBlack()
    {
        CreateFadePanel();
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, fadeCurve.Evaluate(t));
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1;
        Debug.Log("[Transition] Затемнение завершено");
    }
    
    private IEnumerator FadeFromBlack()
    {
        if (fadePanel == null) yield break;
        
        EnableVisualElements();
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, fadeCurve.Evaluate(t));
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0;
        
        Destroy(fadePanel);
        fadePanel = null;
        fadeCanvasGroup = null;
        
        Debug.Log("[Transition] Осветление завершено");
    }
    
    private void DisableVisualElements()
    {
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc != null)
            {
                var renderers = npc.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.enabled)
                        renderer.enabled = false;
                }
            }
        }
        
        PlayerCardsController player = FindFirstObjectByType<PlayerCardsController>();
        if (player != null)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                    renderer.enabled = false;
            }
        }
        
        BankChipsVisualController bank = FindFirstObjectByType<BankChipsVisualController>();
        if (bank != null)
        {
            var renderers = bank.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                    renderer.enabled = false;
            }
        }
        
        ActionButtonsController buttons = FindFirstObjectByType<ActionButtonsController>();
        if (buttons != null)
        {
            buttons.ShowButtons(false);
        }
        
        foreach (var element in additionalVisualElements)
        {
            if (element != null)
            {
                var renderers = element.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.enabled)
                        renderer.enabled = false;
                }
            }
        }
        
        Debug.Log("[Transition] Визуальные элементы выключены");
    }
    
    private void EnableVisualElements()
    {
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
        
        Debug.Log("[Transition] Визуальные элементы включены");
    }
    
    private void CreateFadePanel()
    {
        if (fadePanel != null) return;
        
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
    
    #region Sound Methods
    
    private void PlayLevelCompleteSound()
    {
        if (!playSounds) return;
        
        if (AudioManager.Instance != null && levelCompleteSound != null)
        {
            AudioManager.Instance.PlaySFX(levelCompleteSound, levelCompleteSoundVolume);
            Debug.Log("[Transition] Воспроизведен звук завершения уровня");
        }
        else if (AudioManager.Instance != null && levelCompleteSound == null)
        {
            // Если нет отдельного звука, используем стандартный win sound
            AudioManager.Instance.PlayWinSound();
            Debug.Log("[Transition] Воспроизведен стандартный звук победы");
        }
        else
        {
            Debug.LogWarning("[Transition] AudioManager.Instance is null, cannot play level complete sound");
        }
    }
    
    private void PlayTransitionStartSound()
    {
        if (!playSounds) return;
        
        if (AudioManager.Instance != null && transitionStartSound != null)
        {
            AudioManager.Instance.PlaySFX(transitionStartSound, transitionSoundVolume);
            Debug.Log("[Transition] Воспроизведен звук начала перехода");
        }
        else if (AudioManager.Instance != null)
        {
            // Если нет отдельного звука, используем sound эффект
            Debug.Log("[Transition] transitionStartSound не назначен");
        }
    }
    
    private void PlayTransitionEndSound()
    {
        if (!playSounds) return;
        
        if (AudioManager.Instance != null && transitionEndSound != null)
        {
            AudioManager.Instance.PlaySFX(transitionEndSound, transitionSoundVolume);
            Debug.Log("[Transition] Воспроизведен звук окончания перехода");
        }
        else if (AudioManager.Instance != null)
        {
            // Если нет отдельного звука
            Debug.Log("[Transition] transitionEndSound не назначен");
        }
    }
    
    #endregion
    
    public void Cleanup()
    {
        if (fadePanel != null)
            Destroy(fadePanel);
        
        StopAllParticles();
        EnableVisualElements();
        isTransitioning = false;
    }
}