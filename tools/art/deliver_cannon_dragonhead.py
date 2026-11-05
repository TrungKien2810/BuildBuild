"""Copy the Long Viem cannon pack (tempart/CannonDragonHead) into Assets/Art/Generated/CannonDragonHead.

One-shot delivery step (nguoi duyet lam, khong phai lane): rename frames to the project naming
(<action>-N.png), write pipeline-meta.json per action (GeneratedArtImporter reads aligned_bbox[3] for
the foot pivot), and cut the 48x48 UI icon from the identity.

Run:  <python> tools/art/deliver_cannon_dragonhead.py
"""
import json
import os
import shutil
from PIL import Image

SRC = "tempart/CannonDragonHead"
DST = "Assets/Art/Generated/CannonDragonHead"
ICON = "Assets/Art/Generated/UI/icons/icon-cannon-dragonhead.png"
GROUND_ROW = 97          # day be da trong o 144x128 (bbox alpha do tu chinh bo art)
BODY_CELL = (144, 128)
FX_CELL = (128, 128)

# action trong game -> (thu muc nguon, tien to file nguon, so frame, align)
ACTIONS = [
    ("idle", "idle", "idle", 4, "feet"),
    ("attack", "attack", "attack", 6, "feet"),
    ("skill", "long-tuc-body", "attack", 6, "feet"),
    ("breath-low", "breath-1", "breath", 6, "center"),
    ("breath-mid", "breath-2", "breath", 6, "center"),
    ("breath-high", "breath-3", "breath", 6, "center"),
    ("ult-breath", "long-tuc", "breath", 6, "center"),
    ("burn-status", "burn-status", "burn", 4, "center"),
    ("scorched-ground", "scorched-ground", "ground", 4, "center"),
    ("fire-burst", "fire-burst", "burst", 4, "center"),
]


def alpha_bbox(path):
    im = Image.open(path).convert("RGBA")
    mask = im.getchannel("A").point(lambda a: 255 if a > 8 else 0)
    return list(mask.getbbox())


def deliver():
    frames_meta = {}
    for action, src_dir, src_prefix, count, align in ACTIONS:
        out_dir = os.path.join(DST, action)
        os.makedirs(out_dir, exist_ok=True)
        for old in os.listdir(out_dir):
            if old.endswith(".png"):
                os.remove(os.path.join(out_dir, old))
        frames = []
        for i in range(1, count + 1):
            src = os.path.join(SRC, src_dir, f"{src_prefix}-{i}.png")
            dst_name = f"{action}-{i}.png"
            dst = os.path.join(out_dir, dst_name)
            shutil.copyfile(src, dst)
            frames.append({"frame": dst_name[:-4], "aligned_bbox": alpha_bbox(dst)})
        cell = BODY_CELL if align == "feet" else FX_CELL
        meta = {
            "target": "asset",
            "mode": action,
            "source": f"tempart/CannonDragonHead/{src_dir}",
            "align": align,
            "status": "giao 2026-10-05 (cannon pack, apply 2026-10-30)",
            "frames": frames,
            "cell_size": cell[0],
            "cell_height": cell[1],
        }
        with open(os.path.join(out_dir, "pipeline-meta.json"), "w", encoding="utf-8") as f:
            json.dump(meta, f, indent=2)
        frames_meta[action] = frames

    # Base da co dinh o hang GROUND_ROW: ghi lai thanh mot con so dung cho moi frame than (pivot chan).
    for action in ("idle", "attack", "skill"):
        path = os.path.join(DST, action, "pipeline-meta.json")
        meta = json.load(open(path, encoding="utf-8"))
        for frame in meta["frames"]:
            frame["aligned_bbox"][3] = GROUND_ROW
        json.dump(meta, open(path, "w", encoding="utf-8"), indent=2)
    return frames_meta


def make_icon():
    identity = Image.open(os.path.join(SRC, "identity/identity-1.png")).convert("RGBA")
    # 48x48 lay dung phan DAU RONG (khong lay ca be) — icon nho ma lay ca than thi chi con vet mo.
    head = identity.crop((8, 18, 56, 66))
    head.save(ICON)
    return head.size


if __name__ == "__main__":
    deliver()
    print("icon", make_icon())
    print("done:", DST)
