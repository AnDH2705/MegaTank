# Wolf cutout atlas v1

Created with the built-in `image_gen.imagegen` tool, transparent background enabled.

Reference: `public/assets/beast-0.png`. Original reference preserved.

Output: `public/assets/wolf-rig-atlas-v1.png`, 1536 × 1024, RGBA.

The preview samples the atlas with UVs. No bitmap repainting or background removal was performed after generation. Six painted parts are assembled with hidden roots overlapping under the torso/mane; two legs are reused and tinted for the far side.

## Exact generation prompt

Use case: stylized-concept. Asset type: transparent 2D cutout animation texture atlas, landscape 3 columns by 2 rows, six isolated parts of ONE chibi snarling gray-purple wolf monster based on the reference image. Reference image is identity/style guidance, not a finished sheet to crop. Redraw it from scratch for clean skeletal animation. Keep slate purple fur, cream muzzle and chest, yellow eye, red slash markings, dark bold game-art outline, facing RIGHT, same cute fierce game aesthetic. Actual transparent background. No words, grid lines, shadows, labels, full assembled character, or extra objects.
STRICT six equal cells with generous empty padding between parts, no part crossing cell boundary. Read left to right:
TOP LEFT: complete horizontal pear/bean-shaped wolf TORSO ONLY, from rounded rump on left to shoulder on right, cream chest accent on lower right, red slash on flank. NO head, legs, or tail. Fully paint the body underneath where all limbs and neck will overlap; no cut notches or empty sockets.
TOP MIDDLE: complete oversized wolf HEAD facing right, yellow eye, ears, snarling mouth with teeth and red tongue, cream muzzle and cheek, long shaggy mane extending to the LEFT and DOWN. Include a large complete fur-covered rounded neck base behind the head; it must overlap torso generously. No clipped straight cut line at neck.
TOP RIGHT: complete bushy curved wolf TAIL, tip up-left and a long thick rounded root at lower right. Paint ALL hidden root material, with no straight cut.
BOTTOM LEFT: one complete NEAR FRONT LEG, upper thick shoulder joint rounded at top, tapering bent foreleg, cream paw with claws pointing right at bottom. Include 35 percent extra painted upper shoulder for hiding underneath torso. Full uninterrupted limb.
BOTTOM MIDDLE: one complete NEAR HIND LEG, muscular rounded thigh at upper left, bent hock, paw pointing right at bottom. Include extra full painted round thigh extending behind where body will cover it.
BOTTOM RIGHT: compact crescent-shaped CHEST MANE OVERLAY, cream fur with purple outer tufts, used to cover the neck/shoulder seam. No face or limbs.
Render all six parts at comparable working resolution, each centered inside its cell. Parts are fully closed silhouettes, organic rounded shapes rather than rectangular cuts. Consistent light from upper left and consistent scale of brush line. Deliberately paint all hidden anatomy for overlapping joints so rotation will never reveal transparent holes. Crisp professional hand-painted 2D game sprite style. True alpha transparency.
