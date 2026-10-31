---
name: game-art-director
description: Use when a game cover, app icon, splash, store capsule or key art is requested. Chooses raster image generation over hand-coded SVG, writes the prompt, and exports every platform size.
---

# Oyun kapak / ikon üretimi

Kural: kapak, ikon, splash ve mağaza görseli gibi resimsi işler SVG/kodla ÇİZİLMEZ. Kodla çizilen vektör, boyamalı görsellerin yanında oyuncak durur (ChatGPT ile yan yana karşılaştırmada kanıtlandı). SVG yalnızca düz geometrik logo/glif için.

1. Önce raster üreticiyi dene: bağlı bir görsel aracı (Canva generate-image, Figma generate_image [kredi harcar, önce sor], Gamma generate_image, ya da kurulu bir görsel MCP: gpt-image / ComfyUI). Hiçbiri yoksa kullanıcıya hazır bir istem ver ve görseli ondan iste; kodla çizmeye düşme.
2. İstem şablonu: tür ve kare/geniş oran, "no text, no UI, full bleed, no rounded corners"; tek baskın siluet (küçük boyutta okunur); ana özneyi ortada, karenin %65-70'inde; 3 renkli palet (oyunun mevcut paleti); ışık kaynağı ve derinlik katmanları (ön plan/orta/arka); malzeme dokusu (taş, ahşap, kumaş); "hand-painted stylized game art".
3. Doğrula: 1024'ten 48 px'e küçültüp siluet okunuyor mu bak; Android adaptive için merkez %66 güvenli alan; maskable için %80.
4. Dışa aktar (Pillow): 1024 ana, 512, 192, iOS 1024, Android mipmap (ldpi–xxxhdpi, round, foreground/background), splash (blur dolgu + sığdır). Orijinali `cover/` altında sakla.
5. Dist değiştiyse `npm run offline:build` + `npm test`; web SW sürümünü artır.
6. İki aday üret, yan yana karşılaştır, kullanıcıya seçtir.
