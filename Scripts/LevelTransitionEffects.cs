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
    [SerializeField] private float particleStartDelay = 0.2f;
    
    [Header("Visual Elements")]
    [SerializeField] private GameObject[] additionalVisualElements;
    
    private Camera mainCamera;
    private GameObject fadePanel;
    private CanvasGroup fadeCanvasGroup;
    
    void Awake()
    {
        mainCamera = Camera.main;
        
        // Останавливаем партикл при старте, но оставляем объект включенным
        if (transitionParticles != null)
        {
            transitionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticles.Clear();
            // Убеждаемся что партикл не проигрывается
            transitionParticles.gameObject.SetActive(true);
            Debug.Log("[Transition] Партикл подготовлен (остановлен)");
        }
    }
    
    void Start()
    {
        // Дополнительная остановка на всякий случай
        if (transitionParticles != null)
        {
            transitionParticles.Stop();
            transitionParticles.Clear();
        }
    }
    
    // Запуск перехода - затемнение
    public IEnumerator StartTransition()
    {
        // Запускаем партикл ЗАДОЛГО до затемнения
        if (transitionParticles != null)
        {
            // Очищаем старые частицы
            transitionParticles.Clear();
            
            // Запускаем партикл
            transitionParticles.Play();
            Debug.Log("[Transition] Партикл запущен");
        }
        
        // Ждем указанную задержку перед началом затемнения
        if (particleStartDelay > 0)
        {
            Debug.Log($"[Transition] Ожидание {particleStartDelay} сек перед затемнением");
            yield return new WaitForSeconds(particleStartDelay);
        }
        
        // Затемнение
        yield return StartCoroutine(FadeToBlack());
        
        // Выключаем визуальные элементы в темноте
        DisableVisualElements();
    }
    
    // Завершение перехода - осветление
    public IEnumerator EndTransition()
    {
        // Осветление
        yield return StartCoroutine(FadeFromBlack());
        
        // Останавливаем партикл после осветления
        if (transitionParticles != null)
        {
            transitionParticles.Stop();
            transitionParticles.Clear();
            Debug.Log("[Transition] Партикл остановлен");
        }
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
        
        // Включаем визуальные элементы нового уровня ПЕРЕД осветлением
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
        // Отключаем рендеры у всех NPC
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
        
        // Отключаем рендеры у игрока
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
        
        // Отключаем фишки в банке
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
        
        // Отключаем кнопки действий
        ActionButtonsController buttons = FindFirstObjectByType<ActionButtonsController>();
        if (buttons != null)
        {
            buttons.ShowButtons(false);
        }
        
        // Отключаем дополнительные визуальные элементы
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
        // Включаем рендеры у всех NPC
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
        
        // Включаем рендеры у игрока
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
        
        // Включаем фишки в банке
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
        
        // Включаем дополнительные визуальные элементы
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
    
    public void Cleanup()
    {
        if (fadePanel != null)
            Destroy(fadePanel);
        
        if (transitionParticles != null)
        {
            transitionParticles.Stop();
            transitionParticles.Clear();
        }
        
        EnableVisualElements();
    }
}