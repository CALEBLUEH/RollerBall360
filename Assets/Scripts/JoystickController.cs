using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public float Horizontal { get; private set; }

    private RectTransform background;
    private RectTransform handle;
    private float movementRadius;

    public void Configure(RectTransform backgroundRect, RectTransform handleRect)
    {
        background = backgroundRect;
        handle = handleRect;
        RecalculateRadius();
        ResetInput();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition))
            return;

        if (movementRadius <= 0f)
            RecalculateRadius();

        Vector2 normalized = Vector2.ClampMagnitude(pointerPosition / movementRadius, 1f);
        handle.anchoredPosition = normalized * movementRadius;

        // Only horizontal displacement steers the player; vertical drag is visual only.
        Horizontal = normalized.x;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetInput();
    }

    public void ResetInput()
    {
        Horizontal = 0f;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    private void RecalculateRadius()
    {
        if (background == null || handle == null)
            return;

        float backgroundRadius = Mathf.Min(background.rect.width, background.rect.height) * 0.5f;
        float handleRadius = Mathf.Min(handle.rect.width, handle.rect.height) * 0.5f;
        movementRadius = Mathf.Max(1f, backgroundRadius - handleRadius);
    }

    private void OnDisable()
    {
        ResetInput();
    }
}
