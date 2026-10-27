# JUMP HIGHER — Kök Yankısı

Her zaman daha yükseğe zıpla. Özgün 2D karakterli sonsuz dikey platform oyunu.

Unity projesi: **6000.0.84f1**. Ana sahne: `Assets/Scenes/SampleScene.unity`.

## Oynanış

- A / D veya sol / sağ ok: hareket.
- W veya Boşluk: zıpla. Normalde havada tekrar zıplama yok.
- R: koşuyu yeniden başlat. P / Esc veya ekrandaki II: duraklat.
- Telefon/tablette ekran düğmeleri; T ile bilgisayarda dokunmatik önizleme.
- Tek aktif coin vardır. Her yeni coin bir öncekinin üst katında doğar; yerde coin oluşmaz.
- Kat ve coin sayısında sınır yoktur. Basamaklar gerçek dünya yüksekliğinde üretilir, kamera yukarı ilerler.
- Her 35 katta bölge değişir: Kül Mezarlığı, Yitik Bulutlar, Fırtına Geçidi, Kızıl Kökler, Yıldız Külleri. Bölgeler daha yüksek irtifalarda tekrar eder.
- 2X (turuncu çift ok) 10 saniye boyunca tek ek hava zıplaması; mıknatıs (mor nal) 7,5 saniye coin çeker. Etkinken karakterin çevresinde renkli aura, mıknatısta çekim alanını gösteren dalga görünür; son 3 saniyede aura yanıp söner. Bulut/fırtına bölgesinde yukarı hava akımı vardır.

## Bölüm haritası

- **OYNA** kıvrımlı bir harita açar: 100 bölüm, 1. bölüm en altta. Bir bölüm ancak öncekini bitirince açılır; ilerleme ve yıldızlar cihazda saklanır.
- Hedefler: **kata çık** (o katta platformla birlikte kayan bir kapıdan geçmek gerekir, ona ulaşırken düşülebilir) ve **coin topla**. 100. bölüm en yüksek kat hedefidir; bitince oyun tamamlanır.
- Yıldızlar: 1 = bölümü bitir, 2 = ikinci süre sınırı içinde bitir, 3 = daha sıkı süre içinde bitir. Her 5. bölüm (100 hariç) süreli: süre dolarsa bölüm kaybedilir.
- Eski sonsuz mod haritanın üstündeki **SONSUZ MOD** düğmesiyle sürer. Bölüm kuralları `Assets/Scripts/LevelDefs.cs` içindedir.

## Kaynaklar ve derleme

`Assets`, `.meta` dosyaları, `Packages` ve `ProjectSettings` Unity kaynak projesidir. Unity Hub üzerinden depo klasörünü açın. Kayıtlı sahne doğrudan kullanılabilir; eski sahne kurucularını çalıştırmak gerekli değildir.

`web/` Unity WebGL çıktısıdır. `vercel.json` bu klasörü statik olarak yayınlar; Vercel üzerinde Unity derlemesi çalıştırılmaz. Oynanış değişikliğinden sonra Unity'den yeni WebGL çıktısı alınmalıdır. Bir HTTP sunucusu üzerinden test edin; index dosyasını file:// ile açmayın.

Android/iOS hazırlığı ve dokunmatik kontroller kaynaklarda bulunur. Gerçek telefon/tablet testleri ayrı yapılmalıdır. iOS son uygulama derlemesi ve imzalama macOS/Xcode gerektirir.

`docs/` hikâye ve geliştirme planını içerir. Bunlar oynanabilir uygulama arşivlerine eklenmez.

## Cihaza yükleme ve çevrimdışı oynama (PWA)

WebGL derlemesi yüklenebilir uygulama (PWA) olarak üretilir: `manifest.webmanifest`, `sw.js` ve ikonlar `web/` içine otomatik yazılır (`Assets/Editor/WebPwaExport.cs`; `ReleaseBuildQueue` WebGL derlemesinin sonunda çalıştırır). Mevcut bir `web/` klasörüne eklemek için Unity menüsünden **Cihazlar ▸ Web Klasorune PWA Ekle**.

Kullanım (Vercel adresi HTTPS olmalı):

- Bir kez internetle aç; oyun dosyaları cihaza önbelleklenir ve "çevrimdışı oynanmaya hazır" bildirimi çıkar. Sonrasında internetsiz açılır.
- Android/Chrome ve bilgisayarda Chrome/Edge: ekrandaki **Cihaza yükle** düğmesi veya adres çubuğundaki yükle simgesi.
- iPhone/iPad (Safari): Paylaş ▸ **Ana Ekrana Ekle**.
- Yeni derleme yayınlanınca `sw.js` sürümü değişir; cihaz bir sonraki çevrimiçi açılışta güncellenir.

## Son oturum durumu

Oynanabilir Windows sürümü hazır. Tarayıcı çıktısının ikinci derlemesi başlatıldı; henüz tarayıcıda doğrulanıp depoya eklenmedi. **Vercel yayını için önce `docs/DEVAM_NOKTASI.md` içindeki WebGL adımı tamamlanmalı.** Mevcut `vercel.json` hazır çıktıyı yayınlamak içindir; Unity projesini sunucuda derlemez.
