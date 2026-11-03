using System;
using UnityEngine;

// Oyuncunun kalıcı ayarları (PlayerPrefs). Joystick konumu, boyutu, şeffaflığı ve hassasiyeti buradadır.
public static class GameSettings
{
    public static event Action Changed;

    // Ses: Müzik = ortam müziği ve uzaktan canlı sesleri, Efekt = zıplama, coin, vuruş, arayüz sesleri (0..1).
    public static float MusicVolume { get => Get("MusicVol", 1f); set { PlayerPrefs.SetFloat("MusicVol", value); ApplyAudio(); } }
    public static float SfxVolume { get => Get("SfxVol", 1f); set { PlayerPrefs.SetFloat("SfxVol", value); ApplyAudio(); } }
    public static void ApplyAudio() { SesAtolyesi.Ses.Ambience = MusicVolume; SesAtolyesi.Ses.Sfx = SfxVolume; }

    public static bool JoyEnabled { get => Bool("JoyEnabled", true); set { SetBool("JoyEnabled", value); Changed?.Invoke(); } }
    public static bool JoyVisible { get => Bool("JoyVisible", true); set { SetBool("JoyVisible", value); Changed?.Invoke(); } }
    // 0..1: ekranın soldan / alttan oranı
    public static float JoyX { get => Get("JoyX", .5f); set => Put("JoyX", value); }
    public static float JoyY { get => Get("JoyY", .22f); set => Put("JoyY", value); }
    public static float JoySize { get => Get("JoySize", 1f); set => Put("JoySize", value); }
    public static float JoyOpacity { get => Get("JoyOpacity", .6f); set => Put("JoyOpacity", value); }
    public static float JoySensitivity { get => Get("JoySens", 1f); set => Put("JoySens", value); }

    // Joystick kapalıyken görünen < > ZIPLA düğmeleri. Konumlar varsayılandan kayma oranıdır (ekran genişliği / yüksekliği).
    public static float BtnSize { get => Get("BtnSize", 1f); set => Put("BtnSize", value); }
    public static float BtnOpacity { get => Get("BtnOpacity", 1f); set => Put("BtnOpacity", value); }
    public static float MoveX { get => Get("MoveX", 0f); set => Put("MoveX", value); }   // < > grubu sağa
    public static float MoveY { get => Get("MoveY", 0f); set => Put("MoveY", value); }   // < > grubu yukarı
    public static float JumpX { get => Get("JumpX", 0f); set => Put("JumpX", value); }   // ZIPLA sola
    public static float JumpY { get => Get("JumpY", 0f); set => Put("JumpY", value); }   // ZIPLA yukarı

    // Oyuncunun sürükleyerek seçtiği kontrol konumları: 5 kontrol (joystick, sol, sağ, zıpla, tekme), güvenli alanın 0..1 oranı.
    public const int ControlCount = 5;
    public static bool LayoutDone { get => Bool("LayoutDone", false); set => SetBool("LayoutDone", value); }
    public static bool HasLayout => PlayerPrefs.HasKey("Ctl0X");
    public static Vector2 Control(int i) => new Vector2(Get("Ctl" + i + "X", .5f), Get("Ctl" + i + "Y", .5f));
    public static void SaveLayout(Vector2[] positions)
    {
        for (int i = 0; i < ControlCount; i++) { PlayerPrefs.SetFloat("Ctl" + i + "X", positions[i].x); PlayerPrefs.SetFloat("Ctl" + i + "Y", positions[i].y); }
        PlayerPrefs.Save();
    }
    public static void ResetLayout()
    {
        for (int i = 0; i < ControlCount; i++) { PlayerPrefs.DeleteKey("Ctl" + i + "X"); PlayerPrefs.DeleteKey("Ctl" + i + "Y"); }
        PlayerPrefs.Save(); Changed?.Invoke();
    }

    public static void ResetJoystick()
    {
        ResetLayout();
        foreach (var key in new[] { "MusicVol", "SfxVol", "JoyEnabled", "JoyVisible", "JoyX", "JoyY", "JoySize", "JoyOpacity", "JoySens", "BtnSize", "BtnOpacity", "MoveX", "MoveY", "JumpX", "JumpY" }) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save(); ApplyAudio(); Changed?.Invoke();
    }

    static bool Bool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
    static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }
    static float Get(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
    static void Put(string key, float value) { PlayerPrefs.SetFloat(key, value); Changed?.Invoke(); }
}
