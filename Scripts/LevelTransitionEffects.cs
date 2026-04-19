using System.Collections;
using UnityEngine;

public class LevelTransitionEffects : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f; // В два раза дольше
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.1f;
    
    [Header("Confetti")]
    [SerializeField] private int confettiCount = 50;
    [SerializeField] private float confettiDuration = 2f;
    
    private Camera mainCamera;
    private Vector3 originalCameraPos;
    private GameObject fadePanel;
    private Color originalCameraColor;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPos = mainCamera.transform.position;
            originalCameraColor = mainCamera.backgroundColor;
        }
    }
    
    public IEnumerator PlayLevelComplete()
    {
        // Конфетти
        PlayConfetti();
        
        // Вспышка
        yield return StartCoroutine(CameraFlash());
        
        // Небольшая задержка
        yield return new WaitForSeconds(0.5f);
    }
    
    public IEnumerator PlayLevelTransition()
    {
        // Затемнение (дольше)
        yield return StartCoroutine(FadeToBlackAndDisable());
        
        // Вспышка (в момент максимального затемнения)
        yield return StartCoroutine(CameraFlash());
        
        // Задержка в темноте
        yield return new WaitForSeconds(0.5f);
        
        // Осветление
        yield return StartCoroutine(FadeFromBlack());
    }
    
    public IEnumerator PlayGameComplete()
    {
        // Множественные вспышки
        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(CameraFlash());
            yield return new WaitForSeconds(0.2f);
        }
        
        // Много конфетти
        PlayConfetti();
        PlayConfetti();
        PlayConfetti();
        
        yield return new WaitForSeconds(1f);
    }
    
    private IEnumerator FadeToBlackAndDisable()
    {
        CreateFadePanel();
        CanvasGroup canvasGroup = fadePanel.GetComponent<CanvasGroup>();
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(0, 1, fadeCurve.Evaluate(t));
            
            // В момент максимального затемнения (90% и выше)
            if (canvasGroup.alpha >= 0.95f && elapsed < fadeDuration)
            {
                // Выключаем визуальные элементы
                DisableVisualElements();
            }
            
            yield return null;
        }
        canvasGroup.alpha = 1;
        
        // Фиксируем выключение
        DisableVisualElements();
    }
    
    private IEnumerator FadeFromBlack()
    {
        if (fadePanel == null) yield break;
        
        CanvasGroup canvasGroup = fadePanel.GetComponent<CanvasGroup>();
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(1, 0, fadeCurve.Evaluate(t));
            
            // После начала осветления включаем элементы
            if (canvasGroup.alpha <= 0.1f && elapsed < fadeDuration)
            {
                EnableVisualElements();
            }
            
            yield return null;
        }
        canvasGroup.alpha = 0;
        
        // Включаем элементы полностью
        EnableVisualElements();
        
        Destroy(fadePanel);
        fadePanel = null;
    }
    
    private void DisableVisualElements()
    {
        // Выключаем все визуальные элементы на сцене
        
        // Отключаем рендеры у всех NPC
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            var renderers = npc.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                    renderer.enabled = false;
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
        
        Debug.Log("[Transition] Визуальные элементы выключены");
    }
    
    private void EnableVisualElements()
    {
        // Включаем все визуальные элементы на сцене
        
        // Включаем рендеры у всех NPC
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            var renderers = npc.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
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
        
        // Кнопки пока не включаем (включит GameLoop когда нужно)
        
        Debug.Log("[Transition] Визуальные элементы включены");
    }
    
    private IEnumerator CameraFlash()
    {
        if (mainCamera == null) yield break;
        
        Color originalColor = mainCamera.backgroundColor;
        mainCamera.backgroundColor = Color.white;
        
        yield return new WaitForSeconds(flashDuration);
        
        mainCamera.backgroundColor = originalColor;
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
        
        CanvasGroup canvasGroup = fadePanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        
        var image = fadePanel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
    }
    
    private void PlayConfetti()
    {
        if (mainCamera == null) return;
        
        GameObject confetti = new GameObject("Confetti");
        confetti.transform.SetParent(mainCamera.transform);
        confetti.transform.localPosition = new Vector3(0, -2, 8);
        
        ParticleSystem ps = confetti.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = confettiDuration;
        main.startLifetime = 1.5f;
        main.startSpeed = 5f;
        main.startSize = 0.15f;
        main.startColor = new Color(Random.value, Random.value, Random.value);
        
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, confettiCount) });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 60;
        shape.radius = 1;
        
        ps.Play();
        
        Destroy(confetti, confettiDuration);
    }
    
    public void Cleanup()
    {
        if (fadePanel != null)
            Destroy(fadePanel);
        
        // Включаем все элементы при очистке
        EnableVisualElements();
    }
}