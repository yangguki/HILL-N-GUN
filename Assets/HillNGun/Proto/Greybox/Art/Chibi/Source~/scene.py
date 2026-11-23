from common import *
import random

W, H = 1920, 1080
VX, VY = 960, 356
WOOD = (178, 112, 62, 255)
WOOD_L = (204, 140, 82, 255)
WOOD_D = (138, 84, 46, 255)
SIGN = (96, 58, 34, 255)
CREAM = (252, 236, 200, 255)
GLASS = (96, 150, 200, 255)


def sky(p):
    top, bot = (78, 160, 232), (182, 222, 248)
    for yy in range(0, 380):
        t = yy / 380
        c = tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)) + (255,)
        p.d.rectangle([0, yy * S, W * S, (yy + 1) * S], fill=c)
    random.seed(3)
    for cx, cy, sc in ((230, 70, 1.1), (610, 120, 0.8), (980, 60, 1.0), (1330, 110, 0.85), (1700, 66, 1.15), (1480, 200, 0.6), (420, 210, 0.55)):
        cloud(p, cx, cy, sc)


def cloud(p, cx, cy, sc):
    oc = (120, 168, 214, 255)
    blobs = [(-60, 10, 34), (-25, -12, 42), (20, -18, 46), (62, 4, 34), (0, 14, 36), (-40, 18, 26), (40, 20, 28)]
    for dx, dy, r in blobs:
        p.circle(cx + dx * sc, cy + dy * sc, r * sc + 3, oc, 0)
    for dx, dy, r in blobs:
        p.circle(cx + dx * sc, cy + dy * sc, r * sc, (255, 255, 255, 255), 0)
    p.rect((cx - 92 * sc, cy + 14 * sc, cx + 92 * sc, cy + 36 * sc), (255, 255, 255, 255), 0, r=int(10 * sc))


def mesas(p):
    oc = (112, 52, 40, 255)
    far = (232, 150, 112, 255)
    farD = (206, 120, 88, 255)
    near = (214, 104, 66, 255)
    nearD = (178, 78, 50, 255)
    # far range
    pts = [(0, 330), (40, 300), (120, 296), (150, 236), (290, 234), (320, 300), (430, 304), (470, 250), (600, 252),
           (640, 312), (760, 314), (800, 290), (900, 292), (930, 320), (1040, 322), (1080, 248), (1200, 246),
           (1240, 312), (1350, 312), (1390, 240), (1530, 238), (1560, 304), (1680, 306), (1720, 252), (1840, 252),
           (1880, 306), (1920, 308), (1920, 380), (0, 380)]
    p.poly(pts, far, 3, oc)
    for x0, x1, yt in ((150, 290, 236), (470, 600, 250), (1080, 1200, 248), (1390, 1530, 240), (1720, 1840, 252)):
        p.poly([(x1 - 30, yt + 2), (x1, yt), (x1 + 30, yt + 34), (x1 - 10, yt + 34)], farD, 0)
    # near mesas at sides
    for side in (-1, 1):
        def X(x):
            return x if side < 0 else W - x
        m = [(X(0), 360), (X(0), 190), (X(60), 176), (X(110), 150), (X(250), 148), (X(280), 178), (X(330), 182),
             (X(360), 212), (X(470), 214), (X(500), 250), (X(560), 256), (X(600), 300), (X(640), 360)]
        p.poly(m, near, 3.5, oc)
        p.poly([(X(250), 150), (X(280), 178), (X(330), 182), (X(360), 212), (X(330), 230), (X(270), 226), (X(240), 190)],
               nearD, 0)
        p.poly([(X(470), 214), (X(500), 250), (X(560), 256), (X(600), 300), (X(520), 300), (X(480), 260)], nearD, 0)
        for yy in (230, 270, 310):
            p.line([(X(30), yy), (X(180), yy + 4)], nearD, 3)


def ground(p):
    sand = (238, 198, 140, 255)
    p.rect((0, 340, W, H), sand, 0)
    # street (lighter) converging to vanishing point
    p.poly([(VX - 60, VY), (VX + 60, VY), (W + 260, H), (-260, H)], (244, 212, 160, 255), 0)
    # ruts
    for off in (-0.33, -0.12, 0.12, 0.33):
        p.line([(VX + off * 60, VY + 4), (VX + off * 1700, H)], (226, 186, 132, 255), 4)
    random.seed(11)
    for _ in range(420):
        yy = random.uniform(380, H)
        t = (yy - VY) / (H - VY)
        xx = random.uniform(-50, W + 50)
        r = 2 + 9 * t * random.uniform(0.5, 1.0)
        c = random.choice([(206, 162, 112, 255), (214, 172, 120, 255), (190, 150, 108, 255)])
        p.ellipse((xx - r * 1.5, yy - r, xx + r * 1.5, yy + r * 0.8), c, max(1.2, 2.4 * t) if r > 5 else 0,
                  (150, 108, 70, 255))
    for _ in range(26):
        yy = random.uniform(420, H)
        t = (yy - VY) / (H - VY)
        xx = random.choice([random.uniform(0, 520), random.uniform(1400, W)])
        tuft(p, xx, yy, 0.5 + 1.2 * t)


def tuft(p, x, y, k):
    g, gd = (122, 176, 74, 255), (82, 128, 52, 255)
    for dx, h, lean in ((-8, 16, -6), (0, 22, 0), (8, 15, 6)):
        p.poly([(x + dx * k - 3 * k, y), (x + dx * k + 3 * k, y), (x + (dx + lean) * k, y - h * k)], g, 1.6 * k, gd)


def cactus(p, x, y, k):
    g, oc = (96, 172, 84, 255), (36, 70, 34, 255)
    p.rect((x - 14 * k, y - 120 * k, x + 14 * k, y), g, 3.5, oc, r=int(14 * k))
    p.rect((x - 46 * k, y - 84 * k, x - 24 * k, y - 40 * k), g, 3, oc, r=int(10 * k))
    p.rect((x - 46 * k, y - 50 * k, x - 6 * k, y - 34 * k), g, 3, oc, r=int(8 * k))
    p.rect((x + 24 * k, y - 100 * k, x + 46 * k, y - 56 * k), g, 3, oc, r=int(10 * k))
    p.rect((x + 6 * k, y - 66 * k, x + 46 * k, y - 50 * k), g, 3, oc, r=int(8 * k))
    p.line([(x, y - 108 * k), (x, y - 12 * k)], (70, 140, 64, 255), 2.5)


def barrel(p, x, y, k):
    p.ellipse((x - 26 * k, y - 8 * k, x + 26 * k, y + 8 * k), (60, 40, 28, 120), 0)
    p.rect((x - 24 * k, y - 62 * k, x + 24 * k, y), WOOD, 3, r=int(10 * k))
    for yy in (y - 50 * k, y - 14 * k):
        p.rect((x - 25 * k, yy - 4 * k, x + 25 * k, yy + 4 * k), (110, 110, 118, 255), 2.5)
    p.line([(x - 8 * k, y - 58 * k), (x - 8 * k, y - 4 * k)], WOOD_D, 2.5)
    p.line([(x + 8 * k, y - 58 * k), (x + 8 * k, y - 4 * k)], WOOD_D, 2.5)


def star(p, x, y, r, fill, ow=2.5):
    pts = []
    for k in range(10):
        rr = r if k % 2 == 0 else r * 0.45
        a = -math.pi / 2 + k * math.pi / 5
        pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
    p.poly(pts, fill, ow)


def awning(p, x0, x1, y0, y1, c1, c2, slant):
    n = max(4, int((x1 - x0) / 34))
    w = (x1 - x0) / n
    for i in range(n):
        a, b = x0 + i * w, x0 + (i + 1) * w
        p.poly([(a, y0), (b, y0), (b + slant, y1), (a + slant, y1)], c1 if i % 2 == 0 else c2, 0)
    p.poly([(x0, y0), (x1, y0), (x1 + slant, y1), (x0 + slant, y1)], None, 3.5) if False else None
    p.line([(x0, y0), (x1, y0), (x1 + slant, y1), (x0 + slant, y1), (x0, y0)], OUT, 3.5)
    # scallops
    for i in range(n):
        a = x0 + slant + i * w
        p.ellipse((a, y1 - w * 0.28, a + w, y1 + w * 0.34), c1 if i % 2 == 0 else c2, 3)


def building(p, side, xo, xi, base, h, sign=None, aw=(CREAM, (210, 60, 60, 255)), doors=(110, 170, 90, 255), star_badge=False,
             floors=2):
    """xo=outer x (screen edge side), xi=inner x (street side)."""
    x0, x1 = min(xo, xi), max(xo, xi)
    w = x1 - x0
    k = w / 380.0
    top = base - h
    # side wall toward the vanishing point (street-facing depth)
    depth = 0.09
    sx = xi + (VX - xi) * depth
    sy_top = top + (VY - top) * depth
    sy_base = base + (VY - base) * depth
    p.poly([(xi, top + 30 * k), (sx, sy_top + 30 * k), (sx, sy_base), (xi, base)], WOOD_D, 3)
    for i in range(1, 5):
        t = i / 5
        p.line([(xi + (sx - xi) * t, top + 30 * k + (sy_top - top) * t), (xi + (sx - xi) * t, base + (sy_base - base) * t)],
               (118, 70, 38, 255), 2)
    # porch deck
    deck = 46 * k
    p.poly([(x0 - 6 * k, base), (x1 + 6 * k, base), (x1 + 6 * k + (12 if side < 0 else -12) * k, base + deck),
            (x0 - 6 * k + (12 if side < 0 else -12) * k, base + deck)], (160, 104, 60, 255), 3)
    for i in range(1, 4):
        yy = base + deck * i / 4
        p.line([(x0, yy), (x1, yy)], WOOD_D, 2)
    # main wall
    p.rect((x0, top + 40 * k, x1, base), WOOD, 3.5)
    for i in range(1, int(w / (22 * k)) + 1):
        xx = x0 + i * 22 * k
        if xx < x1 - 4:
            p.line([(xx, top + 44 * k), (xx, base - 3)], WOOD_D, 1.8)
    # false front (cap)
    cap = [(x0 - 8 * k, top + 44 * k), (x0 - 8 * k, top + 20 * k), (x0 + w * 0.22, top + 20 * k), (x0 + w * 0.3, top),
           (x1 - w * 0.3, top), (x1 - w * 0.22, top + 20 * k), (x1 + 8 * k, top + 20 * k), (x1 + 8 * k, top + 44 * k)]
    p.poly(cap, WOOD_L, 3.5)
    p.rect((x0 - 10 * k, top + 40 * k, x1 + 10 * k, top + 52 * k), WOOD_D, 3)
    # sign
    if sign:
        sw, sh = w * 0.74, 64 * k
        scx, scy = (x0 + x1) / 2, top + 52 * k + sh * 0.75
        p.rect((scx - sw / 2, scy - sh / 2, scx + sw / 2, scy + sh / 2), SIGN, 3.5, r=int(8 * k))
        p.rect((scx - sw / 2 + 6 * k, scy - sh / 2 + 6 * k, scx + sw / 2 - 6 * k, scy + sh / 2 - 6 * k), None, 2,
               (196, 150, 96, 255), r=int(6 * k))
        tx = scx - (16 * k if star_badge else 0)
        p.text((tx, scy + 2 * k), sign, 42 * k, CREAM, ow=max(2, 3.2 * k))
        if star_badge:
            star(p, scx + sw / 2 - 34 * k, scy, 22 * k, (250, 204, 60, 255), 3)
        win_top = scy + sh / 2 + 16 * k
    else:
        win_top = top + 70 * k
    # upper windows
    if floors >= 2:
        for f in (0.2, 0.62):
            wx = x0 + w * f
            p.rect((wx, win_top, wx + w * 0.18, win_top + 56 * k), GLASS, 3)
            p.line([(wx + w * 0.09, win_top), (wx + w * 0.09, win_top + 56 * k)], (60, 90, 130, 255), 2.2)
            p.rect((wx - 4 * k, win_top + 52 * k, wx + w * 0.18 + 4 * k, win_top + 62 * k), WOOD_L, 2.5)
    # awning
    a_y0 = base - h * 0.40
    a_y1 = a_y0 + 60 * k
    slant = (14 if side < 0 else -14) * k
    awning(p, x0 - 4 * k, x1 + 4 * k, a_y0, a_y1, aw[0], aw[1], slant)
    # posts
    for xx in (x0 + 10 * k, x1 - 14 * k):
        p.rect((xx, a_y1 + 4 * k, xx + 10 * k, base + 12 * k), WOOD_L, 2.5)
    # door + windows
    dw = w * 0.22
    dcx = (x0 + x1) / 2
    dy0 = a_y1 + 14 * k
    p.rect((dcx - dw / 2, dy0, dcx + dw / 2, base), (70, 44, 30, 255), 3)
    # swinging doors
    p.rect((dcx - dw / 2 + 2, dy0 + (base - dy0) * 0.25, dcx - 2, dy0 + (base - dy0) * 0.75), doors, 2.5)
    p.rect((dcx + 2, dy0 + (base - dy0) * 0.25, dcx + dw / 2 - 2, dy0 + (base - dy0) * 0.75), doors, 2.5)
    for f in (0.1, 0.68):
        wx = x0 + w * f
        p.rect((wx, dy0 + 6 * k, wx + w * 0.2, base - 34 * k), GLASS, 3)
        p.line([(wx, dy0 + 6 * k + (base - 40 * k - dy0) / 2), (wx + w * 0.2, dy0 + 6 * k + (base - 40 * k - dy0) / 2)],
               (60, 90, 130, 255), 2.2)


def town(p):
    blue = (64, 132, 220, 255)
    red = (214, 58, 56, 255)
    green = (64, 156, 92, 255)
    yel = (246, 192, 52, 255)
    brown = (150, 96, 56, 255)
    # far -> near
    building(p, -1, 800, 884, 392, 70, None, (CREAM, brown), floors=1)
    building(p, 1, 1120, 1036, 392, 70, None, (CREAM, brown), floors=1)
    building(p, -1, 672, 808, 418, 118, None, (CREAM, red), floors=1)
    building(p, 1, 1248, 1112, 418, 118, None, (CREAM, blue), floors=1)
    building(p, -1, 400, 690, 478, 226, "STORE", (CREAM, red))
    building(p, 1, 1520, 1230, 478, 226, "HOTEL", (CREAM, green))
    building(p, -1, -24, 360, 572, 400, "SALOON", (CREAM, blue))
    building(p, 1, 1944, 1560, 572, 400, "SHERIFF", (CREAM, yel), star_badge=True)


def props(p):
    cactus(p, 66, 720, 0.9)
    cactus(p, 1858, 690, 0.8)
    cactus(p, 700, 440, 0.3)
    cactus(p, 1230, 436, 0.28)
    barrel(p, 120, 640, 0.85)
    barrel(p, 1800, 650, 0.85)
    barrel(p, 380, 540, 0.6)
    barrel(p, 1540, 540, 0.6)
    random.seed(5)
    for x, y, k in ((300, 900, 1.2), (1650, 940, 1.2), (180, 1010, 1.4), (1780, 860, 1.0)):
        tuft(p, x, y, k)


def build():
    p = Pen(W, H)
    sky(p)
    mesas(p)
    ground(p)
    town(p)
    props(p)
    img = p.final()
    save(img, "Chibi/bg_full_scene.png")
    return img


if __name__ == "__main__":
    build()
    print("scene done")
