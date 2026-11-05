"""Doi mau them da + dai dat day hang rao theo tung biome (khop trung binh mau voi bg-field cua biome do).

Chay: python tools/art/make_fence_biomes.py  -> Assets/Art/Generated/Fence/<biome>/{terrace-aligned,ground-skirt}.png
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Art", "Generated")
BIOMES = ["swamp", "scorched", "snow"]
FILES = ["terrace-aligned.png", "ground-skirt.png"]


def field_stats(sub):
    im = np.asarray(Image.open(os.path.join(ROOT, "Background", sub, "bg-field.png")).convert("RGB"), float)
    region = im[300:800].reshape(-1, 3)
    return region.mean(0), region.std(0)


base_mean, base_std = field_stats("")
for biome in BIOMES:
    mean, std = field_stats(biome)
    ratio = np.clip(std / base_std, 0.8, 1.25)
    out_dir = os.path.join(ROOT, "Fence", biome)
    os.makedirs(out_dir, exist_ok=True)
    for name in FILES:
        im = np.asarray(Image.open(os.path.join(ROOT, "Fence", name)).convert("RGBA"), float)
        rgb, alpha = im[..., :3], im[..., 3]
        opaque = alpha > 200
        src_mean = rgb[opaque].mean(0)
        new = (rgb - src_mean) * ratio + src_mean + (mean - base_mean)
        im[..., :3] = np.clip(new, 0, 255)
        Image.fromarray(im.astype(np.uint8), "RGBA").save(os.path.join(out_dir, name))
        print(biome, name, im[..., :3][opaque].mean(0).astype(int))
