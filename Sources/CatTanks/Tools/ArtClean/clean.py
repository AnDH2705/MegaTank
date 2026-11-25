"""Làm sạch + làm nét ảnh trong phạm vi. Mặc định CHỈ ghi ra out/Art, không đụng game.

    py -3.12 -I clean.py            # xử lý, ghi out/Art + out/clean_log.json
    py -3.12 -I clean.py --apply    # chép out/Art vào Resources/Art (+ bản sao MockupKit/Original nếu có)

Các bước cho mỗi ảnh (trừ bug/beast — do reslice_monsters.py cắt lại từ ảnh gốc):
 1. Xoá mảnh rời: nét mảnh < 4px hoặc mảnh < 0,2% hình chính (đường viền sticker đứt, chấm rác).
 2. Xoá bụi mờ (alpha < 48) không nằm sát hình.
 3. Thân hình alpha 240–254 (hơi trong suốt) → 255. Ảnh mờ (< 60% trung vị nhóm): unsharp mask
    trong lòng hình, tăng dần tới ~65% trung vị.
 4. Cắt sát rồi chừa đều 8px mỗi phía.
 5. Loang màu ra vùng trong suốt để Unity lọc ảnh không sinh quầng.
Giữ nguyên tên file và .meta (GUID).
"""
import os, sys, json, shutil
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
from audit import audit, BLUR

SKIP_GROUPS = {"bug", "beast"}
TARGET = 0.65      # làm nét ảnh mờ tới ~65% trung vị độ nét của nhóm
OUT_ART = os.path.join(OUT, "Art")


def clean_one(a, blurry):
    log = {}
    lab, drop, _ = stray_parts(a)
    a = a.copy()
    if drop:
        gone = np.isin(lab, drop)
        log["strays"] = len(drop)
        log["stray_px"] = int(gone.sum())
        a[..., 3][gone] = 0
    al = a[..., 3]
    kept = (al >= SOLID).astype(np.uint8)
    near = cv2.dilate(kept, np.ones((5, 5), np.uint8)).astype(bool)
    dust = ((al > 0) & (al < SOLID) & ~near) | ((al > 0) & (al < 4))
    if dust.any():
        log["dust_px"] = int(dust.sum())
        a[..., 3][dust] = 0
    semi = (al >= 240) & (al < 255)
    if semi.sum() > 0.2 * (al >= 240).sum():          # thân hình hơi trong suốt (alpha 240–254) → đặc hẳn
        log["solidify_px"] = int(semi.sum())
        a[..., 3][semi] = 255
    if blurry:
        before = sharpness(a)
        target = TARGET * blurry
        for amount in (0.6, 0.9, 1.2, 1.6, 2.0):        # tăng dần tới khi đạt mức nét chuẩn của nhóm
            s = unsharp(a, amount, 1.2)
            if sharpness(s) >= target:
                break
        a = s
        log["sharpen"] = [round(before), round(sharpness(a)), amount]
    h, w = a.shape[:2]
    a = repad(a)
    log["size"] = [f"{w}x{h}", f"{a.shape[1]}x{a.shape[0]}"]
    return bleed_rgb(a), log


def run():
    rows, med = audit(ART)
    os.makedirs(OUT_ART, exist_ok=True)
    logs = {}
    for r in rows:
        if r["group"] in SKIP_GROUPS:
            continue
        blurry = med[r["group"]] if r["sharp"] < BLUR * med[r["group"]] else 0
        a, log = clean_one(load(os.path.join(ART, r["file"])), blurry)
        save(os.path.join(OUT_ART, r["file"]), a)
        logs[r["file"]] = log
    json.dump(logs, open(os.path.join(OUT, "clean_log.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    for f, l in logs.items():
        extra = {k: v for k, v in l.items() if k != "size"}
        if extra:
            print(f"{f:18} {l['size'][0]:>8} -> {l['size'][1]:<8} {extra}")
    print(f"\nĐã ghi {len(logs)} ảnh vào {OUT_ART} (game chưa bị đụng).")


def apply():
    files = sorted(os.listdir(OUT_ART))
    for f in files:
        if not SCOPE.match(f):
            continue
        for dst in (ART, MIRROR):
            target = os.path.join(dst, f)
            if dst == ART or os.path.exists(target):
                shutil.copyfile(os.path.join(OUT_ART, f), target)
    print(f"Đã chép {len(files)} ảnh vào Resources/Art (và bản sao MockupKit/Original nếu có).")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    apply() if "--apply" in sys.argv else run()
