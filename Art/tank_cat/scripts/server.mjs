import http from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { spawn } from 'node:child_process';
const root = process.cwd();
const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.png': 'image/png', '.jpg': 'image/jpeg', '.gif': 'image/gif', '.json': 'application/json' };
http.createServer(async (req, res) => {
  try {
    const route = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    if (route === '/__meow_health') {
      res.writeHead(200, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
      res.end(JSON.stringify({ app: 'meow-bastion', root, pid: process.pid }));
      return;
    }
    const relative = route === '/' ? 'index.html' : route.replace(/^\/+/, '');
    const base = /^(assets|vendor)\//.test(relative) ? path.join(root, 'public') : root;
    const target = path.resolve(base, relative);
    if (!target.startsWith(base + path.sep)) { res.writeHead(403).end(); return; }
    const file = await stat(target);
    if (!file.isFile()) throw new Error('Not a file');
    res.writeHead(200, { 'Content-Type': types[path.extname(target)] || 'application/octet-stream', 'Cache-Control': 'no-cache' });
    res.end(await readFile(target));
  } catch { res.writeHead(404).end('Not found'); }
}).listen(5173, '127.0.0.1', () => {
 console.log('MEOW BASTION: http://localhost:5173');
 if(process.argv.includes('--open'))spawn('cmd.exe',['/c','start','','http://localhost:5173'],{windowsHide:true,stdio:'ignore'});
});
