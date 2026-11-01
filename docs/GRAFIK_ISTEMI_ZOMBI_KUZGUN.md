# Mezar zombisi ve kuzgunlar: ChatGPT grafik istemleri

Oyun bu iki dosyayı bulursa otomatik gösterir (kod hazır, `Assets/Scripts/GraveDecor.cs`). Dosya yoksa hiçbir şey eklenmez.

| Dosya | Konum | Ne |
|---|---|---|
| `ZombiMezar.png` | `Assets/Resources/Decor/` | Sol mezar taşının (kırmızı X) dibinden çıkmaya çalışan zombi, 6 kare |
| `Kuzgun.png` | `Assets/Resources/Decor/` | Sağ üstteki çemberli dalda duran kuzgun, 4 kare (oyunda 3 kopya olarak durur) |

**Kural:** Tüm kareler yan yana TEK satırda, her kare tam kare (yükseklik = genişlik), arka plan şeffaf PNG. Karelerin hepsinde karakterin tabanı karenin alt orta noktasında olmalı.

**ChatGPT'ye ilk önce oyundan aldığın bir ekran görüntüsünü ekle** ve "tarzı birebir bu oyuna uydur" de. Böylece renk ve çizgi tarzı oyunla aynı olur.

---

## İstem 1: Zombi (`ZombiMezar.png`)

> Attached is a screenshot of my 2D dark-fantasy game. Match its art style exactly: muted warm brown/orange palette, hand-painted look, soft dark outlines, graveyard mood. Do NOT make it 3D or photorealistic, and do not make it look like a smooth vector.
>
> Create a sprite strip of a rotting zombie slowly climbing out of a grave. Only the upper body (from the waist up) is visible; the lower body is buried in a small mound of dark loose soil that is part of the sprite, with a few clumps of dirt and a bit of grass at the bottom edge. The zombie is hunched, grey-green decayed skin, torn rags, hollow eyes, one arm reaching forward and up, the other pressed on the ground, swaying left-right as if struggling to pull itself out.
>
> Output: ONE PNG, transparent background, a horizontal strip of exactly 6 frames, each frame 256x256 px (total 1536x256). The 6 frames are a smooth looping sway/struggle cycle (frame 6 flows back into frame 1). The soil mound sits at the bottom center of every frame, in exactly the same place. No text, no shadows on the background, no cropped limbs.

## İstem 2: Kuzgun (`Kuzgun.png`)

> Using the same attached screenshot as the style reference, create a sprite strip of a single raven perched on a branch, seen from the side facing left, black feathers with a faint cold blue sheen, small bright eye. Hand-painted dark-fantasy look matching the game, not 3D, not smooth vector.
>
> Output: ONE PNG, transparent background, a horizontal strip of exactly 4 frames, each 128x128 px (total 512x128): frame 1 = standing still, frame 2 = head turned to look back, frame 3 = head down pecking, frame 4 = wings slightly ruffled. The feet are at the bottom center of every frame in exactly the same place (no branch drawn, only the bird). No text.

---

## Gelince

1. İki PNG'yi `Assets/Resources/Decor/` klasörüne koy (klasör yoksa oluştur) ya da bana ver, ben koyup yayınlarım.
2. Konum/boyut ince ayarı: `GraveDecor.cs` üstündeki `ZombieU`, `ZombieV`, `BranchU`, `BranchV`, `ZombieHeight`, `RavenHeight` sabitleri.
3. Not: arka plan resmi dünyada sabit durur; zombi ve kuzgunlar oyun başında zeminde görünür, kamera yükselince diğer manzarayla birlikte aşağıda kalır. Dar (dikey) telefon ekranında oyunun yan kenarları görünmediği için bu iki süs yalnızca geniş ekranda görülür.
