// Dựng ảnh xem trước cách BattleView ghép mèo + áo + mũ + súng (tọa độ lấy từ BattleView.cs ActorView).
// node battle_preview.js <artDir> <out.png> [offsets.json]
"use strict";
const fs = require("fs"), path = require("path"), { PNG } = require("pngjs");
const [dir, outFile, offFile] = process.argv.slice(2);
const OFF = Object.assign({ towerCat: [-63, 10, 64], towerUni: [-63, -6, 43], towerHat: [-63, 39, 48], towerGun: [20, 0, 117],
  infCat: [-5, 17, 52], infUni: [-5, 0, 39], infHat: [-5, 39, 43], infGun: [13, 0, 57], aux: [13, 0, 57] }, offFile ? JSON.parse(fs.readFileSync(offFile)) : {});
const S = 3, CW = 220 * S, CH = 150 * S; // mỗi ô 220x150 đơn vị thế giới, phóng 3 lần
const units = [["tower", 0, "turret-0"], ["tower", 1, "turret-1"], ["tower", 2, "turret-7"], ["inf", 0, "gun-0"], ["inf", 1, "gun-1"], ["inf", 2, "gun-2"]];
const out = new PNG({ width: CW * units.length, height: CH });
for (let i = 0; i < out.data.length; i += 4) { out.data[i] = 70; out.data[i + 1] = 120; out.data[i + 2] = 150; out.data[i + 3] = 255; }
const load = k => { const p = path.join(dir, k + ".png"); return fs.existsSync(p) ? PNG.sync.read(fs.readFileSync(p)) : null; };
units.forEach(([role, idx, weapon], u) => {
  const ox = u * CW + CW / 2, oy = CH * .6, t = role === "tower";
  const parts = [[`cat-${idx}`, t ? OFF.towerCat : OFF.infCat], [`uniform-${idx}`, t ? OFF.towerUni : OFF.infUni], [`hat-${idx}`, t ? OFF.towerHat : OFF.infHat], [weapon, t ? OFF.towerGun : OFF.infGun]];
  for (const [key, [x, y, w]] of parts) { const img = load(key); if (img) blit(img, ox + x * S, oy - y * S, w * S); }
});
fs.writeFileSync(outFile, PNG.sync.write(out));
function blit(img, cx, cy, w) {
  const s = w / img.width, h = img.height * s, x0 = Math.round(cx - w / 2), y0 = Math.round(cy - h / 2);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const X = x0 + x, Y = y0 + y; if (X < 0 || Y < 0 || X >= out.width || Y >= out.height) continue;
    const si = (Math.min(img.height - 1, (y / s) | 0) * img.width + Math.min(img.width - 1, (x / s) | 0)) * 4, a = img.data[si + 3] / 255; if (!a) continue;
    const o = (Y * out.width + X) * 4; for (let c = 0; c < 3; c++) out.data[o + c] = Math.round(img.data[si + c] * a + out.data[o + c] * (1 - a));
  }
}
