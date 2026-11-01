using System.Collections.Generic;
using UnityEngine;

public static class GameInput
{
    static readonly HashSet<int> left = new HashSet<int>();
    static readonly HashSet<int> right = new HashSet<int>();
    static bool touchJump;
    static float joyHorizontal;
    static bool joyUp, joyDown;
    public static event System.Action Cleared;

    public static float Horizontal
    {
        get
        {
            float keyboard = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(keyboard) > .01f) return keyboard;
            if (joyHorizontal != 0) return joyHorizontal;
            return Mathf.Clamp(right.Count - left.Count, -1, 1);
        }
    }

    // Joystick yukarıda tutuluyorsa yere inince tekrar zıplanır.
    public static bool JumpHeld => joyUp;
    public static bool DownHeld => joyDown;

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

    static bool touchKick;
    public static void Kick() { touchKick = true; }
    public static bool ConsumeKick()
    {
        bool result = touchKick || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift);
        touchKick = false;
        return result;
    }

    public static void SetJoystick(float horizontal, bool up, bool down)
    {
        joyHorizontal = horizontal; joyUp = up; joyDown = down;
    }
    public static void Clear() { left.Clear(); right.Clear(); touchJump = false; touchKick = false; joyHorizontal = 0; joyUp = joyDown = false; Cleared?.Invoke(); }
}
