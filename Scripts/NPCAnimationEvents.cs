// NPCAnimationEvents.cs
using UnityEngine;

public class NPCAnimationEvents : MonoBehaviour
{
    [Header("Cards Visual")]
    [SerializeField] private GameObject cardsVisual;
    
    // Вызывается из Animation Event в анимации TakeCards
    public void ShowCardsVisual()
    {
        if (cardsVisual != null)
        {
            cardsVisual.SetActive(true);
        }
    }
    
    // Вызывается из Animation Event в анимации Fold
    public void HideCardsVisual()
    {
        if (cardsVisual != null)
        {
            cardsVisual.SetActive(false);
        }
    }
}