using UnityEngine;
using SesAtolyesi.Dsp;

namespace SesAtolyesi
{
    /// <summary>
    /// Karaktere ekleyin: yürürken zemine göre adım sesi, zıplama ve iniş sesi çalar.
    /// Tek yapmanız gereken oyun kodunuzda her karede "grounded" değerini vermek (yerde mi?).
    /// 2B ve 3B oyunlarda çalışır. Kendi animasyonunuzla eşleştirmek isterseniz autoStride'ı kapatıp Step() çağırın.
    /// </summary>
    [AddComponentMenu("Ses Atölyesi/Footsteps")]
    public sealed class SesFootsteps : MonoBehaviour
    {
        public enum Mode { Auto, Physics3D, Physics2D }

        [Header("Yürüme")]
        public Mode mode = Mode.Auto;
        [Tooltip("Karakter yerde mi? Oyununuzun zemin kontrolünden her karede atayın.")]
        public bool grounded = true;
        [Tooltip("Her adım arasındaki mesafe (oyun birimi). Küçük karakter için küçültün.")]
        public float strideLength = 1.1f;
        [Tooltip("Bu hızın altında adım sesi çalmaz.")]
        public float minSpeed = 0.3f;
        [Tooltip("Otomatik adım sesi. Kendi animasyon olayınızdan Step() çağıracaksanız kapatın.")]
        public bool autoStride = true;

        [Header("Karakter")]
        [Range(0.5f, 2f)] public float weight = 1f;
        [Range(0f, 1.5f)] public float volume = 1f;
        public bool autoJumpLand = true;
        [Tooltip("Bu düşüş hızında iniş sesi en güçlü olur.")]
        public float landSpeedForMax = 14f;

        [Header("Zemin tanıma")]
        public LayerMask groundMask = ~0;
        public float rayDistance = 1.5f;
        public Vector3 rayOffset = new Vector3(0f, 0.1f, 0f);
        public SurfaceType defaultSurface = SurfaceType.Stone;
        public bool useDefaultOnly;

        bool _is2D, _init, _wasGrounded, _foot;
        Vector3 _last;
        float _acc, _fallSpeed;
        SurfaceType _surface;

        public SurfaceType CurrentSurface { get { return _surface; } }

        void Awake()
        {
            _is2D = mode == Mode.Physics2D ||
                    (mode == Mode.Auto && (GetComponent<Rigidbody2D>() != null || GetComponentInChildren<Collider2D>() != null));
            _surface = defaultSurface;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position;
            if (!_init) { _last = p; _init = true; _wasGrounded = grounded; _acc = strideLength * 0.5f; }
            Vector3 d = p - _last;
            _last = p;
            float vy = d.y / dt;
            float horiz = _is2D ? Mathf.Abs(d.x) : new Vector2(d.x, d.z).magnitude;
            if (horiz > 5f) horiz = 0f; // ışınlanma/sahne geçişi sayılmaz

            if (autoJumpLand)
            {
                if (_wasGrounded && !grounded && vy > 0.5f) Jumped();
                if (!_wasGrounded && grounded) Landed(_fallSpeed);
                _fallSpeed = grounded ? 0f : Mathf.Max(_fallSpeed, -vy);
            }
            _wasGrounded = grounded;

            if (autoStride && grounded && horiz / dt > minSpeed)
            {
                _acc += horiz;
                if (_acc >= strideLength) { _acc -= strideLength; Step(); }
            }
        }

        /// <summary>Tek adım sesi çal. Animasyon olayından da çağrılabilir.</summary>
        public void Step()
        {
            _surface = Detect();
            _foot = !_foot;
            Ses.Footstep(_surface, weight, volume * (_foot ? 1f : 0.93f), transform.position);
        }

        public void Jumped()
        {
            _surface = Detect();
            Ses.Jump(_surface, volume, transform.position);
        }

        public void Landed(float impactSpeed)
        {
            float inten = Mathf.Clamp01(impactSpeed / landSpeedForMax);
            if (inten < 0.06f) return;
            _surface = Detect();
            Ses.Land(_surface, inten, volume, transform.position);
        }

        SurfaceType Detect()
        {
            if (useDefaultOnly) return defaultSurface;
            Vector3 o = transform.position + rayOffset;
            if (_is2D)
            {
                var hit = Physics2D.Raycast(o, Vector2.down, rayDistance, groundMask);
                if (hit.collider != null) return SesSurface.Resolve(hit.collider);
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(o, Vector3.down, out hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
                    return SesSurface.Resolve(hit.collider);
            }
            return defaultSurface;
        }
    }
}
