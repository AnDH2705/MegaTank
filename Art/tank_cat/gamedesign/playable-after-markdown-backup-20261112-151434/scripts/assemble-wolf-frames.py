"""Extract generated complete drawings and encode a review GIF; no rigging.

One global scale, fixed atlas-cell origin. Component bounds prevent the biting
muzzle crossing a grid line from being cut off or leaking into its neighbour.
"""
from pathlib import Path
from collections import deque
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'public/assets/wolf-frames'
OUT.mkdir(exist_ok=True)
atlas = Image.open(ROOT / 'public/assets/wolf-frames-v1.png').convert('RGBA')
assert atlas.size == (1536, 1024)
alpha = atlas.getchannel('A')
pixels = alpha.load()
seen = bytearray(atlas.width * atlas.height)
components = []
for y in range(atlas.height):
    for x in range(atlas.width):
        idx = y * atlas.width + x
        if seen[idx] or pixels[x, y] < 115:
            continue
        seen[idx] = 1
        queue = deque([(x, y)])
        left = right = x
        top = bottom = y
        area = 0
        while queue:
            px, py = queue.popleft()
            area += 1
            left, right = min(left, px), max(right, px)
            top, bottom = min(top, py), max(bottom, py)
            for nx, ny in ((px-1, py), (px+1, py), (px, py-1), (px, py+1)):
                if 0 <= nx < atlas.width and 0 <= ny < atlas.height:
                    n = ny * atlas.width + nx
                    if not seen[n] and pixels[nx, ny] >= 115:
                        seen[n] = 1
                        queue.append((nx, ny))
        if area > 4000:
            components.append((left, top, right+1, bottom+1, area))
assert len(components) == 24, f'Expected 24 complete silhouettes, got {len(components)}'
rows = [[] for _ in range(4)]
for component in components:
    rows[int(((component[1]+component[3])/2)//256)].append(component)
assert all(len(row) == 6 for row in rows)

states = [('idle', 'IDLE', 180), ('run', 'RUN', 90), ('attack', 'ATTACK', 120), ('hit', 'HIT / RECOVER', 120)]
manifest = {'frameWidth': 272, 'frameHeight': 256, 'scale': 1, 'states': {}}
all_gif, all_durations = [], []
font_path = Path('C:/Windows/Fonts/arial.ttf')
font = ImageFont.truetype(str(font_path), 19) if font_path.exists() else ImageFont.load_default()
reference = Image.open(ROOT / 'public/assets/beast-0.png').convert('RGBA')
# One fixed reference scale across every action; one fixed generated scale.
# Original alpha bbox is 337 x 261; approximately 235px silhouette width.
reference.thumbnail((235, 183), Image.Resampling.LANCZOS)

for row_index, (state, label, duration) in enumerate(states):
    records, gif_frames = [], []
    for col, component in enumerate(sorted(rows[row_index])):
        l, t, r, b, area = component
        # 2px outside the solid silhouette retains antialiasing, not neighbours.
        rect = (max(0,l-2), max(0,t-2), min(atlas.width,r+2), min(atlas.height,b+2))
        crop = atlas.crop(rect)
        # Remove generated low-alpha background haze; retain a soft 80-alpha
        # transition at the outline. RGB drawing and all anatomical ratios stay.
        crop.putalpha(crop.getchannel('A').point(lambda a: max(0,min(255,round((a-85)*255/100)))))
        frame = Image.new('RGBA', (272,256))
        offset = (rect[0]-col*256+8, rect[1]-row_index*256)
        assert offset[0] >= 0 and offset[0]+crop.width <= 272
        assert offset[1] >= 0 and offset[1]+crop.height <= 256
        frame.alpha_composite(crop, offset)
        filename = f'{state}-{col}.png'
        frame.save(OUT / filename)
        records.append({'file':f'/assets/wolf-frames/{filename}', 'sourceRect':list(rect), 'offset':list(offset), 'solidBounds':list(component[:4])})
        review = Image.new('RGBA', (704,360), '#142136')
        draw = ImageDraw.Draw(review)
        draw.text((38,20), 'ORIGINAL', font=font, fill='#bcc8d8')
        draw.text((389,20), f'{label}   {col+1}/6', font=font, fill='#dfc2ff')
        draw.line((352,65,352,330), fill='#3c4e69', width=1)
        draw.line((30,302,674,302), fill='#52647d', width=1)
        review.alpha_composite(reference, (52,302-reference.height))
        review.alpha_composite(frame, (384,85))
        # Shared palette avoids independent per-frame GIF palette flicker.
        gif_frames.append(review.convert('RGB'))
    palette = gif_frames[0].quantize(colors=256)
    gif_frames = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in gif_frames]
    gif_frames[0].save(OUT/f'{state}.gif', save_all=True, append_images=gif_frames[1:], duration=duration, loop=0, disposal=2, optimize=False)
    manifest['states'][state] = {'durationMs':duration, 'frames':records}
    # Two full loops of each action in the combined review.
    all_gif.extend(gif_frames*2)
    all_durations.extend([duration]*len(gif_frames)*2)
all_gif[0].save(OUT/'wolf-actions.gif',save_all=True,append_images=all_gif[1:],duration=all_durations,loop=0,disposal=2,optimize=False)
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Assembled 24 unscaled drawings, 4 GIF clips, combined review GIF, and manifest.')
print('Frame dimensions: 272 x 256. Solid bounding boxes:', [c[:4] for c in components])
