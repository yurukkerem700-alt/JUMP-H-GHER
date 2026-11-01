# Biyomlu harita (v3) – Masaüstü Claude'a yapıştırılacak istem

Bu istem, bulut oturumunda yazılan harita kodunu (dal `claude/great-ritchie-wgegh9`) gerçek Unity'de derler,
görselleri ekler, ekran görüntüsüyle doğrular, WebGL'i yeniden derler ve main'e gönderir.
Bulut oturumu Unity'yi çalıştıramadığı ve Drive görsellerini indiremediği için bu adımlar bilgisayarda yapılır.

```
Görev: Jump Higher için biyomlu bölüm haritasını (v3) Unity'de derle, doğrula, WebGL'i yeniden derle ve main'e gönder. Cevapları Türkçe ver, uzun açıklama yazma. Pull request açma.

Yollar:
- Git deposu: C:\Users\Acer\Documents\Codex\2026-10-26\un\work\jump-higher-repo
- Unity projesi: C:\Users\Acer\unityhub-1\2d araba oyunu unity\arabaoyunu
- Unity CLI: C:\Program Files\Unity Hub\resources\unity.exe
- Harita görselleri: biyomlu-bolum-haritasi-v3 klasörünün assets alt klasörü (6 .webp: biome-forest, biome-ruins, biome-crystal, biome-ember, biome-ice, biome-sky). Google Drive'dan veya bilgisayarımda indirilmiş ZIP'ten bul; bulamazsan bana yolu sor.

1. Depoda `git fetch origin` yap, `git checkout main`, `git pull origin main`, sonra `git merge origin/claude/great-ritchie-wgegh9` (çakışma çıkarsa çöz).
2. Görselleri PNG'ye çevir (Unity .webp okumaz): `python -m pip install pillow`, sonra her biome-<ad>.webp için
   Assets\Resources\UI\Biomes\<ad>.png üret (ad: forest, ruins, crystal, ember, ice, sky). En uzun kenar 2048'i geçiyorsa 2048'e küçült.
3. Kodu Unity projesine kopyala (üzerine yaz): Assets\Scripts\*, Assets\Editor\*, Assets\Resources\*, Assets\WebTemplates\*.
   Unity'nin yeniden derlemesini bekle. Derleme hatası varsa (Editor.log) düzeltip depoya da uygula; çözemezsen hatayı bana göster ve dur.
4. Haritayı doğrula: `SHOT_OUT=C:\temp\shots` ayarlayıp
   `unity.exe -batchmode -projectPath <proje> -executeMethod MapShot.Run -quit` çalıştır (-nographics OLMADAN).
   Çıkan harita.png, harita-gecis.png, kart.png, ayarlar.png dosyalarına bak: biyom görselleri görünüyor mu, madalyonlar yolun üstünde mi,
   biyom geçişinde sis var mı, yazılar taşmıyor mu? Sorun varsa Assets\Scripts\LevelMap.cs'i düzelt ve tekrarla.
5. WebGL derle ve yayına hazırla: docs\WEB_DERLEME_ISTEMI.md'deki 3.–7. adımları aynen uygula
   (ReleaseWebOutput.path, ReleaseBuild.stage = web-switch, done bekle, dosya ve 100 MB kontrolü, yerel test).
   Yerel testte menüden BÖLÜM HARİTASI'nı aç: kaydırma, biyom adı, bölüme dokununca kart ve BAŞLA çalışmalı. Bilgisayarda joystick/butonlar görünmemeli.
6. Depoda `git add -A` (Library, Temp, Builds hariç), commit "Biyomlu harita v3 + WebGL yeniden derlendi", `git push origin main`.
   Yeni dal veya pull request açma. Vercel kendiliğinden yayınlar.
7. Bitince şunu söyle: "Push tamam, Vercel yayını birkaç dakikada hazır olur." ve doğrulama ekran görüntülerinin yolunu yaz.
```
