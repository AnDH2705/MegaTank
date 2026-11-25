"""Bảng so sánh trước (game) / sau (out/Art) theo nhóm, trên nền tối và nền sáng.

    py -3.12 -I compare.py          # ghi out/compare/<nhóm>.png
    py -3.12 -I compare.py --zoom cat-11 pack-1    # phóng to 3x từng ảnh để soi chi tiết
"""
import os, sys, collections
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *

DARK, LIGHT = (38, 40, 60, 255), (250, 250, 250, 255)
try:
    from PIL import ImageFont
    FONT = ImageFont.truetype("arial.ttf", 13)
except OSError:
    FONT = None


def tile(a, cell, bg):
    im = Image.new("RGBA", (cell, cell), bg)
    s = Image.fromarray(a, "RGBA")
    s.thumbnail((cell - 6, cell - 6), Image.LANCZOS)
    im.alpha_composite(s, ((cell - s.width) // 2, (cell - s.height) // 2))
    return im


def board(names, before, after, path, cell=160):
    cols = len(names)
    lines = [("TRƯỚC", before, DARK), ("SAU", after, DARK), ("TRƯỚC", before, LIGHT), ("SAU", after, LIGHT)]
    head, lab = 54, 16
    im = Image.new("RGBA", (head + cols * cell, len(lines) * cell + lab), (215, 215, 220, 255))
    d = ImageDraw.Draw(im)
    for r, (title, src, bg) in enumerate(lines):
        d.text((4, r * cell + cell // 2 - 6), title, fill=(0, 0, 0, 255), font=FONT)
        for c, n in enumerate(names):
            im.alpha_composite(tile(src[n], cell, bg), (head + c * cell, r * cell))
    for c, n in enumerate(names):
        d.text((head + c * cell + 4, len(lines) * cell + 2), n[:-4], fill=(0, 0, 0, 255), font=FONT)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


def zoom(name, scale=3):
    b, a = load(os.path.join(ART, name + ".png")), load(os.path.join(OUT, "Art", name + ".png"))
    tiles = []
    for src in (b, a):
        for bg in (DARK, LIGHT):
            s = Image.fromarray(src, "RGBA")
            s = s.resize((s.width * scale, s.height * scale), Image.NEAREST)
            t = Image.new("RGBA", s.size, bg)
            t.alpha_composite(s)
            tiles.append(t)
    w = tiles[0].width + tiles[2].width + 10
    h = max(tiles[0].height, tiles[2].height) * 2 + 10
    im = Image.new("RGBA", (w, h), (255, 0, 255, 255))
    im.paste(tiles[0], (0, 0)); im.paste(tiles[1], (0, tiles[0].height + 10))
    im.paste(tiles[2], (tiles[0].width + 10, 0)); im.paste(tiles[3], (tiles[0].width + 10, tiles[2].height + 10))
    p = os.path.join(OUT, "compare", f"zoom_{name}.png")
    im.save(p)
    print(p)


def main():
    if "--zoom" in sys.argv:
        for n in sys.argv[sys.argv.index("--zoom") + 1:]:
            zoom(n)
        return
    done = sorted(os.listdir(os.path.join(OUT, "Art")))
    groups = collections.defaultdict(list)
    for f in done:
        if SCOPE.match(f):
            groups[group_of(f)].append(f)
    for g, files in groups.items():
        files = in_scope_order(files)
        before = {f: load(os.path.join(ART, f)) for f in files}
        after = {f: load(os.path.join(OUT, "Art", f)) for f in files}
        for part in range(0, len(files), 10):
            chunk = files[part:part + 10]
            suffix = "" if len(files) <= 10 else f"_{part // 10 + 1}"
            board(chunk, before, after, os.path.join(OUT, "compare", f"{g}{suffix}.png"))
    print("Bảng so sánh:", os.path.join(OUT, "compare"))


def in_scope_order(files):
    import re
    return sorted(files, key=lambda f: int((re.findall(r"(\d+)\.png$", f) or ["0"])[0]))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
