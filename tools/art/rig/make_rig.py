"""Walk 6 frame ky si tu than bang RIG CHAN tren idle-1 da duyet.
Than + ky si + giao giu nguyen pixel (chi nhun); 4 chan cat thanh dui/ong/mong, xoay quanh khop theo
quy dao di bo 4 phach cua real-walk-ref/make_ref.py (duty 0,62, thu tu ben). Ngua quay trai: truoc = -x.
"""
import math
from pathlib import Path
import numpy as np
from PIL import Image
from rig_masks import IDLE, LEGS, leg_masks

HERE = Path(__file__).parent
FRAMES = 6
DUTY = 0.62
STRIDE = 14            # px o 1x (ref 84 px o ngua dai ~375 px -> x0,19 = 16, lay 14)
BOB = 1
PHASE = {"far_fore": 0.75, "far_hind": 0.50, "near_fore": 0.25, "near_hind": 0.00}
LIFT = {"far_fore": 5, "near_fore": 5, "far_hind": 4, "near_hind": 4}
ORDER = ("far_hind", "far_fore", "BODY", "near_hind", "near_fore")
SS = 4                 # lay mau 4x4 moi pixel dich


def hoof_path(name, t):
    j = LEGS[name]["joints"]
    cx, gy = j[3]
    p = (t - PHASE[name]) % 1.0
    if p < DUTY:
        return cx - STRIDE / 2 + STRIDE * p / DUTY, gy, 0.0
    u = (p - DUTY) / (1 - DUTY)
    e = 0.5 - 0.5 * math.cos(math.pi * u)
    s = math.sin(math.pi * u)
    return cx + STRIDE / 2 - STRIDE * e, gy - LIFT[name] * s, s


def solve_knee(root, fet, l1, l2, side):
    dx, dy = fet[0] - root[0], fet[1] - root[1]
    d = math.hypot(dx, dy)
    if d >= l1 + l2 - 0.05:             # khong voi toi: duoi thang, gian deu 2 doan (< 1 px)
        k = d / (l1 + l2)
        return (root[0] + dx * l1 / (l1 + l2), root[1] + dy * l1 / (l1 + l2)), k
    a = math.acos(max(-1.0, min(1.0, (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d))))
    ang = math.atan2(dy, dx) + side * a
    return (root[0] + l1 * math.cos(ang), root[1] + l1 * math.sin(ang)), 1.0


def cross(o, a, b):
    return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])


OUTLINE = (30, 20, 28, 255)


def dilate(m, diag=True):
    out = m.copy()
    steps = ((1, 0), (-1, 0), (0, 1), (0, -1)) + (((1, 1), (1, -1), (-1, 1), (-1, -1)) if diag else ())
    for dy, dx in steps:
        out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def seg_index(masks):
    """Gan moi pixel chan vao doan gan nhat cua polyline khop (0 dui, 1 ong, 2 mong)."""
    out = {}
    for name, m in masks.items():
        j = np.array(LEGS[name]["joints"], float)
        ys, xs = np.nonzero(m)
        pts = np.stack([xs + 0.5, ys + 0.5], 1)
        dists = []
        for i in range(3):
            p, q = j[i] + 0.5, j[i + 1] + 0.5
            v = q - p
            tt = np.clip(((pts - p) @ v) / (v @ v), 0, 1)
            dists.append(np.linalg.norm(pts - (p + tt[:, None] * v), axis=1))
        dists = np.stack(dists, 1)
        near = dists <= dists.min(1, keepdims=True) + 1.0   # goi 1 px o khop de khong ho khi xoay
        out[name] = [(ys[near[:, i]], xs[near[:, i]]) for i in range(3)]
    return out


def draw_segment(canvas, src, pix, p, q, P, Q):
    """Chep doan xuong (p->q o idle) sang vi tri (P->Q): xoay + gian doc truc, nearest + bau da so 4x4."""
    ys, xs = pix
    if len(xs) == 0:
        return
    lut = {(int(y), int(x)) for y, x in zip(ys, xs)}
    a0 = math.atan2(q[1] - p[1], q[0] - p[0])
    a1 = math.atan2(Q[1] - P[1], Q[0] - P[0])
    L0 = math.hypot(q[0] - p[0], q[1] - p[1]) or 1.0
    k = (math.hypot(Q[0] - P[0], Q[1] - P[1]) or L0) / L0
    ca, sa = math.cos(a1), math.sin(a1)
    c0, s0 = math.cos(a0), math.sin(a0)
    # hop bao dich
    loc = [((x + .5 - p[0] - .5) * c0 + (y + .5 - p[1] - .5) * s0, -(x + .5 - p[0] - .5) * s0 + (y + .5 - p[1] - .5) * c0) for y, x in lut]
    dst = [(P[0] + .5 + u * k * ca - v * sa, P[1] + .5 + u * k * sa + v * ca) for u, v in loc]
    xs_d, ys_d = [d[0] for d in dst], [d[1] for d in dst]
    h, w = canvas.shape[:2]
    for oy in range(max(0, int(min(ys_d)) - 2), min(h, int(max(ys_d)) + 3)):
        for ox in range(max(0, int(min(xs_d)) - 2), min(w, int(max(xs_d)) + 3)):
            votes = {}
            for sy in range(SS):
                for sx in range(SS):
                    X = ox + (sx + .5) / SS - P[0] - .5
                    Y = oy + (sy + .5) / SS - P[1] - .5
                    u = (X * ca + Y * sa) / k
                    v = -X * sa + Y * ca
                    x = p[0] + .5 + u * c0 - v * s0
                    y = p[1] + .5 + u * s0 + v * c0
                    key = (math.floor(y), math.floor(x))
                    if key in lut:
                        votes[key] = votes.get(key, 0) + 1
            if sum(votes.values()) * 2 > SS * SS:
                sy_, sx_ = max(votes, key=votes.get)
                canvas[oy, ox] = src[sy_, sx_]


def body_layer(a, masks):
    legs = np.zeros(a.shape[:2], bool)
    for m in masks.values():
        legs |= m
    # xoa ca vien toi sat chan; tren y=100 chi xoa pixel khong phai ao choang tim
    r, g, b = [a[..., i].astype(int) for i in range(3)]
    cape = (b > r + 14) & ((r + g + b) / 3 >= 40)
    yy = np.arange(a.shape[0])[:, None]
    legs |= dilate(legs) & (a[..., 3] > 0) & ((yy > 100) | ~cape)
    body = a.copy()
    body[legs] = 0
    body[112:] = 0                            # duoi y=112 chi con sot vien mong cu (ao choang dung ~103)
    solid = (body[..., 3] > 0)
    for y, x in zip(*np.nonzero(legs)):       # va lo sau chan bang pixel ao choang cung hang
        if y > 100:
            continue
        left = [x - d for d in range(1, 6) if x - d >= 0 and solid[y, x - d]]
        right = [x + d for d in range(1, 6) if x + d < a.shape[1] and solid[y, x + d]]
        if left and right:
            near = left[0] if x - left[0] <= right[0] - x else right[0]
            body[y, x] = a[y, near]
    return body


def render(t, a, body, segs):
    bob = round(BOB * math.cos(4 * math.pi * t))
    frame = np.zeros_like(a)
    hooves = {}
    for layer in ORDER:
        if layer == "BODY":
            shifted = np.roll(body, bob, axis=0)
            m = shifted[..., 3] > 0
            frame[m] = shifted[m]
            continue
        j = [tuple(map(float, p)) for p in LEGS[layer]["joints"]]
        root = (j[0][0], j[0][1] + bob)
        hx, hy, s = hoof_path(layer, t)
        # mong: chong thi giu goc idle; vung thi gap co chan, mui mong lat ra sau (+x)
        hv = (j[3][0] - j[2][0], j[3][1] - j[2][1])
        flex = math.radians(35) * s
        hv_r = (hv[0] * math.cos(flex) - hv[1] * math.sin(flex), hv[0] * math.sin(flex) + hv[1] * math.cos(flex))
        fet = (hx - hv_r[0], hy - hv_r[1])
        l1 = math.dist(j[0], j[1]); l2 = math.dist(j[1], j[2])
        side = 1 if cross(j[0], j[2], j[1]) > 0 else -1
        knee, _ = solve_knee(root, fet, l1, l2, side)
        if abs(cross(j[0], j[2], j[1])) < 1.0:   # chan idle gan thang: khop gap ve phia sau than khi co
            knee, _ = solve_knee(root, fet, l1, l2, -1 if layer.endswith("fore") else 1)
        tgt = [root, knee, fet, (hx, hy)]
        legc = np.zeros_like(a)
        for i in range(3):
            draw_segment(legc, a, segs[layer][i], j[i], j[i + 1], tgt[i], tgt[i + 1])
        m = legc[..., 3] >= 128
        n = sum(np.roll(np.roll(m, dy, 0), dx, 1).astype(int)
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)))
        m &= n >= 2                                   # bo pixel le do bau da so
        lum = legc[..., :3].astype(int).mean(2)
        ring = dilate(m & (lum > 95), diag=False) & ~m   # xuong sang bi lo mep -> tra lai vien toi
        frame[m] = legc[m]
        frame[ring & (frame[..., 3] == 0) | ring & layer.startswith("near")] = OUTLINE
        hooves[layer] = (hx, hy)
    frame[..., 3] = np.where(frame[..., 3] >= 128, 255, 0)
    m = frame[..., 3] > 0
    n = sum(np.roll(np.roll(m, dy, 0), dx, 1).astype(int)
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)))
    lone = m & (n == 0)
    frame[lone] = 0
    return frame, hooves, bob


def main():
    a = np.array(Image.open(IDLE).convert("RGBA"))
    masks = leg_masks(a)
    segs = seg_index(masks)
    body = body_layer(a, masks)
    frames = []
    print("frame bob  " + "  ".join(f"{n:>14}" for n in PHASE))
    for f in range(FRAMES):
        fr, hooves, bob = render(f / FRAMES, a, body, segs)
        img = Image.fromarray(fr)
        img.save(HERE / f"walk-{f + 1}.png")
        frames.append(img)
        print(f"f{f + 1}   {bob:+d}   " + "  ".join(f"x{hooves[n][0]:5.1f} up{LIFT[n] and (LEGS[n]['joints'][3][1] - hooves[n][1]):4.1f}" for n in PHASE))
    k = 4
    w, h = frames[0].size
    gif = []
    for im in frames:
        bg = Image.new("RGBA", (w, h), (60, 60, 70, 255)); bg.alpha_composite(im)
        gif.append(bg.convert("RGB").resize((w * k, h * k), Image.NEAREST))
    gif[0].save(HERE / "animation-4x.gif", save_all=True, append_images=gif[1:], duration=120, loop=0)
    strip = Image.new("RGB", (w * k * 7, h * k), (60, 60, 70))
    idle = Image.new("RGBA", (w, h), (60, 60, 70, 255)); idle.alpha_composite(Image.open(IDLE).convert("RGBA"))
    for i, im in enumerate([idle.convert("RGB").resize((w * k, h * k), Image.NEAREST)] + gif):
        strip.paste(im, (i * w * k, 0))
    strip.crop((0, 20 * k, w * k * 7, 124 * k)).save(HERE / "compare-4x.png")


if __name__ == "__main__":
    main()
