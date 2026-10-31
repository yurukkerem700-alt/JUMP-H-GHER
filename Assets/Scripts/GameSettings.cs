using System;
using UnityEngine;

// Oyuncunun kalıcı ayarları (PlayerPrefs). Joystick konumu, boyutu, şeffaflığı ve hassasiyeti buradadır.
public static class GameSettings
{
    public static event Action Changed;

    public static bool JoyEnabled { get => Bool("JoyEnabled", true); set { SetBool("JoyEnabled", value); Changed?.Invoke(); } }
    public static bool JoyVisible { get => Bool("JoyVisible", true); set { SetBool("JoyVisible", value); Changed?.Invoke(); } }
    // 0..1: ekranın soldan / alttan oranı
    public static float JoyX { get => Get("JoyX", .5f); set => Put("JoyX", value); }
    public static float JoyY { get => Get("JoyY", .22f); set => Put("JoyY", value); }
    public static float JoySize { get => Get("JoySize", 1f); set => Put("JoySize", value); }
    public static float JoyOpacity { get => Get("JoyOpacity", .6f); set => Put("JoyOpacity", value); }
    public static float JoySensitivity { get => Get("JoySens", 1f); set => Put("JoySens", value); }

    public static void ResetJoystick()
    {
        foreach (var key in new[] { "JoyEnabled", "JoyVisible", "JoyX", "JoyY", "JoySize", "JoyOpacity", "JoySens" }) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save(); Changed?.Invoke();
    }

    static bool Bool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
    static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }
    static float Get(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
    static void Put(string key, float value) { PlayerPrefs.SetFloat(key, value); Changed?.Invoke(); }
}
