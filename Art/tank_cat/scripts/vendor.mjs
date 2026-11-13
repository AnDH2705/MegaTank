import { mkdir, writeFile } from 'node:fs/promises';
await mkdir('public/vendor', { recursive: true });
const downloaded = [];
for (const file of ['three.module.js', 'three.core.js']) {
  const response = await fetch(`https://cdn.jsdelivr.net/npm/three@0.180.0/build/${file}`, { signal: AbortSignal.timeout(20000) });
  if (!response.ok) throw new Error(`Cannot download ${file}: ${response.status}`);
  downloaded.push([file, await response.text()]);
}
for (const [file, content] of downloaded) {
  await writeFile(`public/vendor/${file}`, content);
  console.log(`Saved public/vendor/${file}`);
}
