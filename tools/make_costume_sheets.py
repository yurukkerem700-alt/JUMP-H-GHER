"""Kostüm kaynak resimlerinden oyunun beklediği 4x2 (8 kare) şeffaf sprite sayfalarını üretir.

Not: Güncel kostüm sayfaları tools/costume_anim_a.py ve costume_anim_b.py ile üretilir (gerçek yürüme/zıplama kareleri).
Bu betik yalnızca ilk, tek pozlu taslak sayfaları üretir ve onların üzerine yazar; yeniden çalıştırmayın.

Gerekenler: pip install pillow numpy scipy
Kullanım:  python tools/make_costume_sheets.py
Girdi:     tools/costume_source/<id>.jpg   (beyaz arka planlı, tek poz, yüz sağa ya da sola)
Çıktı:     Assets/Resources/Costumes/<id>.png   (1536x1024, 8 kare: bekle, yürü x4, zıpla, düş, in)
Önizleme:  tools/costume_source/_onizleme.png

Kare sırası PlayerVisual2D ile aynıdır. Tek pozdan kareler dönme ve ezme/gerdirme ile türetilir;
ileride gerçek animasyon kareleri üretilirse aynı dosya adıyla sayfa doğrudan değiştirilebilir.
"""
import math
import os
import sys
from PIL import Image, ImageFilter
import numpy as np
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "tools", "costume_source")
OUT = os.path.join(ROOT, "Assets", "Resources", "Costumes")

CELL_W, CELL_H = 384, 512
MAX_W, MAX_H = 340, 440          # karakterin kare içindeki en büyük boyutu
MARGIN_BOTTOM = 24

# id -> (yüz sola bakıyorsa True, ince çizgileri sil)
COSTUMES = {
    "kul_mumcusu": dict(flip=True),
    "bulut_balikcisi": dict(flip=False),
    "yildirim_kuklasi": dict(flip=False, thin=7),
    "kok_kalbi": dict(flip=False),
    "ay_kirigi_tilkisi": dict(flip=False),
}

# (dönme derece, yatay ölçek, dikey ölçek): bekle, yürü x4, zıpla, düş, in
POSES = [
    (0, 1.00, 1.00),
    (-4, 1.02, 0.97), (0, 0.98, 1.00), (4, 1.02, 0.97), (0, 0.98, 1.00),
    (-7, 0.95, 1.00),
    (5, 1.04, 0.97),
    (0, 1.12, 0.86),
]


def cut_out(img, thin=0):
    rgb = np.asarray(img.convert("RGB")).astype(np.int16)
    h, w, _ = rgb.shape
    mx, mn = rgb.max(axis=2), rgb.min(axis=2)
    sat = (mx - mn) / np.maximum(mx, 1)
    lum = rgb.mean(axis=2)
    whitish = mn >= 226

    # Ayakların altı: karakterin koyu veya renkli en alt satırı. Zemin gölgesi tam gridir (doygunluk ~0).
    solid = ~whitish & ((lum < 100) | (sat > 0.06))
    rows = np.where(solid.sum(axis=1) > 3)[0]
    feet = rows.max() if len(rows) else h - 1
    band = np.zeros(whitish.shape, bool)
    band[max(0, feet - 40):, :] = True
    shadow = band & (sat < 0.10) & (lum > 112) & (lum <= 245)
    # İki ayağın yüksekliği farklıysa gölge daha yukarıda kalır: orada yalnızca tam gri alan sayılır.
    wide = np.zeros(whitish.shape, bool)
    wide[max(0, feet - 120):, :] = True
    shadow |= wide & (sat < 0.03) & (lum > 118) & (lum <= 245)

    # Kenarlara bağlı beyaz + gölge alanı arka plandır; karakterin içindeki açık renkler korunur.
    labels, _ = ndimage.label(whitish | shadow, structure=np.ones((3, 3)))
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    fg = ~np.isin(labels, edge[edge > 0])

    # İp, bacak ve gölge arasında kapalı kalan büyük beyaz boşluklar da arka plandır; küçük parlamalar kalır.
    pure, count = ndimage.label(mn >= 243, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones_like(pure), pure, index=np.arange(1, count + 1))
    big = np.isin(pure, 1 + np.where(sizes >= 150)[0])
    fg &= ~big

    mask = Image.fromarray((fg * 255).astype(np.uint8), "L")
    if thin:
        k = thin if thin % 2 else thin + 1
        mask = mask.filter(ImageFilter.MinFilter(k)).filter(ImageFilter.MaxFilter(k))
    mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.9))
    out = img.convert("RGBA")
    out.putalpha(mask)
    return out.crop(out.getbbox())


def pose(sprite, rot, sx, sy):
    w, h = sprite.size
    scale = min(MAX_W / w, MAX_H / h)
    base = sprite.resize((max(1, int(w * scale)), max(1, int(h * scale))), Image.LANCZOS)
    base = base.resize((max(1, int(base.width * sx)), max(1, int(base.height * sy))), Image.LANCZOS)
    pad = int(max(base.size) * 0.25)
    canvas = Image.new("RGBA", (base.width + pad * 2, base.height + pad * 2), (0, 0, 0, 0))
    canvas.paste(base, (pad, pad))
    # Ayak ucundan (alt orta) döndür.
    pivot = (canvas.width / 2, pad + base.height)
    canvas = canvas.rotate(rot, resample=Image.BICUBIC, center=pivot)
    cell = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
    left = int(CELL_W / 2 - pivot[0])
    top = int(CELL_H - MARGIN_BOTTOM - pivot[1])
    cell.alpha_composite(canvas, (max(left, 0), max(top, 0)),
                         (max(-left, 0), max(-top, 0)))
    return cell


def build(name, opts):
    img = Image.open(os.path.join(SRC, name + ".jpg"))
    if opts.get("flip"):
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
    sprite = cut_out(img, opts.get("thin", 0))
    sheet = Image.new("RGBA", (CELL_W * 4, CELL_H * 2), (0, 0, 0, 0))
    for i, (rot, sx, sy) in enumerate(POSES):
        sheet.alpha_composite(pose(sprite, rot, sx, sy), ((i % 4) * CELL_W, (i // 4) * CELL_H))
    os.makedirs(OUT, exist_ok=True)
    sheet.save(os.path.join(OUT, name + ".png"))
    return sheet


def main():
    previews = []
    for name, opts in COSTUMES.items():
        previews.append((name, build(name, opts)))
        print("tamam:", name)
    # Koyu zemin üzerinde önizleme (ilk kare + zıpla karesi)
    w = CELL_W * 2
    board = Image.new("RGBA", (w * len(previews) // 1, CELL_H), (38, 26, 40, 255))
    for i, (name, sheet) in enumerate(previews):
        board.alpha_composite(sheet.crop((0, 0, CELL_W, CELL_H)), (i * w, 0))
        board.alpha_composite(sheet.crop((CELL_W, 0, CELL_W * 2, CELL_H)), (i * w + CELL_W, 0))
    board.convert("RGB").save(os.path.join(SRC, "_onizleme.png"))


if __name__ == "__main__":
    sys.exit(main())
