using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A simple custom slider implementation that adds a single boolean field; IsPressed.
/// </summary>
public class CustomSlider: Slider {
    /// <summary>
    /// Whether or not the Slider is currently being pressed on by the Left Click mouse button.
    /// </summary>
    public new bool IsPressed { get; private set; }
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        IsPressed = true;
    }

    public override void OnPointerUp(PointerEventData eventData) {
        base.OnPointerUp(eventData);
        IsPressed = false;
    }
}