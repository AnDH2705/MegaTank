"""Quét ảnh trong phạm vi, gắn cờ lỗi chuẩn hoá.

    py -3.12 -I audit.py                 # quét Resources/Art của game
    py -3.12 -I audit.py --dir out/Art   # quét kết quả clean.py
Ghi report.json + bảng ảnh vào out/audit/.
"""
import os, sys, json, hashlib, statistics, collections
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *

BLUR = 0.6         # độ nét < 60% trung vị nhóm => mờ


def audit(folder):
    rows = []
    for f in in_scope(folder):
        p = os.path.join(folder, f)
        a = load(p)
        al = a[..., 3]
        _, drop, _ = stray_parts(a)
        loose = int(((al > 0) & (al < SOLID)).sum())
        rows.append({"file": f, "group": group_of(f), "w": a.shape[1], "h": a.shape[0],
                     "strays": len(drop), "pad": padding(al), "edge": edge_touch(al),
                     "sharp": round(sharpness(a), 1), "md5": hashlib.md5(open(p, "rb").read()).hexdigest(),
                     "mirror": None if not os.path.exists(os.path.join(MIRROR, f)) else
                     hashlib.md5(open(os.path.join(MIRROR, f), "rb").read()).hexdigest()})
    med = collections.defaultdict(list)
    for r in rows:
        med[r["group"]].append(r["sharp"])
    med = {g: statistics.median(v) for g, v in med.items()}
    seen = collections.defaultdict(list)
    for r in rows:
        seen[r["md5"]].append(r["file"])
    for r in rows:
        fl = []
        if r["strays"]:
            fl.append(f"mảnh rác {r['strays']}")
        p = r["pad"]
        if p and (min(p) < 3 or max(p) - min(p) > 4):
            fl.append(f"lề lệch {p}")
        cut = [k for k, v in r["edge"].items() if v >= 6]
        if cut:
            fl.append("chạm mép " + "".join(cut))
        if r["sharp"] < BLUR * med[r["group"]]:
            fl.append(f"mờ {r['sharp']:.0f}/{med[r['group']]:.0f}")
        twins = [x for x in seen[r["md5"]] if x != r["file"]]
        if twins:
            fl.append("trùng " + ",".join(twins))
        if folder == ART and r["mirror"] and r["mirror"] != r["md5"]:
            fl.append("lệch bản sao MockupKit")
        r["flags"] = fl
    return rows, med


def main():
    folder = ART
    if "--dir" in sys.argv:
        folder = os.path.abspath(sys.argv[sys.argv.index("--dir") + 1])
    rows, med = audit(folder)
    out = os.path.join(OUT, "audit", os.path.basename(os.path.dirname(folder)) + "_" + os.path.basename(folder))
    os.makedirs(out, exist_ok=True)
    json.dump({"folder": folder, "median_sharp": med, "rows": rows}, open(os.path.join(out, "report.json"), "w", encoding="utf-8"),
              ensure_ascii=False, indent=1)
    bad = [r for r in rows if r["flags"] and not all(x.startswith("trùng") for x in r["flags"])]
    for r in bad:
        print(f"{r['file']:18} {r['w']}x{r['h']:<4} " + " | ".join(r["flags"]))
    print(f"\n{len(rows)} ảnh, {len(bad)} ảnh có cờ lỗi. Báo cáo: {out}")
    groups = collections.defaultdict(list)
    for r in rows:
        groups[r["group"]].append((r["file"][:-4], load(os.path.join(folder, r["file"]))))
    for g, items in groups.items():
        sheet(items, os.path.join(out, f"{g}.png"))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
