"""Check the atlas, recipe links, catalog coverage and embedded preview data."""
import json
from pathlib import Path
from re import search

from PIL import Image


PROJECT = Path(__file__).resolve().parents[2]
ASSETS = PROJECT / "Assets/CatTanksVfxKit/Resources/CatTanksVfxKit"
manifest = json.loads((ASSETS / "effects.json").read_text(encoding="utf-8"))
recipes = {row["id"]: row for row in manifest["recipes"]}
weapons = {row["kind"]: row for row in manifest["weapons"]}
atlas = Image.open(ASSETS / "atlas.png")
catalog_file = PROJECT / "Assets/CatTanks/Resources/Data/catalog.json"
cols, rows, cell = (manifest[key] for key in ("columns", "rows", "cell"))
assert len(recipes) == len(manifest["recipes"]) and len(weapons) == len(manifest["weapons"])
assert atlas.mode == "RGBA" and atlas.size == (cols * cell, rows * cell)
assert len(manifest["tiles"]["clips"]) * 8 == manifest["tiles"]["staticStart"]


def tile_alpha(index):
    assert 0 <= index < cols * rows, index
    x, y = index % cols * cell, index // cols * cell
    return atlas.crop((x, y, x + cell, y + cell)).getchannel("A")


checked_tiles = set()
for listed in recipes.values():
    assert listed["seconds"] > 0 and listed["size"] > 0
    assert listed["frames"] in (1, 8)
    for i in range(listed["tile"], listed["tile"] + listed["frames"]):
        checked_tiles.add(i)
    if listed["count"]:
        assert listed["grain"] >= manifest["tiles"]["staticStart"]
        checked_tiles.add(listed["grain"])

for i in range(manifest["tiles"]["staticStart"] + len(manifest["tiles"]["statics"])):
    alpha = tile_alpha(i)
    # Tiny ringing from antialiasing is expected beyond the opaque contour.
    padded = alpha.point(lambda a: 255 if a > 12 else 0).getbbox()
    assert padded, f"Empty tile {i}"
    assert padded[0] > 0 and padded[1] > 0 and padded[2] < cell and padded[3] < cell, (i, padded)

for row in weapons.values():
    for name in ("muzzle", "flight", "impact"):
        assert not row[name] or row[name] in recipes, (row["kind"], row[name])

if catalog_file.exists():
    catalog = json.loads(catalog_file.read_text(encoding="utf-8"))
    catalog_kinds = {weapon["kind"] for weapon in catalog["weapons"]}
    catalog_kinds.update(pet["weapon"]["kind"] for pet in catalog["monsterBook"])
    assert catalog_kinds | {"web", "enemy"} == set(weapons)
    print(f'Covered {len(catalog["weapons"])} catalog weapons, {len(catalog["monsterBook"])} companions and two enemy-only kinds.')

preview = (PROJECT / "VFX_Handoff/Preview.html").read_text(encoding="utf-8")
embedded = search(r'<script id="data" type="application/json">(.+)</script>', preview)
assert embedded and json.loads(embedded.group(1)) == manifest
assert atlas.width == atlas.height == 2048 and len(checked_tiles) >= 200
print(f"Verified {len(recipes)} recipes, {len(weapons)} weapon routes and {i + 1} visible atlas cells.")
