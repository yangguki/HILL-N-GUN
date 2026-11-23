from common import *

SKIN = (246, 196, 152, 255)
SKIN_D = (222, 160, 120, 255)
HAIR = (78, 46, 30, 255)
PURPLE = (112, 62, 176, 255)
PURPLE_D = (78, 40, 128, 255)
RED = (214, 46, 52, 255)
RED_D = (160, 28, 36, 255)
WHITE = (250, 246, 236, 255)
JEAN = (64, 92, 168, 255)
BOOT = (112, 64, 34, 255)
BELT = (92, 56, 28, 255)
GUN = (150, 156, 170, 255)
GUN_D = (92, 96, 110, 255)
GRIP = (128, 78, 40, 255)
GOLD = (250, 200, 60, 255)


def revolver(p, x, y, dirx=1, scale=1.0, up=False):
    s = scale
    if up:
        # barrel pointing up
        p.rect((x - 4 * s, y - 30 * s, x + 4 * s, y - 6 * s), GUN, 2.5)
        p.rect((x - 8 * s, y - 10 * s, x + 8 * s, y + 4 * s), GUN_D, 2.5, r=3)
        p.rect((x - 5 * s, y + 2 * s, x + 6 * s, y + 16 * s), GRIP, 2.5, r=3)
        return
    p.rect((x, y - 5 * s, x + dirx * 30 * s, y + 2 * s), GUN, 2.5)
    p.rect((x - dirx * 2 * s, y - 8 * s, x + dirx * 12 * s, y + 5 * s), GUN_D, 2.5, r=3)
    p.poly([(x - dirx * 2 * s, y + 2 * s), (x + dirx * 6 * s, y + 2 * s), (x + dirx * 1 * s, y + 16 * s),
            (x - dirx * 9 * s, y + 14 * s)], GRIP, 2.5)


def player(pose="idle", frame=0):
    W, H = 306, 213
    p = Pen(W, H)
    cx, foot = 153, 207
    bob = 0
    lf = rf = 0  # leg lifts
    if pose == "idle":
        bob = 2 if frame == 1 else 0
    elif pose == "run":
        bob = [0, 3, 0, 3][frame]
        lf, rf = [(9, 0), (3, 3), (0, 9), (3, 3)][frame]
    elif pose == "shoot":
        bob = 1 if frame == 1 else 0
    y = lambda v: v + bob
    # boots + legs
    for sx, lift in ((-1, lf), (1, rf)):
        x0, x1 = (cx - 30, cx - 5) if sx < 0 else (cx + 5, cx + 30)
        p.rect((x0 + 2, 164 - lift + bob * 0.5, x1 - 2, 192 - lift), JEAN, 3, r=4)
        p.rect((x0 - 2, 184 - lift, x1 + 2, foot - lift), BOOT, 3, r=6)
    # left arm (down)
    p.ellipse((cx - 58, y(120), cx - 32, y(162)), WHITE, 3)
    p.circle(cx - 46, y(164), 9, SKIN, 3)
    # torso
    p.rect((cx - 42, y(114), cx + 42, y(174)), WHITE, 3, r=16)
    p.rect((cx - 40, y(116), cx + 40, y(166)), PURPLE, 3, r=14)
    p.line([(cx, y(118)), (cx, y(162))], PURPLE_D, 3)
    p.rect((cx - 42, y(160), cx + 42, y(172)), BELT, 3, r=3)
    p.rect((cx + 22, y(164), cx + 36, y(188)), BOOT, 3, r=4)  # holster
    p.rect((cx - 6, y(161), cx + 6, y(171)), GOLD, 2, r=2)
    # right arm + gun
    if pose == "shoot":
        lift = 6 if frame == 1 else 0
        p.poly([(cx + 30, y(120)), (cx + 52, y(110 - lift)), (cx + 62, y(122 - lift)), (cx + 40, y(140))], WHITE, 3)
        revolver(p, cx + 60, y(110 - lift), up=True, scale=1.05)
        p.circle(cx + 60, y(114 - lift), 9, SKIN, 3)
    else:
        p.poly([(cx + 32, y(122)), (cx + 62, y(136)), (cx + 58, y(152)), (cx + 34, y(148))], WHITE, 3)
        p.circle(cx + 66, y(146), 9, SKIN, 3)
        revolver(p, cx + 70, y(142), up=True, scale=0.95)
    # head (back) + ears
    p.circle(cx - 42, y(92), 9, SKIN, 3)
    p.circle(cx + 42, y(92), 9, SKIN, 3)
    p.circle(cx, y(86), 42, HAIR, 3.5)
    p.line([(cx - 20, y(108)), (cx - 10, y(118))], (60, 34, 22, 255), 3)
    p.line([(cx + 18, y(110)), (cx + 8, y(120))], (60, 34, 22, 255), 3)
    # bandana knot (back of neck)
    p.poly([(cx - 20, y(118)), (cx + 20, y(118)), (cx + 2, y(146))], RED, 3)
    p.poly([(cx + 4, y(122)), (cx + 22, y(134)), (cx + 14, y(140))], RED_D, 2.5)
    # hat
    p.ellipse((cx - 80, y(46), cx + 80, y(78)), PURPLE, 3.5)
    p.ellipse((cx - 66, y(50), cx + 66, y(70)), PURPLE_D, 0)
    p.rect((cx - 44, y(6), cx + 44, y(64)), PURPLE, 3.5, r=24)
    p.rect((cx - 44, y(46), cx + 44, y(60)), RED, 3)
    p.line([(cx - 14, y(14)), (cx, y(24)), (cx + 14, y(14))], PURPLE_D, 3)
    img = p.final()
    return img


def tint(img, color, k):
    over = Image.new("RGBA", img.size, color + (0,))
    a = img.split()[3]
    solid = Image.new("RGBA", img.size, color + (255,))
    mix = Image.blend(img, solid, k)
    mix.putalpha(a)
    return mix


def rotate_on_canvas(img, deg, pivot, squash=1.0, drop=0):
    W, H = img.size
    big = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 0))
    big.alpha_composite(img, (W // 2, H // 2))
    px, py = pivot[0] + W // 2, pivot[1] + H // 2
    r = big.rotate(deg, resample=Image.BICUBIC, center=(px, py))
    if squash != 1.0:
        r = r.resize((r.width, int(r.height * squash)), Image.LANCZOS)
        py = int(py * squash)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    # keep pivot at same place, then drop by `drop`
    out.alpha_composite(r.crop((px - pivot[0], py - pivot[1] - drop, px - pivot[0] + W, py - pivot[1] - drop + H)))
    return out


def roll_frame(img, deg, k):
    W, H = img.size
    small = img.resize((int(W * k), int(H * k)), Image.LANCZOS)
    big = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 0))
    cxs, cys = int(153 * k), int(110 * k)
    big.alpha_composite(small, (W - cxs, H - cys))
    r = big.rotate(deg, resample=Image.BICUBIC, center=(W, H))
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    cy = int(207 - 97 * k)
    out.alpha_composite(r.crop((W - 153, H - cy, W - 153 + W, H - cy + H)))
    return out


def grunt(pose="idle", frame=0):
    W, H = 168, 147
    p = Pen(W, H)
    cx, foot = 84, 141
    bob = 1.5 if (pose == "idle" and frame == 1) else 0
    y = lambda v: v + bob
    HAT = (196, 40, 46, 255)
    HAT_D = (128, 24, 30, 255)
    VEST = (66, 48, 70, 255)
    PANTS = (128, 84, 52, 255)
    # legs/boots
    for x0, x1 in ((cx - 20, cx - 4), (cx + 4, cx + 20)):
        p.rect((x0, 116, x1, 134), PANTS, 2.5, r=3)
        p.rect((x0 - 2, 128, x1 + 2, foot), (74, 42, 26, 255), 2.5, r=4)
    # body
    p.rect((cx - 25, y(86), cx + 25, y(124)), WHITE, 2.5, r=10)
    p.rect((cx - 25, y(88), cx - 8, y(122)), VEST, 2.5, r=6)
    p.rect((cx + 8, y(88), cx + 25, y(122)), VEST, 2.5, r=6)
    p.rect((cx - 26, y(114), cx + 26, y(122)), BELT, 2.5, r=2)
    # off arm
    p.ellipse((cx + 20, y(90), cx + 36, y(116)), WHITE, 2.5)
    p.circle(cx + 30, y(118), 6, SKIN, 2.5)
    # gun arm (toward viewer-left)
    if pose == "shoot":
        k = 4 if frame == 1 else 0
        p.poly([(cx - 20, y(92)), (cx - 40, y(98 - k)), (cx - 38, y(108 - k)), (cx - 18, y(108))], WHITE, 2.5)
        revolver(p, cx - 44, y(102 - k), dirx=-1, scale=0.75)
        p.circle(cx - 40, y(103 - k), 6.5, SKIN, 2.5)
    else:
        p.poly([(cx - 20, y(92)), (cx - 34, y(104)), (cx - 30, y(114)), (cx - 16, y(106))], WHITE, 2.5)
        revolver(p, cx - 34, y(112), dirx=-1, scale=0.7)
        p.circle(cx - 31, y(112), 6.5, SKIN, 2.5)
    # bandana (neck)
    p.poly([(cx - 20, y(84)), (cx + 20, y(84)), (cx, y(104))], HAT, 2.5)
    # head
    p.circle(cx, y(64), 27, SKIN, 3)
    p.ellipse((cx - 30, y(54), cx - 18, y(72)), HAIR, 2.5)
    p.ellipse((cx + 18, y(54), cx + 30, y(72)), HAIR, 2.5)
    p.circle(cx, y(64), 25, SKIN, 0)
    hit = pose == "hit" or pose == "down"
    if hit:
        for ex in (cx - 10, cx + 10):
            p.line([(ex - 5, y(64)), (ex + 5, y(69))], OUT, 2.5)
            p.line([(ex - 5, y(74)), (ex + 5, y(69))] if ex < cx else [(ex + 5, y(64)), (ex - 5, y(69)), (ex + 5, y(74))], OUT, 2.5)
        p.ellipse((cx - 5, y(78), cx + 5, y(85)), (120, 40, 40, 255), 2)
    else:
        for ex in (cx - 10, cx + 10):
            p.ellipse((ex - 5, y(62), ex + 5, y(75)), OUT, 0)
            p.circle(ex + 1.5, y(65), 2, (255, 255, 255, 255), 0)
        p.line([(cx - 16, y(56)), (cx - 5, y(60))], OUT, 2.5)
        p.line([(cx + 16, y(56)), (cx + 5, y(60))], OUT, 2.5)
        p.line([(cx - 5, y(81)), (cx + 5, y(81))], OUT, 2.2)
        p.circle(cx - 17, y(76), 3.5, (250, 150, 150, 255), 0)
        p.circle(cx + 17, y(76), 3.5, (250, 150, 150, 255), 0)
    # hat
    p.ellipse((cx - 44, y(36), cx + 44, y(54)), HAT, 3)
    p.rect((cx - 25, y(14), cx + 25, y(48)), HAT, 3, r=13)
    p.rect((cx - 25, y(38), cx + 25, y(46)), HAT_D, 2.5)
    p.line([(cx - 8, y(20)), (cx, y(26)), (cx + 8, y(20))], HAT_D, 2.5)
    return p.final()


def stars(img, cx, cy, n=3, r=16, phase=0.0):
    p = Pen(img.width, img.height)
    for i in range(n):
        a = phase + i * 2 * math.pi / n
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r * 0.4
        pts = []
        for k in range(10):
            rr = 6 if k % 2 == 0 else 2.6
            aa = -math.pi / 2 + k * math.pi / 5
            pts.append((x + math.cos(aa) * rr, y + math.sin(aa) * rr))
        p.poly(pts, GOLD, 1.5)
    out = img.copy()
    out.alpha_composite(p.final())
    return out


def build():
    D = "Chibi/"
    for i in range(2):
        save(player("idle", i), D + f"player_back_idle_{i}.png")
        save(player("shoot", i), D + f"player_back_shoot_{i}.png")
    for i in range(4):
        save(player("run", i), D + f"player_back_run_{i}.png")
    base = player("idle", 0)
    n = 11
    for i in range(n):
        t = i / (n - 1)
        deg = -360 * t
        squash = 1.0 - 0.28 * math.sin(math.pi * t)
        save(roll_frame(base, deg, 1.0 - 0.3 * math.sin(math.pi * t)), D + f"player_back_roll_{i:02d}.png")
    hit = rotate_on_canvas(player("idle", 0), 8, (153, 207))
    save(tint(hit, (255, 255, 255), 0.22), D + "player_back_hit_0.png")

    for i in range(2):
        save(grunt("idle", i), D + f"grunt_front_idle_{i}.png")
        save(grunt("shoot", i), D + f"grunt_front_shoot_{i}.png")
    save(tint(grunt("hit"), (255, 255, 255), 0.22), D + "grunt_front_hit_0.png")
    g = grunt("down")
    for i, deg in enumerate((-25, -60, -88)):
        im = rotate_on_canvas(g, deg, (84, 141), drop=0)
        if i >= 1:
            im = stars(im, 84 + (28 if i == 2 else 18), 118 if i == 2 else 100, phase=i)
        save(im, D + f"grunt_front_down_{i}.png")


if __name__ == "__main__":
    build()
    print("chars done")
