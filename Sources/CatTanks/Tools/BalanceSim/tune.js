// Apply candidate balance params to a copy of catalog.json and run the sim on an "expected progression" profile.
const fs = require("fs"), path = require("path");
const src = process.argv[2], params = JSON.parse(process.argv[3] || "{}"), out = path.join(require("os").tmpdir(), "cattanks-catalog.tuned.json");
const D = JSON.parse(fs.readFileSync(src, "utf8"));
const P = Object.assign({ hpBase: .13, atkBase: .17, growth: 1.12, bossHpBase: .018, range: null, heal: null, rockets: null, rocketDamage: null }, params);
D.stages.forEach((s, i) => s.waves.forEach(w => {
  const g = Math.pow(P.growth, i); w.enemyHpMult = P.hpBase * g; w.enemyAtkMult = P.atkBase * g; w.bossHpMult = P.bossHpBase * g;
}));
if (P.range) D.weapons.forEach(w => { if (w.range >= 1450) w.range = /^gun/.test(w.id) ? P.range - 150 : P.range; });
const sk = Object.fromEntries(D.skills.map(s => [s.id, s]));
if (P.heal) sk.heal.healVehicle = P.heal; if (P.rockets) sk.barrage.rockets = P.rockets; if (P.rocketDamage) sk.barrage.rocketDamage = P.rocketDamage;
fs.writeFileSync(out, JSON.stringify(D));
process.argv[2] = out; process.argv[3] = "{}";
const { run } = require("./sim.js");
// expected account level after first-clearing stages 1..s
const xp = D.stages.map(s => s.firstClearRewards.accountXp || 0);
function acctLevel(cleared) { let lv = 1, a = xp.slice(0, cleared).reduce((x, y) => x + y, 0) + 150 * Math.floor(cleared / 3); while (a >= 15 + lv * 4) { a -= 15 + lv * 4; lv++; } return lv; }
function profile(s, scale) {
  const acct = acctLevel(s - 1), L = Math.max(1, Math.round((1 + 1.5 * (s - 1)) * scale));
  return { floors: Math.min(5, 1 + Math.floor(acct / 20)), turretLevel: L, hpLevel: Math.max(1, Math.round((1 + (s - 1) / 2) * scale)),
    squadCount: 2 + (acct >= 8) + (acct >= 15) + (acct >= 25), acct, L,
    operators: [{ id: "cat-0", level: L, tier: "VETERAN" }],
    squad: [{ id: "cat-1", level: L, tier: "RECRUIT" }, { id: "cat-2", level: L, tier: "VETERAN" }, { id: "cat-9", level: L, tier: "RECRUIT" }, { id: "cat-5", level: L, tier: "RECRUIT" }, { id: "cat-4", level: L, tier: "VETERAN" }] };
}
const rows = [];
for (const s of [1, 2, 3, 5, 7, 10, 12, 15, 18, 20]) {
  const cells = [];
  for (const [label, scale] of [["exp", 1], ["low", .6]]) {
    const p = profile(s, scale); let wins = 0, hpl = 0, t = 0, heal = 0, cap = 0;
    for (let k = 1; k <= 12; k++) { const r = run(s, p, k * 104729); t += r.t; heal += r.healUses; if (r.win) { wins++; hpl += r.hpLeft; cap += r.bossCapWindow || 0; } }
    cells.push(`${label}(L${p.L},F${p.floors},sq${p.squadCount}) win ${String(wins).padStart(2)}/12 hp ${wins ? (hpl / wins * 100).toFixed(0).padStart(3) : "  -"}% t ${(t / 12).toFixed(0).padStart(3)}s heal ${(heal / 12).toFixed(1)}` + (D.stages[s - 1].boss && label === "exp" ? ` cap ${wins ? (cap / wins).toFixed(1) : "-"}s` : ""));
  }
  rows.push(`st${String(s).padStart(2)}  ` + cells.join("  |  "));
}
console.log(JSON.stringify(P)); console.log(rows.join("\n"));
