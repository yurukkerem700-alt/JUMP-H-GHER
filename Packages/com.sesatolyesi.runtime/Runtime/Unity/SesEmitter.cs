using UnityEngine;

namespace SesAtolyesi
{
    /// <summary>
    /// Dünyada belli bir noktadan sürekli gelen ses: şömine/kamp ateşi, dere, böcek ormanı.
    /// Uzaklaştıkça kısılır. Bir boş nesneye ekleyip yerleştirin.
    /// </summary>
    [AddComponentMenu("Ses Atölyesi/Emitter")]
    public sealed class SesEmitter : MonoBehaviour
    {
        public LoopKind kind = LoopKind.Fire;
        [Range(0f, 1.5f)] public float volume = 1f;
        [Tooltip("Kapalıysa uzaklığa bakmadan düz (2B) çalar.")]
        public bool spatial = true;
        public float minDistance = 2f;
        public float maxDistance = 25f;

        AudioSource _src;

        void Start()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = Ses.Loop(kind);
            _src.loop = true;
            _src.playOnAwake = false;
            _src.spatialBlend = spatial ? 1f : 0f;
            _src.rolloffMode = AudioRolloffMode.Logarithmic;
            _src.minDistance = minDistance;
            _src.maxDistance = maxDistance;
            if (_src.clip != null) _src.time = Random.value * _src.clip.length * 0.9f;
            Apply();
            _src.Play();
        }

        void Update() { Apply(); }

        void Apply()
        {
            if (_src != null) _src.volume = Mathf.Clamp01(volume * Ses.Master * Ses.Ambience);
        }
    }
}
