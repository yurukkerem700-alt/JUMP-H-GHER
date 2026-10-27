# Kök Yankısı

## Belgenin amacı

Bu dosya geliştirme ve GitHub üzerinden devam etme planıdır. Oyunun EXE/RAR paketinin parçası değildir. Mobil sürüm, sonsuz dikey ilerleme ve değişen kat temaları bu belgede gelecek çalışma olarak tanımlanır; mevcut sürümde tamamlanmış özellikler olarak sunulmaz.

İsim, toprağın altından gelen sesleri ve coin toplarken ortaya çıkan geçmiş izlerini birleştirir. Kısa ve karanlık atmosfere uygundur. Mağaza ve marka adı uygunluğu henüz araştırılmamıştır; yayın öncesinde kontrol edilmelidir.

## Oyunun özü

Hareketli toprak ve taş parçalarının üzerinden yükselen oyuncu, köklerin sakladığı coinleri toplar. Hedef mümkün olduğunca çok coin toplamak ve ileride mümkün olduğunca yükseğe çıkmaktır. Sabit bir coin sayısında oyun bitmez. Hareket, kısa zıplama ve doğru basamağı seçme temel becerilerdir; ortadan zıplama zorunluluğu yoktur.

Görsel dil: yandan görülen 2D, belirgin siluetler, yoğun toprak renkleri, koyu taşlar, yer altına uzanan kökler ve sınırlı kırmızı ışık. Oyuncu, platform ve coin arka plandan kolayca ayrılmalıdır.

## Mevcut teslim sürümü

- Windows EXE, Unity 6.0.84f1, C#, Rigidbody2D ve TextMeshPro.
- Kare gövdeli, yuvarlak kafalı karakter.
- Zıt fazlarda sağa-sola hareket eden, altından geçilebilen platformlar.
- Platform üzerinde taşınma, zeminde veya platformda herhangi bir yatay konumdan zıplama.
- A/D veya yön tuşlarıyla hareket; W veya Boşluk ile zıplama; R ile yeniden başlatma.
- Havada tekrar zıplama yok; kısa ve hızlı bir zıplama yayı korunur.
- Aynı anda bir coin: toplanmadan yeni konuma geçmez.
- Zeminde rastgele coin, hareketli platforma bağlı coin ve havada sabit coin yerleşimleri.
- Sınırsız coin sayacı; yedi coin sınırı, merkez çizgisi ve zafer ekranı yok.
- Mezarlık teması ve zemin altına dallanarak uzanan kökler.

Mevcut arena sınırlı yüksekliktedir. Aşağıdaki sonsuz yükseliş, kat geçişleri, mobil kontroller, anlatı, kayıt ve yeni efektler sonraki geliştirmelerdir. Coin sayacının sınırsız olması, mevcut arenanın sonsuz kat ürettiği anlamına gelmez.

## Hikâye kurgusu

Eski bir mezarlığın altındaki kök ağı, unutulan anıların yankılarını saklar. Yerin içinden yükselen taş parçaları, bu ağın yüzeye çıkardığı kırılmış yolların parçalarıdır. Oyuncu, ismini ve geçmişini hatırlamayan küçük bir yolcudur. Topladığı coinler yalnızca para değildir; köklerin tuttuğu anı izlerini uyandıran eski bakır sikkelerdir.

Her yeni bölge, yolcunun neden bu ağın içinde olduğunu açıklayan küçük bir görsel veya ses ipucu bırakır. Ana gizem: kökler insanları hapsetmek için mi büyümüştür, yoksa geride kalan anıları korumak için mi?

Anlatım uzun konuşmalarla oyunu kesmez. İlk aşamada çevresel hikâye tercih edilir: aşınmış taş kabartmaları, değişen kök desenleri, uzakta görünen yapılar, kısa yankı sesleri. Günlük veya anı parçaları ileride isteğe bağlı bir menüden okunabilir. Ana toplama nesnesinin ekrandaki adı COIN kalır.

## Gelecekteki oyun döngüsü

1. Oyuncu bir basamağa ulaşır, hareketi okur ve uygun anda zıplar.
2. Mevcut coin, güvenli biçimde ulaşılabilir bir noktada bekler.
3. Coin alınınca yenisi, oyuncunun mevcut yüksekliği ve ulaşılabilir platform ağına göre oluşturulur.
4. Yükseldikçe kamera yukarıyı takip eder ve üstte yeni kat parçaları hazırlanır.
5. Belli kat eşiklerinde tema değişir; çekirdek hareket kuralları korunur.
6. Düşüş ve yeniden deneme sistemi, mevcut koşu ile en iyi dereceyi ayrı tutar.

Kat bilgisi, coin sayısı ve yükseklik birbirinden ayrı ölçülmelidir. Örneğin coin toplamak katı otomatik artırmamalı; kat, fiziksel yükselişle ilerlemelidir.

## Bölge ve tema önerileri

Eşikler dengeleme için başlangıç önerisidir; mevcut oyuna uygulanmış değerler değildir.

### Kat 0–19: Sessiz Mezarlık

Yanmış kahverengiler, koyu zeytin ve kızıl kökler. Eğimli mezar taşları, kırık çitler ve küçük mumlar. Basamak hareketi burada öğretilir. Efektler: hafif toz, mum titreşimi, seyrek kızıl kök nabzı. Sesler: hafif rüzgâr, kuru taş ve kök gıcırtısı.

### Kat 20–39: Gömülü Mahzen

Koyu morumsu taş ve soluk mantar ışıkları. Kemerler ve eski nişler. Yeni mekanik önerisi: yavaşça yükselip alçalan birkaç platform; temel sağa-sola platformlar devam eder. Efektler: ince toz zerrecikleri ve küçük damlalar.

### Kat 40–59: Paslı Damarlar

Pas, bakır ve kömür tonlarında terk edilmiş maden izleri. Kökler, eski metal parçaları çevreler. Yeni mekanik önerisi: kısa süre sonra hareket yönü değişen platformlar; değişim önceden ışık ve sesle anlaşılır. Efektler: az sayıda kıvılcım ve taş kırıntısı.

### Kat 60–79: Unutulmuş Koruluk

Koyu yeşil, petrol ve loş turkuaz. Taşın içinden çıkan kökler ve sarkan yapraklar. Yeni mekanik önerisi: hafif salınan basamaklar. Efektler: yaprak sürüklenmesi, birkaç ateş böceği ve yumuşak sis katmanı.

### Kat 80–99: Kül Sarayı

Kül grisi, soğuk mavi ve eski altın detayları. Yıkılmış sütunlar ve açık gökyüzü siluetleri. Yeni mekanik önerisi: daha uzun sıçrama zincirleri; okunabilirlik, zorluktan önce gelir. Efektler: kül taneleri, kontrollü rüzgâr ve uzaktan gelen yankılar.

### Kat 100 ve sonrası

Bölgeler daha gelişmiş düzenlerle yeniden karışabilir. Sonsuz içerik, platform hızını sınırsız artırmak anlamına gelmez. Zorluk için güvenli üst sınırlar, nefes alma katları ve erişilebilir coin yerleşimleri gerekir. İleride yeni bölgeler veri dosyaları üzerinden eklenebilir.

## Animasyon ve efekt yaklaşımı

- Karakter: bekleme, yürüme, kalkış, havada yükselme, düşüş ve iniş durumları. İnişte küçük esneme ve kısa toz etkisi.
- Platform: köklerde yavaş ışık nabzı; hareket yönü değişirken küçük toprak dökülmesi. Efektler çarpışma sınırını gizlememeli.
- Coin: hafif salınım veya ince dönüş, toplanınca kısa parıltı ve sade ses. Uzun süreli ekran kaplayan patlama kullanılmamalı.
- Tema geçişi: arka planın ve ışık paletinin kısa geçişle değişmesi; fizik aynı karede keskin biçimde değişmemeli.
- Arka plan: ileride 2–3 parallax katmanı. En hızlı katman bile oyuncunun konumunu okumayı zorlaştırmamalı.
- Mobilde düşük efekt seçeneği, sınırlı parçacık sayısı ve azaltılmış hareket seçeneği düşünülmeli.

## Sonsuz yükselişin teknik planı

### Önce içerik sistemi

Tek tek sahneye basamak eklemek yerine tekrar kullanılabilir kat parçaları hazırlanmalı. Her parça; giriş yüksekliği, çıkış yüksekliği, platform hareket aralıkları, coin aday noktaları ve tema kimliği taşımalı. Kat parçaları ve coin efektleri nesne havuzundan kullanılmalı.

### Sonra erişilebilirlik

Üreteç, zıplama yüksekliği ve havada kalma süresinden yatay erişimi hesaplamalı. Hareketli platformların yalnızca o anki konumu değil, iniş anındaki tahmini konumu da hesaba katılmalı. Her parçada en az bir geçerli rota olmalı. Rastgele coin, kapalı taşın içinde, ekran dışında veya erişilemeyen bir boşlukta doğmamalı.

### Kamera ve geride kalan coin

Kamera, yukarı ilerlemeyi takip etmeli; küçük inişlerde titrememeli. Mevcut coin toplanmadan yenisi çıkmama kuralı korunacaksa kamera o coin'i erişilemez biçimde aşağıda bırakmamalı. Öneri: coin alınmadan bir sonraki kamera bölgesini kalıcı olarak kapatmamak. Bu kural, sonsuz yükseliş eklenmeden önce prototipte test edilmeli.

### Tema verileri

ScriptableObject tabanlı tema tanımı; arka plan, platform görseli, coin paleti, müzik, efekt seti ve zorluk aralığını taşıyabilir. Fizik ve skor kodu görsel temadan bağımsız kalmalı.

### Kayıt ve koşu sonu

Mevcut koşu coin'i, en iyi coin, en yüksek kat ve ayarlar ayrı kaydedilmeli. Düşüşte hangi değerlerin sıfırlanacağı açıkça tanımlanmalı. İlk öneri: düşüş koşuyu bitirir; en iyi dereceler korunur. Daha affedici kontrol noktaları, ayrı bir oyun modu olarak değerlendirilebilir.

## Mobil sürüm planı

Bu bölüm yalnızca gelecek içindir; mevcut Windows oyununda mobil yayın iddiası veya mağaza bilgisi gösterilmez.

- Önce Android prototipi; ardından gerçek cihazda fizik ve kontrol hissi testi.
- Dikey ekran, yukarı yükselme için güçlü bir adaydır; mevcut yatay arena doğrudan kırpılmamalı. Görüş alanı ve platform genişlikleri yeniden düzenlenmeli.
- Sol/sağ ve zıplama için parmakla rahat erişilen dokunmatik alanlar. Klavye desteği masaüstünde korunmalı.
- Ekran çentiği ve güvenli alan desteği, farklı en-boy oranları, okunaklı coin sayacı.
- Oyunu arka plana alınca duraklatma; geri gelince güvenli devam.
- Hedef 60 FPS; düşük donanımda istikrarlı bir kalite seçeneği. Pil, bellek, aşırı ısınma ve dokunma gecikmesi ölçülmeli.
- Mağaza adı, ikon, ekran görüntüleri, görsel/ses kullanım hakları ve gerekli gizlilik belgeleri yayın öncesinde hazırlanmalı.
- Reklam, satın alma, hesap ve analitik gibi sistemler şu an kararlaştırılmış değildir; kullanıcı onayı olmadan eklenmemeli.

## GitHub üzerinden devam etme notları

Mevcut Unity projesindeki ana dosyalar:

- `Assets/Scripts/PlayerController.cs`: hareket, zıplama, platformda taşınma, skor ve yeniden başlatma.
- `Assets/Scripts/MovingPlatform2D.cs`: zıt fazlı yatay basamak hareketi.
- `Assets/Scripts/CoinSequence.cs`: tek aktif coin, üç yerleşim türü ve sonraki coin'i oluşturma.
- `Assets/Scenes/SampleScene.unity`: mevcut arena.
- `Assets/CarGameArt/`: oyun görselleri ve font varlıkları.
- `Assets/Editor/DarkGameSetup.cs`: mevcut sahneyi oluşturan editör aracı. Gelecek elle yapılmış sahneleri yanlışlıkla yeniden kurmaması için kontrollü kullanılmalı.
- `Assets/Editor/DarkGameVerification.cs`: editörde çalışan kontroller; oyun derlemesinde bulunmaz.
- `Assets/Editor/TeacherExport.cs`: Windows çıktısı; oyuncuya belge veya Beni Oku dosyası eklememeli.

Depoya `Assets`, `.meta` dosyaları, `Packages`, `ProjectSettings`, uygun `.gitignore` ve bu plan eklenmeli. `Library`, `Temp`, `Logs`, `obj`, derlenmiş EXE/RAR ve yerel yedekler kaynak deposuna konmamalı. Görsel kaynaklarının ve lisans bilgilerinin proje içinde izlenebilirliği korunmalı.

Başlangıçta depoda gerçekten bulunan dosyalar ve bu plan karşılaştırılmalı. Eski editör sahne kurucuları çalıştırılmadan önce mevcut sahne yedeklenmeli. Eski testlerin artık kaldırılmış merkez kuralını veya yedi coin sınırını beklemediği kontrol edilmeli.

## Önerilen geliştirme sırası

1. Mevcut kontrolleri ve coin erişilebilirliğini sağlamlaştır; küçük bir oynanış testi yap.
2. Kamera ve üç kat parçasıyla dikey ilerleme prototipini kur.
3. Geçerli rota garantisi, coin/kamera ilişkisi ve nesne havuzunu tamamla.
4. İlk iki tema ve geçişini ekle; performansı ölç.
5. Karakter animasyonlarını ve temel ses/efekt geri bildirimini ekle.
6. Koşu sonu, en iyi derece ve kayıt sistemini kur.
7. Mobil kontrol ve dikey ekran prototipini gerçek cihazda dene.
8. Diğer bölgeleri içerik verileri üzerinden ekle; yayın hazırlığını ayrı bir aşama olarak ele al.

## Gelecek güncellemenin kabul ölçütleri

- Mevcut coin alınmadan yenisi çıkmaz; her yeni coin erişilebilirdir.
- Merkez çizgisi veya yalnızca merkezde zıplama şartı geri gelmez.
- W ve Boşluk masaüstünde aynı zıplamayı verir; havada tekrar zıplama kapalıdır.
- Kat üretimi, hareketli platformların iniş zamanını hesaba katar; zorunlu imkânsız boşluk oluşmaz.
- Coin sayısı sınırsız ilerler; kat, coin sayısından bağımsız yükselir.
- Tema değişince görseller/efektler uyumlu değişir; fizik beklenmedik biçimde bozulmaz.
- Arka planda kalmış coin ilerlemeyi kilitlemez.
- Windows teslim arşivi yalnızca gerekli oyun dosyalarını içerir. Bu plan, mobil yayın bilgileri, test günlükleri ve kaynak proje oyun arşivine konmaz.

## GitHub açıldığında kullanılabilecek görev metni

“Kök Yankısı projesini bu planla karşılaştır. Önce mevcut hareketi, tek aktif coin kuralını ve sınırsız skoru koruyarak nesne havuzlu dikey kat üretimi ve yukarı takip kamerası kur. Coin erişilebilirliği ile kamera ilişkisini çöz. Sonra ilk iki tema arasındaki geçişi, temaya uygun temel animasyon ve efektleri ekle. Mobil ve mağaza sistemlerini ayrıca istemedikçe uygulama. Her aşamada oynanabilir bir sürüm bırak ve mevcut sahne kurucularının yeni sahneyi ezmesini önle.”

## İlk çoklu cihaz geliştirme adımı — teslim sürümünden sonra

Öncelik, yerel Android uygulaması, iPhone/iPad uygulaması ve bilgisayar uygulamasıdır; tarayıcı sürümü bu aşamanın hedefi değildir.

Hocaya gönderilecek `KEREM YÜRÜK 254309044.rar` sabit tutuldu. Aşağıdaki hazırlıklar yalnızca devam edilen Unity geliştirme projesindedir; bu teslim RAR'ına eklenmedi:

- Ortak `GameInput` girişi: klavye ve dokunmatik aynı oyuncu hareketini kullanır.
- Sol/sağ/zıpla düğmeleri; çoklu dokunuş ve bağımsız parmak bırakma davranışı.
- Masaüstünde T ile dokunmatik arayüzü önizleme.
- Ekranın güvenli alanına uyan arayüz; yatay/dikey görünüm değişiminde kamera ve oynanabilir yatay sınırların uyarlanması.
- Dar ekranlarda platform hareket aralığının ve coin doğma alanının sınırlanması.
- Dokunmatik yeniden başlatma; odak kaybında giriş durumunun temizlenmesi.
- Android ARM64/IL2CPP ve iOS/IL2CPP proje ayarları, uygulama kimliği taslağı, ekran yönü ayarları.
- Eski sahne kurucularının projeyi açarken sahneyi otomatik yeniden oluşturmaması için koruma.

Editör doğrulaması: dokunmatik yönler, çoklu dokunma, tek basışla zıplama, giriş temizleme; 720×1280, 1280×720, 1536×2048 ve 2560×1080 yerleşim profilleri geçti. Bunlar gerçek cihaz testinin yerine geçmez.

Mevcut yerel kurulumda Windows derleme desteği var; Android ve iOS derleme modülleri yok. APK/AAB veya iOS uygulaması henüz üretilmedi. Sonraki somut adım, uygun modüller ve araç zincirleriyle Android cihaz derlemesi almak; ardından iOS imzalama/derleme ve fiziksel iPhone/iPad doğrulamasını gerçekleştirmektir. Tüm cihazlarda çalıştığı henüz iddia edilmez.

Ek kaynak dosyaları: `GameInput.cs`, `TouchControlButton.cs`, `GameDeviceLayout.cs`; editör tarafında `DeviceSetup.cs`, `DeviceVerification.cs`, `NativePlatformSettings.cs`. Yeni sahne güncellemelerinde dokunmatik bileşenler korunmalı; sahne kurucuları yalnızca açık bir görevle çalıştırılmalıdır.

## 27 Ekim 2026 — gerçek yükseliş ve özgün karakter

- Basamaklar dünya yüksekliğinde üretiliyor; son basamağa iniş sonraki yedi katı açıyor. Kamera yukarı ilerliyor, mezarlık dünya koordinatında aşağıda kalıyor.
- Bulut katmanları yükseklikte devam ediyor; üst bölgede kanat çırpan mutant kuşlar geçiyor. Kuşlar bu aşamada dekor, hasar vermiyor.
- Muhafız FBX kullanılmıyor. Kök Gezgin adlı özgün 2D karakter; bekleme, yürüme, yükselme, düşme ve iniş pozlarıyla oyuna bağlandı.
- Coinler yerde doğmuyor. Hareketli platform üstü ve havadaki konumlar kullanılıyor; tek aktif coin ve sınırsız skor korunuyor.
- Uzak katlar nesne havuzuna alınıyor; alınmamış coin bulunan kat korunuyor. 170'ten fazla kat üretimi, tek coin akışı ve yeniden başlatma editör testleri geçti.
- Android ve iOS modülleri Unity 6000.0.84f1'e kuruldu; Android SDK/NDK/OpenJDK araçları mevcut. Yukarıdaki modüllerin eksik olduğu notu önceki aşamayı anlatır.
- Yerel telefon/tablet oynanışı henüz fiziksel cihazda doğrulanmadı. iOS son imzalama ve uygulama derlemesi için macOS/Xcode gerekiyor.
- Öğretmene gönderilen arşiv sabit; geliştirme çıktıları ayrı isimle tutulacak. Plan ve test günlükleri oyun arşivine eklenmeyecek.

## 27 Ekim — Jump Higher özel depo ve web sürümü

Özel kaynak deposu: https://github.com/yurukkerem700-alt/JUMP-H-GHER . Oyun adı Kök Yankısı; depo Jump Higher başlığını taşır. Kaynaklar ile Unity WebGL çıktısı aynı depoda tutulur. Kullanıcı Vercel bağlantısını ve yayına almayı kendi yapacak.

9. katı aşamama bildirimi sonrası üretim önden sürekli kat hazırlayacak şekilde değiştirildi. Her yeni coin tam bir üst katta doğar; rastgele alt kat seçimi kaldırıldı. 100 artan coin, çift zıplama gücü, tek ek hava zıplaması, mıknatıs toplaması ve 8–22. katlar arası gerçek fizik tırmanışı doğrulandı.

Bölgeler her 20 katta değişir ve daha yüksek irtifalarda tekrar eder. İlk beşli: Kül Mezarlığı, Yitik Bulutlar, Fırtına Geçidi, Kızıl Kökler, Yıldız Külleri. Her üst bölgenin ayrı arka plan resmi vardır. Platform renkleri de bölgeye uyar. İçerik sayısı sonludur; yükseklik ve coin sayısı sınırsızdır.

2X: 20 saniyelik çift zıplama; M: 15 saniyelik 2.5 birim toplama yarıçapı. Güçler yaklaşık her on katta dönüşümlü görünür; alınmaları zorunlu değildir. Bulut ve fırtına bölgesinde yukarı hava akımı bulunur. Güçler temel atlayış rotasının gereği değildir.

Açılış görseli, ana menü, kontrol açıklaması, duraklat/devam et ve yerel en yüksek kat kaydı eklendi. Uygulama ikonu özgün Kök Gezgin yüzüdür. Android/iOS fiziksel cihaz doğrulaması ve mağaza yayını sonraki aşamadır.
