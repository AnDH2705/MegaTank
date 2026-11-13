export const SKILL_DEFS = {
  heal:      { id: 'heal',      name: 'Sửa chữa',     unlock: { start: true },           baseCost: 80,  baseCd: 8,  kind: 'heal',     healVehicle: 9000, healUnitPct: 0.40 },
  barrage:   { id: 'barrage',   name: 'Mưa tên lửa',  unlock: { start: true },           baseCost: 140, baseCd: 10, kind: 'barrage',  rockets: 18, rocketDamage: 190 },
  shield:    { id: 'shield',    name: 'Lá chắn',      unlock: { stage: 2 },              baseCost: 80,  baseCd: 8,  kind: 'shield',   duration: 7 },
  rally:     { id: 'rally',     name: 'Tiếp viện',    unlock: { stage: 4 },              baseCost: 60,  baseCd: 7,  kind: 'rally',    duration: 10, fireMult: 1.8 },
  freeze:    { id: 'freeze',    name: 'Bão băng',     unlock: { stage: 5 },              baseCost: 100, baseCd: 9,  kind: 'freeze',   slow: 6, damage: 120 },
  drone:     { id: 'drone',     name: 'Drone',        unlock: { stage: 10 },             baseCost: 70,  baseCd: 8,  kind: 'drone',    duration: 14 },
  orbital:   { id: 'orbital',   name: 'Không kích',   unlock: { stage: 15 },             baseCost: 150, baseCd: 10, kind: 'orbital',  damage: 4000, bossMult: 1.5, delay: 0.4 },
  decoy:     { id: 'decoy',     name: 'Mồi nhử',      unlock: { accountLevel: 20 },      baseCost: 70,  baseCd: 8,  kind: 'decoy',    duration: 5, hp: 800, radius: 420 },
  magnet:    { id: 'magnet',    name: 'Nam châm',     unlock: { accountLevel: 40 },      baseCost: 50,  baseCd: 6,  kind: 'magnet' },
  kamikaze:  { id: 'kamikaze',  name: 'Mèo cảm tử',   unlock: { accountLevel: 60 },      baseCost: 90,  baseCd: 9,  kind: 'kamikaze', damage: 2200, splash: 180 }
};
