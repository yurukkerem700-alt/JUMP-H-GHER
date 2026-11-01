using UnityEngine;

// Oyunun ses dünyası: sürekli çalan Kök Yankısı müziği ve aralıklarla uzaktan gelen canlı sesleri (baykuş, yarasa, kara kedi).
// Dosyalar Resources/Sfx altındadır; yoksa ilgili ses sessizce atlanır. Ses seviyesi Ana ses x Ortam ayarına bağlıdır.
public class Soundscape : MonoBehaviour
{
    const float MusicGain = .38f, CallGain = .34f;
    static readonly string[] Calls = { "baykus", "yarasa", "karakedi", "ruh_isigi" };
    AudioSource music, calls; AudioClip[] callClips; float nextCall;

    void Start()
    {
        var clip = Resources.Load<AudioClip>("Sfx/ambient_dongu");
        if (clip)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.clip = clip; music.loop = true; music.playOnAwake = false; music.spatialBlend = 0; music.volume = 0; music.Play();
        }
        calls = gameObject.AddComponent<AudioSource>(); calls.playOnAwake = false; calls.spatialBlend = 0;
        callClips = new AudioClip[Calls.Length];
        for (int i = 0; i < Calls.Length; i++) callClips[i] = Resources.Load<AudioClip>("Sfx/" + Calls[i]);
        nextCall = Time.time + Random.Range(12f, 25f);
        if (!music) enabled = true;
    }

    void Update()
    {
        float bus = SesAtolyesi.Ses.Master * SesAtolyesi.Ses.Ambience;
        if (music)
        {
            if (!music.isPlaying) music.Play();
            music.volume = Mathf.MoveTowards(music.volume, MusicGain * bus, Time.unscaledDeltaTime * .25f);
        }
        if (Time.time < nextCall) return;
        nextCall = Time.time + Random.Range(16f, 38f);
        var clip = callClips[Random.Range(0, callClips.Length)]; if (!clip) return;
        calls.pitch = Random.Range(.92f, 1.08f);
        calls.PlayOneShot(clip, Mathf.Clamp01(CallGain * bus * Random.Range(.6f, 1f)));
    }
}
