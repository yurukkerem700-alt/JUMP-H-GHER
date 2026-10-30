using UnityEngine;

namespace SesAtolyesi
{
    /// <summary>
    /// Sahneye bir kez ekleyin. Rüzgar, yağmur, yaprak hışırtısı ve gece böcekleri aşağıdaki değerlere göre
    /// kendiliğinden karışır; değerler yumuşakça değişir (kod veya Inspector'dan 0-1 arası verin).
    /// Yağmur şiddetliyken ara sıra şimşek/gök gürültüsü olur (OnLightning ile ekran parlatabilirsiniz).
    /// </summary>
    [AddComponentMenu("Ses Atölyesi/Ortam")]
    public sealed class SesOrtam : MonoBehaviour
    {
        [Header("Hava (0-1)")]
        [Range(0f, 1f)] public float wind;
        [Range(0f, 1f)] public float rain;
        [Range(0f, 1f)] public float foliage;
        [Range(0f, 1f)] public float night;
        [Tooltip("1 = kapalı mekan: sesler boğuklaşır ve kısılır.")]
        [Range(0f, 1f)] public float indoors;

        [Header("Davranış")]
        [Tooltip("Değerlerin değişme süresi (sn).")]
        public float response = 1.5f;
        [Range(0f, 1.5f)] public float volume = 1f;

        [Header("Gök gürültüsü")]
        public bool thunder = true;
        [Range(0f, 1f)] public float thunderMinRain = 0.55f;
        public Vector2 thunderInterval = new Vector2(14f, 45f);
        /// <summary>Şimşek çaktığı an. Parametre uzaklık (0-1). Ses, uzaklığa göre gecikmeyle gelir.</summary>
        public event System.Action<float> OnLightning;

        sealed class Layer
        {
            public LoopKind kind;
            public AudioSource src;
            public AudioLowPassFilter lpf;
            public float cur;
        }

        Layer _windL, _windS, _leaves, _rainL, _rainH, _crickets;
        float _indoorsS, _nextThunder = -1f, _pending = -1f, _pendingDist;

        void Awake()
        {
            _windL = new Layer { kind = LoopKind.WindLight };
            _windS = new Layer { kind = LoopKind.WindStrong };
            _leaves = new Layer { kind = LoopKind.Leaves };
            _rainL = new Layer { kind = LoopKind.RainLight };
            _rainH = new Layer { kind = LoopKind.RainHeavy };
            _crickets = new Layer { kind = LoopKind.Crickets };
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float step = dt / Mathf.Max(0.05f, response);
            _indoorsS = Mathf.MoveTowards(_indoorsS, indoors, step);

            float strong = Smooth(0.35f, 0.9f, wind);
            Drive(_windL, Mathf.Sqrt(1f - strong) * Mathf.Clamp01(wind * 3f) * 0.9f, step);
            Drive(_windS, Mathf.Sqrt(strong), step);
            Drive(_leaves, foliage * Mathf.Clamp01(wind * 1.5f) * 0.8f, step);

            float heavy = Smooth(0.4f, 0.8f, rain);
            Drive(_rainL, Mathf.Sqrt(1f - heavy) * Mathf.Clamp01(rain * 4f) * 0.9f, step);
            Drive(_rainH, Mathf.Sqrt(heavy), step);

            Drive(_crickets, night * (1f - rain) * (1f - wind * 0.7f) * 0.5f, step);

            UpdateThunder(dt);
        }

        void Drive(Layer l, float target, float step)
        {
            target *= volume;
            l.cur = Mathf.MoveTowards(l.cur, target, step);
            if (l.cur < 0.002f && target < 0.002f)
            {
                if (l.src != null && l.src.isPlaying) l.src.Pause();
                return;
            }
            if (l.src == null) Create(l);
            if (!l.src.isPlaying) l.src.UnPause();
            float room = Mathf.Lerp(1f, 0.45f, _indoorsS);
            l.src.volume = Mathf.Clamp01(l.cur * room * Ses.Master * Ses.Ambience);
            if (l.lpf != null) l.lpf.cutoffFrequency = Mathf.Lerp(22000f, 800f, _indoorsS);
        }

        void Create(Layer l)
        {
            var go = new GameObject("ortam_" + l.kind);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = Ses.Loop(l.kind);
            a.loop = true;
            a.playOnAwake = false;
            a.spatialBlend = 0f;
            a.volume = 0f;
            if (a.clip != null) a.time = Random.value * a.clip.length * 0.9f; // çoklu sahnede aynı anda başlamasın
#if !UNITY_WEBGL
            // WebGL'de ses filtreleri desteklenmez; orada kapalı mekan yalnızca kısılma ile anlatılır.
            l.lpf = go.AddComponent<AudioLowPassFilter>();
            l.lpf.cutoffFrequency = 22000f;
#endif
            l.src = a;
            a.Play();
        }

        void UpdateThunder(float dt)
        {
            if (_pending >= 0f)
            {
                _pending -= dt;
                if (_pending < 0f) Ses.Thunder(_pendingDist, Mathf.Lerp(1f, 0.5f, _indoorsS) * volume);
            }
            if (!thunder || rain < thunderMinRain) { _nextThunder = -1f; return; }
            if (_nextThunder < 0f) _nextThunder = Random.Range(thunderInterval.x, thunderInterval.y) * 0.5f;
            _nextThunder -= dt;
            if (_nextThunder > 0f) return;
            _pendingDist = Random.Range(0.1f, 1f);
            _pending = _pendingDist * 3.5f; // ışık hızla gelir, ses gecikir
            if (OnLightning != null) OnLightning(_pendingDist);
            _nextThunder = Random.Range(thunderInterval.x, thunderInterval.y);
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
    }
}
