# Ses Atölyesi

Unity oyunları için ücretsiz, **kayıt dosyası gerektirmeyen** ses sistemi. Sesler oyun açılırken kodla üretilir; bu yüzden telif, lisans veya "kaynak göster" sorunu yoktur. 2B/3B, mobil, PC ve WebGL için tasarlandı.

## Ne var?

| Ses | Nasıl çalışır |
|---|---|
| **Adım** | 9 zemin: taş, çim, toprak, çakıl, ahşap, kar, su, metal, kum. Her adım biraz farklıdır, art arda aynı ses çalmaz. Ağırlığa göre pes/tiz. |
| **Zıplama / iniş** | Zemine ve düşüş şiddetine göre. |
| **Çarpma** | Nesnenin kütlesi ve hızına göre (ağır sandık pes ve uzun, hafif taş tiz ve kısa). |
| **Ortam** | Rüzgar (esintili, güçlenince ıslık), yağmur (hafiften sağanağa), yaprak hışırtısı, gece böcekleri. Hepsi 0-1 arası değerle karışır. |
| **Gök gürültüsü** | Yağmur şiddetliyken kendiliğinden; yakın (keskin) ve uzak (pes, uzun). Şimşek olayı vardır, ekran parlatabilirsiniz. |
| **Dünya sesleri** | Ateş, dere (bir noktaya yerleştirilir, uzaklaştıkça kısılır). |
| **Ödül / arayüz** | Coin, güç alma, darbe, swoosh, tık, onay. |

## Kurulum (1 dakika)

**Yol 1 — klasörü kopyala:** Bu klasörü projenizin `Packages/` klasörüne koyun. Unity kendiliğinden tanır.

**Yol 2 — GitHub'dan:** Klasörü bir GitHub deposuna yükleyin. Unity'de *Window ▸ Package Manager ▸ + ▸ Add package from git URL* ile depo adresini yapıştırın. Sonraki oyunlarda da aynı adres yeter.

## Kullanım

Sahneye hiçbir şey eklemeniz gerekmez. Kodunuzdan tek satırla çağırın:

```csharp
using SesAtolyesi;
using SesAtolyesi.Dsp;

Ses.Coin();
Ses.Land(SurfaceType.Stone, 0.8f);     // 0 = hafif, 1 = yüksekten düşüş
Ses.Footstep(SurfaceType.Grass);
```

**Karakter için en kolay yol:** Karaktere `Ses Atölyesi/Footsteps` bileşenini ekleyin, kodunuzda her karede `grounded` değerini verin. Adım, zıplama ve iniş sesleri kendiliğinden çalar:

```csharp
footsteps.grounded = zemindeMi;
```

**Zemin tanıma:** Zemin nesnesine `SesSurface` bileşeni ekleyip yüzeyi seçin. Eklemezseniz etiket, fizik malzemesi veya nesne adına bakılır ("Grass", "Çim", "Taş", "Ahşap", "Kar"...). Hiçbiri uymazsa `Ses.DefaultSurface`.

**Ortam:** Sahneye bir kez `Ses Atölyesi/Ortam` ekleyin:

```csharp
ortam.wind = 0.6f;  ortam.rain = 0.8f;  ortam.indoors = 1f;   // içeri girince sesler boğuklaşır
ortam.OnLightning += uzaklik => ekraniParlat();
```

**Çarpma:** Fizikle hareket eden nesneye `Ses Atölyesi/Impact` ekleyin.

**Dinleyerek deneme:** Boş sahnede bir nesneye `Ses Atölyesi/Demo Paneli` ekleyip oynatın; tüm sesler düğmelerle dinlenir (telefonda ve tarayıcıda da çalışır).

**Ses menüsüne bağlama:** `Ses.Master`, `Ses.Sfx`, `Ses.Ambience` (0-1).

**İlk seste takılma olmasın:** Yükleme ekranında `Ses.Preload(SurfaceType.Grass, SurfaceType.Stone)`.

## Daha gerçekçi: kendi kaydınızı karıştırma

Üretilen ses, iyi bir gerçek kayıtla karşılaştırılamaz; ama ikisini birlikte kullanabilirsiniz. Ücretsiz ticari kullanıma uygun bir kayıt bulduysanız (lisansını kontrol edin) üretilen sesin yerine koyun:

```csharp
Ses.Override(SesSound.Footstep, SurfaceType.Gravel, clip1, clip2, clip3);
Ses.Override(SesSound.Footstep, SurfaceType.Gravel);   // geri al
```

## Dinlemeden önce: deneme dosyaları ve doğrulama

`Tools~/SesOnizleme` tüm sesleri WAV olarak üretir ve sayısal kontroller yapar (patlama/cızırtı yok, başlangıç-bitiş tıkırtısı yok, döngüler dikişsiz, aynı sesin varyasyonları gerçekten farklı, ağır adım hafiften pes...). Unity gerekmez:

```bash
cd "Tools~/SesOnizleme"
dotnet run -c Release -- out
```

`Tools~/UnityCompileCheck` kodu Unity'nin kendi kütüphaneleriyle Unity'yi açmadan derler.

## Sınırlar (dürüst notlar)

- Sesler matematikle üretilir. Kayıtlar kadar zengin değildir; **kulakla ayar gerektirir**. Hoşunuza gitmeyen bir sesi söyleyin, tarifi (`Runtime/Dsp/Recipes.cs`) değiştiririz.
- WebGL'de ses filtreleri çalışmaz; kapalı mekan etkisi orada yalnızca kısılma ile verilir.
- Üretim süresi masaüstünde 50 sesin tamamı ~0,3 sn. Telefon ve WebGL'de daha yavaştır; `Ses.Preload` ile yükleme ekranına alın.
- Tüm sesler önbelleğe alınır, oyun sırasında üretim yapılmaz.
