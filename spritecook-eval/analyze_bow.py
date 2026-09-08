from PIL import Image
import sys

im = Image.open('sc_bow/sc_bow_spritesheet.png').convert('RGBA')
W, H = im.size
n = 6
fw = W // n
print(f"sheet {W}x{H}, frame {fw}x{H}")

frames = []
for i in range(n):
    f = im.crop((i*fw, 0, (i+1)*fw, H))
    frames.append(f)
    f.save(f'sc_bow/frame_{i+1}.png')

for i, f in enumerate(frames):
    px = f.load()
    colors = set()
    minx, miny, maxx, maxy = fw, H, 0, 0
    opaque_count = 0
    for y in range(H):
        for x in range(fw):
            r,g,b,a = px[x,y]
            if a > 10:
                opaque_count += 1
                colors.add((r,g,b))
                if x < minx: minx = x
                if x > maxx: maxx = x
                if y < miny: miny = y
                if y > maxy: maxy = y
    print(f"frame {i+1}: unique_opaque_colors={len(colors)} bbox=({minx},{miny})-({maxx},{maxy}) bbox_w={maxx-minx} bbox_h={maxy-miny} opaque_px={opaque_count}")
