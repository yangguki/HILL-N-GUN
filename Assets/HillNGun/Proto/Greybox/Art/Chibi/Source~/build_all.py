"""HILL N GUN slice J - regenerate every 2D chibi sprite (self-made, CC0).
Run: python3 build_all.py  (needs Pillow + Lilita One TTF, OFL)."""
from PIL import Image
import chars, scene, hud_props
from common import save

chars.build()
full = scene.build()
hud_props.build()
# Flat-mode fallbacks (also satisfy IsGameplayReady): crops of the same plate.
save(full.crop((0, 0, 1920, 420)), "Chibi/bg_far.png")
save(full.crop((0, 360, 1920, 1080)), "Chibi/ground.png")
save(Image.new("RGBA", (1920, 300), (0, 0, 0, 0)), "Chibi/bg_town.png")
print("all chibi sprites built")
