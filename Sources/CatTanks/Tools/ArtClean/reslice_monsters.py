"""Cắt lại bug-0..9 / beast-0..9 từ ảnh lưới gốc (ArtSource/quai_goc), thay bản cũ bị cắt cụt theo ô.

    py -3.12 -I reslice_monsters.py      # ghi out/Art/bug-*.png, beast-*.png

Ảnh gốc là RGB nền trắng, lưới 5 cột x 2 hàng, thứ tự trùng id trong catalog.
Nguồn: Desktop/tank_cat (bản web gốc) — "Mười quái vật côn trùng chibi rực rỡ.png", "Mười quái thú huyền thoại chibi.png".
"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *

SRC = os.path.join(ROOT, "ArtSource", "quai_goc")
SHEETS = {"bug": "bugs.png", "beast": "beasts.png"}
COLS, ROWS = 5, 2


def cut_out(rgb):
    """Tách nền trắng: loang từ mép qua các điểm gần màu nền; mép hình lấy alpha mềm theo độ sáng."""
    h, w = rgb.shape[:2]
    f = rgb.astype(np.float32)
    border = np.concatenate([f[0], f[-1], f[:, 0], f[:, -1]])
    bgc = np.median(border, 0)
    diff = np.abs(f - bgc).max(-1)
    cand = (diff < 40).astype(np.uint8)
    n, lab = cv2.connectedComponents(cand, connectivity=4)
    edge_labels = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(edge_labels)) & (cand > 0)
    fg = ~bg
    k = np.ones((3, 3), np.uint8)
    band = (cv2.dilate(bg.astype(np.uint8), k, iterations=2).astype(bool) & fg) | \
           (cv2.dilate(fg.astype(np.uint8), k, iterations=1).astype(bool) & bg)
    lum = f.mean(-1)
    bgl = float(bgc.mean())
    core = fg & ~band
    ring = cv2.dilate(bg.astype(np.uint8), k, iterations=5).astype(bool) & core     # nét viền đen ngay trong mép
    outline = np.median(f[ring], 0) if ring.any() else np.array([20, 20, 28], np.float32)
    ol = float(outline.mean())
    alpha = np.zeros((h, w), np.float32)
    alpha[core] = 255
    alpha[band] = np.clip((bgl - lum[band]) / max(1.0, bgl - ol), 0, 1) * 255
    out = np.zeros((h, w, 4), np.uint8)
    out[..., :3] = rgb
    out[..., :3][band] = outline.astype(np.uint8)
    out[..., 3] = alpha.astype(np.uint8)
    return out


def slice_sheet(a):
    solid = (a[..., 3] >= SOLID).astype(np.uint8)
    n, lab, st, cen = cv2.connectedComponentsWithStats(solid, 8)
    big = st[1:, cv2.CC_STAT_AREA].max()
    h, w = solid.shape
    cells = {}
    for c in range(1, n):
        if st[c, cv2.CC_STAT_AREA] < 0.003 * big:
            continue
        cx, cy = cen[c]
        idx = int(cy // (h / ROWS)) * COLS + int(cx // (w / COLS))
        cells.setdefault(idx, []).append(c)
    sprites = []
    for idx in range(COLS * ROWS):
        comps = cells.get(idx, [])
        if not comps:
            raise SystemExit(f"Ô {idx} không có hình — kiểm tra lại ảnh gốc.")
        keep = np.isin(lab, comps)
        near = cv2.dilate(keep.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
        sel = a.copy()
        sel[..., 3][~near] = 0
        sel[..., 3][near & ~keep & (a[..., 3] >= SOLID)] = 0     # không lấy phần đặc của con bên cạnh
        sprites.append(bleed_rgb(repad(sel)))
    return sprites


def run():
    os.makedirs(os.path.join(OUT, "Art"), exist_ok=True)
    for group, name in SHEETS.items():
        rgb = np.array(__import__("PIL.Image", fromlist=["Image"]).open(os.path.join(SRC, name)).convert("RGB"))
        for i, s in enumerate(slice_sheet(cut_out(rgb))):
            f = f"{group}-{i}.png"
            old = load(os.path.join(ART, f))
            save(os.path.join(OUT, "Art", f), s)
            print(f"{f:12} {old.shape[1]}x{old.shape[0]} -> {s.shape[1]}x{s.shape[0]}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    run()
