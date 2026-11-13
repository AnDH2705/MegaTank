# Local implementation handoff

Read `SESSION_NOTES.md` and `README.md` before continuing.

Keep the three original UI concepts in `../concept_UIUX` and the source reference images untouched. Preserve the current tank combat, controls, five fixed tower cats, independently aiming weapons, infantry and animations. The original simulation, data, renderer and wolf animation files remain identical to `../../src`; progression goes through `src/meta/tank-adapter.js`.

This is the writable standalone variant at port 5174. The original project at `../..` runs at 5173 and must not be overwritten from this workspace.

Run `npm.cmd run check` for simulation and progression logic, `npm.cmd run check:ui` for workshop and progression flows, plus the existing background and gameplay animation scripts when the corresponding integration changes. The local server must be running for browser checks. Inspect rendered screenshots before claiming visual verification. Rebuild with `npm.cmd run build` after final changes.

Treat `public/data` as the tuning source. Avoid formulas in UI files. Persist only source progression data, never derived hero stats or live simulation objects. Keep spending, RNG, progression and reward settlement inside GameStore transactions; preserve cross-tab locking and result idempotency.
