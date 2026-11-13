export class AudioEngine {
 constructor(){this.enabled=true;this.context=null;this.lastShot=0;}
 unlock(){if(!this.context){this.context=new AudioContext();this.master=this.context.createGain();this.master.gain.value=.22*(this.volume??1);this.master.connect(this.context.destination);}if(this.context.state==='suspended')this.context.resume();}
 toggle(){this.enabled=!this.enabled;if(this.enabled){if(this.volume===0)this.volume=1;this.unlock();if(this.master)this.master.gain.value=.22*(this.volume??1);}return this.enabled;}
 tone(freq,duration,type='sine',volume=.1,end=freq){if(!this.enabled||!this.context)return;const c=this.context,t=c.currentTime,o=c.createOscillator(),g=c.createGain();o.type=type;o.frequency.setValueAtTime(freq,t);o.frequency.exponentialRampToValueAtTime(Math.max(20,end),t+duration);g.gain.setValueAtTime(volume,t);g.gain.exponentialRampToValueAtTime(.001,t+duration);o.connect(g);g.connect(this.master);o.start();o.stop(t+duration);}
 noise(duration=.15,volume=.2,cutoff=900){if(!this.enabled||!this.context)return;const c=this.context,b=c.createBuffer(1,Math.floor(c.sampleRate*duration),c.sampleRate),a=b.getChannelData(0);for(let i=0;i<a.length;i++)a[i]=(Math.random()*2-1)*(1-i/a.length);const src=c.createBufferSource(),f=c.createBiquadFilter(),g=c.createGain();src.buffer=b;f.type='lowpass';f.frequency.value=cutoff;g.gain.value=volume;src.connect(f);f.connect(g);g.connect(this.master);src.start();}
 event(e){if(!this.enabled||!this.context)return;
  if(e.type==='fire'&&this.context.currentTime-this.lastShot>.09){this.lastShot=this.context.currentTime;const k=e.weapon.kind;if(['laser','tesla','plasma','ice'].includes(k))this.tone(850,.14,'sawtooth',.025,130);else{this.tone(140,.08,'triangle',.2,40);this.noise(.07,.12,1600);}}
  if(e.type==='kill'){this.noise(.24,.45,500);this.tone(80,.28,'sine',.4,25);}
  if(e.type==='wave'){this.tone(330,.2,'triangle',.12);setTimeout(()=>this.tone(494,.4,'triangle',.12),160);}
  if(e.type==='skill'){this.tone(300,.5,'sine',.2,1200);this.noise(.3,.16,700);}
 }
}
