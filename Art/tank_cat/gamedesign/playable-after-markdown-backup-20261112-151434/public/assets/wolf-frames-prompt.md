# Wolf frame-by-frame v1

Tool: built-in `image_gen.imagegen`, `transparent_background: true`.

Identity reference: `public/assets/beast-0.png` only. Previous tall rig was intentionally excluded as an identity reference.

Generated source (preserved unchanged): `public/assets/wolf-frames-v1.png` (1536 × 1024 RGBA).

Extracted assets, manifest, individual action GIFs and combined GIF: `public/assets/wolf-frames/`.

Assembly: `scripts/assemble-wolf-frames.py`. Complete connected silhouettes determine extraction bounds; the attack muzzle extends 5 pixels across a grid boundary and is retained. All frames use the same pixel scale and cell-relative origin; no independent resizing or body-part deformation. Only low-alpha generated background haze is removed from extracted copies, leaving source unchanged. Generated drawings still have small frame-to-frame variations; exact pixel identity is not guaranteed.

## Exact prompt

Use case: stylized-concept.
Asset type: production 2D sprite animation sheet, 24 full-character hand-drawn frames, strict 6 columns x 4 rows, landscape 1536x1024, transparent background.
Reference image 1 is the ONLY identity/proportion master. Redraw this exact same low crouching chibi wolf monster in sequential animation poses. Maintain reference anatomy precisely: very large head, SHORT NECK with head nestled directly into shoulders, low horizontal compact quadruped body, short stout legs, bushy upright curved tail, slate purple fur, cream muzzle/cheek/chest and paws, one yellow angry eye facing RIGHT, red slash marks on shoulder and rump, thick dark outlines and simple clean cel shading. No tall neck, no upright posture, no long legs, no round dog substitute, no design evolution. Same number and placement of markings in all frames. Four legs in anatomically consistent perspective. All frames should be complete drawings, NOT separate body parts.
LAYOUT MUST BE EXACT: 6 equal 256x256 cells per row, 4 equal rows. Each cell contains exactly ONE whole wolf, no duplicates, no clipped ears, paws, tail or snout, nothing on cell borders. The cell grid is implicit: DO NOT DRAW grid, text, numbers, ground shadows, effects, background or labels. Actual alpha transparency. Keep 14 pixels minimum empty padding around each silhouette. FIXED CAMERA and IDENTICAL CHARACTER SCALE in all 24 cells. Approximate wolf resting silhouette 222 pixels wide x 175 pixels tall, paws baseline local y=222, head top around y=45, local body center around (120,151). Never resize a character to fit a pose; preserve exact head/body/leg lengths and volume. Anchor body consistently in each frame. Slight natural motion only, not extreme deformation. This is an in-place game animation.
Read frames left to right:
ROW 1 (6-frame seamless IDLE loop): original threatening crouch, very subtle inhale over frames 1-3 and exhale over frames 4-6, tiny tail tip sway, head stable, paws planted. Last flows into first.
ROW 2 (6-frame seamless RUN loop): low quadruped running cycle in place, near front and opposite rear legs alternate contact, push-off, tuck, extend, contact, push-off. All 6 poses distinct, body rise/fall maximum 5 pixels, no size changes. Keep the reference compact squat anatomy and large head at every phase.
ROW 3 (6-frame ATTACK sequence): 1 original crouch, 2 compact anticipation shifting weight backwards, 3 lean forward with jaws opening, 4 short forward biting lunge (no detached head, no neck stretching), 5 jaws returning and weight settling, 6 recover original crouch. Maximum 10 pixels horizontal shift; keep full silhouette inside cell. Do not generate hit effects or projectiles.
ROW 4 (6-frame HIT/RECOVER sequence): 1 original crouch, 2 slight recoil backwards and head flinch, 3 compressed low stance, 4 regain footing, 5 settle, 6 original crouch. No wounds, no flashes or extra objects.
Critical: preserve original reference character's silhouette proportions, SHORT NECK, coloring, facial shape, shape of mane, tail and markings consistently across every frame. Six frames in every row; exactly 24 wolves total. True transparent background. Draw all hidden anatomy naturally per pose so there are no cutout seams.
