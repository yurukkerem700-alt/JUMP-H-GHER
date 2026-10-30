using UnityEngine;
using SesAtolyesi.Dsp;

namespace SesAtolyesi
{
    /// <summary>
    /// Deneme paneli: boş bir sahnede bir nesneye ekleyip oynatın. Tüm sesleri düğmelerle dinleyin,
    /// rüzgar/yağmur/kapalı mekan kaydırıcılarıyla ortamı karıştırın. Telefon ve tarayıcıda da çalışır.
    /// </summary>
    [AddComponentMenu("Ses Atölyesi/Demo Paneli")]
    public sealed class SesDemo : MonoBehaviour
    {
        SesOrtam _ortam;
        SurfaceType _surface = SurfaceType.Grass;
        float _weight = 1f, _inten = 0.6f;
        Vector2 _scroll;
        static readonly string[] SurfaceNames = { "Taş", "Çim", "Toprak", "Çakıl", "Ahşap", "Kar", "Su", "Metal", "Kum" };

        void Awake()
        {
            _ortam = GetComponent<SesOrtam>();
            if (_ortam == null) _ortam = gameObject.AddComponent<SesOrtam>();
        }

        void OnGUI()
        {
            float s = Mathf.Max(1f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;
            GUILayout.BeginArea(new Rect(8, 8, Mathf.Min(w - 16, 460), h - 16), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("SES ATÖLYESİ — Deneme");
            GUILayout.Label("Zemin");
            _surface = (SurfaceType)GUILayout.SelectionGrid((int)_surface, SurfaceNames, 5);
            GUILayout.Label("Ağırlık: " + _weight.ToString("0.0"));
            _weight = GUILayout.HorizontalSlider(_weight, 0.5f, 2f);
            GUILayout.Label("İniş şiddeti: " + _inten.ToString("0.0"));
            _inten = GUILayout.HorizontalSlider(_inten, 0f, 1f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Adım")) Ses.Footstep(_surface, _weight);
            if (GUILayout.Button("Zıpla")) Ses.Jump(_surface);
            if (GUILayout.Button("İniş")) Ses.Land(_surface, _inten);
            if (GUILayout.Button("Çarpma")) Ses.Impact(_surface, _weight * 4f, 3f + _inten * 9f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Coin")) Ses.Coin();
            if (GUILayout.Button("Güç")) Ses.Pickup();
            if (GUILayout.Button("Darbe")) Ses.Hit();
            if (GUILayout.Button("Swoosh")) Ses.Swoosh();
            if (GUILayout.Button("Tık")) Ses.UiClick();
            if (GUILayout.Button("Onay")) Ses.UiConfirm();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Gürültü yakın")) Ses.Thunder(0.15f);
            if (GUILayout.Button("Gürültü uzak")) Ses.Thunder(0.85f);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Ortam");
            _ortam.wind = Slider("Rüzgar", _ortam.wind);
            _ortam.rain = Slider("Yağmur", _ortam.rain);
            _ortam.foliage = Slider("Yaprak", _ortam.foliage);
            _ortam.night = Slider("Gece", _ortam.night);
            _ortam.indoors = Slider("Kapalı mekan", _ortam.indoors);
            Ses.Master = Slider("Ana ses", Ses.Master);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static float Slider(string label, float v)
        {
            GUILayout.Label(label + ": " + v.ToString("0.00"));
            return GUILayout.HorizontalSlider(v, 0f, 1f);
        }
    }
}
