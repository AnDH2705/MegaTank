// Cắt ảnh lưới sticker thành từng PNG riêng.
// node slice.js <sheet.png> <outDir> [--grid RxC] [--black-bg] [--flip] [--merge N] [--min N]
//   --grid RxC : ảnh là lưới đều R hàng x C cột; mỗi mảnh vào ô chứa tâm của nó, file đặt tên theo số ô (ô trống bị bỏ qua)
//   --black-bg : nền đen dính cứng (ảnh RGB) -> loang từ mép để làm trong suốt
//   --flip     : lật ngang từng hình (vd tháp pháo đang quay nòng sang trái)
// Kết quả: outDir/00.png, 01.png... theo thứ tự hàng -> cột, và outDir/_preview.png để xem nhanh.
"use strict";
const fs = require("fs"), path = require("path"), { PNG } = require("pngjs");

const args = process.argv.slice(2);
const opt = (k, d) => { const i = args.indexOf(k); return i < 0 ? d : (args[i + 1] && !args[i + 1].startsWith("--") ? Number(args[i + 1]) : true); };
const [src, outDir] = args;
const blackBg = !!opt("--black-bg", false), flip = !!opt("--flip", false), merge = opt("--merge", 6), minArea = opt("--min", 400), athr = opt("--athr", 200), pad = 8;

const split = args.includes("--split");
const gi = args.indexOf("--grid"), grid = gi < 0 ? null : args[gi + 1].split("x").map(Number);
const png = PNG.sync.read(fs.readFileSync(src)); const W = png.width, H = png.height, d = png.data;
if (blackBg) removeBlackBackground();
const { labels, groups } = findGroups();
const rows = grid ? gridRows(groups) : sortIntoRows(groups);
fs.mkdirSync(outDir, { recursive: true });
const crops = []; let n = 0;
rows.forEach(row => row.forEach(g => { const idx = grid ? g.cell : n; n++; const img = crop(g); if (flip) flipH(img); fs.writeFileSync(path.join(outDir, String(idx).padStart(2, "0") + ".png"), PNG.sync.write(img)); crops.push(img); }));
fs.writeFileSync(path.join(outDir, "_preview.png"), PNG.sync.write(preview(crops, rows.map(r => r.length))));
console.log(`${src}: ${crops.length} hình, hàng = [${rows.map(r => r.length).join(", ")}]`);

function removeBlackBackground() {
  // Loang 4 hướng từ mép qua các điểm gần đen tuyệt đối; viền đen của sticker luôn có lớp mép sáng/xám bao ngoài nên không bị ăn vào.
  const T = 22, seen = new Uint8Array(W * H), q = [];
  const dark = i => d[i * 4] <= T && d[i * 4 + 1] <= T && d[i * 4 + 2] <= T;
  for (let x = 0; x < W; x++) { q.push(x, (H - 1) * W + x); } for (let y = 0; y < H; y++) { q.push(y * W, y * W + W - 1); }
  while (q.length) {
    const i = q.pop(); if (seen[i] || !dark(i)) continue; seen[i] = 1; d[i * 4 + 3] = 0;
    const x = i % W, y = (i / W) | 0;
    if (x > 0) q.push(i - 1); if (x < W - 1) q.push(i + 1); if (y > 0) q.push(i - W); if (y < H - 1) q.push(i + W);
  }
  // Khử răng cưa: điểm còn lại sát nền mà rất tối thì cho bán trong suốt.
  for (let i = 0; i < W * H; i++) {
    if (seen[i]) continue; const x = i % W, y = (i / W) | 0;
    const nb = (x > 0 && seen[i - 1]) || (x < W - 1 && seen[i + 1]) || (y > 0 && seen[i - W]) || (y < H - 1 && seen[i + W]);
    if (nb) { const m = Math.max(d[i * 4], d[i * 4 + 1], d[i * 4 + 2]); if (m < 60) d[i * 4 + 3] = Math.round(255 * Math.max(.35, m / 60)); }
  }
}
function findGroups() {
  const labels = new Int32Array(W * H).fill(-1), comps = [];
  for (let s = 0; s < W * H; s++) {
    if (labels[s] >= 0 || d[s * 4 + 3] < athr) continue;
    const id = comps.length, c = { ids: [id], x0: W, y0: H, x1: 0, y1: 0, area: 0 }, st = [s]; labels[s] = id;
    while (st.length) {
      const i = st.pop(), x = i % W, y = (i / W) | 0; c.area++;
      if (x < c.x0) c.x0 = x; if (x > c.x1) c.x1 = x; if (y < c.y0) c.y0 = y; if (y > c.y1) c.y1 = y;
      for (const j of [x > 0 ? i - 1 : -1, x < W - 1 ? i + 1 : -1, y > 0 ? i - W : -1, y < H - 1 ? i + W : -1])
        if (j >= 0 && labels[j] < 0 && d[j * 4 + 3] >= athr) { labels[j] = id; st.push(j); }
    }
    comps.push(c);
  }
  if (grid) return { labels, groups: comps.filter(c => c.area >= 20) };
  // Gộp mảnh có khung chạm nhau (nới rộng `merge` px), lặp tới khi ổn định.
  let groups = comps.slice(), changed = true;
  while (changed) {
    changed = false;
    for (let a = 0; a < groups.length && !changed; a++) for (let b = a + 1; b < groups.length; b++) {
      const A = groups[a], B = groups[b];
      if (A.x0 - merge <= B.x1 && B.x0 - merge <= A.x1 && A.y0 - merge <= B.y1 && B.y0 - merge <= A.y1) {
        groups[a] = { ids: A.ids.concat(B.ids), x0: Math.min(A.x0, B.x0), y0: Math.min(A.y0, B.y0), x1: Math.max(A.x1, B.x1), y1: Math.max(A.y1, B.y1), area: A.area + B.area };
        groups.splice(b, 1); changed = true; break;
      }
    }
  }
  return { labels, groups: groups.filter(g => g.area >= minArea) };
}
function gridRows(comps) {
  // Chia vùng có hình thành R x C ô đều nhau; mỗi mảnh (kể cả mảnh nhỏ như chân, tia lửa) vào ô chứa tâm của nó.
  const big = comps.filter(c => c.area >= minArea), [R, C] = grid;
  const X0 = Math.min(...big.map(c => c.x0)), X1 = Math.max(...big.map(c => c.x1)), Y0 = Math.min(...big.map(c => c.y0)), Y1 = Math.max(...big.map(c => c.y1));
  // xếp mảnh vào ô theo tâm, rồi trong mỗi ô chỉ giữ mảnh lớn nhất và các mảnh nằm sát nó (<= 12px) — bỏ vụn viền lạc
  const lists = {};
  for (const c of comps) {
    const cx = (c.x0 + c.x1) / 2, cy = (c.y0 + c.y1) / 2;
    const col = Math.min(C - 1, Math.max(0, Math.floor((cx - X0) / ((X1 - X0 + 1) / C)))), row = Math.min(R - 1, Math.max(0, Math.floor((cy - Y0) / ((Y1 - Y0 + 1) / R))));
    (lists[row * C + col] = lists[row * C + col] || []).push(c);
  }
  const cells = {};
  for (const [k, list] of Object.entries(lists)) {
    const keep = [], todo = list.slice().sort((a, b) => b.area - a.area); keep.push(todo.shift());
    const touches = (a, b, m) => a.x0 <= b.x1 + m && b.x0 <= a.x1 + m && a.y0 <= b.y1 + m && b.y0 <= a.y1 + m;
    let grew = true; while (grew) { grew = false; for (let i = 0; i < todo.length; i++) if (keep.some(b => touches(todo[i], b, 12)) || (split && todo[i].area >= minArea)) { keep.push(todo.splice(i, 1)[0]); grew = true; break; } }
    const g = cells[k] = { cell: Number(k), ids: [], x0: W, y0: H, x1: 0, y1: 0, area: 0 };
    for (const c of keep) { g.ids.push(...c.ids); g.x0 = Math.min(g.x0, c.x0); g.y0 = Math.min(g.y0, c.y0); g.x1 = Math.max(g.x1, c.x1); g.y1 = Math.max(g.y1, c.y1); g.area += c.area; }
  }
  if (split) {
    // mỗi ô có 2 phần xếp dọc (mũ trên, áo dưới): cắt tại khe dọc lớn nhất giữa các mảnh lớn
    const parts = {};
    for (const g of Object.values(cells)) {
      const own = comps.filter(c => g.ids.includes(c.ids[0])), bigs = own.filter(c => c.area >= minArea).sort((a, b) => a.y0 - b.y0);
      let cut = null, best = -1, reach = bigs.length ? bigs[0].y1 : 0;
      for (let i = 1; i < bigs.length; i++) { const gap = bigs[i].y0 - reach; if (gap > best) { best = gap; cut = (reach + bigs[i].y0) / 2; } reach = Math.max(reach, bigs[i].y1); }
      if (best <= 0) {
        // mũ và áo dính liền: cắt ngang tại dòng có ít điểm lõi nhất trong khoảng 30–70% chiều cao
        const set = new Set(g.ids); let minC = Infinity, yCut = null;
        for (let y = Math.round(g.y0 + (g.y1 - g.y0) * .3); y <= g.y0 + (g.y1 - g.y0) * .7; y++) {
          let c = 0; for (let x = g.x0; x <= g.x1; x++) if (set.has(labels[y * W + x])) c++;
          if (c < minC) { minC = c; yCut = y; }
        }
        for (const [half, y0, y1] of [["a", g.y0, yCut - 1], ["b", yCut, g.y1]])
          parts[g.cell + half] = { cell: String(g.cell).padStart(2, "0") + half, order: g.cell * 2 + (half === "b"), ids: g.ids, x0: g.x0, x1: g.x1, y0, y1, clipY0: y0, clipY1: y1, area: g.area };
        continue;
      }
      for (const [half, test] of [["a", c => cut == null || (c.y0 + c.y1) / 2 < cut], ["b", c => cut != null && (c.y0 + c.y1) / 2 >= cut]]) {
        const sel = own.filter(test); if (!sel.length) continue;
        parts[g.cell + half] = { cell: String(g.cell).padStart(2, "0") + half, order: g.cell * 2 + (half === "b"), ids: sel.flatMap(c => c.ids), x0: Math.min(...sel.map(c => c.x0)), y0: Math.min(...sel.map(c => c.y0)), x1: Math.max(...sel.map(c => c.x1)), y1: Math.max(...sel.map(c => c.y1)), area: sel.reduce((a, c) => a + c.area, 0) };
      }
    }
    return [Object.values(parts).filter(p => p.area >= minArea).sort((a, b) => a.order - b.order)];
  }
  const rows = []; for (let r = 0; r < R; r++) rows.push(Object.values(cells).filter(g => Math.floor(g.cell / C) === r && g.area >= minArea).sort((a, b) => a.cell - b.cell));
  return rows;
}
function sortIntoRows(groups) {
  const gs = groups.slice().sort((a, b) => (a.y0 + a.y1) - (b.y0 + b.y1)), rows = [];
  for (const g of gs) {
    const cy = (g.y0 + g.y1) / 2, h = g.y1 - g.y0, row = rows[rows.length - 1];
    if (row && Math.abs(cy - row.cy) < Math.max(h, row.h) * .5) { row.items.push(g); row.cy = row.items.reduce((s, v) => s + (v.y0 + v.y1) / 2, 0) / row.items.length; }
    else rows.push({ cy, h, items: [g] });
  }
  return rows.map(r => r.items.sort((a, b) => a.x0 - b.x0));
}
function crop(g) {
  const set = new Set(g.ids), w = g.x1 - g.x0 + 1 + pad * 2, h = g.y1 - g.y0 + 1 + pad * 2, out = new PNG({ width: w, height: h });
  // mặt nạ "gần lõi": nới lõi của chính hình này ra pad px, viền mờ chỉ được giữ trong vùng đó
  const near = new Uint8Array(w * h);
  for (let y = g.y0; y <= g.y1; y++) for (let x = g.x0; x <= g.x1; x++) if (set.has(labels[y * W + x])) {
    const lx = x - g.x0 + pad, ly = y - g.y0 + pad;
    if (near[ly * w + lx] === 2) continue; near[ly * w + lx] = 2;
    for (let dy = -pad; dy <= pad; dy += 2) for (let dx = -pad; dx <= pad; dx += 2) { const nx = lx + dx, ny = ly + dy; if (nx >= 0 && ny >= 0 && nx < w && ny < h && !near[ny * w + nx]) near[ny * w + nx] = 1; }
  }
  for (let y = Math.max(0, g.y0 - pad); y <= Math.min(H - 1, g.y1 + pad); y++) for (let x = Math.max(0, g.x0 - pad); x <= Math.min(W - 1, g.x1 + pad); x++) {
    if (g.clipY0 != null && (y < g.clipY0 || y > g.clipY1)) continue;
    const i = y * W + x; let keep = set.has(labels[i]);
    // giữ cả điểm mờ (alpha thấp) nằm sát hình để mép mềm không bị cụt
    if (!keep && labels[i] < 0 && d[i * 4 + 3] > 0 && near[(y - g.y0 + pad) * w + (x - g.x0 + pad)]) keep = true;
    if (!keep) continue; const o = ((y - g.y0 + pad) * w + (x - g.x0 + pad)) * 4;
    out.data[o] = d[i * 4]; out.data[o + 1] = d[i * 4 + 1]; out.data[o + 2] = d[i * 4 + 2]; out.data[o + 3] = d[i * 4 + 3];
  }
  return out;
}
function flipH(img) {
  const { width: w, height: h, data } = img;
  for (let y = 0; y < h; y++) for (let x = 0; x < w / 2; x++) {
    const a = (y * w + x) * 4, b = (y * w + (w - 1 - x)) * 4;
    for (let k = 0; k < 4; k++) { const t = data[a + k]; data[a + k] = data[b + k]; data[b + k] = t; }
  }
}
function preview(imgs, perRow) {
  // Ghép lên nền ca rô xám-hồng để thấy rõ chỗ trong suốt, giữ bố cục hàng như ảnh gốc.
  const cell = 300, cols = Math.max(...perRow), rowsN = perRow.length, out = new PNG({ width: cols * cell, height: rowsN * cell });
  for (let y = 0; y < out.height; y++) for (let x = 0; x < out.width; x++) {
    const o = (y * out.width + x) * 4, c = ((x >> 4) + (y >> 4)) & 1;
    out.data[o] = c ? 255 : 200; out.data[o + 1] = c ? 120 : 200; out.data[o + 2] = c ? 220 : 200; out.data[o + 3] = 255;
  }
  let k = 0;
  perRow.forEach((n, r) => { for (let cI = 0; cI < n; cI++) {
    const img = imgs[k++], s = Math.min(1, (cell - 10) / Math.max(img.width, img.height));
    const ow = Math.round(img.width * s), oh = Math.round(img.height * s), ox = cI * cell + ((cell - ow) >> 1), oy = r * cell + ((cell - oh) >> 1);
    for (let y = 0; y < oh; y++) for (let x = 0; x < ow; x++) {
      const si = (Math.min(img.height - 1, (y / s) | 0) * img.width + Math.min(img.width - 1, (x / s) | 0)) * 4, a = img.data[si + 3] / 255;
      if (!a) continue; const o = ((oy + y) * out.width + ox + x) * 4;
      for (let ch = 0; ch < 3; ch++) out.data[o + ch] = Math.round(img.data[si + ch] * a + out.data[o + ch] * (1 - a));
    }
  } });
  return out;
}
