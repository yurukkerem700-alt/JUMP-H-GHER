using System;
using UnityEngine;

// Oyuncu kostümleri. Seçim PlayerPrefs'te saklanır; sprite sayfaları Resources/Costumes/<id>.png (4x2, 8 kare).
// "gezgin" varsayılan kostümdür ve sahnedeki PlayerVisual2D.animationSheet sayfasını kullanır.
public static class Costumes
{
    public const string Default = "gezgin";
    public const string Elara = "elara";
    public static int FrameRows(string id) => id == Elara || CostumeRig2D.Index(id)>=0 ? 3 : 2;
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

    // Mağaza: fiyatı olmayan kostümler ücretsizdir. Mağaza ilk kez görüldüğünde o an seçili kostüm sahipliğe yazılır (mevcut oyuncu kaybetmez).
    static readonly System.Collections.Generic.Dictionary<string, int> Prices = new System.Collections.Generic.Dictionary<string, int>
    { { "kok_kalbi", 150 }, { "ay_kirigi_tilkisi", 200 }, { "gece_zirhcisi", 250 }, { "mercan_cadisi", 300 }, { "kum_saati_bekcisi", 350 }, { "buz_pervanesi", 400 }, { "bordo_kovboy", 500 }, { "elara", 600 } };
    public static int Price(string id) => Prices.TryGetValue(id, out var p) ? p : 0;
    static void EnsureShop()
    {
        if (PlayerPrefs.GetInt("ShopV1", 0) != 0) return;
        PlayerPrefs.SetInt("ShopV1", 1);
        var current = PlayerPrefs.GetString("Costume", Default);
        if (Price(current) > 0) PlayerPrefs.SetInt("Own_" + current, 1);
        PlayerPrefs.Save();
    }
    public static bool Owned(string id)
    {
#if UNITY_EDITOR
        return true;
#else
        EnsureShop();
        return Price(id) == 0 || PlayerPrefs.GetInt("Own_" + id, 0) != 0;
#endif
    }
    public static bool Buy(string id)
    {
        EnsureShop();
        if (Owned(id)) return true;
        int price = Price(id);
        if (PlayerProgress.Wallet < price) return false;
        PlayerPrefs.SetInt("Wallet", PlayerProgress.Wallet - price); PlayerPrefs.SetInt("Own_" + id, 1); PlayerPrefs.Save();
        return true;
    }
    // Oyun sahnesi başlarken PlayerVisual2D tarafından doldurulur; varsayılan kostümün önizlemesi için.
    public static Texture2D DefaultSheet;

    public static string SelectedId
    {
        get
        {
            var id = PlayerPrefs.GetString("Costume", Default);
            foreach (var e in All) if (e.Id == id) return Owned(id) ? id : Default;
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
        var sheet = Sheet(id);
        if (!sheet) return null;
        int rows = FrameRows(id), w = sheet.width / 4, h = sheet.height / rows;
        var pixels=sheet.GetPixels(0,(rows-1)*h,w,h);
        int minX=w,minY=h,maxX=-1,maxY=-1;
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(pixels[y*w+x].a>.1f)
        { minX=Mathf.Min(minX,x); minY=Mathf.Min(minY,y); maxX=Mathf.Max(maxX,x); maxY=Mathf.Max(maxY,y); }
        if(maxX<0) return null;
        minX=Mathf.Max(0,minX-4); minY=Mathf.Max(0,minY-4); maxX=Mathf.Min(w-1,maxX+4); maxY=Mathf.Min(h-1,maxY+4);
        return Sprite.Create(sheet, new Rect(minX,(rows-1)*h+minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,.5f),100);
    }
}
