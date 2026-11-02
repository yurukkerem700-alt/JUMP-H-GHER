#!/usr/bin/env python3
"""PlayerController zıplama sabitlerinin kat aralığını koruduğunu Unity fizik adımıyla (0.02 sn, yarı-örtük Euler) doğrular.
Sabitler PlayerController.cs ile aynı olmalıdır; dosyadan okunur."""
import re, sys, pathlib

src = pathlib.Path(__file__).resolve().parent.parent / "Assets/Scripts/PlayerController.cs"
text = src.read_text()
def const(name):
    return float(re.search(r"const float %s\s*=\s*([\d.]+)f" % name, text).group(1))
def field(name):
    return float(re.search(r"public float %s\s*=\s*([\d.]+)f" % name, text).group(1))

DT, G, BASE = 0.02, 9.81, 2.0
FALL, APEX, APEXV = const("FallGravityMultiplier"), const("ApexGravityMultiplier"), const("ApexVelocity")
MAXFALL, HOP, JUMP, SPEED = const("MaxFallSpeed"), const("ShortHopVelocity"), field("jumpForce"), field("moveSpeed")
GAP = 1.15  # InfiniteTower.FloorGap

def simulate(release_at=None):
    """Yerden zıpla; release_at saniyede tuşu bırak. (tepe yüksekliği, tepe süresi, toplam havada kalma) döner."""
    y, vy, t, rising, apex, apex_t = 0.0, JUMP, 0.0, True, 0.0, 0.0
    while True:
        # FixedUpdate: değişken zıplama + yerçekimi ölçeği
        if rising:
            if vy <= 0: rising = False
            elif release_at is not None and t >= release_at:
                rising = False
                vy = min(vy, HOP)
        scale = BASE
        if vy < -.1: scale *= FALL
        elif abs(vy) < APEXV: scale *= APEX
        vy = max(vy - G * scale * DT, -MAXFALL)
        y += vy * DT; t += DT
        if y > apex: apex, apex_t = y, t
        if y <= 0 and t > DT: return apex, apex_t, t

fail = 0
def check(name, ok, detail):
    global fail
    print(("PASS" if ok else "FAIL"), name, "-", detail)
    fail += 0 if ok else 1

hold = simulate(None)
tap = simulate(0.0)            # anında bırakma (hızlı dokunuş)
mid = simulate(0.1)
check("tam zıplama kat aralığını aşar", hold[0] > GAP + 0.3, "tepe %.2f, kat aralığı %.2f" % (hold[0], GAP))
check("hızlı dokunuş da bir kat çıkar", tap[0] > GAP + 0.15, "tepe %.2f" % tap[0])
check("değişken zıplama gerçekten fark yaratır", hold[0] - tap[0] > 0.15, "fark %.2f" % (hold[0] - tap[0]))
check("ara bırakma iki ucun arasında", tap[0] <= mid[0] <= hold[0], "%.2f <= %.2f <= %.2f" % (tap[0], mid[0], hold[0]))
check("tepeye çıkış süresi makul (<0.6 sn)", hold[1] < 0.6, "%.2f sn" % hold[1])
check("yatay erişim: tam zıplamada kat başına yatay mesafe yeterli", SPEED * hold[2] > 1.5, "%.1f birim havada" % (SPEED * hold[2]))
# Eski davranışla kıyas: düşüş artık daha kısa olmalı (daha tok his), yükseliş aynı
old_apex = JUMP**2 / (2 * G * BASE)
check("yükseliş yüksekliği eskiye yakın (apex süzülmesi küçük fark)", abs(hold[0] - old_apex) < 0.15, "eski %.2f yeni %.2f" % (old_apex, hold[0]))
print("\n%d başarısız" % fail)
sys.exit(1 if fail else 0)
