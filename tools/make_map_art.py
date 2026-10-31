#!/usr/bin/env python
"""Referans harita görselinden (1672x941) oyun içi harita parçalarını üretir.

Çıktılar (Assets/Resources/UI): MapBackground.png (levha, yazı, yıldız ve numarasız arka plan; yol noktaları
ve düğüm halkaları yerinde kalır), MapPlateSide.png, MapPlateTitle.png (yazısız levhalar).
Kullanım: python tools/make_map_art.py <referans.webp> <cikti_klasoru>
"""
import sys

import cv2
import numpy as np
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
img = np.array(Image.open(src).convert("RGB"))
bgr = cv2.cvtColor(img, cv2.COLOR_RGB2BGR)
H, W = bgr.shape[:2]
gray = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY)

PLATES = {"left": (45, 22, 318, 102), "title": (585, 12, 1088, 98), "right": (1350, 20, 1640, 102)}
TEXT = {"left": (92, 35, 292, 90), "title": (700, 28, 975, 85), "right": (1375, 38, 1612, 88)}
NODES = [(1205, 938), (1313, 745), (1032, 552), (667, 382), (435, 230)]  # alttan üste
R = 64


def rect_mask(shape, box, pad=0):
    m = np.zeros(shape, np.uint8)
    x0, y0, x1, y1 = box
    m[max(0, y0 - pad):y1 + pad, max(0, x0 - pad):x1 + pad] = 255
    return m


def dilate(m, k):
    return cv2.dilate(m, np.ones((k, k), np.uint8))


# Levhalar: yazıyı sil (parlak pikseller)
plates_clean = bgr.copy()
for box in TEXT.values():
    m = cv2.bitwise_and((gray > 105).astype(np.uint8) * 255, rect_mask(gray.shape, box))
    plates_clean = cv2.inpaint(plates_clean, dilate(m, 7), 5, cv2.INPAINT_TELEA)

# Arka plan: levhalar, yıldız sayacı ve düğümlerin altındaki yıldız sıraları silinir
mask = np.zeros((H, W), np.uint8)
for box in PLATES.values():
    mask |= rect_mask(mask.shape, box, 6)
mask |= rect_mask(mask.shape, (722, 100, 955, 138), 2)
for cx, cy in NODES:
    mask |= rect_mask(mask.shape, (cx - 66, cy + 34, cx + 66, cy + 92))
clean = cv2.inpaint(bgr, dilate(mask, 3), 5, cv2.INPAINT_TELEA)
clean0 = clean.copy()

# Düğme halkası: 4. düğmeden alınır, alt yarı üst yarının aynasıdır (yıldızlar alt yarıyı kapatıyor), numara silinir
cx, cy = 667, 382
ring = bgr[cy - R:cy + R, cx - R:cx + R].copy()
yy, xx = np.mgrid[0:2 * R, 0:2 * R]
dist = np.sqrt((xx - R) ** 2 + (yy - R) ** 2)
ring[R:, :] = ring[:R, :][::-1]
flat = np.median(ring[(dist > 38) & (dist < 44)], axis=0).astype(np.uint8)
ring[dist < 38] = flat
alpha = np.clip((R - 1.5 - dist) * 1.2 * 255 / 2, 0, 255).astype(np.float32) / 255.0
for nx, ny in NODES:
    y0, x0 = ny - R, nx - R
    ys, xs = max(0, y0), max(0, x0)
    ye, xe = min(H, ny + R), min(W, nx + R)
    a = alpha[ys - y0:ye - y0, xs - x0:xe - x0, None]
    r_ = ring[ys - y0:ye - y0, xs - x0:xe - x0].astype(np.float32)
    clean[ys:ye, xs:xe] = (clean[ys:ye, xs:xe].astype(np.float32) * (1 - a) + r_ * a).astype(np.uint8)
Image.fromarray(cv2.cvtColor(clean, cv2.COLOR_BGR2RGB)).save(out + "/MapBackground.png")
print("dugme ic rengi (RGB):", [int(v) for v in flat[::-1]])


def plate_sprite(key, name, ends=0):
    x0, y0, x1, y1 = PLATES[key]
    crop = plates_clean[y0:y1, x0:x1]
    diff = np.abs(crop.astype(int) - clean0[y0:y1, x0:x1].astype(int)).sum(axis=2)
    h, w = diff.shape
    # çerçeve dikdörtgeni: güçlü kenar piksellerinin çoğunun bulunduğu satır / sütunlar (uçlardaki dikenler hariç)
    strong = diff > 110
    mid = strong[:, ends:w - ends] if ends else strong
    ys, xs = np.where(mid)
    ty, by = np.percentile(ys, 1), np.percentile(ys, 99)
    lx = (np.percentile(xs, 0.5) + ends) if ends else np.percentile(xs, 0.5)
    rx = (np.percentile(xs, 99.5) + ends) if ends else np.percentile(xs, 99.5)
    a = np.zeros((h, w), np.uint8)
    cv2.rectangle(a, (int(lx) - (0 if ends else 0), int(ty) - 1), (int(rx), int(by) + 1), 255, -1)
    if ends:  # dikenli uçlar: farkı düşük eşikle al, yalnız uç bölgelerinde
        spikes = (diff > 40).astype(np.uint8) * 255
        keep = np.zeros_like(spikes); keep[:, :ends + 10] = 255; keep[:, w - ends - 10:] = 255
        spikes = cv2.morphologyEx(cv2.bitwise_and(spikes, keep), cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
        a = a | spikes
        a[:2, :] = 0; a[-2:, :] = 0
    a = cv2.GaussianBlur(a, (3, 3), 0)
    Image.fromarray(np.dstack([cv2.cvtColor(crop, cv2.COLOR_BGR2RGB), a]), "RGBA").save(out + "/" + name)


plate_sprite("left", "MapPlateSide.png")
plate_sprite("title", "MapPlateTitle.png", ends=48)
print("tamam")
