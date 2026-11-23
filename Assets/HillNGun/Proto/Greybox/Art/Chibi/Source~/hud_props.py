from common import *
from scene import WOOD, WOOD_L, WOOD_D, star
D = "Chibi/"
SLATE = (58, 64, 78, 255)
SLATE_L = (92, 100, 118, 255)
GOLD = (250, 200, 60, 255)
WHITE = (255, 255, 255, 255)


def round_button(size, icon, label, ring=SLATE_L, face=SLATE):
    p = Pen(size, size)
    c = size / 2
    r = c - 4
    p.circle(c, c, r, ring, 4)
    p.circle(c, c, r * 0.86, face, 3)
    # glossy top
    p.ellipse((c - r * 0.6, c - r * 0.78, c + r * 0.6, c - r * 0.3), (255, 255, 255, 40), 0)
    icon(p, c, c - r * 0.14, r * 0.5)
    if label:
        p.text((c, c + r * 0.56), label, r * 0.27, WHITE, ow=max(2, r * 0.05))
    return p.final()


def icon_guns(p, x, y, s):
    for sgn in (-1, 1):
        pts = [(x - s * 0.9 * sgn, y - s * 0.55), (x + s * 0.55 * sgn, y + s * 0.35), (x + s * 0.4 * sgn, y + s * 0.55),
               (x - s * 1.0 * sgn, y - s * 0.35)]
        p.poly(pts, (220, 224, 232, 255), 3)
        p.poly([(x + s * 0.3 * sgn, y + s * 0.2), (x + s * 0.65 * sgn, y + s * 0.42), (x + s * 0.5 * sgn, y + s * 0.95),
                (x + s * 0.15 * sgn, y + s * 0.75)], (150, 96, 54, 255), 3)


def icon_bullet(p, x, y, s):
    p.rect((x - s * 0.32, y - s * 0.4, x + s * 0.32, y + s * 0.8), (70, 130, 230, 255), 3, r=int(s * 0.1))
    p.ellipse((x - s * 0.32, y - s * 0.95, x + s * 0.32, y - s * 0.1), (110, 170, 250, 255), 3)
    p.rect((x - s * 0.36, y + s * 0.6, x + s * 0.36, y + s * 0.85), (60, 100, 190, 255), 2.5)


def icon_star(p, x, y, s):
    star(p, x, y, s * 0.95, GOLD, 3)


def icon_roll(p, x, y, s):
    import math
    pts = []
    for i in range(0, 271, 15):
        a = math.radians(i - 200)
        pts.append((x + math.cos(a) * s * 0.7, y + math.sin(a) * s * 0.7))
    p.line(pts, OUT, s * 0.36)
    p.line(pts, WHITE, s * 0.2)
    ex, ey = pts[-1]
    p.poly([(ex - s * 0.38, ey - s * 0.08), (ex + s * 0.3, ey - s * 0.2), (ex + s * 0.02, ey + s * 0.42)], WHITE, 2.5)


def icon_cross(p, x, y, s):
    p.circle(x, y, s * 0.7, None, s * 0.16)
    p.circle(x, y, s * 0.7, None, s * 0.08, WHITE)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        p.line([(x + dx * s * 0.35, y + dy * s * 0.35), (x + dx * s * 1.0, y + dy * s * 1.0)], WHITE, s * 0.12)


def plate(w, h, fill=(56, 40, 32, 236), border=(196, 150, 90, 255)):
    p = Pen(w, h)
    p.rect((3, 3, w - 3, h - 3), fill, 4, r=int(h * 0.28))
    p.rect((9, 9, w - 9, h - 9), None, 2.5, border, r=int(h * 0.22))
    return p


def heart(w, h, fill, ow=4):
    import math
    p = Pen(w, h)
    pts = []
    for i in range(0, 360, 5):
        t = math.radians(i)
        x = 16 * math.sin(t) ** 3
        y = -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))
        pts.append((w / 2 + x * w / 38, h / 2 + 2 + y * h / 36))
    p.poly(pts, fill, ow)
    if fill[0] > 200:
        p.ellipse((w * 0.24, h * 0.22, w * 0.42, h * 0.4), (255, 255, 255, 230), 0)
    return p.final()


def fence(w, h):
    p = Pen(w, h)
    rail = (170, 110, 62, 255)
    p.rect((-4, h * 0.22, w + 4, h * 0.42), rail, 3)
    p.rect((-4, h * 0.56, w + 4, h * 0.76), rail, 3)
    p.rect((w * 0.06, h * 0.06, w * 0.06 + 14, h - 2), WOOD_L, 3, r=3)
    p.rect((w * 0.56, h * 0.06, w * 0.56 + 14, h - 2), WOOD_L, 3, r=3)
    return p.final()


def fence_post(w, h):
    p = Pen(w, h)
    p.rect((1.5, 2, w - 1.5, h - 2), WOOD_L, 2.5, r=3)
    return p.final()


def crate(w, h):
    p = Pen(w, h)
    k = w / 80
    p.rect((3 * k, 6 * k, w - 3 * k, h - 3 * k), (206, 146, 80, 255), 3.2 * k + 0.5, r=int(4 * k))
    p.rect((10 * k, 13 * k, w - 10 * k, h - 10 * k), None, 2.5 * k + 0.4, WOOD_D)
    p.line([(10 * k, 13 * k), (w - 10 * k, h - 10 * k)], WOOD_D, 3 * k + 0.4)
    p.line([(w - 10 * k, 13 * k), (10 * k, h - 10 * k)], WOOD_D, 3 * k + 0.4)
    return p.final()


def bullet_ally(w, h):
    p = Pen(w, h)
    p.ellipse((1, 2, w - 1, h - 2), (255, 236, 120, 255), 1.5, (200, 120, 30, 255))
    p.ellipse((w * 0.45, 3.5, w - 3, h - 3.5), (255, 255, 230, 255), 0)
    return p.final()


def bullet_enemy(w, h):
    p = Pen(w, h)
    # trail to the left, head at pivot 0.75
    p.poly([(0, h * 0.5), (w * 0.7, h * 0.22), (w * 0.7, h * 0.78)], (255, 170, 60, 150), 0)
    p.ellipse((w * 0.55, h * 0.2, w * 0.95, h * 0.8), (255, 120, 40, 255), 3)
    p.ellipse((w * 0.66, h * 0.32, w * 0.88, h * 0.62), (255, 236, 140, 255), 0)
    return p.final()


def burst(size, k, inner, outer):
    import math
    p = Pen(size, size)
    c = size / 2
    pts = []
    n = 8
    for i in range(n * 2):
        rr = (c - 2) * (1 if i % 2 == 0 else 0.45) * k
        a = i * math.pi / n + 0.2
        pts.append((c + math.cos(a) * rr, c + math.sin(a) * rr))
    p.poly(pts, outer, 2)
    p.circle(c, c, (c - 2) * 0.3 * k, inner, 0)
    return p.final()


def crosshair(s):
    p = Pen(s, s)
    c = s / 2
    red = (232, 40, 40, 255)
    p.circle(c, c, s * 0.34, None, 6, OUT)
    p.circle(c, c, s * 0.34, None, 3.2, red)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        p.line([(c + dx * s * 0.18, c + dy * s * 0.18), (c + dx * s * 0.48, c + dy * s * 0.48)], OUT, 6)
        p.line([(c + dx * s * 0.18, c + dy * s * 0.18), (c + dx * s * 0.48, c + dy * s * 0.48)], red, 3)
    p.circle(c, c, 3, red, 1.5)
    return p.final()


def joystick_base(s):
    p = Pen(s, s)
    c = s / 2
    p.circle(c, c, c - 4, (70, 76, 90, 230), 5)
    p.circle(c, c, c * 0.72, (52, 56, 68, 230), 3)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        tx, ty = c + dx * c * 0.82, c + dy * c * 0.82
        px, py = -dy, dx
        p.poly([(tx + dx * 10, ty + dy * 10), (tx + px * 12 - dx * 6, ty + py * 12 - dy * 6),
                (tx - px * 12 - dx * 6, ty - py * 12 - dy * 6)], (236, 236, 240, 255), 2.5)
    return p.final()


def joystick_knob(s):
    p = Pen(s, s)
    c = s / 2
    p.circle(c, c, c - 4, (196, 196, 202, 255), 4)
    p.circle(c, c, c * 0.62, (170, 170, 178, 255), 0)
    p.ellipse((c - c * 0.5, c - c * 0.75, c + c * 0.3, c - c * 0.25), (255, 255, 255, 200), 0)
    return p.final()


def pause_btn(s):
    p = Pen(s, s)
    p.rect((4, 4, s - 4, s - 4), (56, 40, 32, 240), 4, r=14)
    p.rect((s * 0.32, s * 0.27, s * 0.44, s * 0.73), WHITE, 2.5, r=3)
    p.rect((s * 0.56, s * 0.27, s * 0.68, s * 0.73), WHITE, 2.5, r=3)
    return p.final()


def build():
    save(round_button(180, icon_guns, "ATTACK", ring=(196, 70, 60, 255), face=SLATE), D + "btn_fire.png")
    save(round_button(120, icon_bullet, "RELOAD"), D + "btn_reload.png")
    save(round_button(120, icon_star, "SKILL"), D + "btn_grenade.png")
    save(round_button(120, icon_roll, "ROLL"), D + "btn_roll.png")
    save(pause_btn(78), D + "btn_pause.png")
    save(joystick_base(192), D + "joystick_base.png")
    save(joystick_knob(90), D + "joystick_knob.png")
    save(crosshair(51), D + "crosshair.png")
    save(heart(64, 58, (255, 255, 255, 255)), D + "heart_full.png")
    save(heart(64, 58, (70, 64, 70, 255)), D + "heart_empty.png")
    sp = plate(360, 80)
    save(sp.final(), D + "score_plate.png")
    st = plate(400, 80)
    star(st, 40, 40, 20, GOLD, 3)
    star(st, 360, 40, 20, GOLD, 3)
    save(st.final(), D + "stage_plate.png")
    hp = plate(560, 80)
    hp.text((44, 40), "HP", 34, WHITE, ow=3.5)
    save(hp.final(), D + "hp_plate.png")
    hf = plate(336, 36)
    save(hf.final(), D + "hp_frame.png")
    fill = Pen(318, 18)
    fill.rect((1, 1, 317, 17), (232, 58, 70, 255), 2, r=6)
    save(fill.final(), D + "hp_fill.png")
    save(fence(132, 54), D + "fence.png")
    save(fence_post(15, 54), D + "fence_post.png")
    for name, (w, h) in {"crate_d40": (42, 39), "crate_d46": (60, 57), "crate_d52": (78, 75), "crate_near": (159, 153),
                         "crate_far": (72, 75)}.items():
        save(crate(w, h), D + name + ".png")
    save(bullet_ally(30, 12), D + "bullet_ally.png")
    save(bullet_enemy(102, 48), D + "bullet_enemy.png")
    save(burst(42, 1.0, (255, 255, 230, 255), (255, 200, 60, 255)), D + "muzzle_flash_0.png")
    save(burst(42, 0.75, (255, 255, 230, 255), (255, 160, 40, 255)), D + "muzzle_flash_1.png")
    for i, k in enumerate((0.55, 0.85, 1.0)):
        save(burst(42, k, (255, 255, 255, 255), (255, 236, 150, 255)), D + f"hit_spark_{i}.png")


if __name__ == "__main__":
    build()
    print("hud done")
