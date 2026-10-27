# Masaüstü Claude'a yapıştırılacak istem (WebGL derleme + yayın)

Aşağıdaki kutudaki metni olduğu gibi yapıştır.

```
Görev: Jump Higher oyununun WebGL çıktısını derleyip GitHub main dalına gönder. Gereksiz keşif yapma, aşağıdaki adımları sırayla uygula; her adımı tek komutla yap, uzun açıklama yazma. Cevapları Türkçe ver.

Yollar:
- Git deposu: C:\Users\Acer\Documents\Codex\2026-10-26\un\work\jump-higher-repo
- Unity projesi: C:\Users\Acer\unityhub-1\2d araba oyunu unity\arabaoyunu
- Unity CLI: C:\Program Files\Unity Hub\resources\unity.exe

1. Depoda `git pull origin main` çalıştır (uzak depo yurukkerem700-alt/JUMP-H-GHER, dal main).
2. Depodan Unity projesine şu dosya/klasörleri kopyala (üzerine yaz): Assets\Scripts\*, Assets\Editor\*, Assets\WebTemplates\*. Kopyaladıktan sonra Unity'nin yeniden derlemesini bekle (Unity açık değilse aç).
3. Unity projesinde `Library\ReleaseWebOutput.path` dosyasına depo içindeki web klasörünü yaz: C:\Users\Acer\Documents\Codex\2026-10-26\un\work\jump-higher-repo\web
4. `Library\ReleaseBuild.stage` dosyasının içeriğini `web-switch` yap (Windows derlemesini atlar). Unity otomatik olarak WebGL'e geçip derleyecek.
5. `Library\ReleaseBuild.stage` içeriği `done` olana kadar bekle (arada 30 sn aralıkla kontrol et). `FAILED` yazarsa dosyadaki hatayı ve Library\Release-WebGL.txt'yi bana göster ve dur.
6. Çıktıda web\index.html, web\sw.js, web\manifest.webmanifest ve web\Build\ altında .wasm/.data/.framework.js/.loader.js olduğunu doğrula. 100 MB'tan büyük dosya varsa GitHub reddeder: bana söyle ve dur.
7. Yerel test: web klasöründe `python -m http.server 8765` çalıştır, tarayıcıda http://localhost:8765 aç, menünün geldiğini ve Oyna ile karakterin hareket ettiğini doğrula (mümkün değilse en azından index.html'in 200 döndüğünü ve Build dosyalarının yüklendiğini kontrol et).
8. Depoda: `git add web`, commit mesajı "Add WebGL build", `git push origin main`. Yeni dal veya pull request açma. Library, Temp, Builds klasörlerini ekleme.
9. Vercel kendiliğinden yayınlar. Bitince bana şunu söyle: "Push tamam, Vercel yayını birkaç dakikada hazır olur."
```
