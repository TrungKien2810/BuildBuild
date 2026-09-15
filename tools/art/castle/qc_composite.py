"""Dat castle moi len nen v4 kem hero de xem ty le + camera co khop khong.
Hero ve 3x nhu final-check.png cua nen; castle 1x (384 px = co that trong game)."""
from PIL import Image
import os

def scene(castle_path, out):
    comp = Image.new("RGBA", (1920, 1080), (0, 0, 0, 255))
    for n, y in [("bg-sky",0), ("bg-far",200), ("bg-mid",400), ("bg-ground",780)]:
        comp.alpha_composite(Image.open(f"Assets/Art/Generated/Background/{n}.png").convert("RGBA"), (0, y))
    c = Image.open(castle_path).convert("RGBA")
    comp.alpha_composite(c, (0, 1000 - 348))          # baseline 348 -> chan lau dai y=1000
    for p, x, yb in [("Assets/Art/Generated/Frostweaver-v2/idle-anchored/idle-1.png", 520, 960),
                     ("Assets/Art/Generated/Cragfist/idle/idle-1.png", 760, 1000),
                     ("Assets/Art/Generated/Goblin/walk/walk-1.png", 1400, 1015)]:
        u = Image.open(p).convert("RGBA")
        u = u.resize((u.width*3, u.height*3), Image.NEAREST)
        comp.alpha_composite(u, (x, yb - u.height))
    comp.convert("RGB").save(out)

scene("tempart/castle-v2/castle.png", "tempart/castle-v2/qc-new.png")
scene("Assets/Art/Generated/Castle/castle.png", "tempart/castle-v2/qc-old.png")
for f in ("qc-new", "qc-old"):
    Image.open(f"tempart/castle-v2/{f}.png").resize((1200, 675), Image.LANCZOS).save(
        os.path.join(os.environ["TEMP"], f"{f}-small.png"))
print("ok")
