import { mkdir, cp, copyFile } from 'node:fs/promises';
await mkdir('dist', { recursive: true });
for (const file of ['index.html', 'style.css', 'workshop.css', 'meta.css', 'animation-preview.html', 'monster-preview.html', 'wolf-frames-preview.html']) await copyFile(file, `dist/${file}`);
await cp('src', 'dist/src', { recursive: true });
await cp('public', 'dist', { recursive: true });
console.log('Built dist/. Run npm run vendor before building for offline Three.js.');
