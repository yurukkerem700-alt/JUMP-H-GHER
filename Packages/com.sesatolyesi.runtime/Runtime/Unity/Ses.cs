using System.Collections.Generic;
using UnityEngine;
using SesAtolyesi.Dsp;

namespace SesAtolyesi
{
    public enum LoopKind { WindLight, WindStrong, Leaves, RainLight, RainHeavy, Fire, Stream, Crickets }

    /// <summary>Kayıt dosyasıyla değiştirilebilen ses türleri (bkz. Ses.Override).</summary>
    public enum SesSound { Footstep, Jump, Land, Impact, Coin, Pickup, Hit, Swoosh, UiClick, UiConfirm }

    /// <summary>
    /// Ses Atölyesi'nin ana girişi. Sahneye bir şey eklemeniz gerekmez: ilk çağrıda kendini kurar.
    /// Örnek: Ses.Footstep(SurfaceType.Grass); Ses.Coin(); Ses.Land(SurfaceType.Stone, 0.8f);
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ses : MonoBehaviour
    {
        // ---- Ayarlar (oyun içi ses menüsüne bağlayabilirsiniz) ----
        public static float Master = 1f;
        public static float Sfx = 1f;
        public static float Ambience = 1f;
        /// <summary>Konum verilen seslerde 3B ses kullan. 2B oyunlarda false yaparsanız her ses düz çalar.</summary>
        public static bool Use3D = true;
        public static float MinDistance = 4f;
        public static float MaxDistance = 40f;
        /// <summary>Zemin tanınamazsa kullanılacak yüzey.</summary>
        public static SurfaceType DefaultSurface = SurfaceType.Stone;

        const int VoiceCount = 24;
        const int K_FOOT = 1, K_LAND = 2, K_JUMP = 3, K_COIN = 4, K_HIT = 5, K_UICLICK = 6, K_UICONF = 7,
                  K_SWOOSH = 8, K_PICKUP = 9, K_IMPACT = 10, K_THUNDER = 11;
        static readonly float[] FootWeights = { 0.7f, 1.0f, 1.7f };
        static readonly float[] ThunderDistances = { 0.15f, 0.5f, 0.85f };

        static Ses _inst;
        static bool _quitting;

        readonly Dictionary<int, AudioClip[]> _cache = new Dictionary<int, AudioClip[]>();
        readonly Dictionary<int, AudioClip[]> _overrides = new Dictionary<int, AudioClip[]>();
        readonly Dictionary<int, int> _last = new Dictionary<int, int>();
        readonly Dictionary<int, AudioClip> _loops = new Dictionary<int, AudioClip>();
        AudioSource[] _voices;
        float[] _voiceStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _inst = null; _quitting = false; }

        static Ses Instance
        {
            get
            {
                if (_inst == null && !_quitting)
                {
                    var go = new GameObject("SesAtolyesi");
                    DontDestroyOnLoad(go);
                    _inst = go.AddComponent<Ses>();
                }
                return _inst;
            }
        }

        void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
            _voices = new AudioSource[VoiceCount];
            _voiceStart = new float[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var go = new GameObject("voice" + i);
                go.transform.SetParent(transform, false);
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.rolloffMode = AudioRolloffMode.Logarithmic;
                _voices[i] = a;
            }
        }

        void OnApplicationQuit() { _quitting = true; }

        // ================================================================ GENEL KULLANIM

        /// <summary>Adım sesi. weight: 0.7 hafif, 1 normal, 1.7 ağır. pos verilirse sesin geldiği yer.</summary>
        public static void Footstep(SurfaceType surface, float weight = 1f, float volume = 1f, Vector3? pos = null)
        {
            var self = Instance; if (self == null) return;
            int bucket = weight < 0.85f ? 0 : (weight < 1.35f ? 1 : 2);
            int key;
            var clips = self.Lookup(K_FOOT, (int)surface, bucket, out key) ?? self.BuildFoot(key, surface, bucket);
            self.Play(self.Pick(key, clips), volume * Random.Range(0.85f, 1f), Random.Range(0.95f, 1.05f), pos);
        }

        public static void Jump(SurfaceType surface, float volume = 1f, Vector3? pos = null)
        {
            var self = Instance; if (self == null) return;
            int key;
            var clips = self.Lookup(K_JUMP, (int)surface, 0, out key) ?? self.BuildJump(key, surface);
            self.Play(self.Pick(key, clips), volume * Random.Range(0.85f, 1f), Random.Range(0.96f, 1.04f), pos);
        }

        /// <summary>Yere iniş. intensity01: 0 hafif atlama, 1 yüksekten düşüş.</summary>
        public static void Land(SurfaceType surface, float intensity01 = 0.5f, float volume = 1f, Vector3? pos = null)
        {
            var self = Instance; if (self == null) return;
            int bucket = intensity01 < 0.34f ? 0 : (intensity01 < 0.67f ? 1 : 2);
            int key;
            var clips = self.Lookup(K_LAND, (int)surface, bucket, out key) ?? self.BuildLand(key, surface, bucket);
            self.Play(self.Pick(key, clips), volume * Random.Range(0.9f, 1f), Random.Range(0.96f, 1.04f), pos);
        }

        /// <summary>Cisim çarpması. mass: kilogram, speed: çarpma hızı (m/s).</summary>
        public static void Impact(SurfaceType surface, float mass, float speed, float volume = 1f, Vector3? pos = null)
        {
            var self = Instance; if (self == null) return;
            int mb = mass < 1.5f ? 0 : (mass < 12f ? 1 : 2);
            float speed01 = Mathf.Clamp01(speed / 12f);
            int sb = speed01 < 0.4f ? 0 : 1;
            int bucket = mb * 2 + sb;
            int key;
            var clips = self.Lookup(K_IMPACT, (int)surface, bucket, out key) ?? self.BuildImpact(key, surface, mb, sb);
            self.Play(self.Pick(key, clips), volume * Mathf.Lerp(0.5f, 1f, speed01), Random.Range(0.95f, 1.05f), pos);
        }

        public static void Coin(float volume = 1f, Vector3? pos = null) { Simple(K_COIN, volume, pos); }
        public static void Pickup(float volume = 1f, Vector3? pos = null) { Simple(K_PICKUP, volume, pos); }
        public static void Hit(float volume = 1f, Vector3? pos = null) { Simple(K_HIT, volume, pos); }
        public static void Swoosh(float volume = 1f, Vector3? pos = null) { Simple(K_SWOOSH, volume, pos); }
        public static void UiClick(float volume = 1f) { Simple(K_UICLICK, volume, null); }
        public static void UiConfirm(float volume = 1f) { Simple(K_UICONF, volume, null); }

        /// <summary>Gök gürültüsü. distance01: 0 çok yakın, 1 çok uzak.</summary>
        public static void Thunder(float distance01 = 0.5f, float volume = 1f)
        {
            var self = Instance; if (self == null) return;
            int bucket = distance01 < 0.33f ? 0 : (distance01 < 0.67f ? 1 : 2);
            int key = Key(K_THUNDER, 0, bucket);
            AudioClip[] clips;
            if (!self._cache.TryGetValue(key, out clips)) clips = self.BuildThunder(key, bucket);
            self.Play(self.Pick(key, clips), volume * Random.Range(0.9f, 1f), 1f, null);
        }

        /// <summary>
        /// Kendi kaydınızı kullanmak isterseniz: üretilen sesin yerine geçer. surface'in anlamsız olduğu seslerde (Coin gibi) boş bırakın.
        /// clips boş/null verirseniz üretilen sese geri dönülür.
        /// </summary>
        public static void Override(SesSound sound, SurfaceType surface, params AudioClip[] clips)
        {
            var self = Instance; if (self == null) return;
            int kind = KindOf(sound);
            int key = Key(kind, (int)surface, 255);
            if (clips == null || clips.Length == 0) self._overrides.Remove(key);
            else self._overrides[key] = clips;
        }

        /// <summary>Yükleme ekranında çağırırsanız ilk seslerde takılma olmaz.</summary>
        public static void Preload(params SurfaceType[] surfaces)
        {
            var self = Instance; if (self == null) return;
            if (surfaces == null || surfaces.Length == 0) surfaces = new[] { DefaultSurface };
            foreach (var s in surfaces)
            {
                for (int b = 0; b < 3; b++)
                {
                    int key;
                    if (self.Lookup(K_FOOT, (int)s, b, out key) == null) self.BuildFoot(key, s, b);
                    if (self.Lookup(K_LAND, (int)s, b, out key) == null) self.BuildLand(key, s, b);
                }
                int jk;
                if (self.Lookup(K_JUMP, (int)s, 0, out jk) == null) self.BuildJump(jk, s);
            }
        }

        public static void StopAll()
        {
            var self = Instance; if (self == null) return;
            for (int i = 0; i < VoiceCount; i++) self._voices[i].Stop();
        }

        /// <summary>Ortam döngüsü (rüzgar, yağmur...). SesOrtam ve SesEmitter bunu kullanır.</summary>
        public static AudioClip Loop(LoopKind kind)
        {
            var self = Instance; if (self == null) return null;
            AudioClip c;
            if (self._loops.TryGetValue((int)kind, out c)) return c;
            float[] d;
            switch (kind)
            {
                case LoopKind.WindLight: d = Recipes.Wind(0.25f, 1); break;
                case LoopKind.WindStrong: d = Recipes.Wind(0.9f, 2); break;
                case LoopKind.Leaves: d = Recipes.Leaves(0.6f, 1); break;
                case LoopKind.RainLight: d = Recipes.Rain(0.25f, 1); break;
                case LoopKind.RainHeavy: d = Recipes.Rain(0.9f, 2); break;
                case LoopKind.Fire: d = Recipes.Fire(1); break;
                case LoopKind.Stream: d = Recipes.Stream(1); break;
                default: d = Recipes.Crickets(1); break;
            }
            c = MakeClip("ses_loop_" + kind, d, Recipes.LoopRate);
            self._loops[(int)kind] = c;
            return c;
        }

        // ================================================================ İÇ İŞLER

        static void Simple(int kind, float volume, Vector3? pos)
        {
            var self = Instance; if (self == null) return;
            int key;
            var clips = self.Lookup(kind, 0, 0, out key) ?? self.BuildSimple(key, kind);
            float pitch = (kind == K_UICLICK || kind == K_UICONF) ? 1f : Random.Range(0.97f, 1.03f);
            self.Play(self.Pick(key, clips), volume * Random.Range(0.92f, 1f), pitch, pos);
        }

        static int Key(int kind, int a, int b) { return (kind << 16) | (a << 8) | b; }

        static int KindOf(SesSound s)
        {
            switch (s)
            {
                case SesSound.Footstep: return K_FOOT;
                case SesSound.Jump: return K_JUMP;
                case SesSound.Land: return K_LAND;
                case SesSound.Impact: return K_IMPACT;
                case SesSound.Coin: return K_COIN;
                case SesSound.Pickup: return K_PICKUP;
                case SesSound.Hit: return K_HIT;
                case SesSound.Swoosh: return K_SWOOSH;
                case SesSound.UiClick: return K_UICLICK;
                default: return K_UICONF;
            }
        }

        static AudioClip MakeClip(string name, float[] data, int rate)
        {
            var c = AudioClip.Create(name, data.Length, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        AudioClip[] Lookup(int kind, int a, int bucket, out int key)
        {
            key = Key(kind, a, bucket);
            AudioClip[] c;
            if (_overrides.TryGetValue(Key(kind, a, 255), out c)) return c;
            if (_cache.TryGetValue(key, out c)) return c;
            return null;
        }

        AudioClip[] Store(int key, AudioClip[] c) { _cache[key] = c; return c; }

        AudioClip[] BuildFoot(int key, SurfaceType s, int bucket)
        {
            var c = new AudioClip[6];
            for (int i = 0; i < c.Length; i++) c[i] = MakeClip("foot", Recipes.Footstep(s, FootWeights[bucket], i + bucket * 10), Recipes.OneShotRate);
            return Store(key, c);
        }

        AudioClip[] BuildJump(int key, SurfaceType s)
        {
            var c = new AudioClip[4];
            for (int i = 0; i < c.Length; i++) c[i] = MakeClip("jump", Recipes.Jump(s, i), Recipes.OneShotRate);
            return Store(key, c);
        }

        AudioClip[] BuildLand(int key, SurfaceType s, int bucket)
        {
            var c = new AudioClip[4];
            float inten = bucket == 0 ? 0.2f : (bucket == 1 ? 0.55f : 0.9f);
            for (int i = 0; i < c.Length; i++) c[i] = MakeClip("land", Recipes.Land(s, inten, i + bucket * 10), Recipes.OneShotRate);
            return Store(key, c);
        }

        AudioClip[] BuildImpact(int key, SurfaceType s, int massBucket, int speedBucket)
        {
            var c = new AudioClip[4];
            float mass = massBucket == 0 ? 0.5f : (massBucket == 1 ? 4f : 30f);
            float sp = speedBucket == 0 ? 0.3f : 0.85f;
            for (int i = 0; i < c.Length; i++) c[i] = MakeClip("impact", Recipes.Impact(s, mass, sp, i + massBucket * 10), Recipes.OneShotRate);
            return Store(key, c);
        }

        AudioClip[] BuildThunder(int key, int bucket)
        {
            var c = new AudioClip[2];
            for (int i = 0; i < c.Length; i++) c[i] = MakeClip("thunder", Recipes.Thunder(ThunderDistances[bucket], i + bucket * 10), Recipes.LoopRate);
            return Store(key, c);
        }

        AudioClip[] BuildSimple(int key, int kind)
        {
            var c = new AudioClip[4];
            for (int i = 0; i < c.Length; i++)
            {
                float[] d;
                switch (kind)
                {
                    case K_COIN: d = Recipes.Coin(i); break;
                    case K_PICKUP: d = Recipes.Pickup(i); break;
                    case K_HIT: d = Recipes.Hit(i); break;
                    case K_SWOOSH: d = Recipes.Swoosh(0.3f + 0.05f * i, i); break;
                    case K_UICLICK: d = Recipes.UiClick(i); break;
                    default: d = Recipes.UiConfirm(i); break;
                }
                c[i] = MakeClip("sfx", d, Recipes.OneShotRate);
            }
            return Store(key, c);
        }

        /// <summary>Aynı sesi üst üste tekrar çalmamak için bir önceki seçilenden farklı olanı seçer.</summary>
        AudioClip Pick(int key, AudioClip[] clips)
        {
            if (clips.Length == 1) return clips[0];
            int last;
            _last.TryGetValue(key, out last);
            int idx = Random.Range(0, clips.Length);
            if (idx == last) idx = (idx + 1 + Random.Range(0, clips.Length - 1)) % clips.Length;
            _last[key] = idx;
            return clips[idx];
        }

        void Play(AudioClip clip, float volume, float pitch, Vector3? pos)
        {
            if (clip == null) return;
            int vi = -1;
            float oldest = float.MaxValue;
            int oldestIdx = 0;
            for (int i = 0; i < VoiceCount; i++)
            {
                if (!_voices[i].isPlaying) { vi = i; break; }
                if (_voiceStart[i] < oldest) { oldest = _voiceStart[i]; oldestIdx = i; }
            }
            if (vi < 0) vi = oldestIdx; // hepsi doluysa en eskisini kes
            var v = _voices[vi];
            v.Stop();
            v.clip = clip;
            v.volume = Mathf.Clamp01(volume * Master * Sfx);
            v.pitch = pitch;
            if (pos.HasValue && Use3D)
            {
                v.transform.position = pos.Value;
                v.spatialBlend = 1f;
                v.minDistance = MinDistance;
                v.maxDistance = MaxDistance;
            }
            else v.spatialBlend = 0f;
            v.Play();
            _voiceStart[vi] = Time.unscaledTime;
        }
    }
}
