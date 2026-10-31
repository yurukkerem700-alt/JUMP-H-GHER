# Kök Yankısı — mağaza yayını (sürüm 1.0.0, kod 32)
Unity 6000.0.84f1. Bu ortamda Unity yok; derlemeler **sizin Unity makinenizde** veya CI'da alınır.

## Derleme
- Menü **Cihazlar ▸ Magaza ▸ Android AAB / iOS Xcode Projesi / Windows (Steam)** veya:
  `Unity -batchmode -quit -projectPath . -executeMethod StoreRelease.Android` (`.SteamWindows`, `.IOS`). Çıktı `Builds/` (`STORE_OUT` ile değişir).
- Android imzası ortam değişkenleri: `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`. Anahtar dosyasını depoya koymayın.
- CI (isteğe bağlı): `.github/workflows/unity-store.yml` (game-ci). Secret'lar: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
- WebGL (Vercel): mevcut `web/` 27 Ekim sesli derlemedir ve yayındadır. Oynanış değişirse Unity'den yeniden WebGL alıp `tools/postprocess_web.py` + PWA adımını çalıştırın.

## Hesaplar (benim yapamayacağım adımlar)
1. **Google Play** Console (25 USD): AAB yükle, Play App Signing, içerik derecelendirme anketi, veri güvenliği = veri toplanmıyor, gizlilik URL'si. Yeni kişisel hesapta 12 test kullanıcısı/14 gün kapalı test gerekir.
2. **App Store**: Apple Developer (99 USD/yıl). `IOS` ile Xcode projesi üret → macOS'ta Archive → App Store Connect. `APPLE_TEAM_ID` ortam değişkeni otomatik imzayı açar.
3. **Steam**: Steamworks (Steam Direct ücreti). `SteamWindows` çıktısını depot olarak `steamcmd` ile yükle (AppID/DepotID sizde). Kapsül görselleri gerekir.
4. Düşman sprite'ları Mixamo FBX'lerinden üretilmiştir; Mixamo koşulları ticari kullanıma izin verir ama kaynak FBX'ler depoda değildir — lisans kaydınızı saklayın.
