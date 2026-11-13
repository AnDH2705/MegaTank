const canvas=document.querySelector('#canvas'),ctx=canvas.getContext('2d'),status=document.querySelector('#status');
const names={idle:'Đứng thở',run:'Chạy',attack:'Tấn công',hit:'Trúng đòn'};
async function loadImage(src){const im=new Image();im.src=src;await im.decode();return im;}
try{
  const response=await fetch('/assets/wolf-frames/manifest.json');if(!response.ok)throw new Error('Không tải được manifest');
  const manifest=await response.json();
  const reference=await loadImage('/assets/beast-0.png'),images={};
  await Promise.all(Object.entries(manifest.states).map(async([state,clip])=>{images[state]=await Promise.all(clip.frames.map(f=>loadImage(f.file)));}));
  let state='run',index=0,paused=false,elapsed=0,last=performance.now();
  function draw(){
    const light=document.querySelector('#light').checked;
    ctx.clearRect(0,0,canvas.width,canvas.height);
    ctx.fillStyle=light?'#3c4557':'#c4cfe0';ctx.font='600 19px system-ui';ctx.textAlign='center';
    ctx.fillText('ẢNH GỐC',280,35);ctx.fillText(`${names[state].toUpperCase()} · ${index+1}/6`,840,35);
    ctx.strokeStyle=light?'#b7bec9':'#42536d';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(560,60);ctx.lineTo(560,435);ctx.moveTo(35,410);ctx.lineTo(1085,410);ctx.stroke();
    // Fixed display scales. Never fit individual generated frame bounding boxes.
    const displayScale=1.8,refScale=Math.min(235/reference.width,183/reference.height)*displayScale;
    const refW=reference.width*refScale,refH=reference.height*refScale;
    ctx.drawImage(reference,280-refW/2,410-refH,refW,refH);
    ctx.drawImage(images[state][index],595,19,272*displayScale,256*displayScale);
    if(document.querySelector('#guide').checked){
      ctx.strokeStyle='#f6a950';ctx.setLineDash([6,5]);
      for(const x of [68.5,628])ctx.strokeRect(x,81,423,329);
      ctx.setLineDash([]);
    }
    status.textContent=`${names[state]} · Frame ${index+1}/6 · ${Math.round(1000/manifest.states[state].durationMs*Number(document.querySelector('#speed').value)*10)/10} fps${paused?' · Đã dừng':''}`;
    document.querySelectorAll('#strip button').forEach((b,i)=>b.setAttribute('aria-current',String(i===index)));
  }
  function setPaused(value){paused=value;elapsed=0;const b=document.querySelector('#pause');b.setAttribute('aria-pressed',String(value));b.textContent=value?'Tiếp tục':'Tạm dừng';}
  function showStrip(){const strip=document.querySelector('#strip');strip.replaceChildren();manifest.states[state].frames.forEach((f,i)=>{const b=document.createElement('button'),img=new Image();img.src=f.file;img.alt=`${names[state]} frame ${i+1}`;b.append(img,document.createTextNode(`Frame ${i+1}`));b.onclick=()=>{setPaused(true);index=i;draw();};strip.append(b);});}
  function setState(next){if(!manifest.states[next])throw new Error('Unknown clip');state=next;index=0;elapsed=0;document.querySelectorAll('[data-state]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.state===state)));showStrip();draw();}
  document.querySelectorAll('[data-state]').forEach(b=>b.onclick=()=>setState(b.dataset.state));
  document.querySelector('#pause').onclick=()=>{setPaused(!paused);draw();};
  for(const [id,step]of [['previous',-1],['next',1]])document.querySelector('#'+id).onclick=()=>{setPaused(true);index=(index+step+6)%6;draw();};
  document.querySelector('#light').onchange=e=>{document.querySelector('#stage').classList.toggle('light',e.target.checked);draw();};
  document.querySelector('#guide').onchange=draw;
  function tick(now){const dt=Math.min(now-last,100);last=now;if(!paused){elapsed+=dt*Number(document.querySelector('#speed').value);const duration=manifest.states[state].durationMs;while(elapsed>=duration){elapsed-=duration;index=(index+1)%6;}}draw();requestAnimationFrame(tick);}
  setState('run');requestAnimationFrame(tick);
  window.wolfFramesPreview={manifest,images,canvas,get state(){return state;},get index(){return index;},get paused(){return paused;},setState,setFrame(i){setPaused(true);index=((i%6)+6)%6;draw();}};
}catch(error){status.textContent=`Không mở được mẫu: ${error.message}`;console.error(error);}
