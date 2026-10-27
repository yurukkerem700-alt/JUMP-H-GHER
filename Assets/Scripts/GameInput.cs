using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class GameInput
{
    static readonly HashSet<int> left = new HashSet<int>();
    static readonly HashSet<int> right = new HashSet<int>();
    static bool touchJump;
    static bool touchEnabled, buttonMode, trackingFinger, jumpGestureUsed;
    static int fingerId = -1;
    static Vector2 fingerStart;
    static float fingerStartedAt;
    static float gestureHorizontal;

    public static float Horizontal
    {
        get
        {
            float keyboard = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(keyboard) > .01f) return keyboard;
            if (touchEnabled && !buttonMode) return gestureHorizontal;
            return Mathf.Clamp(right.Count - left.Count, -1, 1);
        }
    }

    public static bool ConsumeJump()
    {
        bool result = touchJump || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);
        touchJump = false;
        return result;
    }

    public static void Hold(bool isLeft, int pointer, bool pressed)
    {
        var set = isLeft ? left : right;
        if (pressed) set.Add(pointer); else set.Remove(pointer);
    }
    public static void Jump() { touchJump = true; }

    public static void Configure(bool enabled, bool useButtons)
    {
        touchEnabled = enabled;
        buttonMode = useButtons;
        ResetGesture();
    }

    // Called after EventSystem has processed pointer events so UI touches never steer the player.
    public static void UpdateGesture()
    {
        if (!touchEnabled || buttonMode)
        {
            ResetGesture();
            return;
        }

        if (!trackingFinger)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began) continue;
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) continue;
                trackingFinger = true;
                fingerId = touch.fingerId;
                fingerStart = touch.position;
                fingerStartedAt = Time.unscaledTime;
                gestureHorizontal = 0;
                jumpGestureUsed = false;
                break;
            }
        }

        if (!trackingFinger) return;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != fingerId) continue;
            Vector2 delta = touch.position - fingerStart;
            gestureHorizontal = Mathf.Clamp(delta.x / Mathf.Max(1, Screen.width * .14f), -1, 1);

            float upThreshold = Mathf.Max(42, Screen.height * .045f);
            if (!jumpGestureUsed && delta.y >= upThreshold)
            {
                touchJump = true;
                jumpGestureUsed = true;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                float tapRadius = Screen.width * .055f;
                if (!jumpGestureUsed && Time.unscaledTime - fingerStartedAt <= .42f && delta.sqrMagnitude <= tapRadius * tapRadius)
                    touchJump = true;
                ResetGesture();
            }
            return;
        }

        // Browsers occasionally omit the final touch event when focus changes.
        ResetGesture();
    }

    static void ResetGesture()
    {
        trackingFinger = false;
        fingerId = -1;
        gestureHorizontal = 0;
        jumpGestureUsed = false;
    }

    public static void Clear() { left.Clear(); right.Clear(); touchJump = false; ResetGesture(); }
}
