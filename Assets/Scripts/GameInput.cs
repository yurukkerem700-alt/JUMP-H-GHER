using System.Collections.Generic;
using UnityEngine;

public static class GameInput
{
    static readonly HashSet<int> left = new HashSet<int>();
    static readonly HashSet<int> right = new HashSet<int>();
    static bool touchJump;

    public static float Horizontal
    {
        get
        {
            float keyboard = Input.GetAxisRaw("Horizontal");
            return Mathf.Abs(keyboard) > .01f ? keyboard : Mathf.Clamp(right.Count - left.Count, -1, 1);
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
    public static void Clear() { left.Clear(); right.Clear(); touchJump = false; }
}
