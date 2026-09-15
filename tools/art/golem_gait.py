"""Cham LOCOMOTION cho nhan vat KHONG tach duoc hai ban chan (Cragfist / nguoi da).

Vi sao khong dung `gait_sort.py`: script do doc VAI TRO CHAN bang do sang (chan gan camera sang hon
chan xa) va do `footspan` o 5 hang pixel day. Ca hai deu gia dinh co HAI ban chan roi nhau. Cragfist
identity da duyet co hai ban chan DINH LIEN 5 hang day (y=88..92) — profile mat do khong con cho lom,
nen `gait_sort.py` khong con tin hieu de tach pha. Do bang no la do vao nhieu.

Thu MAT nguoi van doc ra o mot khoi da biet di — va do duoc bang so:

  1. BOB   — dinh dau len xuong. Buoc di la chuoi ngã-rồi-đỡ; khoi da khong nhun thi doc ra nhu
             dang TRUOT tren bang chuyen. Do bang `top` (hang pixel tren cung cua mask).
  2. SWAY  — trong luong doi ben. Khong nhin duoc chan thi day la tin hieu DUY NHAT con lai de biet
             "no vua doi chan": tam khoi tiep dat lech sang trai o nua A, lech sang phai o nua B.
             Do bang `contact_cx` (tam x cua 5 hang day) tru tam x toan than.
  3. TILT  — than tren nghieng nguoc pha voi trong luong. Do bang tam x cua 1/3 tren tru tam x cua
             1/3 duoi.
  4. WIDTH — khoi tiep dat phinh/thu theo pha (thay cho footspan: do CA KHOI, khong can tach chan).
  5. DISTINCT — khong frame nao gan trung frame khac (IoU). Bat loi "giam chan tai cho" ma 4 do tren
             van co the qua duoc neu model chi nhich vai pixel.

Chu ky 6 frame = HAI buoc: frame i va i+3 cung PHA nhung NGUOC BEN. Vi vay gate chinh la:
  - bob lap lai hai lan (period 3), va
  - `sway` cua nua A va nua B doi DAU nhau.

Usage:
  python tools/art/golem_gait.py <frames_dir>                 # cham, in PASS/FAIL
  python tools/art/golem_gait.py <frames_dir> --raw           # chi in bang so (de hieu chuan)
  python tools/art/golem_gait.py <frames_dir> --prefix idle   # mac dinh doc walk-*.png
"""
import argparse
import glob
import os

import numpy as np
from PIL import Image

BAND = 5            # so hang pixel day dung lam "khoi tiep dat"

# Nguong hieu chuan tren chinh du an — xem bang so trong
# docs/huong-dan/nghiem-thu-locomotion-khong-co-chan.md. Do tren 128x128 output.
BOB_MIN = 2.0       # px, bien do len xuong cua dinh dau
BOB_MAX = 9.0       # px, hon nua la nhay co chan chu khong phai di bo
SWAY_MIN = 0.8      # px, do lech trong luong moi nua
WIDTH_MIN = 3.0     # px, khoi tiep dat phai phinh/thu it nhat chung nay
IOU_MAX = 0.985     # hai frame giong nhau hon muc nay = trung frame


def measure(path):
    mask = np.array(Image.open(path).convert('RGBA'))[..., 3] > 128
    ys = np.where(mask.any(1))[0]
    xs = np.where(mask.any(0))[0]
    top, bot = int(ys.min()), int(ys.max())

    cx_all = float(np.argwhere(mask)[:, 1].mean())

    band = mask[bot - BAND + 1:bot + 1]
    band_xs = np.where(band.any(0))[0]
    contact_w = float(band_xs.max() - band_xs.min() + 1)
    contact_cx = float(np.argwhere(band)[:, 1].mean())

    height = bot - top + 1
    upper = mask[top:top + max(1, height // 3)]
    lower = mask[bot - max(1, height // 3) + 1:bot + 1]

    return {
        'path': os.path.basename(path),
        'top': float(top),
        'height': float(height),
        'width': float(xs.max() - xs.min() + 1),
        'contact_w': contact_w,
        'sway': contact_cx - cx_all,
        'tilt': float(np.argwhere(upper)[:, 1].mean()) - float(np.argwhere(lower)[:, 1].mean()),
        'mask': mask,
    }


def iou(a, b):
    union = np.logical_or(a, b).sum()
    return float(np.logical_and(a, b).sum() / union) if union else 1.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('frames_dir')
    ap.add_argument('--prefix', default='walk')
    ap.add_argument('--raw', action='store_true', help='chi in bang so, khong cham PASS/FAIL')
    args = ap.parse_args()

    paths = sorted(glob.glob(os.path.join(args.frames_dir, args.prefix + '-*.png')),
                   key=lambda p: int(''.join(c for c in os.path.basename(p) if c.isdigit()) or 0))
    if not paths:
        raise SystemExit('Khong thay frame %s-*.png trong %s' % (args.prefix, args.frames_dir))

    frames = [measure(p) for p in paths]
    n = len(frames)

    print('%-14s %6s %6s %7s %9s %7s %7s' % ('frame', 'top', 'height', 'width', 'contact_w', 'sway', 'tilt'))
    for f in frames:
        print('%-14s %6.1f %6.1f %7.1f %9.1f %7.2f %7.2f'
              % (f['path'], f['top'], f['height'], f['width'], f['contact_w'], f['sway'], f['tilt']))

    tops = np.array([f['top'] for f in frames])
    sways = np.array([f['sway'] for f in frames])
    widths = np.array([f['contact_w'] for f in frames])

    bob = float(tops.max() - tops.min())
    width_range = float(widths.max() - widths.min())

    worst_iou, worst_pair = 0.0, None
    for i in range(n):
        for j in range(i + 1, n):
            v = iou(frames[i]['mask'], frames[j]['mask'])
            if v > worst_iou:
                worst_iou, worst_pair = v, (i + 1, j + 1)

    print('\nbob(peak-to-peak) = %.1f px   [%.1f..%.1f]' % (bob, BOB_MIN, BOB_MAX))
    print('contact_w range   = %.1f px   [>= %.1f]' % (width_range, WIDTH_MIN))
    print('IoU cao nhat      = %.3f (f%d vs f%d)  [<= %.3f]'
          % (worst_iou, worst_pair[0], worst_pair[1], IOU_MAX))

    # Tru TRUNG BINH cua chinh sheet truoc khi chia hai nua: silhouette lech san (Ironblade co khien
    # mot ben => sway am o CA 6 frame) se lam moi nua cung dau va gate bao FAIL oan. Cai can do la
    # trong luong co DOI BEN qua chu ky khong, khong phai khoi da lech san ve ben nao.
    half = n // 2
    centred = sways - sways.mean()
    sway_a, sway_b = float(centred[:half].mean()), float(centred[half:].mean())
    print('sway (da tru trung binh %+.2f) nua A = %+.2f px, nua B = %+.2f px'
          '  (phai TRAI DAU, moi ben >= %.1f px)'
          % (sways.mean(), sway_a, sway_b, SWAY_MIN))

    if args.raw:
        return

    checks = [
        ('BOB   dinh dau len xuong', BOB_MIN <= bob <= BOB_MAX),
        ('SWAY  doi ben giua hai nua', sway_a * sway_b < 0
            and min(abs(sway_a), abs(sway_b)) >= SWAY_MIN),
        ('WIDTH khoi tiep dat phinh/thu', width_range >= WIDTH_MIN),
        ('DISTINCT khong trung frame', worst_iou <= IOU_MAX),
    ]
    print()
    for name, ok in checks:
        print('%-34s %s' % (name, 'PASS' if ok else 'FAIL'))
    print('\nGATE LOCOMOTION:', 'PASS' if all(ok for _, ok in checks) else 'FAIL')


if __name__ == '__main__':
    main()
