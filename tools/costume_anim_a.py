"""Kul Mumcusu, Bulut Balikcisi ve Yildirim Kuklasi icin parcali (rig) animasyon sayfalari uretir.

Kullanim: python tools/costume_anim_a.py
Cikti:    Assets/Resources/Costumes/<id>.png (1536x1024, 4x2 kare: bekle, yurume x4, ziplama, dusme, inis)
Onizleme: tools/costume_source/_anim_a_preview.png
Her kostum tek pozdan parcalara (bacak, kol, asa, fener, pelerin ...) ayrilir; parcalar mafsal
noktalarindan dondurulup oteleniyor, kumaslar sinusla egiliyor, arkadan one birlestiriliyor.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_costume_sheets as m  # noqa: E402

CELL_W, CELL_H, MARGIN_BOTTOM = m.CELL_W, m.CELL_H, m.MARGIN_BOTTOM
CW, CH = 720, 760          # calisma tuvali
OX, OY = 190, 150          # sprite'in tuval icindeki yeri


# ---------------------------------------------------------------- yardimcilar
def bleed(img):
    """Seffaf piksellerin RGB'sini en yakin opak pikselden doldur (kenarda beyaz hale olmasin)."""
    a = np.asarray(img).copy()
    solid = a[..., 3] > 8
    idx = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
    rgb = a[..., :3][idx[0], idx[1]]
    a[..., :3] = rgb
    return Image.fromarray(a, "RGBA")


def clean_specks(sprite, min_area):
    a = np.asarray(sprite).copy()
    solid = a[..., 3] > 40
    grown = ndimage.binary_dilation(solid, iterations=6)
    lab, n = ndimage.label(grown)
    sizes = ndimage.sum(solid, lab, index=np.arange(1, n + 1))
    keep = np.isin(lab, 1 + np.where(sizes >= min_area)[0])
    a[..., 3] = np.where(keep, a[..., 3], 0)
    img = Image.fromarray(a, "RGBA")
    return img.crop(img.getbbox())


def load_sprite(name, flip=False, thin=0, speck=300):
    img = Image.open(os.path.join(m.SRC, name + ".jpg"))
    if flip:
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
    s = m.cut_out(img, thin)
    s = clean_specks(s, speck)
    sc = min(m.MAX_W / s.width, m.MAX_H / s.height)
    s = s.resize((max(1, round(s.width * sc)), max(1, round(s.height * sc))), Image.LANCZOS)
    return bleed(s)


def poly_mask(size, polys, feather=1.0):
    im = Image.new("L", size, 0)
    d = ImageDraw.Draw(im)
    for p in polys:
        d.polygon([tuple(q) for q in p], fill=255)
    if feather:
        im = im.filter(ImageFilter.GaussianBlur(feather))
    return np.asarray(im).astype(np.float32) / 255.0


def affine(layer, pivot, rot=0.0, dx=0.0, dy=0.0, sx=1.0, sy=1.0):
    """layer (tuval boyutunda RGBA, premultiplied degil) -> pivot etrafinda olcek+donme, sonra otele."""
    if rot == 0 and dx == 0 and dy == 0 and sx == 1 and sy == 1:
        return layer
    t = math.radians(rot)
    c, s = math.cos(t), math.sin(t)
    px, py = pivot
    R = np.array([[c, -s], [s, c]]) @ np.diag([sx, sy])
    fwd = np.eye(3)
    fwd[:2, :2] = R
    fwd[:2, 2] = np.array([px + dx, py + dy]) - R @ np.array([px, py])
    inv = np.linalg.inv(fwd)
    data = (inv[0, 0], inv[0, 1], inv[0, 2], inv[1, 0], inv[1, 1], inv[1, 2])
    return layer.transform(layer.size, Image.AFFINE, data, Image.BICUBIC)


def bend(layer, y0, y1, amp, power=1.6, x0=None, x1=None):
    """Satirlari yatay kaydir: y0'da 0, y1'de amp piksel (kumas / sac savrulmasi)."""
    if amp == 0:
        return layer
    a = np.asarray(layer).astype(np.float32)
    h, w = a.shape[:2]
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:4] / 255.0
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    t = np.clip((ys - y0) / float(y1 - y0), 0, 1) ** power
    if y1 < y0:
        t = np.clip((ys - y0) / float(y1 - y0), 0, 1) ** power
    shift = amp * t
    out = np.zeros_like(pre)
    for ch in range(4):
        out[..., ch] = ndimage.map_coordinates(pre[..., ch], [ys, xs - shift], order=1, mode="constant")
    al = out[..., 3:4]
    rgb = np.where(al > 0.5, out[..., :3] * 255.0 / np.maximum(al, 1), 0)
    res = np.concatenate([rgb, al], axis=2)
    return Image.fromarray(np.clip(res, 0, 255).astype(np.uint8), "RGBA")


def ghost_fill(arr_rgba, hole):
    """Parca cikarilinca govdede kalan boslugu komsu renkle doldur (hayalet kopya olmasin)."""
    solid = (arr_rgba[..., 3] > 128) & ~hole
    idx = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
    filled = arr_rgba[..., :3][idx[0], idx[1]].astype(np.float32)
    filled = np.stack([ndimage.gaussian_filter(filled[..., i], 1.5) for i in range(3)], axis=2)
    out = arr_rgba.copy()
    sel = hole & (arr_rgba[..., 3] > 20)
    out[..., :3][sel] = filled[sel].astype(np.uint8)
    out[..., 3][hole & (arr_rgba[..., 3] > 20)] = arr_rgba[..., 3][hole & (arr_rgba[..., 3] > 20)]
    return out


class Rig:
    def __init__(self, sprite, parts, order, anchor, parent=None):
        """parts: ad -> dict(poly=[...], cut=[...] (opsiyonel; verilmezse poly), pivot=(x,y), ghost=bool)
        order: arkadan one cizim sirasi; 'rest' kalan govde."""
        self.sprite = sprite
        self.w, self.h = sprite.size
        self.order = order
        self.parent = parent or {}
        self.anchor = anchor                    # (x, y) sprite koordinati: ayaklarin orta noktasi / zemin
        full = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
        full.paste(sprite, (OX, OY))
        self.full = np.asarray(full).copy()
        alpha = self.full[..., 3].astype(np.float32) / 255.0
        self.layers = {}
        self.pivots = {}
        cutsum = np.zeros((CH, CW), np.float32)
        for name, p in parts.items():
            mk = np.zeros((CH, CW), np.float32)
            mk[OY:OY + self.h, OX:OX + self.w] = poly_mask((self.w, self.h), p["poly"], p.get("feather", 1.0))
            cut = mk
            if "cut" in p:
                cut = np.zeros((CH, CW), np.float32)
                cut[OY:OY + self.h, OX:OX + self.w] = poly_mask((self.w, self.h), p["cut"], p.get("feather", 1.0))
            layer = self.full.copy()
            eff = mk
            for other in p.get("minus", []):
                o = parts[other]
                om = np.zeros((CH, CW), np.float32)
                om[OY:OY + self.h, OX:OX + self.w] = poly_mask((self.w, self.h), o["poly"], 0.6)
                eff = eff * (1 - om)
            layer[..., 3] = (alpha * eff * 255).astype(np.uint8)
            self.layers[name] = Image.fromarray(layer, "RGBA")
            self.pivots[name] = (p["pivot"][0] + OX, p["pivot"][1] + OY)
            p["_cut"] = cut
            cutsum = np.maximum(cutsum, cut)
        rest = self.full.copy()
        keep_extra = np.zeros((CH, CW), bool)
        hole = np.zeros((CH, CW), bool)
        for name, p in parts.items():
            if p.get("ghost"):
                gm = np.ones((CH, CW), bool)
                if "ghost_poly" in p:
                    gm = np.zeros((CH, CW), np.float32)
                    gm[OY:OY + self.h, OX:OX + self.w] = poly_mask((self.w, self.h), p["ghost_poly"], 0)
                    gm = gm > 0.5
                h_ = (p["_cut"] > 0.5) & gm
                hole |= h_
                keep_extra |= h_ & (self.full[..., 3] > 20)
        if hole.any():
            rest = ghost_fill(rest, hole)
        rest[..., 3] = (alpha * (1 - cutsum) * 255).astype(np.uint8)
        rest[..., 3][keep_extra] = self.full[..., 3][keep_extra]
        self.layers["rest"] = Image.fromarray(rest, "RGBA")
        self.pivots["rest"] = (anchor[0] + OX, anchor[1] + OY)

    def render(self, pose, glob=None):
        canvas = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
        order = pose.get("_order", self.order)
        for name in order:
            op = pose.get(name, {})
            lay = self.layers[name]
            if "bend" in op:
                for b in (op["bend"] if isinstance(op["bend"], list) else [op["bend"]]):
                    y0, y1, amp = b[:3]
                    lay = bend(lay, y0 + OY, y1 + OY, amp, *(b[3:4]))
            cur, o = name, op
            while True:
                piv = self.pivots[cur]
                if "pivot" in o:
                    piv = (o["pivot"][0] + OX, o["pivot"][1] + OY)
                lay = affine(lay, piv, o.get("rot", 0), o.get("dx", 0), o.get("dy", 0),
                             o.get("sx", 1), o.get("sy", 1))
                par = self.parent.get(cur)
                if not par:
                    break
                cur, o = par, pose.get(par, {})
            canvas.alpha_composite(lay)
        if glob:
            gp = (self.anchor[0] + OX, self.anchor[1] + OY)
            canvas = affine(canvas, gp, glob.get("rot", 0), glob.get("dx", 0), glob.get("dy", 0),
                            glob.get("sx", 1), glob.get("sy", 1))
        return canvas

    def to_cell(self, canvas):
        a = np.asarray(canvas)[..., 3]
        # kucuk lekeleri (ayrik kalan parlak pikseller) at
        solid = a > 40
        lab, n = ndimage.label(ndimage.binary_dilation(solid, iterations=5))
        if n > 1:
            sizes = ndimage.sum(solid, lab, index=np.arange(1, n + 1))
            keep = np.isin(lab, 1 + np.where(sizes >= 60)[0])
            arr = np.asarray(canvas).copy()
            arr[..., 3] = np.where(keep, arr[..., 3], 0)
            canvas = Image.fromarray(arr, "RGBA")
            a = arr[..., 3]
        ys = np.where((a > 128).sum(axis=1) >= 5)[0]
        low = ys.max()
        # yatay: sprite'in anchor x'i hucre ortasina, en alt opak satir zemine
        left = int(round(CELL_W / 2 - (self.anchor[0] + OX)))
        top = int(round(CELL_H - MARGIN_BOTTOM - (low + 1)))
        cell = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        cell.alpha_composite(canvas, (0, 0), (0, 0)) if False else None
        tmp = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        sx0, sy0 = max(-left, 0), max(-top, 0)
        dx0, dy0 = max(left, 0), max(top, 0)
        wcut = min(CW - sx0, CELL_W - dx0)
        hcut = min(CH - sy0, CELL_H - dy0)
        tmp.alpha_composite(canvas.crop((sx0, sy0, sx0 + wcut, sy0 + hcut)), (dx0, dy0))
        return tmp


def S(phase):
    return math.sin(phase)


def walk_phases():
    return [k * math.pi / 2 for k in range(4)]


# ---------------------------------------------------------------- KUL MUMCUSU
def rig_kul():
    sp = load_sprite("kul_mumcusu", flip=True)
    parts = {
        "staff": dict(poly=[[(0, 130), (80, 130), (80, 196), (108, 240), (112, 282), (112, 330), (106, 380), (122, 440), (0, 440)]],
                      pivot=(80, 290), minus=["lantern"]),
        "lantern": dict(poly=[[(0, 192), (44, 192), (48, 232), (58, 262), (60, 300), (60, 348), (0, 348)]], pivot=(34, 206)),
        "flame": dict(poly=[[(110, 0), (190, 0), (190, 66), (110, 66)]], pivot=(150, 66), feather=1.5),
        "arm": dict(poly=[[(157, 256), (214, 258), (218, 330), (206, 362), (172, 362), (160, 336)]],
                    pivot=(182, 266), ghost=True),
        "legB": dict(poly=[[(126, 358), (168, 358), (170, 440), (126, 440)]], cut=[[(126, 382), (168, 382), (170, 440), (126, 440)]],
                     pivot=(148, 372)),
        "legF": dict(poly=[[(164, 358), (218, 358), (224, 440), (164, 440)]], cut=[[(166, 382), (222, 382), (224, 440), (166, 440)]],
                     pivot=(190, 372)),
    }
    order = ["legB", "staff", "lantern", "legF", "rest", "arm", "flame"]
    return Rig(sp, parts, order, anchor=(150, 440), parent={"lantern": "staff"})


def kul_poses():
    P = []
    P.append({})  # bekle
    for ph in walk_phases():
        s, c = S(ph), math.cos(ph)
        bob = -4.0 * (1 - abs(s))
        lift = lambda v: -9.0 * max(0.0, v)
        P.append({
            "legF": dict(rot=-15 * s, dy=bob + lift(c), dx=4 * s),
            "legB": dict(rot=15 * s, dy=bob + lift(-c), dx=-4 * s),
            "rest": dict(dy=bob, rot=2.0 + 1.2 * c, bend=[(330, 392, -3.5 + 3 * c, 1.8)]),
            "arm": dict(rot=-16 * s, dy=bob),
            "staff": dict(rot=7 * s, dy=bob, dx=-1),
            "lantern": dict(rot=14 * math.sin(ph - 1.0) + 7 * s),
            "flame": dict(rot=5 * math.sin(ph * 2), dy=bob, dx=-1.5 + 1.5 * c),
        })
    # ziplama: yukselis
    P.append({
        "legB": dict(rot=34, dx=-6, dy=-6),
        "legF": dict(rot=-26, dx=4, dy=-18),
        "rest": dict(bend=[(300, 392, -4, 1.4)]),
        "arm": dict(rot=-62, dy=-2),
        "staff": dict(rot=-8, dy=-14),
        "lantern": dict(rot=16),
        "flame": dict(rot=-6, sy=1.1, dy=-2),
        "_g": dict(sy=1.02, sx=0.98),
    })
    # dusus
    P.append({
        "legB": dict(rot=14, dx=-3, dy=0),
        "legF": dict(rot=-10, dx=3, dy=4),
        "rest": dict(bend=[(300, 392, 3.0, 1.2)]),
        "arm": dict(rot=-70, dy=-1),
        "staff": dict(rot=12, dy=-6),
        "lantern": dict(rot=-30),
        "flame": dict(rot=7, sy=1.05, sx=0.95, dy=-1),
        "_g": dict(sy=1.01, sx=0.99),
    })
    # inis
    P.append({
        "legB": dict(rot=20, dx=-10, dy=2),
        "legF": dict(rot=-20, dx=8, dy=2),
        "rest": dict(bend=[(330, 392, 3.5, 1.4)]),
        "arm": dict(rot=14, dy=3),
        "staff": dict(rot=-3, dy=3),
        "lantern": dict(rot=-14),
        "flame": dict(rot=-4, sy=0.85, sx=1.1, dy=6),
        "_g": dict(sy=0.86, sx=1.10),
    })
    return P



# ---------------------------------------------------------------- BULUT BALIKCISI
def rig_bulut():
    sp = load_sprite("bulut_balikcisi")
    parts = {
        "hat": dict(poly=[[(0, 176), (40, 154), (100, 130), (150, 110), (186, 100), (214, 108), (240, 108), (246, 0), (0, 0)]],
                    pivot=(150, 112), ghost=True),
        "fish": dict(poly=[[(0, 178), (50, 178), (50, 272), (0, 272)]], pivot=(18, 178)),
        "beard": dict(poly=[[(142, 156), (200, 152), (224, 170), (224, 200), (204, 224), (186, 228), (162, 212), (142, 186)]],
                      pivot=(178, 158), ghost=True),
        "arm": dict(poly=[[(125, 205), (185, 196), (205, 205), (214, 214), (245, 214), (245, 257), (217, 261), (217, 292),
                           (186, 298), (150, 298), (122, 262)]], pivot=(145, 218), ghost=True, ghost_poly=[[(100, 190), (218, 190), (218, 300), (100, 300)]]),
        "rod": dict(poly=[[(236, 90), (345, 90), (345, 340), (236, 340)]], pivot=(237, 224), minus=["cloud"]),
        "cloud": dict(poly=[[(280, 204), (345, 204), (345, 340), (238, 340), (238, 244), (280, 244)]], pivot=(303, 205)),
        "hem": dict(poly=[[(66, 286), (182, 286), (182, 346), (66, 346)]], pivot=(128, 288), ghost=True),
        "legB": dict(poly=[[(108, 340), (153, 340), (153, 410), (108, 410)]], cut=[[(108, 350), (153, 350), (153, 410), (108, 410)]],
                     pivot=(130, 346)),
        "legF": dict(poly=[[(148, 338), (196, 338), (196, 396), (148, 396)]], cut=[[(150, 350), (196, 350), (196, 396), (150, 396)]],
                     pivot=(172, 346)),
    }
    order = ["legB", "legF", "fish", "rest", "hem", "beard", "hat", "arm", "rod", "cloud"]
    return Rig(sp, parts, order, anchor=(172, 410), parent={"rod": "arm", "cloud": "rod"})


def bulut_poses():
    P = [{}]
    for ph in walk_phases():
        s, c = S(ph), math.cos(ph)
        fl = 0.5 + 0.5 * s
        bob = -8.0 * fl
        P.append({
            "rest": dict(dy=bob, rot=1.5 * c + 1.0, bend=[(250, 346, -2.0 + 2.5 * c, 1.8)]),
            "hem": dict(dy=bob, rot=3.0 * c - 1),
            "legF": dict(dy=bob + 3, rot=-26 * s - 5, dx=4 * s),
            "legB": dict(dy=bob + 3, rot=26 * s + 5, dx=-4 * s),
            "fish": dict(rot=13 * math.sin(ph - 1.2), dy=bob),
            "beard": dict(dy=bob, rot=-5 * c - 2, bend=[(158, 228, -3.0 * c, 1.5)]),
            "hat": dict(dy=bob, rot=2.2 * math.sin(ph + 0.8), bend=[(176, 0, -4.0 * math.sin(ph - 0.6), 2.0)]),
            "arm": dict(dy=bob, rot=4.0 * math.sin(ph + 0.6)),
            "rod": dict(rot=3.0 * math.sin(ph + 2.0)),
            "cloud": dict(rot=11 * math.sin(ph - 0.9)),
        })
    P.append({  # yukselis
        "rest": dict(dy=-6, bend=[(250, 346, -4, 1.4)]),
        "hem": dict(dy=-6, rot=-12),
        "legB": dict(dy=-4, rot=40, dx=-5),
        "legF": dict(dy=-10, rot=-34, dx=4),
        "fish": dict(rot=24, dy=-8),
        "beard": dict(dy=-6, rot=-14, bend=[(158, 228, -5, 1.4)]),
        "hat": dict(dy=-6, rot=-5, bend=[(176, 0, 8, 2.0)]),
        "arm": dict(dy=-6, rot=-13),
        "rod": dict(rot=-6),
        "cloud": dict(rot=24),
        "_g": dict(sy=1.03, sx=0.98),
    })
    P.append({  # dusus
        "rest": dict(dy=-2, bend=[(250, 346, 3, 1.4)]),
        "hem": dict(dy=-2, rot=22),
        "legB": dict(dy=2, rot=12, dx=-3),
        "legF": dict(dy=0, rot=-10, dx=3),
        "fish": dict(rot=-34, dy=-18, sy=0.92),
        "beard": dict(dy=-2, rot=16, bend=[(158, 228, 5, 1.4)]),
        "hat": dict(dy=-2, rot=9, bend=[(176, 0, -6, 2.0)]),
        "arm": dict(dy=-2, rot=-18),
        "rod": dict(rot=-4),
        "cloud": dict(rot=-26),
        "_g": dict(sy=1.0),
    })
    P.append({  # inis
        "rest": dict(dy=4, bend=[(250, 346, 3, 1.4)]),
        "hem": dict(dy=4, rot=-4),
        "legB": dict(dy=4, rot=22, dx=-9),
        "legF": dict(dy=4, rot=-22, dx=9),
        "fish": dict(rot=-12, dy=4),
        "beard": dict(dy=4, rot=6),
        "hat": dict(dy=5, rot=2.5),
        "arm": dict(dy=4, rot=6),
        "rod": dict(rot=4),
        "cloud": dict(rot=-14),
        "_g": dict(sy=0.88, sx=1.09),
    })
    return P



# ---------------------------------------------------------------- YILDIRIM KUKLASI
def rig_kukla():
    sp = load_sprite("yildirim_kuklasi", thin=7, speck=1500)
    parts = {
        "cape": dict(poly=[[(40, 198), (121, 198), (121, 400), (40, 400)]], pivot=(112, 212)),
        "mask": dict(poly=[[(150, 66), (250, 66), (250, 200), (150, 200)]], pivot=(200, 190), ghost=True, feather=0.8),
        "head": dict(poly=[[(0, 0), (261, 0), (261, 214), (0, 214)]], cut=[[(0, 0), (261, 0), (261, 204), (0, 204)]],
                     pivot=(150, 204), minus=["cape"]),
        "armL": dict(poly=[[(112, 220), (140, 220), (142, 272), (152, 298), (170, 306), (170, 342), (122, 344), (112, 300)]], pivot=(132, 232), ghost=True, ghost_poly=[[(121, 215), (192, 215), (192, 346), (121, 346)]]),
        "armR": dict(poly=[[(184, 232), (250, 232), (250, 320), (205, 320), (186, 290)]], pivot=(196, 244), ghost=True, ghost_poly=[[(121, 215), (190, 215), (190, 346), (121, 346)]]),
        "legL": dict(poly=[[(116, 326), (156, 326), (156, 440), (116, 440)]], cut=[[(116, 346), (156, 346), (156, 440), (116, 440)]],
                     pivot=(138, 340)),
        "legR": dict(poly=[[(154, 326), (200, 326), (200, 440), (154, 440)]], cut=[[(154, 346), (200, 346), (200, 440), (154, 440)]],
                     pivot=(174, 340)),
    }
    order = ["cape", "legL", "legR", "rest", "head", "mask", "armL", "armR"]
    return Rig(sp, parts, order, anchor=(150, 440))


def kukla_poses():
    P = [{}]
    for ph in walk_phases():
        s, c = S(ph), math.cos(ph)
        bob = -3.5 * (1 - abs(s))
        lift = lambda v: -8.0 * max(0.0, v)
        sw = 0.5 + 0.5 * s
        P.append({
            "legR": dict(rot=-17 * s, dy=bob + lift(c), dx=3 * s),
            "legL": dict(rot=17 * s, dy=bob + lift(-c), dx=-3 * s),
            "rest": dict(dy=bob, rot=1.2 * c, bend=[(300, 346, 0, 1)]),
            "cape": dict(dy=bob, rot=-3.0 + 2.5 * c, bend=[(212, 398, -9 + 6 * s, 1.5)]),
            "armL": dict(rot=-18 * s, dy=bob),
            "armR": dict(rot=18 * s, dy=bob),
            "head": dict(dy=bob - 1.5 * abs(c), rot=3.0 * math.sin(ph + 1.2), bend=[(204, 0, 3.0 * c, 1.3)]),
            "mask": dict(dy=bob - 1.5 * abs(c), rot=4.5 * math.sin(ph + 1.2) + 1, dx=1.5 * s),
        })
    P.append({  # yukselis
        "legL": dict(rot=30, dx=-5, dy=-8),
        "legR": dict(rot=-36, dx=5, dy=-22),
        "rest": dict(dy=-6),
        "cape": dict(dy=-6, rot=-4, bend=[(212, 398, -4, 1.2)]),
        "armL": dict(rot=112, dy=-6),
        "armR": dict(rot=-98, dy=-6),
        "head": dict(dy=-8, rot=-3, bend=[(204, 0, 5, 1.3)]),
        "mask": dict(dy=-8, rot=-2),
        "_g": dict(sy=1.03, sx=0.98),
    })
    P.append({  # dusus
        "legL": dict(rot=10, dx=-3, dy=-4),
        "legR": dict(rot=-14, dx=3, dy=-6),
        "rest": dict(dy=-4),
        "cape": dict(dy=-4, rot=24, bend=[(212, 398, 6, 1.3)]),
        "armL": dict(rot=125, dy=-4),
        "armR": dict(rot=-105, dy=-4),
        "head": dict(dy=-5, rot=4, bend=[(204, 0, -5, 1.3)]),
        "mask": dict(dy=-5, rot=5),
    })
    P.append({  # inis
        "legL": dict(rot=20, dx=-8, dy=2),
        "legR": dict(rot=-22, dx=8, dy=2),
        "rest": dict(dy=3),
        "cape": dict(dy=3, rot=-12, bend=[(212, 398, 7, 1.3)]),
        "armL": dict(rot=16, dy=3),
        "armR": dict(rot=-16, dy=3),
        "head": dict(dy=5, rot=-2, bend=[(204, 0, 2, 1.3)]),
        "mask": dict(dy=5, rot=-3),
        "_g": dict(sy=0.88, sx=1.10),
    })
    return P


# ---------------------------------------------------------------- ortak calistirici
def run_costume(name, rig, poses, out_dir):
    sheet = Image.new("RGBA", (CELL_W * 4, CELL_H * 2), (0, 0, 0, 0))
    cells = []
    for i, pose in enumerate(poses):
        glob = pose.get("_g")
        cv = rig.render(pose, glob)
        cell = rig.to_cell(cv)
        cells.append(cell)
        sheet.alpha_composite(cell, ((i % 4) * CELL_W, (i // 4) * CELL_H))
    os.makedirs(out_dir, exist_ok=True)
    sheet.save(os.path.join(out_dir, name + ".png"))
    return sheet


def height_report(name, sheet):
    hs = []
    for i in range(8):
        c = np.asarray(sheet.crop(((i % 4) * CELL_W, (i // 4) * CELL_H, (i % 4 + 1) * CELL_W, (i // 4 + 1) * CELL_H)))[..., 3]
        ys = np.where((c > 128).sum(axis=1) >= 5)[0]
        xs = np.where((c > 128).sum(axis=0) >= 1)[0]
        hs.append((int(ys.max() - ys.min() + 1), int(xs.min()), int(xs.max())))
    print(name, "yukseklik/xmin/xmax:", hs)


def zoom_preview(sheet, path, scale=0.7):
    W, H = sheet.size
    bg = Image.new("RGBA", (W, H), (38, 26, 40, 255))
    bg.alpha_composite(sheet)
    bg.convert("RGB").resize((int(W * scale), int(H * scale)), Image.LANCZOS).save(path)


def preview(sheets, path):
    rows = []
    W = CELL_W * 8
    board = Image.new("RGBA", (W, CELL_H * len(sheets) + CELL_H // 2 * len(sheets)), (38, 26, 40, 255))
    y = 0
    for name, sh in sheets:
        for i in range(8):
            cell = sh.crop(((i % 4) * CELL_W, (i // 4) * CELL_H, (i % 4 + 1) * CELL_W, (i // 4 + 1) * CELL_H))
            board.alpha_composite(cell, (i * CELL_W, y))
        y += CELL_H
    board = board.crop((0, 0, W, y))
    board.convert("RGB").resize((W // 2, y // 2), Image.LANCZOS).save(path)


COSTUMES = {
    "kul_mumcusu": (rig_kul, kul_poses),
    "bulut_balikcisi": (rig_bulut, bulut_poses),
    "yildirim_kuklasi": (rig_kukla, kukla_poses),
}


def main(only=None):
    out_dir = m.OUT
    done = []
    for name, (rf, pf) in COSTUMES.items():
        if only and name not in only:
            continue
        sh = run_costume(name, rf(), pf(), out_dir)
        height_report(name, sh)
        done.append((name, sh))
        print("tamam:", name)
    preview(done, os.path.join(m.SRC, "_anim_a_preview.png"))


if __name__ == "__main__":
    main(sys.argv[1:] or None)
