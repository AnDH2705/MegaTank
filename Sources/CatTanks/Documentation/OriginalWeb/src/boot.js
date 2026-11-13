const message = document.querySelector('#loading-message');
let THREE;
const sources = ['meow:/vendor/three.module.js', 'https://cdn.jsdelivr.net/npm/three@0.180.0/build/three.module.js', 'https://unpkg.com/three@0.180.0/build/three.module.js'];
for (const source of sources) {
  try { THREE = await Promise.race([import(source),new Promise((_,reject)=>setTimeout(()=>reject(new Error('Hết thời gian tải')),7000))]); break; } catch (error) { console.warn('Three.js source unavailable:', source, error.message); }
}
if (!THREE) {
  message.textContent = 'Chưa tải được Three.js. Mở mạng và chạy npm run vendor, sau đó tải lại trang. Code và ảnh đã có sẵn trong folder.';
  document.querySelector('#status').textContent = 'THREE.JS CHƯA KHẢ DỤNG';
} else {
  try { const { launch } = await import('./game.js'); await launch(THREE); }
  catch (error) { console.error(error); message.textContent = `Không khởi tạo được game: ${error.message}`; }
}
