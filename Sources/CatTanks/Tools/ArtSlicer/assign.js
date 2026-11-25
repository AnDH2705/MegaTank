// Gán các hình đã cắt (out/) vào asset của game và cất phần chưa dùng vào ArtSource.
// Chạy sau slice.js:  node assign.js
// - Key đã có: ghi đè ở Assets/CatTanks/Resources/Art và Assets/CatTanksMockupKit/Textures/Original (giữ nguyên .meta/GUID).
// - Key mới (cat-10..19): chỉ ghi vào Resources/Art; UI tự rơi về đó khi theme không có key.
"use strict";
const fs = require("fs"), path = require("path"), { PNG } = require("pngjs");
const ROOT = path.join(__dirname, "../.."), OUT = path.join(__dirname, "out");
const ART = path.join(ROOT, "Assets/CatTanks/Resources/Art"), ORIG = path.join(ROOT, "Assets/CatTanksMockupKit/Textures/Original");
const STORE = path.join(ROOT, "ArtSource/nhanvat_dodacmoi/unused");
const n2 = i => String(i).padStart(2, "0");

const plan = []; // [srcFile, key, transform?]
for (let i = 0; i < 20; i++) plan.push([`cats/${n2(i)}.png`, `cat-${i}`]);
for (let i = 0; i < 10; i++) plan.push([`guns/${n2(i)}.png`, `gun-${i}`]);
for (let i = 0; i < 10; i++) plan.push([`turrets/${n2(i)}.png`, `turret-${i}`]);
// 10 bộ quân phục (bỏ 2 bộ lặp: ô 06 phi công, ô 10 mũ tai bèo)
[0, 1, 2, 3, 4, 5, 7, 8, 9, 11].forEach((cell, i) => {
  plan.push([`hats/${n2(cell)}a.png`, `hat-${i}`]);
  plan.push([`hats/${n2(cell)}b.png`, `uniform-${i}`]);
  plan.push([`hats/${n2(cell)}b.png`, `armor-${i}`]);
});
plan.push([`boots/00.png`, `gear-boots`]);
// Chim thay module pack-* (giữ loại bắn): ô trong lưới chim -> pack
const BIRD = { 0: 5, 1: 2, 2: 7, 3: 6, 4: 3, 5: 19, 6: 15, 7: 10, 8: 14, 9: 9, 10: 18, 11: 11, 12: 13, 13: 8, 14: 17, 15: 16, 16: 4, 19: 12 };
for (const [pack, cell] of Object.entries(BIRD)) plan.push([`birds/${n2(cell)}.png`, `pack-${pack}`]);
plan.push([`birds/03.png`, `pack-17`, img => hue(img, 140)]); // bomb: Quạ đổi tông (tạm, thiếu chim)
plan.push([`birds/06.png`, `pack-18`, img => hue(img, 150)]); // crystal: Giẻ cùi đổi tông (tạm, thiếu chim)

let wrote = 0, added = [];
for (const [src, key, fx] of plan) {
  let img = PNG.sync.read(fs.readFileSync(path.join(OUT, src))); if (fx) fx(img);
  const buf = PNG.sync.write(img), a = path.join(ART, key + ".png"), o = path.join(ORIG, key + ".png");
  if (!fs.existsSync(a)) added.push(key);
  fs.writeFileSync(a, buf); wrote++;
  if (fs.existsSync(o)) { fs.writeFileSync(o, buf); wrote++; }
}
// Cất kho: súng hàng 3–4, giày 01–19, 6 bộ mũ áo vui + 2 bộ lặp
const store = [];
for (let i = 10; i < 20; i++) store.push([`guns/${n2(i)}.png`, `guns/gun-extra-${i - 10}.png`]);
for (let i = 1; i < 20; i++) store.push([`boots/${n2(i)}.png`, `boots/boots-${i}.png`]);
for (const cell of [6, 10, 12, 13, 14, 15, 16, 17]) for (const h of ["a", "b"]) store.push([`hats/${n2(cell)}${h}.png`, `hats_uniforms/${n2(cell)}${h === "a" ? "-hat" : "-uniform"}.png`]);
for (const [src, dst] of store) { const p = path.join(STORE, dst); fs.mkdirSync(path.dirname(p), { recursive: true }); fs.copyFileSync(path.join(OUT, src), p); }
console.log(`ghi ${wrote} file vào game; key mới: ${added.join(", ") || "không"}; cất kho ${store.length} file`);

function hue(img, deg) {
  const c = Math.cos(deg * Math.PI / 180), s = Math.sin(deg * Math.PI / 180), d = img.data;
  const m = [.213 + c * .787 - s * .213, .715 - c * .715 - s * .715, .072 - c * .072 + s * .928,
    .213 - c * .213 + s * .143, .715 + c * .285 + s * .140, .072 - c * .072 - s * .283,
    .213 - c * .213 - s * .787, .715 - c * .715 + s * .715, .072 + c * .928 + s * .072];
  for (let i = 0; i < d.length; i += 4) {
    const r = d[i], g = d[i + 1], b = d[i + 2];
    d[i] = clamp(m[0] * r + m[1] * g + m[2] * b); d[i + 1] = clamp(m[3] * r + m[4] * g + m[5] * b); d[i + 2] = clamp(m[6] * r + m[7] * g + m[8] * b);
  }
}
function clamp(v) { return Math.max(0, Math.min(255, Math.round(v))); }
