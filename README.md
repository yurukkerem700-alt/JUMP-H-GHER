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

## Can ve düşmanlar

- Üç canlık can barı (sol üstte kalpler). Düşman darbesi 1 can götürür, ardından 1,6 sn dokunulmazlık vardır. Zemine kadar düşmek de 1 can götürür ve başlangıca döndürür. Can bitince bölüm kaybedilir (sonsuz modda koşu yeniden başlar).
- **Zombi**: aşağıdan gelir, üstteki basamağın kenarına zıplayıp asılır, tırmanır, sonra oyuncuya yürür. **Goblin**: oyuncu gibi basamaktan basamağa doğrudan zıplar, ayakta hızlı koşar. Ekranın altında kalan düşman altta kırmızı bir noktayla gösterilir. Düşmanlar bölüm başladıktan 6 sn sonra, oyuncu 4. kata çıkınca belirir.
- Dağılım (`LevelDefs.AssignEnemies`): 1–30 düşmansız; 31–50 yalnız zombi (31–40: 1, 41–50: 2); 51–70 yalnız goblin (51–60: 1, 61–70: 2); 71–99 dört bölümlük döngü — 4'e bölünenler 3 zombi, 4k+2'ler 3 goblin, 4k+1'ler zombi + goblin, 4k+3'ler goblin + zombi (85'ten sonra tek tür 2'ye çıkar); 100. bölüm 2 zombi + 2 goblin. Düşman hızı 31. bölümden itibaren %1 artar (en çok %50).
- Düşman sprite'ları Mixamo FBX modellerinden üretilir (`Assets/Editor/EnemyBake.cs`, menü: Cihazlar ▸ Düşman Sprite Üret). Kaynak FBX'ler depoda değildir (`ENEMY_FBX_DIR` ya da Downloads klasörü); üretilen küçük sprite sayfaları `Assets/Resources/Enemies/` içindedir.

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
