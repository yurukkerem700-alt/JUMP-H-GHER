"""Kök Kalbi ve Ay Kırığı Tilkisi için gerçek animasyon kareleri (8 kare, 4x2) üretir.

Kullanım:  python tools/costume_anim_b.py
Girdi:     tools/costume_source/<id>.jpg  (tek poz, beyaz zemin)
Çıktı:     Assets/Resources/Costumes/<id>.png  (1536x1024, 8 kare)
Önizleme:  tools/costume_source/_anim_b_preview.png

Yöntem: tek resim parçalara ayrılır (gövde, baş, kollar, bacaklar, kuyruk ...), her parça
eklem noktası etrafında döndürülür / ötelenir, dal ve kuyruk gibi uçlar sinüs kaydırmayla
(ağ bükme) gecikmeli sallanır ve arkadan öne birleştirilir. 2x süper örneklemeyle çalışır.
Gerekenler: pillow, numpy, scipy.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools"))
import make_costume_sheets as m  # noqa: E402

SRC = m.SRC
OUT = m.OUT
CELL_W, CELL_H = m.CELL_W, m.CELL_H
GROUND = CELL_H - m.MARGIN_BOTTOM
S = 2                      # süper örnekleme
CW, CH = CELL_W * S, CELL_H * S


# --------------------------------------------------------------------------- yardımcılar
def over(dst, src):
    """Önceden çarpılmış RGBA float dizileri: src, dst üzerine."""
    return src + dst * (1.0 - src[..., 3:4])


def decontaminate(rgba):
    """Düşük alfalı kenar piksellerinin rengini en yakın tam opak pikselden al (beyaz hale yok)."""
    a = rgba[..., 3]
    solid = a > 0.92
    idx = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
    out = rgba.copy()
    out[..., :3] = rgba[idx[0], idx[1], :3]
    return out


def drop_small(rgba, min_area, keep_boxes=()):
    """Küçük kopuk parçaları (toz, benek) sil; keep_boxes içindeki (x0,y0,x1,y1) parçalar kalır."""
    a = rgba[..., 3] > 0.3
    lab, n = ndimage.label(a, structure=np.ones((3, 3)))
    if n == 0:
        return rgba
    area = ndimage.sum(a, lab, index=np.arange(1, n + 1))
    coms = ndimage.center_of_mass(a, lab, index=np.arange(1, n + 1))
    kill = np.zeros(n + 1, bool)
    for i in range(n):
        if area[i] < min_area:
            cy, cx = coms[i]
            if not any(b[0] <= cx <= b[2] and b[1] <= cy <= b[3] for b in keep_boxes):
                kill[i + 1] = True
    out = rgba.copy()
    out[kill[lab]] = 0
    return out


class Rig:
    """Bir sprite'ı tuval üzerinde, 'sprite birimleri' (ölçeklenmiş tek poz, piksel) ile yönetir."""

    def __init__(self, sprite_rgba, k0, foot_x, base_y, clean=None):
        self.k = k0 * S                      # sprite birimi -> tuval pikseli
        self.fx = foot_x                     # sprite içinde karenin yatay merkezi
        self.by = base_y                     # sprite içinde zemin (en alt satır)
        self.sw, self.sh = sprite_rgba.size
        self.ox = CW / 2 - foot_x * self.k
        self.oy = GROUND * S - base_y * self.k
        big = sprite_rgba.resize((int(round(self.sw * self.k)), int(round(self.sh * self.k))), Image.LANCZOS)
        arr = np.asarray(big).astype(np.float32) / 255.0
        arr = decontaminate(arr)
        if clean is not None:
            arr = clean(arr)
        self.sprite_straight = arr
        self.base = self._place(arr)
        self.yy, self.xx = np.mgrid[0:CH, 0:CW].astype(np.float32)

    def _place(self, arr):
        h, w = arr.shape[:2]
        c = np.zeros((CH, CW, 4), np.float32)
        x0, y0 = int(round(self.ox)), int(round(self.oy))
        xs, ys = max(0, x0), max(0, y0)
        xe, ye = min(CW, x0 + w), min(CH, y0 + h)
        c[ys:ye, xs:xe] = arr[ys - y0:ye - y0, xs - x0:xe - x0]
        c[..., :3] *= c[..., 3:4]
        return c

    def P(self, x, y):
        return self.ox + x * self.k, self.oy + y * self.k

    # ---- maskeler (sprite birimiyle) ----
    def poly_mask(self, pts, blur=1.5):
        im = Image.new("L", (CW, CH), 0)
        ImageDraw.Draw(im).polygon([self.P(x, y) for x, y in pts], fill=255)
        if blur:
            im = im.filter(ImageFilter.GaussianBlur(blur * self.k))
        return np.asarray(im).astype(np.float32) / 255.0

    def part(self, mask, src=None):
        src = self.base if src is None else src
        return src * mask[..., None]

    # ---- dönüşümler ----
    def xform(self, layer, pivot=(0, 0), angle=0.0, d=(0, 0), s=(1, 1)):
        px, py = self.P(*pivot)
        th = math.radians(angle)
        c, sn = math.cos(th), math.sin(th)
        R = np.array([[c, sn], [-sn, c]])         # görsel saat yönü tersi (y aşağı)
        A = R @ np.diag([s[0], s[1]])
        t = np.array([px + d[0] * self.k, py + d[1] * self.k]) - A @ np.array([px, py])
        Ai = np.linalg.inv(A)
        ti = -Ai @ t                               # çıkış (x,y) -> giriş
        mat = np.array([[Ai[1, 1], Ai[1, 0]], [Ai[0, 1], Ai[0, 0]]])
        off = np.array([ti[1], ti[0]])
        out = np.empty_like(layer)
        for ch in range(4):
            out[..., ch] = ndimage.affine_transform(layer[..., ch], mat, offset=off, order=1, mode="constant", cval=0.0)
        return out

    def warp(self, layer, dx, dy=None):
        """dx, dy: tuval boyutunda (sprite birimi cinsinden) kaydırma alanları; çıkış(x) = giriş(x - dx)."""
        k = self.k
        coords = np.array([self.yy - (0 if dy is None else dy) * k, self.xx - dx * k])
        out = np.empty_like(layer)
        for ch in range(4):
            out[..., ch] = ndimage.map_coordinates(layer[..., ch], coords, order=1, mode="constant", cval=0.0)
        return out

    def field(self, fn):
        """fn(x_sprite, y_sprite) -> dizi; tuval ızgarasında hesaplar."""
        xs = (self.xx - self.ox) / self.k
        ys = (self.yy - self.oy) / self.k
        return fn(xs, ys).astype(np.float32)

    # ---- çıkış ----
    def finish(self, canvas, min_area=40):
        a = canvas[..., 3]
        cell = canvas.reshape(CELL_H, S, CELL_W, S, 4).mean(axis=(1, 3))
        al = cell[..., 3]
        rgb = np.where(al[..., None] > 1e-4, cell[..., :3] / np.maximum(al[..., None], 1e-4), 0)
        out = np.concatenate([np.clip(rgb, 0, 1), np.clip(al, 0, 1)[..., None]], axis=2)
        out = drop_small(out, min_area)
        out[out[..., 3] < 0.02] = 0
        return Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")


def smooth(t):
    return t * t * (3 - 2 * t)


def fit_sprite(name, thin=0):
    img = Image.open(os.path.join(SRC, name + ".jpg"))
    sp = m.cut_out(img, thin)
    sc = min(m.MAX_W / sp.width, m.MAX_H / sp.height)
    return sp.resize((int(sp.width * sc), int(sp.height * sc)), Image.LANCZOS)


def strip_bottom_gray(rgba_img, band=26):
    """Zemin gölgesinden kalan düşük doygunluklu gri pikselleri en alt banttan sil."""
    a = np.asarray(rgba_img).astype(np.int16)
    h = a.shape[0]
    mx, mn = a[..., :3].max(2), a[..., :3].min(2)
    sat = (mx - mn) / np.maximum(mx, 1)
    lum = a[..., :3].mean(2)
    kill = np.zeros(a.shape[:2], bool)
    kill[h - band:] = ((sat < 0.16) & (lum > 70))[h - band:]
    a[kill, 3] = 0
    return Image.fromarray(a.astype(np.uint8), "RGBA")


# --------------------------------------------------------------------------- KÖK KALBİ
def tree_frames():
    sp = fit_sprite("kok_kalbi")                      # 280x440
    sp = Image.fromarray(np.asarray(sp))
    def clean_tree(arr):
        mn = arr[..., :3].min(2)
        arr = arr.copy()
        arr[(mn > 0.72) & (arr[..., 3] > 0)] = 0      # baş üstündeki beyaz ışık lekeleri
        return arr

    rig = Rig(sp, k0=0.92, foot_x=176, base_y=sp.height - 1, clean=clean_tree)
    P = rig.poly_mask

    # kütle maskeleri (sprite birimi)
    head_m = P([(0, 0), (300, 0), (300, 226), (260, 232), (176, 236), (110, 232), (0, 226)], 2.5)
    larm_m = P([(60, 236), (120, 236), (124, 258), (140, 266), (136, 292), (130, 312), (127, 392), (60, 392)], 1.2)
    rarm_m = P([(206, 252), (236, 244), (270, 290), (270, 362), (230, 362), (222, 338), (210, 318), (205, 300), (204, 272)], 1.0)
    lleg_m = P([(127, 338), (177, 338), (183, 460), (120, 460)], 0.8)
    rleg_m = P([(177, 338), (226, 338), (238, 460), (183, 460)], 0.8)
    # gövde: kollar, bacaklar ve baş hariç her şey (belden aşağısı eğri)
    torso_m = P([(100, 216), (270, 216), (270, 345), (215, 352), (176, 360), (136, 352), (100, 345)], 1.5)
    torso_m = np.clip(torso_m - larm_m * 0.97 - rarm_m * 0.97, 0, 1)

    head_l = rig.part(head_m)
    torso_l = rig.part(torso_m)
    larm_l = rig.part(larm_m)
    rarm_l = rig.part(rarm_m)
    lleg_l = rig.part(lleg_m)
    rleg_l = rig.part(rleg_m)

    # kalp parlaması için maske (turuncu/kırmızı parlak pikseller, kalp çevresinde)
    rgb = rig.sprite_straight[..., :3]
    # baş üstündeki beyazımsı ışık lekelerini sil
    hx = None

    def gain_layer(layer, g):
        if abs(g - 1.0) < 1e-3:
            return layer
        xs = (rig.xx - rig.ox) / rig.k
        ys = (rig.yy - rig.oy) / rig.k
        near = np.exp(-(((xs - 183) / 26.0) ** 2 + ((ys - 276) / 24.0) ** 2))
        # layer önceden çarpılmış; alfa ile aynı oranda parlat
        hot = np.clip((layer[..., 0] - layer[..., 2]) / np.maximum(layer[..., 3], 1e-3) - 0.25, 0, 1)
        f = 1.0 + (g - 1.0) * near * np.clip(hot * 2.0, 0, 1)
        out = layer.copy()
        out[..., :3] = np.clip(out[..., :3] * f[..., None], 0, out[..., 3:4])
        return out

    # antler / yaprak sallanma ağırlığı: baş merkezinden uzaklaştıkça artar
    wgt = rig.field(lambda x, y: np.clip((np.hypot((x - 172) * 0.9, (y - 150)) - 70) / 120.0, 0, 1) ** 1.3)
    # yalnızca baş bölgesi (y<232) için
    wgt = wgt * (rig.field(lambda x, y: y) < 236)

    def render(f):
        """f: sözlük pozu."""
        sway = f.get("sway", 0.0)          # antler yatay sapma (sprite birimi, tepe noktası)
        lift = f.get("lift", 0.0)          # antler dikey sapma (+ aşağı)
        bob = f.get("bob", 0.0)            # gövde + baş aşağı kayma (+ aşağı)
        lean = f.get("lean", 0.0)          # gövde eğimi (derece, + ileri)
        hp = (176, 345)                    # kalça
        leg_l = f.get("legL", (0, 0, 0))   # (açı, dx, dy)
        leg_r = f.get("legR", (0, 0, 0))
        la = f.get("armL", 0.0)
        ra = f.get("armR", 0.0)
        arm_dx = f.get("armDy", 0.0)
        headnod = f.get("nod", 0.0)
        legscale = f.get("legScale", (1, 1))
        legscaleR = f.get("legScaleR", legscale)
        gain = f.get("heart", 1.0)

        cv = np.zeros((CH, CW, 4), np.float32)
        # bacaklar (en arkada): hangisi arkada olacak: legL arkada
        lt = rig.xform(lleg_l, (153, 338), leg_l[0], (leg_l[1], leg_l[2] + bob * 0.0), legscale)
        rt = rig.xform(rleg_l if False else rleg_l, (200, 338), leg_r[0], (leg_r[1], leg_r[2]), legscaleR)
        order = f.get("legOrder", "LR")
        for lay in ([lt, rt] if order == "LR" else [rt, lt]):
            cv = over(cv, lay)
        # kollar (gövdenin arkasında)
        al = rig.xform(larm_l, (134, 266), la, (f.get("armLd", (0, 0))[0], f.get("armLd", (0, 0))[1] + bob), (1, 1))
        ar = rig.xform(rarm_l, (207, 266), ra, (f.get("armRd", (0, 0))[0], f.get("armRd", (0, 0))[1] + bob), (1, 1))
        if not f.get("armsFront"):
            cv = over(cv, al)
            cv = over(cv, ar)
        # gövde
        tl = gain_layer(torso_l, gain)
        tl = rig.xform(tl, hp, lean, (0, bob), (f.get("tsx", 1.0), f.get("tsy", 1.0)))
        cv = over(cv, tl)
        # baş + dallar
        hl = rig.warp(head_l, wgt * sway, wgt * lift)
        hl = rig.xform(hl, (176, 232), headnod + lean, (0, bob), (f.get("hsx", 1.0), f.get("hsy", 1.0)))
        cv = over(cv, hl)
        if f.get("armsFront"):
            cv = over(cv, al)
            cv = over(cv, ar)
        return cv

    # --- pozlar ---
    WALK_L = 22   # temas açısı
    frames = []
    # 0 bekle (hafif nefes: yok; sabit)
    frames.append(dict(heart=1.0))
    # yürüyüş: dört kare
    frames.append(dict(  # temas: sol bacak ileri, sağ geri; gövde alçak
        legL=(WALK_L, 0, 0), legR=(-WALK_L, 0, -3), armL=-20, armR=20, bob=3.5, lean=1.5,
        sway=-8, lift=2, nod=1.0, heart=1.12, legOrder="RL"))
    frames.append(dict(  # geçiş: sağ bacak havada ileri gelir; gövde yüksek
        legL=(4, 0, 0), legR=(20, 0, -10), armL=-2, armR=2, bob=-1.0, lean=0.0,
        sway=-1, lift=-5, nod=0.0, heart=1.0, legOrder="LR"))
    frames.append(dict(  # temas: sağ bacak ileri, sol geri
        legL=(-WALK_L, 0, -3), legR=(WALK_L, 0, 0), armL=20, armR=-20, bob=3.5, lean=1.5,
        sway=8, lift=2, nod=1.0, heart=1.12, legOrder="LR"))
    frames.append(dict(  # geçiş: sol bacak havada ileri gelir
        legL=(20, 0, -10), legR=(4, 0, 0), armL=2, armR=-2, bob=-1.0, lean=0.0,
        sway=1, lift=-5, nod=0.0, heart=1.0, legOrder="RL"))
    # 5 zıpla (yüksel): bacaklar toplanmış, kollar yukarı, dallar aşağı doğru geride
    frames.append(dict(
        legL=(28, 4, -12), legR=(-22, -2, -4), legScale=(1, 0.86), legScaleR=(1, 0.9), armL=-92, armR=92, armsFront=True,
        armLd=(0, 0), armRd=(0, 0), bob=-3.0, lean=-1.5, sway=-6, lift=14, nod=-2, heart=1.25,
        tsy=1.03, hsy=1.02))
    # 6 düş: kollar yukarı savruk, bacaklar sarkık, dallar yukarı uçar
    frames.append(dict(
        legL=(10, 0, 0), legR=(-12, 0, 2), legScale=(1, 1.03), legScaleR=(1, 1.02), armL=-112, armR=110, armsFront=True,
        bob=-2.0, lean=1.0, sway=7, lift=-14, nod=2, heart=1.15, tsy=1.02))
    # 7 iniş: çömel, ezil
    frames.append(dict(
        legL=(-6, -4, 0), legR=(8, 4, 0), legScale=(1.12, 0.72), legScaleR=(1.12, 0.72), armL=-38, armR=38,
        bob=22.0, lean=0, sway=-10, lift=10, nod=0, heart=0.9, tsy=0.94, tsx=1.05, hsx=1.07, hsy=0.9))

    return rig, render, frames


def build_tree():
    rig, render, poses = tree_frames()
    out = []
    for p in poses:
        out.append(rig.finish(render(p)))
    return out


# --------------------------------------------------------------------------- AY KIRIĞI TİLKİSİ
def fox_frames():
    sp = fit_sprite("ay_kirigi_tilkisi")                    # 340x390
    sp = strip_bottom_gray(sp, band=34)

    def clean_fox(arr):
        # toz benekleri sil (küçük kopuk parçalar); taş ve yıldız büyük olduğu için kalır
        return drop_small(arr, 260)

    base_y = sp.height - 1
    a = np.asarray(sp)[..., 3]
    rows = np.where((a > 128).sum(1) >= 5)[0]
    base_y = int(rows.max())
    rig = Rig(sp, k0=0.82, foot_x=232, base_y=base_y, clean=clean_fox)
    P = rig.poly_mask
    ones = np.ones((CH, CW), np.float32)

    # ----- maskeler -----
    head_m = P([(165, 0), (345, 0), (345, 198), (300, 204), (262, 209), (215, 207), (170, 196)], 1.2)
    tail_m = P([(0, 200), (166, 200), (172, 235), (176, 262), (170, 300), (150, 322), (128, 334), (0, 346)], 1.2)
    stone_m = P([(100, 160), (150, 160), (150, 205), (100, 205)], 0.5)
    star_m = P([(30, 175), (75, 175), (75, 225), (30, 225)], 0.5)
    # bacak üst uzantıları (gövde altında kalır) ve alt kesimleri
    leg_polys = {
        "H1": [(128, 296), (174, 296), (174, 392), (118, 392)],
        "H2": [(178, 300), (232, 300), (240, 350), (243, 378), (205, 380), (178, 335)],
        "F1": [(222, 298), (268, 298), (270, 392), (238, 392), (230, 350)],
        "F2": [(264, 298), (314, 298), (314, 392), (262, 392)],
    }
    cut_polys = {
        "H1": [(125, 322), (175, 322), (175, 392), (115, 392)],
        "H2": [(176, 322), (230, 322), (240, 350), (243, 378), (205, 380), (178, 335)],
        "F1": [(224, 322), (268, 322), (270, 392), (238, 392), (232, 350)],
        "F2": [(264, 322), (316, 322), (316, 392), (262, 392)],
    }
    ycol = rig.field(lambda x, y: y)

    def grad(y0, y1):
        return np.clip((ycol - y0) / float(y1 - y0), 0, 1)

    leg_layers = {}
    body_m = ones.copy()
    for k in leg_polys:
        lm = P(leg_polys[k], 1.4) * grad(296, 332)
        leg_layers[k] = rig.part(lm)
        body_m = body_m - P(cut_polys[k], 1.0)
    body_m = np.clip(body_m - head_m - tail_m - stone_m - star_m, 0, 1)
    body_l = rig.part(body_m)
    head_l = rig.part(head_m)
    tail_l = rig.part(tail_m)
    stone_l = rig.part(stone_m)
    star_l = rig.part(star_m)
    piv = {"H1": (156, 300), "H2": (200, 305), "F1": (243, 296), "F2": (282, 296)}
    xcol = rig.field(lambda x, y: x)

    def render(f):
        legs = f["legs"]       # k -> (açı, dx, dy, ölçek_y)
        bob = f.get("bob", 0.0)
        pitch = f.get("pitch", 0.0)
        stretch = f.get("stretch", (1.0, 1.0))
        nod = f.get("nod", 0.0)
        tail_ang = f.get("tail", -26.0)
        tail_wave = f.get("wave", (0.0, 0.0))     # (genlik, faz)
        # kuyruk: dalga + dönme
        amp, ph = tail_wave
        u = np.clip((172 - xcol) / 172.0, 0, 1)
        dy = amp * np.sin(2 * math.pi * u * 0.9 - ph) * u
        tl = rig.warp(tail_l, 0 * dy, dy)
        tl = rig.xform(tl, (170, 262), tail_ang + f.get("tailSwing", 0.0), (0, 0), (1, 1))
        hl = rig.xform(head_l, (262, 204), nod, (f.get("headDx", 0.0), f.get("headDy", 0.0)), (1, 1))
        upper = over(over(tl, body_l), hl)
        upper = rig.xform(upper, (215, 330), pitch, (f.get("dx", 0.0), bob), stretch)

        def leg(k):
            a, dx, dyy, sy = legs[k]
            return rig.xform(leg_layers[k], piv[k], a, (dx, dyy), (1, sy))

        cv = np.zeros((CH, CW, 4), np.float32)
        cv = over(cv, leg("H2"))
        cv = over(cv, leg("F1"))
        so = f.get("orbit", (0, 0))
        cv = over(cv, rig.xform(stone_l, (125, 182), 0, (so[0], so[1] + bob * 0.5), (1, 1)))
        so2 = f.get("orbit2", (0, 0))
        cv = over(cv, rig.xform(star_l, (50, 200), 0, (so2[0], so2[1] + bob * 0.5), (1, 1)))
        cv = over(cv, upper)
        cv = over(cv, leg("H1"))
        cv = over(cv, leg("F2"))
        sq = f.get("squash")
        if sq:
            cv = rig.xform(cv, (232, base_y), 0, (0, 0), sq)
        return cv

    def L(h1, h2, f1, f2, lift=(0, 0, 0, 0), sy=(1, 1, 1, 1), dx=(0, 0, 0, 0)):
        out = {}
        for i, k in enumerate(("H1", "H2", "F1", "F2")):
            ang = (h1, h2, f1, f2)[i]
            out[k] = (ang, dx[i], -lift[i], sy[i])
        return out

    A = 26.0
    frames = []
    # 0 bekle
    frames.append(dict(legs=L(0, 0, 0, 0), wave=(3, 0.0), orbit=(0, 0), orbit2=(0, 0)))
    # yürüyüş: A çifti (H1+F1) ve B çifti (H2+F2) çapraz
    ph = [0, 90, 180, 270]
    for i, p in enumerate(ph):
        c = math.cos(math.radians(p))
        sn = math.sin(math.radians(p))
        liftA = 12 * max(0.0, -sn)
        liftB = 12 * max(0.0, sn)
        a_ang = A * c
        b_ang = -A * c
        legs = L(a_ang, b_ang - 10, a_ang * 0.9, b_ang * 0.9,
                 lift=(liftA, liftB, liftA, liftB),
                 sy=(1 - liftA * 0.012, 1 - liftB * 0.012, 1 - liftA * 0.012, 1 - liftB * 0.012))
        bob = 3.5 * abs(c) - 1.0           # temas karelerinde alçak
        frames.append(dict(
            legs=legs, bob=bob, pitch=-1.5 * c, nod=-2.0 * c + 1.0, wave=(9, math.radians(p) + 1.0), tail=-26 + 5 * math.sin(math.radians(p) + 2.6),
            orbit=(7 * math.cos(math.radians(p + 40)), 6 * math.sin(math.radians(p + 40))),
            orbit2=(4 * math.cos(math.radians(p + 200)), 5 * math.sin(math.radians(p + 200))),
            dx=0))
    # 5 zıpla: ön bacaklar ileri, arka bacaklar geride, gövde gerilmiş
    frames.append(dict(
        legs=L(-38, -30, 40, 28, lift=(10, 8, 10, 12), sy=(1, 1, 0.95, 0.95)),
        bob=-3, pitch=7, stretch=(1.05, 1.0), nod=4, tail=-36, dx=6, wave=(10, 2.0), headDy=-2,
        orbit=(-12, 10), orbit2=(-8, 14)))
    # 6 düş: bacaklar açık, kuyruk yukarı
    frames.append(dict(
        legs=L(-22, 12, 22, 40, lift=(6, 8, 10, 8)),
        bob=-1, pitch=-6, stretch=(1.0, 1.0), nod=-5, tail=-50, wave=(8, 3.5), headDy=0,
        orbit=(-8, -16), orbit2=(-6, -18)))
    # 7 iniş: çömel
    frames.append(dict(
        legs=L(8, -4, -4, 4, sy=(0.78, 0.78, 0.78, 0.78), dx=(-3, -2, 2, 3)),
        bob=16, pitch=2, stretch=(1.02, 1.0), nod=-3, tail=-12, wave=(5, 1.0), headDy=2,
        squash=(1.02, 0.87), orbit=(2, 8), orbit2=(0, 8)))
    return rig, render, frames


def build_fox():
    rig, render, poses = fox_frames()
    return [rig.finish(render(p)) for p in poses]


# --------------------------------------------------------------------------- çıktı
def pack(frames):
    sheet = Image.new("RGBA", (CELL_W * 4, CELL_H * 2), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.alpha_composite(f, ((i % 4) * CELL_W, (i // 4) * CELL_H))
    return sheet


def contact(frames, path, extra=None):
    bg = (38, 26, 40, 255)
    sheet = Image.new("RGBA", (CELL_W * len(frames), CELL_H), bg)
    for i, f in enumerate(frames):
        sheet.alpha_composite(f, (i * CELL_W, 0))
    return sheet


def main():
    os.makedirs(OUT, exist_ok=True)
    rows = []
    for name, fn in (("kok_kalbi", build_tree), ("ay_kirigi_tilkisi", build_fox)):
        fr = fn()
        pack(fr).save(os.path.join(OUT, name + ".png"))
        rows.append(fr)
        print("tamam:", name)
    # önizleme: her kostüm için 8 kare + 4 kare yürüyüş döngüsü (1-4-1)
    bg = (38, 26, 40, 255)
    board = Image.new("RGBA", (CELL_W * 8, CELL_H * 2), bg)
    for r, fr in enumerate(rows):
        for i, f in enumerate(fr):
            board.alpha_composite(f, (i * CELL_W, r * CELL_H))
    board.convert("RGB").save(os.path.join(SRC, "_anim_b_preview.png"))


if __name__ == "__main__":
    main()
