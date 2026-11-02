using System;
using UnityEngine;

// Oyuncu kostümleri. Seçim PlayerPrefs'te saklanır; sprite sayfaları Resources/Costumes/<id>.png (4x2, 8 kare).
// "gezgin" varsayılan kostümdür ve sahnedeki PlayerVisual2D.animationSheet sayfasını kullanır.
public static class Costumes
{
    public const string Default = "gezgin";
    public const string Elara = "elara";
    public static int FrameRows(string id) => id == Elara ? 3 : 2;
    public static int FrameCount(string id) => 4 * FrameRows(id);

    public readonly struct Entry
    {
        public readonly string Id, Name, Story;
        public Entry(string id, string name, string story) { Id = id; Name = name; Story = story; }
    }

    public static readonly Entry[] All =
    {
        new Entry(Default, "KÖK GEZGİN", "Kökten doğan sessiz gezgin"),
        new Entry("kul_mumcusu", "KÜL MUMCUSU", "Mezarlığın kül sakallı bekçisi"),
        new Entry("bulut_balikcisi", "BULUT BALIKÇISI", "Gökte ters yüzen yaşlı kaptan"),
        new Entry("yildirim_kuklasi", "YILDIRIM KUKLASI", "Fırtınadan yapılma ipli kukla"),
        new Entry("kok_kalbi", "KÖK KALBİ", "Göğsünde kızıl kalp atan ağaç ruhu"),
        new Entry("ay_kirigi_tilkisi", "AY KIRIĞI TİLKİSİ", "Kuyruğu kırık bir ay parçası"),
        new Entry("gece_zirhcisi", "GECE ZIRHÇISI", "Geceyi boynuzlarında taşıyan muhafız"),
        new Entry("mercan_cadisi", "MERCAN CADISI", "Mercan tacıyla derinliklerden yükselir"),
        new Entry("kum_saati_bekcisi", "KUM SAATİ BEKÇİSİ", "Zamanın altın tozlarını korur"),
        new Entry("buz_pervanesi", "BUZ PERVANESİ", "Kristal kanatlarıyla soğuğu dokur"),
        new Entry("bordo_kovboy", "BORDO KOVBOY", "Kırmızı gözlü, sarı saçlı yetişkin gece yolcusu"),
        new Entry(Elara, "ELARA", "Kök tılsımının ışığında yolunu bulan gezgin"),
    };

    public static event Action Changed;
    // Oyun sahnesi başlarken PlayerVisual2D tarafından doldurulur; varsayılan kostümün önizlemesi için.
    public static Texture2D DefaultSheet;

    public static string SelectedId
    {
        get
        {
            var id = PlayerPrefs.GetString("Costume", Default);
            foreach (var e in All) if (e.Id == id) return id;
            return Default;
        }
        set
        {
            if (value == SelectedId) return;
            PlayerPrefs.SetString("Costume", value); PlayerPrefs.Save(); Changed?.Invoke();
        }
    }

    // Kostüm sayfası; varsayılan için ya da dosya okunamıyorsa null (çağıran varsayılana döner).
    public static Texture2D Sheet(string id)
    {
        if (id == Default) return DefaultSheet;
        var sheet = Resources.Load<Texture2D>("Costumes/" + id);
        return sheet && sheet.isReadable ? sheet : null;
    }

    // Bekleme karesi (sayfanın sol üst hücresi); kart önizlemesi için.
    public static Sprite Preview(string id)
    {
        int rig = CostumeRig2D.Index(id); if(rig >= 0) return CostumeRig2D.Preview(rig);
        var sheet = Sheet(id);
        if (!sheet) return null;
        int rows = FrameRows(id), w = sheet.width / 4, h = sheet.height / rows;
        return Sprite.Create(sheet, new Rect(0, (rows-1)*h, w, h), new Vector2(.5f, .5f), 100);
    }
}
