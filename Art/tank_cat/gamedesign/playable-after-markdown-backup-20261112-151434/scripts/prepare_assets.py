from PIL import Image, ImageDraw
from pathlib import Path
from collections import deque
import json
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'public' / 'assets'
OUT.mkdir(parents=True, exist_ok=True)
SHEETS = {
 'beast': ('Mười quái thú huyền thoại chibi.png', 5, 2),
 'bug': ('Mười quái vật côn trùng chibi rực rỡ.png', 5, 2),
 'cat': ('Bộ sticker 10 mèo chibi đáng yêu.png', 5, 2),
 'turret': ('Bộ sưu tập 10 tháp pháo xe tăng đáng yêu.png', 5, 2),
 'turret_alt': ('Bộ sưu tập 10 tháp pháo mèo hoạt hình.png', 5, 2),
 'gun': ('Bộ sưu tập vũ khí mèo khoa học viễn tưởng.png', 5, 2),
 'pack': ('Bộ vũ khí ba lô chiến đấu đa dạng.png', 5, 4),
 'hat': ('Bộ sưu tập mũ chiến thuật đáng yêu.png', 5, 2),
 'uniform': ('Bộ trang phục quân phục chibi đa sắc màu.png', 5, 2),
 'armor': ('Bộ biểu tượng áo giáp hoạt hình chiến thuật.png', 5, 2),
 'ammo': ('Bộ đạn vũ khí phong cách mèo dễ thương.png', 5, 2),
 'vehicle': ('Bộ phương tiện quân sự mèo ngộ nghĩnh.png', 5, 2)
}
manifest = {}
for key, (filename, cols, rows) in SHEETS.items():
    sheet = Image.open(ROOT / filename).convert('RGBA')
    w, h = sheet.size
    for i in range(cols * rows):
        y0 = (100 if rows == 2 and key != 'cat' else 0) if i < cols else h // rows
        x0 = i % cols * w // cols; x1 = (i % cols + 1) * w // cols
        if rows != 2: y0 = i // cols * h // rows
        y1 = (i // cols + 1) * h // rows
        tile = sheet.crop((x0, y0, x1, y1))
        px = tile.load(); tw, th = tile.size
        seen = bytearray(tw * th); q = deque()
        def push(x,y):
            k = y*tw+x
            if seen[k]: return
            seen[k] = 1
            r,g,b,a = px[x,y]
            if a < 20 or (key != 'cat' and min(r,g,b) > 222):
                px[x,y]=(r,g,b,0); q.append((x,y))
        for x in range(tw): push(x,0); push(x,th-1)
        for y in range(th): push(0,y); push(tw-1,y)
        while q:
            x,y=q.popleft()
            if x: push(x-1,y)
            if x+1<tw: push(x+1,y)
            if y: push(x,y-1)
            if y+1<th: push(x,y+1)
        bbox = tile.getbbox()
        if bbox: tile = tile.crop(bbox)
        tile.thumbnail((400,400), Image.Resampling.LANCZOS)
        name = f'{key}-{i}'; tile.save(OUT / f'{name}.png')
        manifest[name] = {'width':tile.width, 'height':tile.height, 'source':filename}
(OUT / 'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False),encoding='utf8')
contact=Image.new('RGB',(1200, ((len(manifest)+9)//10)*125),(35,42,49)); draw=ImageDraw.Draw(contact)
for i,name in enumerate(manifest):
    im=Image.open(OUT/f'{name}.png'); im.thumbnail((110,100)); x=i%10*120; y=i//10*125
    contact.paste(im,(x+(110-im.width)//2,y),im); draw.text((x+5,y+102),name,fill='white')
contact.save(OUT/'contact.jpg')
print(f'Prepared {len(manifest)} sprites.')
