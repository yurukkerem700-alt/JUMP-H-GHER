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
- Her 20 katta bölge değişir: Kül Mezarlığı, Yitik Bulutlar, Fırtına Geçidi, Kızıl Kökler, Yıldız Külleri. Bölgeler daha yüksek irtifalarda tekrar eder.
- 2X güçlendirmesi 20 saniye boyunca tek ek hava zıplaması; M güçlendirmesi 15 saniye mıknatıs verir. Bulut/fırtına bölgesinde yukarı hava akımı vardır.

## Kaynaklar ve derleme

`Assets`, `.meta` dosyaları, `Packages` ve `ProjectSettings` Unity kaynak projesidir. Unity Hub üzerinden depo klasörünü açın. Kayıtlı sahne doğrudan kullanılabilir; eski sahne kurucularını çalıştırmak gerekli değildir.

`web/` Unity WebGL çıktısıdır. `vercel.json` bu klasörü statik olarak yayınlar; Vercel üzerinde Unity derlemesi çalıştırılmaz. Oynanış değişikliğinden sonra Unity'den yeni WebGL çıktısı alınmalıdır. Bir HTTP sunucusu üzerinden test edin; index dosyasını file:// ile açmayın.

Android/iOS hazırlığı ve dokunmatik kontroller kaynaklarda bulunur. Gerçek telefon/tablet testleri ayrı yapılmalıdır. iOS son uygulama derlemesi ve imzalama macOS/Xcode gerektirir.

`docs/` hikâye ve geliştirme planını içerir. Bunlar oynanabilir uygulama arşivlerine eklenmez.

## Son oturum durumu

Oynanabilir Windows sürümü hazır. Tarayıcı çıktısının ikinci derlemesi başlatıldı; henüz tarayıcıda doğrulanıp depoya eklenmedi. **Vercel yayını için önce `docs/DEVAM_NOKTASI.md` içindeki WebGL adımı tamamlanmalı.** Mevcut `vercel.json` hazır çıktıyı yayınlamak içindir; Unity projesini sunucuda derlemez.
