using UnityEngine;
using SesAtolyesi.Dsp;

namespace SesAtolyesi
{
    /// <summary>
    /// Fizikle hareket eden nesneye (sandık, taş, varil, düşen eşya) ekleyin: çarpınca kütlesine ve hızına
    /// göre ses çıkarır. Ses, nesnenin kendi SesSurface'i varsa ona, yoksa çarptığı yüzeye göre seçilir.
    /// </summary>
    [AddComponentMenu("Ses Atölyesi/Impact")]
    public sealed class SesImpact : MonoBehaviour
    {
        [Range(0f, 1.5f)] public float volume = 1f;
        [Tooltip("Bu hızın altındaki sürtünme/temas sessiz kalır.")]
        public float minSpeed = 1.5f;
        [Tooltip("Arka arkaya çarpmalar arasındaki en kısa süre (sn).")]
        public float cooldown = 0.08f;

        float _lastTime = -10f;

        void OnCollisionEnter(Collision c)
        {
            float speed = c.relativeVelocity.magnitude;
            if (!Allowed(speed)) return;
            var rb = GetComponent<Rigidbody>();
            float mass = rb != null ? rb.mass : 1f;
            Vector3 pos = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            Ses.Impact(Pick(SesSurface.Resolve(c.collider)), mass, speed, volume, pos);
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            float speed = c.relativeVelocity.magnitude;
            if (!Allowed(speed)) return;
            var rb = GetComponent<Rigidbody2D>();
            float mass = rb != null ? rb.mass : 1f;
            Vector3 pos = c.contactCount > 0 ? (Vector3)c.GetContact(0).point : transform.position;
            Ses.Impact(Pick(SesSurface.Resolve(c.collider)), mass, speed, volume, pos);
        }

        bool Allowed(float speed)
        {
            if (speed < minSpeed || Time.time - _lastTime < cooldown) return false;
            _lastTime = Time.time;
            return true;
        }

        SurfaceType Pick(SurfaceType other)
        {
            var own = GetComponent<SesSurface>();
            return own != null ? own.surface : other;
        }
    }
}
