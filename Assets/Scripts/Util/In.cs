using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Thin, null-safe wrapper over the Input System devices.</summary>
public static class In
{
    public static bool Held(Key k)
    {
        var kb = Keyboard.current;
        return kb != null && kb[k].isPressed;
    }

    public static bool Down(Key k)
    {
        var kb = Keyboard.current;
        return kb != null && kb[k].wasPressedThisFrame;
    }

    public static bool AnyKeyDown()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
        var m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame;
    }

    public static Vector2 MouseDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
    public static Vector2 MousePos => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    public static float Scroll
    {
        get
        {
            if (Mouse.current == null) return 0f;
            float y = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(y) < 0.01f ? 0f : Mathf.Sign(y);
        }
    }

    public static bool LMBDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    public static bool LMB => Mouse.current != null && Mouse.current.leftButton.isPressed;
    public static bool RMB => Mouse.current != null && Mouse.current.rightButton.isPressed;
    public static bool RMBDown => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
}
