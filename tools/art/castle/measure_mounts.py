"""Do 3 be phao tren castle.png roi so voi mounts.json."""
from PIL import Image
import numpy as np, json, sys

a = np.asarray(Image.open((sys.argv[1] if len(sys.argv)>1 else "tempart/castle-v2/castle.png")).convert("RGBA")).astype(int)
al, lum = a[..., 3], a[..., :3].mean(2)
op = al > 0
th = np.percentile(lum[op], 80)

runs = []
for y in range(30, 360):
    row = (lum[y] > th) & op[y]
    xs = np.where(row)[0]
    if len(xs) < 40: continue
    best = cur = 1; s = bs = xs[0]
    for i in range(1, len(xs)):
        cur = cur + 1 if xs[i] == xs[i-1] + 1 else 1
        if xs[i] != xs[i-1] + 1: s = xs[i]
        if cur > best: best, bs = cur, s
    if best >= 60: runs.append((y, bs, bs + best - 1, best))

# gom thanh cum lien tuc theo y = mot be
groups, cur = [], [runs[0]]
for r in runs[1:]:
    if r[0] - cur[-1][0] <= 2:
        cur.append(r)
    else:
        groups.append(cur); cur = [r]
groups.append(cur)

spec = {m["floor"]: m for m in json.load(open("Assets/Art/Generated/Castle/mounts.json"))["mounts"]}
print(f"{'floor':<6}{'mat be y':<10}{'tam x':<8}{'rong':<7}{'trong tren':<12}  spec (y / x / rong / trong)")
for i, gp in enumerate(sorted(groups, key=lambda g: -g[0][0]), start=1):   # floor1 = thap nhat
    y = gp[0][0]                                   # hang tren cung cua mat be
    widest = max(gp, key=lambda r: r[3])
    cx, w = (widest[1] + widest[2]) // 2, widest[3]
    clear = 0                                      # troi ho phia tren mep truoc, tai tam x
    yy = y - 1
    while yy >= 0 and al[yy, cx] == 0: clear += 1; yy -= 1
    s = spec[i]
    print(f"{i:<6}{y:<10}{cx:<8}{w:<7}{clear:<12}  {s['y']} / {s['x']} / {s['platform_w']} / {s['clearance_h']}")
