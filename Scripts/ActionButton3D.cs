using UnityEngine;
using TMPro;

public class ActionButton3DSimple : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private PlayerAction action;
    [SerializeField] private string buttonText = "FOLD";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private TextMeshPro label;
    
    private Vector3 originalScale;
    private Color originalColor;
    private bool isInteractable = true;
    
    public System.Action<PlayerAction> OnClick;
    
    void Start()
    {
        originalScale = transform.localScale;
        
        if (background != null)
            originalColor = background.color;
        
        if (label != null)
            label.text = buttonText;
    }
    
    void OnMouseEnter()
    {
        if (!isInteractable) return;
        
        if (background != null)
            background.color = hoverColor;
        
        transform.localScale = originalScale * 1.1f;
    }
    
    void OnMouseExit()
    {
        if (!isInteractable) return;
        
        if (background != null)
            background.color = originalColor;
        
        transform.localScale = originalScale;
    }
    
    void OnMouseDown()
    {
        if (!isInteractable) return;
        transform.localScale = originalScale * 0.95f;
    }
    
    void OnMouseUp()
    {
        if (!isInteractable) return;
        transform.localScale = originalScale * 1.1f;
        OnClick?.Invoke(action);
    }
    
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = interactable;
        
        if (background != null)
            background.color = interactable ? originalColor : Color.gray;
        
        if (label != null)
            label.color = interactable ? Color.white : Color.gray;
    }
}