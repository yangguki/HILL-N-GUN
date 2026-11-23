# HILL N GUN - 2D chibi skin generator (self-made, CC0). Pure PIL vector drawing, supersampled.
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops
import math, os

S = 4  # supersample
OUT = (34, 22, 18, 255)
FONT = "/usr/share/fonts/truetype/sand-box/google/Lilita One/LilitaOne-Regular.ttf"
OUTDIR = "/workspace/hng_j/chibi_out"


def font(px):
    return ImageFont.truetype(FONT, int(px * S))


class Pen:
    """Draw in final-pixel coords on an S-times canvas."""

    def __init__(self, w, h, bg=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w * S, h * S), bg)
        self.d = ImageDraw.Draw(self.im)

    def _p(self, pts):
        return [(x * S, y * S) for x, y in pts]

    def _b(self, box):
        x0, y0, x1, y1 = box
        return [min(x0, x1) * S, min(y0, y1) * S, max(x0, x1) * S, max(y0, y1) * S]

    def _translucent(self, fill):
        return fill is not None and len(fill) == 4 and fill[3] < 255

    def _layer(self, fn):
        layer = Image.new("RGBA", self.im.size, (0, 0, 0, 0))
        d = self.d
        self.d = ImageDraw.Draw(layer)
        fn()
        self.d = d
        self.im.alpha_composite(layer)

    def ellipse(self, box, fill, ow=3, oc=OUT):
        if self._translucent(fill):
            return self._layer(lambda: self.d.ellipse(self._b(box), fill=fill))
        self.d.ellipse(self._b(box), fill=fill, outline=oc if ow else None, width=int(ow * S))

    def circle(self, cx, cy, r, fill, ow=3, oc=OUT):
        self.ellipse((cx - r, cy - r, cx + r, cy + r), fill, ow, oc)

    def rect(self, box, fill, ow=3, oc=OUT, r=0):
        if self._translucent(fill) and self.im.getpixel((0, 0))[3] == 255:
            return self._layer(lambda: self.rect(box, fill[:3] + (fill[3],), ow, oc, r) if False else (
                self.d.rounded_rectangle(self._b(box), radius=r * S, fill=fill) if r > 0 else self.d.rectangle(self._b(box), fill=fill)))
        if r > 0:
            self.d.rounded_rectangle(self._b(box), radius=r * S, fill=fill, outline=oc if ow else None, width=int(ow * S))
        else:
            self.d.rectangle(self._b(box), fill=fill, outline=oc if ow else None, width=int(ow * S))

    def poly(self, pts, fill, ow=3, oc=OUT):
        p = self._p(pts)
        if self._translucent(fill):
            self._layer(lambda: self.d.polygon(p, fill=fill))
        else:
            self.d.polygon(p, fill=fill)
        if ow:
            self.d.line(p + [p[0]], fill=oc, width=int(ow * S), joint="curve")
            for x, y in p:
                r = ow * S / 2
                self.d.ellipse([x - r, y - r, x + r, y + r], fill=oc)

    def line(self, pts, fill=OUT, w=3):
        p = self._p(pts)
        self.d.line(p, fill=fill, width=int(w * S), joint="curve")
        for x, y in (p[0], p[-1]):
            r = w * S / 2
            self.d.ellipse([x - r, y - r, x + r, y + r], fill=fill)

    def text(self, xy, s, px, fill=(255, 255, 255, 255), ow=3, oc=OUT, anchor="mm"):
        f = font(px)
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor,
                    stroke_width=int(ow * S), stroke_fill=oc)

    def final(self):
        return self.im.resize((self.w, self.h), Image.LANCZOS)


def silhouette_outline(img, px, color=OUT):
    """Add a thick dark outline around the alpha silhouette (final-pixel units, works on S canvas)."""
    a = img.split()[3]
    k = int(px * S) | 1
    grown = a.filter(ImageFilter.MaxFilter(k if k <= 99 else 99))
    base = Image.new("RGBA", img.size, color)
    base.putalpha(grown)
    base.alpha_composite(img)
    return base


def save(img, name):
    path = os.path.join(OUTDIR, name)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    return path


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)
