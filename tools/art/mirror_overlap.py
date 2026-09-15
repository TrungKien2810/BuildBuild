"""Do mirror-overlap cua mot sprite: lat ngang roi so alpha voi ban goc.

Ton tai vi moi agent tu che mot cong thuc khac nhau va ra so khac nhau — codex do luot 1
la 63,67% (fail) trong khi cung file do bang IoU la 57,88% (pass), nen no sinh lai va di
xa khoi mot pose von da dat. Gate chi co nghia khi moi ben dung CHUNG mot phep do.

Dung: python tools/art/mirror_overlap.py <file.png> [...]
Dat = 45-62%. >62% la dang ve chinh dien doi xung (loi that su hay xay ra), phai sua POSE.
<45% la side profile 90 do, cung sai nhung chua gap bao gio.

Band hieu chinh 2026-09-11: ban dau dat 54-62% do tren 3 asset da giao (Ironblade 56,4 /
Frostweaver idle 58,7 / Goblin 67,3). Qua hep — Hawkfletch idle ra 48-52%, dung bang con so
Frostweaver ghi trong art-style.md (49-51%) va nhin bang mat la 3/4 RO HON, khong phai sai.
Loai bo mot sheet vi no o 48% la loai nham.
"""
import sys
from PIL import Image, ImageOps
import numpy as np

for path in sys.argv[1:]:
    im = Image.open(path).convert("RGBA")
    bb = im.getbbox()
    if bb is None:
        print(f"{path}: EMPTY")
        continue
    a = im.crop(bb).split()[3].point(lambda v: 255 if v > 8 else 0)
    A = np.array(a) > 0
    M = np.array(ImageOps.mirror(a)) > 0
    iou = (A & M).sum() / (A | M).sum() * 100
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    verdict = "PASS" if 45 <= iou <= 62 else ("FAIL-frontal" if iou > 62 else "FAIL-side-profile")
    print(f"{path}: {w}x{h}px aspect={w/h:.3f} mirror_overlap={iou:.2f}% {verdict}")
