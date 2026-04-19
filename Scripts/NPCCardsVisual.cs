using UnityEngine;
using Pixelplacement;

public class NPCCardsVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cardsRoot;

    [Header("Animation Settings")]
    [SerializeField] private float showDuration = 0.2f;
    [SerializeField] private float hideDuration = 0.2f;

    private Vector3 defaultScale;
    private bool isVisible = false;

    void Awake()
    {
        if (cardsRoot == null)
            cardsRoot = gameObject;

        defaultScale = cardsRoot.transform.localScale;

    }

    public void ShowCards()
    {
        if (isVisible) return;

        isVisible = true;

        cardsRoot.SetActive(true);
        cardsRoot.transform.localScale = Vector3.zero;

        // просто плавное увеличение
        Tween.LocalScale(
            cardsRoot.transform,
            defaultScale,
            showDuration,
            0f,
            Tween.EaseOut
        );
    }

    public void HideCards()
    {
        if (!isVisible) return;

        isVisible = false;

        // просто плавное уменьшение
        Tween.LocalScale(
            cardsRoot.transform,
            Vector3.zero,
            hideDuration,
            0f,
            Tween.EaseIn,
            completeCallback: () =>
            {
                cardsRoot.SetActive(false);
                cardsRoot.transform.localScale = defaultScale;
            });
    }

    public void ShowInstant()
    {
        isVisible = true;
        cardsRoot.SetActive(true);
        cardsRoot.transform.localScale = defaultScale;
    }

    public void HideInstant()
    {
        isVisible = false;
        cardsRoot.SetActive(false);
    }
}