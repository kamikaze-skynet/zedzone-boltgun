"""Draws every sprite and preview for the mod's items (original pixel art, no outside assets).
Run: python tools/make_art.py   ->  writes items/<Type>/<name>/sprite.png and preview.jpg"""
import os, sys
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "items")

# palette
OUT   = (16, 14, 16, 255)
IRON0 = (34, 32, 36, 255)
IRON1 = (52, 50, 56, 255)
IRON2 = (74, 72, 80, 255)
IRON3 = (104, 102, 112, 255)
IRON4 = (140, 138, 148, 255)
BRASS0= (96, 66, 24, 255)
BRASS1= (150, 108, 40, 255)
BRASS2= (204, 158, 66, 255)
BRASS3= (240, 206, 120, 255)
RED0  = (84, 14, 16, 255)
RED1  = (140, 24, 26, 255)
RED2  = (186, 44, 40, 255)
PARCH = (222, 206, 160, 255)
GRIP0 = (40, 30, 26, 255)
GRIP1 = (62, 46, 36, 255)

def box(d, x0, y0, x1, y1, fill, light=None, dark=None, outline=OUT):
    """filled rect with 1px outline, top/left highlight and bottom/right shade"""
    d.rectangle((x0, y0, x1, y1), fill=outline)
    d.rectangle((x0 + 1, y0 + 1, x1 - 1, y1 - 1), fill=fill)
    if light: d.line((x0 + 1, y0 + 1, x1 - 1, y0 + 1), fill=light); d.line((x0 + 1, y0 + 1, x0 + 1, y1 - 1), fill=light)
    if dark: d.line((x0 + 1, y1 - 1, x1 - 1, y1 - 1), fill=dark); d.line((x1 - 1, y0 + 1, x1 - 1, y1 - 1), fill=dark)

def poly(d, pts, fill):
    d.polygon(pts, fill=fill, outline=OUT)

def rivet(d, x, y):
    d.point((x, y), fill=BRASS3); d.point((x + 1, y + 1), fill=BRASS0)

def seal(d, x, y):
    """wax seal with a hanging parchment strip"""
    d.rectangle((x + 2, y + 5, x + 5, y + 16), fill=OUT); d.rectangle((x + 3, y + 6, x + 4, y + 15), fill=PARCH)
    for yy in (8, 11, 14): d.point((x + 3, y + yy), fill=IRON2)
    d.ellipse((x - 1, y - 1, x + 8, y + 8), fill=OUT); d.ellipse((x, y, x + 7, y + 7), fill=RED1)
    d.ellipse((x + 2, y + 2, x + 5, y + 5), fill=RED2); d.point((x + 2, y + 2), fill=(230, 120, 110, 255))

def brass_band(d, x0, y0, x1, y1):
    box(d, x0, y0, x1, y1, BRASS1, BRASS3, BRASS0)

def rifle():
    im = Image.new("RGBA", (256, 144), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    # grip (raked back)
    poly(d, [(58, 78), (92, 78), (84, 128), (52, 128), (46, 120)], GRIP1)
    for y in range(86, 124, 5): d.line((56 + (124 - y) // 6, y, 84 - (y - 86) // 6, y), fill=GRIP0)
    d.line((60, 80, 90, 80), fill=(90, 70, 54, 255))
    # trigger guard + trigger
    d.rectangle((92, 78, 124, 81), fill=OUT); d.rectangle((121, 78, 124, 98), fill=OUT); d.rectangle((92, 95, 124, 98), fill=OUT)
    d.rectangle((93, 79, 120, 80), fill=IRON2); d.rectangle((93, 96, 123, 97), fill=IRON2)
    poly(d, [(100, 79), (104, 79), (102, 91), (99, 90)], IRON3)
    # magazine well
    box(d, 126, 76, 158, 92, IRON1, IRON2, IRON0)
    # receiver
    box(d, 28, 30, 170, 80, IRON2, IRON3, IRON1)
    d.rectangle((30, 33, 168, 36), fill=IRON3)
    poly(d, [(28, 30), (40, 30), (40, 80), (28, 80), (24, 70), (24, 40)], IRON1)   # rear cap
    box(d, 44, 40, 96, 72, IRON1, IRON2, IRON0)                                     # side plate
    for x, y in ((47, 43), (92, 43), (47, 68), (92, 68)): rivet(d, x, y)
    # gothic arch window on the plate
    d.rectangle((60, 50, 78, 66), fill=OUT); d.rectangle((61, 51, 77, 65), fill=RED0)
    d.pieslice((60, 42, 78, 60), 180, 360, fill=OUT); d.pieslice((61, 43, 77, 59), 180, 360, fill=RED0)
    d.line((69, 46, 69, 65), fill=BRASS2); d.line((63, 56, 75, 56), fill=BRASS2)
    # ejection port / charging handle (MovingPart 99,31,45x17)
    box(d, 100, 34, 142, 46, IRON0, None, None)
    box(d, 106, 36, 128, 44, IRON3, IRON4, IRON1)
    box(d, 128, 38, 143, 42, IRON4, None, IRON2)
    # brass bands
    brass_band(d, 160, 28, 168, 82)
    brass_band(d, 38, 28, 42, 82)
    # barrel shroud
    box(d, 168, 36, 226, 76, IRON1, IRON2, IRON0)
    for x in range(176, 222, 9):
        d.rectangle((x, 46, x + 4, 66), fill=OUT); d.rectangle((x + 1, 47, x + 3, 65), fill=(22, 10, 8, 255))
        d.point((x + 2, 64), fill=(200, 90, 30, 255))
    d.rectangle((170, 70, 224, 72), fill=RED1)
    # muzzle brake (Barrel 221,42,29x25)
    box(d, 222, 42, 250, 68, IRON2, IRON3, IRON0)
    for y in (47, 54, 61): d.rectangle((228, y, 248, y + 2), fill=OUT)
    brass_band(d, 220, 40, 224, 70)
    # top rail + sight (Aimer 73,17,128x11)
    box(d, 74, 20, 200, 29, IRON1, IRON3, IRON0)
    for x in range(78, 198, 6): d.line((x, 21, x, 27), fill=IRON0)
    poly(d, [(180, 20), (200, 20), (200, 11), (194, 7), (188, 11)], IRON2)        # front sight hood
    d.rectangle((192, 12, 195, 18), fill=RED2)
    box(d, 80, 13, 98, 20, IRON2, IRON3, IRON1)                                     # rear sight
    # spike / ornament on rear cap, seal hanging from the band
    poly(d, [(24, 42), (16, 50), (16, 60), (24, 68)], BRASS1)
    seal(d, 145, 79)
    # tiny skull-free lettering plate
    box(d, 104, 52, 152, 62, BRASS1, BRASS3, BRASS0)
    for x in range(108, 150, 4): d.line((x, 55, x + 2, 55), fill=BRASS0); d.line((x + 1, 58, x + 2, 58), fill=BRASS0)
    return im

def pistol():
    im = Image.new("RGBA", (256, 195), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    # grip
    poly(d, [(46, 88), (112, 88), (102, 168), (40, 168), (32, 150)], GRIP1)
    for y in range(98, 164, 7): d.line((44 + (164 - y) // 8, y, 104 - (y - 98) // 8, y), fill=GRIP0)
    box(d, 36, 160, 104, 172, IRON2, IRON3, IRON1)                                   # butt plate
    # trigger guard
    d.rectangle((112, 88, 160, 92), fill=OUT); d.rectangle((156, 88, 160, 124), fill=OUT); d.rectangle((112, 120, 160, 124), fill=OUT)
    d.rectangle((113, 89, 155, 91), fill=IRON2); d.rectangle((113, 121, 159, 123), fill=IRON2)
    poly(d, [(124, 90), (130, 90), (127, 110), (122, 108)], IRON3)
    # frame
    box(d, 18, 26, 210, 92, IRON2, IRON3, IRON1)
    poly(d, [(18, 26), (32, 26), (32, 92), (18, 92), (12, 80), (12, 38)], IRON1)
    box(d, 40, 36, 100, 82, IRON1, IRON2, IRON0)
    for x, y in ((43, 39), (96, 39), (43, 78), (96, 78)): rivet(d, x, y)
    d.rectangle((58, 50, 82, 74), fill=OUT); d.rectangle((59, 51, 81, 73), fill=RED0)
    d.pieslice((58, 38, 82, 62), 180, 360, fill=OUT); d.pieslice((59, 39, 81, 61), 180, 360, fill=RED0)
    d.line((70, 43, 70, 73), fill=BRASS2); d.line((62, 56, 78, 56), fill=BRASS2)
    # slide / moving part (70,54,38x26)
    box(d, 104, 40, 150, 70, IRON0)
    box(d, 108, 44, 140, 66, IRON3, IRON4, IRON1)
    for y in (48, 53, 58, 63): d.line((111, y, 137, y), fill=IRON1)
    # bands
    brass_band(d, 26, 22, 32, 96)
    brass_band(d, 196, 22, 204, 96)
    # barrel + muzzle (Barrel 207,41,43x39)
    box(d, 204, 40, 250, 80, IRON2, IRON3, IRON0)
    for y in (48, 57, 66): d.rectangle((214, y, 248, y + 3), fill=OUT)
    # top sight (Aimer 83,0,128x22)
    box(d, 84, 8, 210, 22, IRON1, IRON3, IRON0)
    poly(d, [(196, 8), (210, 8), (210, 0), (203, 0)], IRON2)
    d.rectangle((202, 2, 206, 7), fill=RED2)
    box(d, 90, 1, 110, 9, IRON2, IRON3, IRON1)
    # ornament + seal
    poly(d, [(12, 40), (2, 52), (2, 66), (12, 78)], BRASS1)
    seal(d, 166, 91)
    box(d, 152, 70, 194, 84, BRASS1, BRASS3, BRASS0)
    for x in range(156, 192, 5): d.line((x, 75, x + 2, 75), fill=BRASS0); d.line((x + 1, 79, x + 3, 79), fill=BRASS0)
    return im

def shell():
    im = Image.new("RGBA", (256, 100), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    # case (brass) with rim, then red band, then the rocket body, then the dark AP tip
    box(d, 14, 18, 30, 86, BRASS1, BRASS3, BRASS0)        # rim
    box(d, 28, 24, 104, 80, BRASS2, BRASS3, BRASS1)       # case
    d.line((32, 30, 100, 30), fill=BRASS3)
    for x in range(40, 100, 14): d.line((x, 28, x, 76), fill=BRASS1)
    box(d, 102, 22, 116, 82, RED1, RED2, RED0)             # seal band
    box(d, 114, 24, 196, 80, IRON2, IRON3, IRON1)          # body
    for x in (128, 150, 172): d.rectangle((x, 26, x + 3, 78), fill=IRON1)
    poly(d, [(196, 24), (222, 36), (234, 52), (222, 68), (196, 80)], IRON1)   # nose
    poly(d, [(222, 38), (234, 52), (222, 66), (218, 52)], (190, 196, 210, 255))  # hard tip
    d.line((200, 30, 220, 40), fill=IRON3)
    return im

def rifle_mag():
    im = Image.new("RGBA", (256, 144), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    box(d, 98, 8, 162, 18, BRASS1, BRASS3, BRASS0)
    box(d, 100, 16, 160, 132, IRON2, IRON3, IRON1)
    for y in range(28, 120, 16): d.line((104, y, 156, y), fill=IRON1)
    box(d, 96, 126, 164, 138, IRON1, IRON2, IRON0)
    box(d, 112, 30, 148, 50, BRASS1, BRASS3, BRASS0)
    d.text((118, 33), "75", fill=OUT)
    seal(d, 140, 60)
    return im

def pistol_mag():
    im = Image.new("RGBA", (256, 256), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    box(d, 60, 18, 196, 44, BRASS1, BRASS3, BRASS0)
    box(d, 66, 40, 190, 230, IRON2, IRON3, IRON1)
    for y in range(64, 214, 24): d.rectangle((72, y, 184, y + 4), fill=IRON1)
    box(d, 54, 220, 202, 244, IRON1, IRON2, IRON0)
    box(d, 96, 80, 160, 120, BRASS1, BRASS3, BRASS0)
    seal(d, 150, 130)
    return im

def preview(sprite):
    bg = Image.new("RGBA", (512, 512), (20, 18, 22, 255))
    w, h = sprite.size
    s = min(440 / w, 440 / h)
    sp = sprite.resize((int(w * s), int(h * s)), Image.NEAREST)
    bg.alpha_composite(sp, ((512 - sp.size[0]) // 2, (512 - sp.size[1]) // 2))
    return bg.convert("RGB")

ITEMS = {
    ("RangedWeapons", "rifle"): rifle,
    ("RangedWeapons", "pistol"): pistol,
    ("Ammo", "shell75"): shell,
    ("Magazine", "rifle_mag"): rifle_mag,
    ("Magazine", "pistol_mag"): pistol_mag,
}

if __name__ == "__main__":
    for (kind, name), fn in ITEMS.items():
        out = os.path.join(ROOT, kind, name); os.makedirs(out, exist_ok=True)
        im = fn()
        im.save(os.path.join(out, "sprite.png"))
        preview(im).save(os.path.join(out, "preview.jpg"), quality=92)
        print("wrote", kind, name, im.size, im.getchannel("A").getbbox())
