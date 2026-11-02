# Masaüstü Claude'a yapıştırılacak istem (oyun hissi sürümü: WebGL derleme + test + yayın)

```
Görev: Jump Higher'ın `claude/admiring-fermi-lg3e3c` dalındaki oyun hissi değişikliklerini Unity'de derleyip test et, WebGL çıktısını üret ve aynı dala gönder. Gereksiz keşif yapma, sırayla uygula, Türkçe cevap ver. Bir adım başarısız olursa dur ve hatayı göster; hatayı geçiştirip ilerleme.

Yollar:
- Git deposu: C:\Users\Acer\Documents\Codex\2026-10-26\un\work\jump-higher-repo
- Unity projesi: C:\Users\Acer\unityhub-1\2d araba oyunu unity\arabaoyunu
- Unity CLI: C:\Program Files\Unity Hub\resources\unity.exe

1. Depoda: git fetch origin claude/admiring-fermi-lg3e3c ; git checkout claude/admiring-fermi-lg3e3c ; git pull origin claude/admiring-fermi-lg3e3c
2. Zıplama sabitleri testi: python tools\test_jump_physics.py (7 kontrolün hepsi PASS olmalı).
3. Depodan Unity projesine kopyala (üzerine yaz, .meta dosyalarıyla birlikte): Assets\Scripts\*, Assets\Editor\*, Assets\WebTemplates\*. Özellikle yeni Assets\Scripts\Juice.cs ve Juice.cs.meta gelmeli. Unity'nin yeniden derlemesini bekle.
4. Konsolda DERLEME HATASI var mı bak (Console / Editor.log). Hata varsa (özellikle PlayerController, PlayerVisual2D, PlayerHealth, GameInput, TouchControlButton, Juice içinde) dosya ve satırıyla göster ve DUR. Bu kod Unity'de hiç derlenmedi.
5. Oynanış testi (Unity'de Play ile): sahneyi Assets\Scenes\SampleScene.unity aç, Oyna'ya girip 1. bölümde şunları dene ve her biri için geçti/kaldı yaz:
   a) Basamaktan basamağa zıplayıp kat çıkılıyor (tam basılı ve hızlı dokunuş ikisi de).
   b) Platform kenarından ayrıldıktan hemen sonra zıplanabiliyor (coyote).
   c) Yere inmeden hemen önce basılan zıplama inince uygulanıyor.
   d) Tuşu erken bırakınca zıplama biraz kısalıyor.
   e) İniş ve zıplamada toz, coin alınca altın kıvılcım, hasar alınca kırmızı patlama + kısa sarsıntı görünüyor; sarsıntıdan sonra kamera kayık kalmıyor.
   f) Karakter zıplarken uzuyor, inerken ezilip normale dönüyor; kostüm değişince de çalışıyor.
   g) Duraklat/devam, R ile yeniden başlat ve zemine düşüp can kaybı normal çalışıyor.
   h) Dokunmatik önizleme (T tuşu): ZIPLA düğmesi ve joystick yukarı zıplatıyor.
   Herhangi biri kalırsa DUR ve bana bildir.
6. Unity projesinde Library\ReleaseWebOutput.path dosyasına şunu yaz: C:\Users\Acer\Documents\Codex\2026-10-26\un\work\jump-higher-repo\web
7. Library\ReleaseBuild.stage içeriğini `web-switch` yap. Unity WebGL'e geçip derler. İçerik `done` olana kadar 30 sn aralıkla bekle. `FAILED` olursa Library\Release-WebGL.txt'yi göster ve dur.
8. Çıktıyı doğrula: web\index.html, web\sw.js, web\manifest.webmanifest, web\Build\ altında .wasm/.data/.framework.js/.loader.js. 100 MB'tan büyük dosya varsa DUR.
9. Yerel test: web klasöründe `python -m http.server 8765`, tarayıcıda http://localhost:8765 aç; menü gelmeli, Oyna ile karakter zıplayıp hareket etmeli. Tarayıcı konsolunda kırmızı hata olmamalı.
10. Depoda: git add web ; git commit -m "WebGL: oyun hissi sürümü" ; git push origin claude/admiring-fermi-lg3e3c. Library, Temp, Builds klasörlerini ekleme. `main`'e PUSH ETME ve pull request açma.
11. Vercel bu dal için önizleme yayını oluşturur. Bitince bana test sonuçlarını (5. adımdaki a-h) ve "push tamam" yaz. main'e birleştirme kararını ben vereceğim.
```
