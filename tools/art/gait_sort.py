"""Phan loai + sap xep pha gait cho walk sheet 6 frame.

Chay tren cac frame DA processed (walk-1..6.png). Muc dich: BO viec "xep dung thu tu" ra khoi tay
image model. Model chi can ve du 6 pose; script do tung frame roi tra ve hoan vi can ap dung o RAW.

Tin hieu quyet dinh la VAI TRO CHAN (chan gan camera duoc ve sang hon chan xa), khong phai dem
pixel boot. Ly do: o sprite nho (~39 px) hai ban chan CHAM NHAU nen phep dem blob va moi nguong
hinh hoc deu lat -- do that: Ironblade f3 (passing) dip-ratio 0,40 vs Goblin f4 (stride) 0,44,
chong lan, khong ton tai nguong dung cho ca hai. Do sang thi doc thang cai ma brief bat model
hoan doi giua nua A va nua B.

Usage:
  python tools/art/gait_sort.py <frames_dir>
  python tools/art/gait_sort.py <frames_dir> --apply <raw.png> --rows 3 --cols 2 --out <raw-ordered.png>
"""
import sys, os, argparse
import numpy as np
from PIL import Image

BAND = 5          # so hang pixel day dung lam vung "ban chan"
DIP_MAX = 0.75    # dip/plateau vuot nguong nay = khong tach duoc hai ban chan


def measure(path):
    a = np.array(Image.open(path).convert('RGBA'))
    al = a[..., 3] > 128
    lum = (a[..., :3].astype(float) @ [0.299, 0.587, 0.114])
    ys = np.where(al.any(1))[0]
    top, bot = int(ys.min()), int(ys.max())
    band = al[bot - BAND + 1:bot + 1]
    blum = lum[bot - BAND + 1:bot + 1]
    cols = np.where(band.any(0))[0]
    x0, x1 = int(cols.min()), int(cols.max())
    prof = np.array([band[:, x].sum() for x in range(x0, x1 + 1)], float)
    n = len(prof)
    m0, m1 = int(n * 0.2), max(int(np.ceil(n * 0.8)), int(n * 0.2) + 1)
    k = int(np.argmin(prof[m0:m1])) + m0
    left_max = prof[:k].max() if k > 0 else 0.0
    right_max = prof[k + 1:].max() if k < n - 1 else 0.0
    plateau = (left_max + right_max) / 2 or prof.max()
    ratio = float(prof[k] / plateau)

    role = None
    lum_l = lum_r = None
    if ratio <= DIP_MAX and k > 0 and k < n - 1:
        sl, sr = slice(x0, x0 + k), slice(x0 + k + 1, x1 + 1)
        if band[:, sl].any() and band[:, sr].any():
            lum_l = float(blum[:, sl][band[:, sl]].mean())
            lum_r = float(blum[:, sr][band[:, sr]].mean())
            # chan sang hon = chan GAN camera. Vai tro A/B theo ben nao dang la chan gan.
            role = 'A' if lum_l > lum_r else 'B'
    return dict(file=os.path.basename(path), span=n, height=bot - top + 1, dip_ratio=round(ratio, 2),
                role=role, lum_l=None if lum_l is None else round(lum_l, 1),
                lum_r=None if lum_r is None else round(lum_r, 1))


def order(fr):
    """Tra ve thu tu chuan: contactA, recoilA, passingA, contactB, recoilB, passingB.

    Khong gia dinh so luong frame moi vai tro. Chi can moi nua co >=2 frame doc duoc vai tro;
    hai frame con lai (hep nhat / khong doc duoc vai tro) la passing cua tung nua.
    """
    # Chi doc vai tro tu frame SAI CHAN THAT. Frame hai ban chan chum cho so do sang vo nghia
    # (hai ban chan cham nhau thi "ben nao sang hon" la nhieu), va no tung lam gate PASS mot sheet
    # co ca 4 frame sai chan cung mot ben -- tuc di khap khieng.
    wide = max(f['span'] for f in fr) * 0.5
    A = [f for f in fr if f['role'] == 'A' and f['span'] >= wide]
    B = [f for f in fr if f['role'] == 'B' and f['span'] >= wide]
    U = [f for f in fr if f['span'] < wide or f['role'] is None]
    if len(A) < 2 or len(B) < 2:
        return None, [
            f'can >=2 frame SAI CHAN vai tro A va >=2 frame SAI CHAN vai tro B, '
            f'dang co A={len(A)} B={len(B)} (frame chan chum/khong doc duoc={len(U)}; '
            f'nguong sai chan = span >= {wide:.0f})'
            + (' -> nua chu ky bi LAP, hai chan khong doi vai tro, phai sinh them pose'
               if not A or not B else '')]
    # trong moi nua: contact rong nhat, roi recoil
    A.sort(key=lambda f: -f['span'])
    B.sort(key=lambda f: -f['span'])
    core = A[:2] + B[:2]
    rest = [f for f in fr if f not in core]      # giu thu tu o goc
    return [A[0], A[1], rest[0], B[0], B[1], rest[1]], []


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('frames_dir')
    ap.add_argument('--prefix', default='walk')
    ap.add_argument('--apply'); ap.add_argument('--out')
    ap.add_argument('--rows', type=int, default=3); ap.add_argument('--cols', type=int, default=2)
    a = ap.parse_args()

    paths = [os.path.join(a.frames_dir, f'{a.prefix}-{i}.png') for i in range(1, 7)]
    missing = [p for p in paths if not os.path.exists(p)]
    if missing:
        sys.exit('THIEU FRAME: ' + ', '.join(missing))
    fr = [measure(p) for p in paths]
    for i, f in enumerate(fr, 1):
        print(f"cell{i} vai_tro={str(f['role']):4s} lum(trai/phai)={str(f['lum_l']):6s}/{str(f['lum_r']):6s} "
              f"span={f['span']:3d} dip={f['dip_ratio']:.2f} cao={f['height']}")
    seq, problems = order(fr)
    if problems:
        print('\nGATE: FAIL')
        for p in problems:
            print(' -', p)
        print('\nGhi chu: vai tro A/B = ben nao dang la CHAN GAN camera (ve sang hon).')
        print('Mot walk cycle that phai co ca A lan B; toan A nghia la model ve lai nua dau.')
        sys.exit(2)
    perm = [paths.index(os.path.join(a.frames_dir, f['file'])) + 1 for f in seq]
    print('\nGATE: PASS - co ca hai vai tro chan')
    print('thu tu chuan (theo cell raw hien tai):', perm)
    print('vai tro         : contactA recoilA passingA contactB recoilB passingB')
    if perm == [1, 2, 3, 4, 5, 6]:
        print('=> raw sheet da dung thu tu, khong can xep lai.')
    elif a.apply:
        img = Image.open(a.apply).convert('RGBA')
        w, h = img.size
        cw, ch = w // a.cols, h // a.rows
        out = Image.new('RGBA', (w, h), (255, 0, 255, 255))
        for slot, src in enumerate(perm):
            sr, sc = divmod(src - 1, a.cols)
            dr, dc = divmod(slot, a.cols)
            out.paste(img.crop((sc * cw, sr * ch, sc * cw + cw, sr * ch + ch)), (dc * cw, dr * ch))
        out.save(a.out)
        print('da ghi raw sheet xep lai ->', a.out)
    else:
        print('=> chay lai voi --apply <raw.png> --out <raw-ordered.png> de xep lai O RAW, '
              'roi chay lai processor. Khong ton generation.')


if __name__ == '__main__':
    main()
