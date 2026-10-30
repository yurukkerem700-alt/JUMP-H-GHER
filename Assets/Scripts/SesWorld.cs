using UnityEngine;
using SesAtolyesi;
using SesAtolyesi.Dsp;

// Jump Higher'ın ses dünyası: bölgeye (tema) göre zemin sesi ve ortam (rüzgar, yağmur, gece).
// Ses Atölyesi paketini (Packages/com.sesatolyesi.runtime) oyuna bağlayan tek yer burasıdır.
public class SesWorld : MonoBehaviour
{
    // TowerThemes sırası: Kül Mezarlığı, Yitik Bulutlar, Fırtına Geçidi, Kızıl Kökler, Yıldız Külleri
    static readonly SurfaceType[] Surfaces = { SurfaceType.Gravel, SurfaceType.Snow, SurfaceType.Stone, SurfaceType.Wood, SurfaceType.Sand };
    static readonly float[] Wind = { .15f, .6f, .9f, .25f, .1f };
    static readonly float[] Rain = { 0f, 0f, .7f, 0f, 0f };
    static readonly float[] Foliage = { 0f, 0f, 0f, .8f, 0f };
    static readonly float[] Night = { .5f, 0f, 0f, .3f, 1f };

    InfiniteTower tower;
    SesOrtam ortam;

    public static SurfaceType Surface(int floor) { return Surfaces[TowerThemes.ThemeAtFloor(floor)]; }

    public static void Ensure()
    {
        if (FindFirstObjectByType<SesWorld>() == null) new GameObject("Ses Dünyası").AddComponent<SesWorld>();
    }

    // Düşman bir basamağa indi: yakındaysa duyulur, uzaktaysa sessiz kalır.
    public static void EnemyLand(Vector3 enemy, Vector3 player, int floor)
    {
        float d = Vector2.Distance(enemy, player);
        if (d < 9f) Ses.Land(Surface(floor), .35f, (1f - d / 9f) * .8f);
    }

    void Awake()
    {
        Ses.Use3D = false; // 2B oyun: sesler uzaklığa göre değil düz çalar
        ortam = gameObject.AddComponent<SesOrtam>();
        ortam.response = 3f;
    }

    void Update()
    {
        if (!tower) tower = FindFirstObjectByType<InfiniteTower>();
        int t = tower ? TowerThemes.ThemeAtFloor(tower.HighestReachedFloor) : 0;
        ortam.wind = Wind[t]; ortam.rain = Rain[t]; ortam.foliage = Foliage[t]; ortam.night = Night[t];
        ortam.volume = GameFlow.IsMenu ? .4f : 1f;
    }
}
