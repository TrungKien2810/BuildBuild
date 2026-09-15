"""Despill: xoa vet magenta con sot lai tren vien sau khi chroma-key #FF00FF.

Chroma-key cat cung (threshold) giu lai nhung pixel vien da tron mau voi nen magenta ->
chung thanh vien tim/hong. Thay moi pixel nhu vay bang mau pho bien nhat trong 8 lang gieng
KHONG bi nhiem magenta, lap nhieu vong tu ngoai vao trong.
"""
import sys, collections
from PIL import Image


def is_spill(p):
    # Nguong SIET (2026-09-10): ban dau dung r>110 & b>110 & g<min-45, nhung residue thuc te
    # co dang (97,0,121) / (95,5,130) -- lot luoi vi r chua toi 110. Mau tim HOP LE trong art
    # (suong nui lavender ~(180,170,200)) co g gan r/b nen khong bi bat nham.
    r, g, b, a = p
    return a > 0 and r > 55 and b > 55 and g < min(r, b) - 25


def despill(path, max_passes=8):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    w, h = im.size

    # GUARD (2026-09-11): file co nen magenta phu kin (raw sheet TRUOC chroma-key) thi TU CHOI.
    # Da xay ra that tren Ashfang idle: glob `*.png` quet ca raw-sheet.png, script despill luon
    # NEN, an het sheet va pha huy source (phai sinh lai). Frame da key xong chi co vai tram px
    # spill o vien; >30% la raw sheet, khong phai residue.
    total = w * h
    spill_total = sum(1 for y in range(h) for x in range(w) if is_spill(px[x, y]))
    if spill_total > 0.30 * total:
        print("  BO QUA (raw sheet nen magenta, %.1f%% spill) - despill se pha source"
              % (100.0 * spill_total / total))
        return spill_total

    for _ in range(max_passes):
        # Bau theo BAN CHUP dau pass, khong doc `px` dang bi sua.
        # Truoc day doc thang `px`: pixel vua doi o buoc truoc lap tuc thanh "mau sach" de bau
        # cho pixel ke, nen vet lem TU LAN theo huong duyet (cheo xuong duoi-phai) thay vi an
        # dung mot vong moi pass.
        snap = [[px[x, y] for x in range(w)] for y in range(h)]
        bad = [(x, y) for y in range(h) for x in range(w) if is_spill(snap[y][x])]
        if not bad:
            break
        fixed = 0
        for x, y in bad:
            votes = collections.Counter()
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        q = snap[ny][nx]
                        if q[3] > 0 and not is_spill(q):
                            votes[q] += 1
            if votes:
                px[x, y] = votes.most_common(1)[0][0]
                fixed += 1
        if not fixed:  # cum magenta khong cham mau sach nao -> dung, bao ra ngoai
            break
    left = sum(1 for y in range(h) for x in range(w) if is_spill(px[x, y]))
    im.save(path)
    return left


for f in sys.argv[1:]:
    print(f, "-> magenta con lai:", despill(f))
