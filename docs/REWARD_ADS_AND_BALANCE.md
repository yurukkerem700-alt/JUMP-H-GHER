# Kök Yankısı — tur devamı ve zorluk

## Tur devamı

- Her bölüm veya sonsuz turda reklam izleyerek bir kez devam edilebilir.
- Devam noktası oyuncunun ulaştığı en yüksek güvenli basamaktır; skor, coin ve güçlendirmeler korunur.
- Reklamdan sonra en az 1 can ve 2,4 saniye dokunulmazlık verilir. Süreli bölümde 5 saniye süre bedeli uygulanır ve oyuncuya 8 saniyelik görünür ek süre tanınır.
- Devam düğmesi yalnızca Google Ad Placement API `beforeReward` ile uygun reklam bulunduğunda açılır. Ödül `adViewed` sonrasında verilir. Reklam yoksa oyuncu tekrar arayabilir veya bölümü yeniden başlatabilir.

## Bölge ve tehdit ritmi

- Her bölge 20 kat sürer; böylece 100 katlık kampanyada beş bölgenin tamamı görünür.
- İlk yedi bölüm düşmansızdır. Sekizinci bölümden başlayarak tek, yavaş yaratık eklenir; 56. bölümden itibaren çoğunlukla iki, ileri bölümlerde ara sıra üç yaratık görülür.
- Yaratık türü bulunduğu bölgeye göre belirlenir: Kül Kargası, Bulut Güvesi, Fırtına Sülüğü, Kök Pusucusu ve Yıldız Cini.

## Reklam kurulumu

WebGL dışa aktarımı sırasında `ADSENSE_CLIENT_ID` ortam değişkenine AdSense yayıncı kimliğini (`ca-pub-...`) ver. Unity editörünü bu değişken ayarlı bir süreçten başlat; `WebPwaExport` kimlik geçerliyse Ad Placement API etiketini üretir.

Geliştirme derlemesinde `ADSENSE_TEST_MODE=1` ayarlanırsa Google'ın test reklam modu açılır. Yayın derlemesinde bu değişkeni ayarlama. Yayıncı kimliği yoksa reklam betiği eklenmez; devam düğmesi reklam hazır olduğunda sunulmaz ve tur yeniden başlatılabilir.

Unity WebGL için Google Ad Placement API kullanılır; her ödül reklamı `beforeReward`, `adViewed`, `adDismissed` ve `adBreakDone` geri çağrılarıyla yönetilir. [Google entegrasyon örneği](https://developers.google.com/ad-placement/docs/example) ve [test modu](https://developers.google.com/ad-placement/docs/test).
