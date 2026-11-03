using System;
using System.Collections.Generic;
using UnityEngine;

// Kalıcı cüzdan, günlük görevler ve günlük giriş ödülü (PlayerPrefs). Görevler tarihe göre seçilir; gün değişince yenilenir.
public static class PlayerProgress
{
    public enum Kind { Coins, Floor, Kicks, Levels }
    public struct DailyTask { public Kind kind; public int target, reward; public string text; }

    static readonly int[][] Targets = { new[] { 40, 80 }, new[] { 15, 30 }, new[] { 4, 8 }, new[] { 1, 2 } };
    static readonly int[][] Rewards = { new[] { 20, 40 }, new[] { 20, 40 }, new[] { 15, 30 }, new[] { 15, 30 } };

    public static int Wallet => PlayerPrefs.GetInt("Wallet", 0);
    public static int DayNumber => (int)(DateTime.Now.Date - new DateTime(2026, 1, 1)).TotalDays;

    static int cachedDay = -1; static DailyTask[] cache;

    public static DailyTask[] Today()
    {
        int day = DayNumber;
        if (cache != null && cachedDay == day) return cache;
        var rng = new System.Random(day * 7919 + 13);
        var kinds = new List<Kind> { Kind.Coins, Kind.Floor, Kind.Kicks, Kind.Levels };
        for (int i = kinds.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var tmp = kinds[i]; kinds[i] = kinds[j]; kinds[j] = tmp; }
        cache = new DailyTask[3]; cachedDay = day;
        for (int i = 0; i < 3; i++)
        {
            var kind = kinds[i]; int tier = rng.Next(2), k = (int)kind, target = Targets[k][tier];
            cache[i] = new DailyTask { kind = kind, target = target, reward = Rewards[k][tier], text = Describe(kind, target) };
        }
        return cache;
    }

    static string Describe(Kind kind, int n)
    {
        switch (kind)
        {
            case Kind.Coins: return n + " coin topla";
            case Kind.Floor: return "Tek turda kat " + n + "'e çık";
            case Kind.Kicks: return n + " düşmanı tekmele";
            default: return n + " bölüm tamamla";
        }
    }

    static string Key(Kind kind, string part) => "Task" + DayNumber + "_" + (int)kind + part;
    public static int Progress(Kind kind) => PlayerPrefs.GetInt(Key(kind, "P"), 0);
    public static bool Claimed(Kind kind) => PlayerPrefs.GetInt(Key(kind, "C"), 0) != 0;
    public static bool Ready(DailyTask t) => Progress(t.kind) >= t.target && !Claimed(t.kind);

    static void Add(Kind kind, int n) { PlayerPrefs.SetInt(Key(kind, "P"), Progress(kind) + n); }

    // Oyun içi olaylar
    public static int Stat(string key) => PlayerPrefs.GetInt("Stat" + key, 0);
    static void Bump(string key) { PlayerPrefs.SetInt("Stat" + key, Stat(key) + 1); }
    public static void AddCoin() { PlayerPrefs.SetInt("Wallet", Wallet + 1); Add(Kind.Coins, 1); Bump("Coins"); }
    public static void AddKick() { Add(Kind.Kicks, 1); Bump("Kicks"); }
    public static void AddLevel() { Add(Kind.Levels, 1); Bump("Levels"); PlayerPrefs.Save(); }
    public static void ReportFloor(int floor) { if (floor > Progress(Kind.Floor)) PlayerPrefs.SetInt(Key(Kind.Floor, "P"), floor); }

    public static bool Claim(int index)
    {
        var tasks = Today(); if (index < 0 || index >= tasks.Length) return false;
        var t = tasks[index]; if (!Ready(t)) return false;
        PlayerPrefs.SetInt(Key(t.kind, "C"), 1); PlayerPrefs.SetInt("Wallet", Wallet + t.reward); PlayerPrefs.Save();
        return true;
    }

    // Günlük giriş ödülü: üst üste gelinen gün sayısı arttıkça büyür (en çok 50).
    public static bool LoginClaimable => PlayerPrefs.GetInt("LoginLastDay", -1) != DayNumber;
    public static int LoginStreak
    {
        get
        {
            int last = PlayerPrefs.GetInt("LoginLastDay", -1), streak = PlayerPrefs.GetInt("LoginStreak", 0);
            return last == DayNumber || last == DayNumber - 1 ? streak : 0;
        }
    }
    public static int NextStreak => LoginClaimable ? LoginStreak + 1 : LoginStreak;
    public static int LoginReward(int streak) => Mathf.Min(10 + (Mathf.Max(1, streak) - 1) * 5, 50);

    public static bool ClaimLogin()
    {
        if (!LoginClaimable) return false;
        int streak = LoginStreak + 1;
        PlayerPrefs.SetInt("LoginLastDay", DayNumber); PlayerPrefs.SetInt("LoginStreak", streak);
        PlayerPrefs.SetInt("Wallet", Wallet + LoginReward(streak)); PlayerPrefs.Save();
        return true;
    }

    public struct Achievement { public string title, desc; public int target, reward; public Func<int> value; }
    public static readonly Achievement[] Achievements =
    {
        new Achievement { title = "İlk Coin", desc = "Bir coin topla", target = 1, reward = 10, value = () => Stat("Coins") },
        new Achievement { title = "Coin Avcısı", desc = "Toplam 100 coin topla", target = 100, reward = 25, value = () => Stat("Coins") },
        new Achievement { title = "Altın Sandık", desc = "Toplam 1000 coin topla", target = 1000, reward = 100, value = () => Stat("Coins") },
        new Achievement { title = "İlk Adımlar", desc = "Kat 10'a ulaş", target = 10, reward = 15, value = () => PlayerPrefs.GetInt("BestFloor", 0) },
        new Achievement { title = "Tırmanıcı", desc = "Kat 25'e ulaş", target = 25, reward = 30, value = () => PlayerPrefs.GetInt("BestFloor", 0) },
        new Achievement { title = "Bulutların Üstü", desc = "Kat 50'ye ulaş", target = 50, reward = 60, value = () => PlayerPrefs.GetInt("BestFloor", 0) },
        new Achievement { title = "Zirve Avcısı", desc = "Kat 100'e ulaş", target = 100, reward = 150, value = () => PlayerPrefs.GetInt("BestFloor", 0) },
        new Achievement { title = "Tekmeci", desc = "Toplam 20 düşmanı tekmele", target = 20, reward = 25, value = () => Stat("Kicks") },
        new Achievement { title = "Bölüm Ustası", desc = "Toplam 10 bölüm tamamla", target = 10, reward = 40, value = () => Stat("Levels") },
        new Achievement { title = "Yıldız Toplayıcı", desc = "Toplam 30 yıldız kazan", target = 30, reward = 50, value = () => LevelDefs.TotalStars() },
        new Achievement { title = "Düzenli Oyuncu", desc = "3 gün üst üste gir", target = 3, reward = 40, value = () => PlayerPrefs.GetInt("LoginStreak", 0) },
    };
    public static bool AchievementClaimed(int i) => PlayerPrefs.GetInt("AchC" + i, 0) != 0;
    public static bool AchievementReady(int i) => i >= 0 && i < Achievements.Length && !AchievementClaimed(i) && Achievements[i].value() >= Achievements[i].target;
    public static bool ClaimAchievement(int i)
    {
        if (!AchievementReady(i)) return false;
        PlayerPrefs.SetInt("AchC" + i, 1); PlayerPrefs.SetInt("Wallet", Wallet + Achievements[i].reward); PlayerPrefs.Save();
        return true;
    }

    public static int ClaimableCount()
    {
        int n = LoginClaimable ? 1 : 0;
        foreach (var t in Today()) if (Ready(t)) n++;
        for (int i = 0; i < Achievements.Length; i++) if (AchievementReady(i)) n++;
        return n;
    }
}
