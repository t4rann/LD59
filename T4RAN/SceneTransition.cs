using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    [SerializeField]
    private SpriteMask mask;

    [Header("Scene Transition Settings")]
    public string nextSceneName;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Transition(true);
    }

    public void BlackOut()
    {
        if (mask != null)
            mask.alphaCutoff = 1f;
    }

    public void Transition(bool transitionIn)
    {
        if (mask == null)
            return;

        StopAllCoroutines();

        if (transitionIn)
            StartCoroutine(FadeIn());
        else
            StartCoroutine(FadeOut());
    }

    public void TransitionToScene(string sceneName = "")
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = nextSceneName;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is not specified!");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void WinTransition()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("Next scene name is not specified in inspector!");
            return;
        }

        Debug.Log($"Starting win transition to scene: {nextSceneName}");
        TransitionToScene(nextSceneName);
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene(sceneName);

        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        Debug.Log("Fade Out started");

        while (mask.alphaCutoff < 1f)
        {
            mask.alphaCutoff += Time.deltaTime * 2f;
            yield return null;
        }
        mask.alphaCutoff = 1f;

        Debug.Log("Fade Out completed");
    }

    private IEnumerator FadeIn()
    {
        Debug.Log("Fade In started");

        mask.alphaCutoff = 1f;
        while (mask.alphaCutoff > 0f)
        {
            mask.alphaCutoff -= Time.deltaTime * 2f;
            yield return null;
        }
        mask.alphaCutoff = 0f;

        Debug.Log("Fade In completed");
    }
}
