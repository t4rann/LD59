using System.Collections.Generic;
using UnityEngine;

public class GameCursor : MonoBehaviour
{
    public static GameCursor instance;

    public Interactable CurrentInteractable { get; private set; }
    private Interactable cursorDownInteractable;

    public ReferenceSetToggle DisableMovement = new ReferenceSetToggle();
    public ReferenceSetToggle DisableInput = new ReferenceSetToggle();

    [SerializeField] private SpriteRenderer cursorRenderer;
    [SerializeField] private float defaultCursorScale = 1f;
    [SerializeField] private float pressedCursorScale = 0.8f;
    [SerializeField] private float scaleSpeed = 10f;

    private List<string> excludedLayers = new();

    private float targetScale;

    private void Awake()
    {
        instance = this;
        targetScale = defaultCursorScale;
    }

    private void Update()
    {
        UpdateMainInput();
        UpdateDragInput();
        UpdateVisuals();
        AnimateCursorScale();
    }

private void UpdateMainInput()
{
    CurrentInteractable = UpdateCurrentInteractable(CurrentInteractable, excludedLayers.ToArray());

    if (!DisableInput.True)
    {
        if (Input.GetMouseButtonDown(0))
        {
            targetScale = pressedCursorScale;
            CurrentInteractable?.CursorSelectStart();
            cursorDownInteractable = CurrentInteractable;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            targetScale = defaultCursorScale;
            CurrentInteractable?.CursorSelectEnd();
            cursorDownInteractable = null;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            CurrentInteractable?.CursorAltSelectStart();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            CurrentInteractable?.CursorAltSelectEnd();
        }
        else if (Input.mouseScrollDelta.magnitude != 0f)
        {
            CurrentInteractable?.CursorScroll(Input.mouseScrollDelta.magnitude);
        }

        CurrentInteractable?.CursorStay();
    }
}


    private void UpdateDragInput()
    {
        if (cursorDownInteractable != null && !DisableInput.True)
        {
            if (CurrentInteractable != cursorDownInteractable)
            {
                cursorDownInteractable.CursorDragOff();
                cursorDownInteractable = null;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            cursorDownInteractable = null;
        }
    }

    private Interactable UpdateCurrentInteractable(Interactable current, string[] excludeLayers)
    {
        var hitInteractable = RaycastForInteractable(~LayerMask.GetMask(excludeLayers), transform.position);

        if (hitInteractable != current)
        {
            if (current != null && current.CollisionEnabled)
            {
                current.CursorExit();
            }

            if (hitInteractable != null && !DisableInput.True)
            {
                hitInteractable.CursorEnter();
            }
            else
            {
                return null;
            }
        }

        return hitInteractable;
    }

    private void UpdateVisuals()
    {
        var cursorPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(cursorPos.x, cursorPos.y, 0f);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

private void AnimateCursorScale()
{
    if (cursorRenderer != null)
    {
        float scale = Mathf.Lerp(cursorRenderer.transform.localScale.x, targetScale, Time.deltaTime * scaleSpeed);
        cursorRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}


    private Interactable RaycastForInteractable(int layerMask, Vector3 cursorPosition)
    {
        Interactable hitInteractable = null;

        var rayHits = Physics2D.RaycastAll(cursorPosition, Vector2.zero, 1000f, layerMask);
        var hitInteractables = GetInteractablesFromRayHits(rayHits);

        if (hitInteractables.Count > 0)
        {
            hitInteractables.Sort((Interactable a, Interactable b) =>
            {
                return a.CompareInteractionSortOrder(b);
            });
            hitInteractable = hitInteractables[0];
        }

        return hitInteractable;
    }

    private List<Interactable> GetInteractablesFromRayHits(RaycastHit2D[] rayHits)
    {
        var hitInteractables = new List<Interactable>();
        foreach (var hit in rayHits)
        {
            var interactable = hit.transform.GetComponent<Interactable>();
            if (interactable != null)
            {
                hitInteractables.Add(interactable);
            }
        }
        return hitInteractables;
    }
}

public class Interactable : MonoBehaviour
{
    public bool CollisionEnabled => true;
    public void CursorEnter() { }
    public void CursorExit() { }
    public void CursorStay() { }
    public void CursorSelectStart() { }
    public void CursorSelectEnd() { }
    public void CursorAltSelectStart() { }
    public void CursorAltSelectEnd() { }
    public void CursorDragOff() { }
    public void CursorScroll(float delta) { }
    public int CompareInteractionSortOrder(Interactable other) => 0;
}

public class ReferenceSetToggle
{
    public bool True => false;
}
