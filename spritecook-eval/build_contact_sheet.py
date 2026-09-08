from PIL import Image, ImageDraw, ImageFont

frames = [Image.open(f'sc_bow/frame_{i}.png').convert('RGBA') for i in range(1, 7)]
fw, fh = frames[0].size
scale = 2
cell_w, cell_h = fw*scale, fh*scale
label_h = 20
pad = 4

sheet_w = cell_w * 6 + pad*7
sheet_h = label_h + cell_h + pad*2

def checkerboard(w, h, sq=8):
    board = Image.new('RGBA', (w, h), (255,255,255,255))
    d = ImageDraw.Draw(board)
    light = (200,200,200,255)
    dark = (160,160,160,255)
    for y in range(0, h, sq):
        for x in range(0, w, sq):
            c = light if ((x//sq)+(y//sq)) % 2 == 0 else dark
            d.rectangle([x,y,x+sq-1,y+sq-1], fill=c)
    return board

sheet = Image.new('RGBA', (sheet_w, sheet_h), (40,40,40,255))
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("arial.ttf", 12)
except Exception:
    font = ImageFont.load_default()
draw.text((pad, 2), "Bow Draw (SpriteCook)", fill=(255,200,80,255), font=font)

for i, f in enumerate(frames):
    x = pad + i*(cell_w+pad)
    y = label_h + pad
    bg = checkerboard(cell_w, cell_h)
    up = f.resize((cell_w, cell_h), Image.NEAREST)
    bg.alpha_composite(up)
    sheet.paste(bg, (x, y))

sheet.convert('RGB').save('spritecook_result.png')
print("saved", sheet.size)
