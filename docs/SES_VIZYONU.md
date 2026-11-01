# Kök Yankısı — Ses Vizyonu

Amaç: ses, görsel ve karakterler tek bir dünyaya ait hissettirsin. Dünya: sisli, elle boyanmış, sıcak kahve-turuncu bir mezarlık ve kök ağı. Hafif ürpertici, ama gore yok; korku "tekinsizlik" ile verilir, şiddetle değil.

## Şu an ne var (Kasım 2026 durumu)

- Ses Atölyesi (kodla üretim): adım, zıplama/iniş, çarpma, coin, güç alma, darbe (can gitme), swoosh, arayüz tıkı. Ortam: rüzgar, yağmur, yaprak, gece böcekleri (`SesWorld.cs`, bölgeye göre karışım).
- `EnemySfx.cs`: Zombi ve Goblin için 6 ses (doğuş, saldırı, acı).
- Müzik: YOK. Ana menü sesi, bölüm geçme sesi, kuzgun/mezar zombisi sesi YOK.
- Tekme sesi ve düşman bölümlerine özel sesler ayrı değil (Swoosh ve Hit paylaşılıyor).

## Ses paleti (tek kural: "toprak, kemik, rüzgar, uzak metal")

| Doku | Ne için | Duygu |
|---|---|---|
| Alçak çerçeve davulu (daf/bendir, 50-70 Hz gövde) | nabız, tekme darbesi, kök kıpırtısı | yerin altından gelen kalp atışı |
| Kemik / kuru ahşap tıkırtısı | tekme, Karakoncolos adımı, kemik detayı | kuru, tekinsiz |
| Rüzgar (kısık, alçak geçiren) | sürekli zemin | yalnızlık |
| Kök gıcırtısı (ahşap gerilmesi, yavaş) | platform hareketi, bölge geçişi | canlı toprak |
| Uzak çan (inharmonik, 5-7 sn kuyruk) | bölüm geçme, müzikte seyrek vurgu | yas, yankı |
| Ney / kaval (nefesli, hafif vibrato, çok seyrek) | ana ezgi, ödül anı | Anadolu dokusu, ince |
| Uzun pedler (sinüs + detune) | müziğin zemini | sis |
| Cam gibi ince çınlama | ruh ışığı (ateşböceği) | umut, sıcak ışık |

Kaçınılacaklar: elektrik gitarı, keskin sentezleyici lead, gerçek çığlık, kan/et sesi, 4/4 EDM vuruşu, parlak majör akorlar.

Karışım kuralları: tüm SFX tepe -3 dB, müzik SFX'in 8-12 dB altında; coin ve zıplama her zaman anlaşılır kalmalı; düşman sesleri ekran dışındayken kısık.

## Müzik yönü

- Tonalite: La Frigyen (A-Bb-C-D-E-F-G). Bb (b2) tekinsizliği verir. Akor yürüyüşü: Am - Bb - Am - G.
- Tempo: 60 BPM (menü, sis); oyun içi 72-80 BPM'e çıkabilir. Ölçü 4/4 ama vurgu "1 ... 3.5 4.25" kaydırılmış (daf hissi).
- Katmanlar: (1) alçak drone La1 + La2, (2) 24 sn'lik yavaş akor pedleri, (3) seyrek çerçeve davulu, (4) 4 adet uzak çan, (5) ney ezgisi (10 nota, Frigyen), (6) rüzgar.
- Uzunluk: 30-60 sn, dikişsiz döngü (taslak 48 sn, üretildi).
- Bölgeye göre varyasyon önerisi (sonra): Kül Mezarlığı = tam taslak; Yitik Bulutlar = davulsuz, ney önde; Fırtına Geçidi = davul + rüzgar yüksek; Kızıl Kökler = alçak ve sıcak, çan çok; Yıldız Külleri = sadece ped + ruh ışığı çınlaması.
- Dinamik: kat arttıkça davul katmanı (ayrı stem olarak) açılır; can 1 kalınca yalnızca drone + kalp atışı.

## Yeni canlılar için ses listesi

| Canlı / olay | Ses | Dokular | Durum |
|---|---|---|---|
| Karakoncolos (tırmanan) | hırıltı, tüy hışırtısı, pençe tırmalaması | alçak testere dalgası + formant, bandlı gürültü, kayaya tırmalama | ÜRETİLDİ: `karakoncolos_hirilti/tuy/pence` |
| Kara Kurt (zıplayan) | kısa uluma, pati | gırtlak sinüsü sweep, alçak darbe | ÜRETİLDİ: `karakurt_uluma/pati` (+ CC0 `wolf_monster_6` katman) |
| Baykuş | uhu-uhu | iki-üç hece, 380-400 Hz, yumuşak | ÜRETİLDİ: `baykus` |
| Yarasa | yüksek cıvıltı + kanat | 5 kHz sweep, kısa | ÜRETİLDİ: `yarasa` (+ CC0 `bat_01-03`) |
| Kara kedi | pesli miyav | testere + formant | ÜRETİLDİ: `karakedi` (+ CC0 `Meow`, `cat_softmew`) |
| Ruh ışığı (ateşböceği) | hafif çınlama | 1318 Hz + üst harmonikler | ÜRETİLDİ: `ruh_isigi` |
| Jeton | iki tonlu, yumuşak metalik "tink" | 988 + 1480 Hz | ÜRETİLDİ: `jeton` |
| Zıplama | kısa yükselen nefes | sweep 180-420 Hz | ÜRETİLDİ: `ziplama` |
| Tekme | gövde darbesi + tıkırtı | düşen sinüs + bandlı gürültü (+ CC0 kemik tıkırtısı) | ÜRETİLDİ: `tekme` |
| Can gitme | düşen, boğuk | iki oktav sweep + 60 Hz | ÜRETİLDİ: `can_gitme` |
| Bölüm geçme | La minör arpej, çan gibi | 5 nota, uzun kuyruk | ÜRETİLDİ: `bolum_gecme` |
| Menü | alçak, sıcak çınlama | Mi3 + oktav | ÜRETİLDİ: `menu_ac` |
| Zombi, Goblin | mevcut `EnemySfx.cs` | formant sentezi | VAR; paleti bozmuyor |
| Kuzgun | (öneri) kanat + kısık "kraa" | CC0 `wings_flap_large` + EnemySfx tarzı kısa hece | EKSİK |

## Telif politikası

Yalnızca CC0 / kamu malı. CC-BY kaynaklar kullanılmaz (atıf yükü). Kod ile üretilen sesler projenin kendi eseridir. Kayıtlar `ses-adaylari/KAYIT_LISANSLAR.json` içinde; proje dışında bekliyor, oyuna girmeden önce kullanıcı onayı gerekir.

## Entegrasyon notu

Mevcut sistem `Ses.Override` ile kayıt desteği veriyor, ama `EnemySfx` yeni canlılara genişletilmedi. Uygulama sırası: (1) ambient döngüyü menü + oyun için tek AudioSource ile `Master*Music` ayarına bağla, (2) jeton/zıplama/tekme/can gitme için `Ses.Override`, (3) yeni canlıları `EnemySound` listesine ekle. Bunlar kod değişikliği olduğundan başka birinin işi bitince yapılmalı.
