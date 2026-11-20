import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const source = path.dirname(fileURLToPath(import.meta.url));
const manifest = path.resolve(source, '../../Assets/CatTanksVfxKit/Resources/CatTanksVfxKit/effects.json');
const data = JSON.parse(readFileSync(manifest, 'utf8'));
const template = readFileSync(path.join(source, 'gallery.template.html'), 'utf8');
const safeJson = JSON.stringify(data).replaceAll('<', '\\u003c');
writeFileSync(path.resolve(source, '../Preview.html'), template.replace('__EFFECTS_JSON__', () => safeJson));
console.log(`Built Preview.html with ${data.recipes.length} VFX recipes`);
