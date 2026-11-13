// Redrawn closed silhouettes with fully painted hidden joint areas.
// All meshes sample one unchanged RGBA atlas. Overlap hides rotating roots.
export function createWolfRig(T, texture) {
  const root=new T.Group(), bones={}, meshes={}, geometries=[], materials=[];
  const motion=new T.Bone();root.add(motion);bones.motion=motion;
  function part(name,parent,x,y,crop,anchor,scale,order,color=0xffffff){
    const bone=new T.Bone();bone.name=name;bone.position.set(x,y,0);parent.add(bone);bones[name]=bone;
    const [l,t,r,b]=crop,[ax,ay]=anchor;
    const geometry=new T.PlaneGeometry((r-l)*scale,(b-t)*scale);
    geometry.translate(((l+r)/2-ax)*scale,(ay-(t+b)/2)*scale,0);
    const uv=geometry.attributes.uv;
    uv.setXY(0,l/1536,1-t/1024);uv.setXY(1,r/1536,1-t/1024);
    uv.setXY(2,l/1536,1-b/1024);uv.setXY(3,r/1536,1-b/1024);
    const material=new T.MeshBasicMaterial({map:texture,color,transparent:true,depthWrite:false,alphaTest:.45,side:T.DoubleSide});
    const mesh=new T.Mesh(geometry,material);mesh.renderOrder=order;bone.add(mesh);meshes[name]=mesh;
    geometries.push(geometry);materials.push(material);return bone;
  }
  const front=[120,525,425,980],hind=[565,530,885,980];
  part('farHind',motion,-84,162,hind,[685,620],.45,1,0xa6a2b7);
  part('farFront',motion,86,165,front,[250,605],.45,1,0xa6a2b7);
  const body=part('body',motion,0,180,[30,125,510,480],[260,300],.63,5);
  part('tail',body,-150,-12,[1090,55,1500,490],[1415,395],.39,0);
  part('hind',motion,-114,160,hind,[685,620],.46,4);
  part('front',motion,57,160,front,[250,605],.45,4);
  part('mane',body,83,5,[1090,615,1475,950],[1260,755],.25,6);
  part('head',body,55,59,[525,20,1068,510],[700,380],.43,7);
  const rest=Object.fromEntries(Object.entries(bones).map(([n,b])=>[n,b.position.clone()]));
  const offsets={tail:[-100,35],head:[75,95],mane:[70,-10],front:[30,-35],hind:[-50,-35],farFront:[65,0],farHind:[-70,0]};
  function pose(state='idle',t=0,explode=0){
    for(const [n,b]of Object.entries(bones)){b.position.copy(rest[n]);b.rotation.set(0,0,0);b.scale.set(1,1,1);}
    const breath=Math.sin(t*2.7);
    body.scale.y=1+breath*.012;
    bones.head.rotation.z=breath*.012;
    bones.tail.rotation.z=Math.sin(t*2)*.055;
    if(state==='run'){
      const phase=t*9, s=Math.sin(phase), c=Math.cos(phase);
      body.position.y+=Math.abs(c)*7;
      bones.front.rotation.z=s*.24;bones.farFront.rotation.z=-s*.24;
      bones.hind.rotation.z=-s*.23;bones.farHind.rotation.z=s*.23;
      bones.front.position.y+=Math.max(0,c)*7;bones.farFront.position.y+=Math.max(0,-c)*7;
      bones.hind.position.y+=Math.max(0,-c)*7;bones.farHind.position.y+=Math.max(0,c)*7;
      bones.head.rotation.z=-s*.025;bones.tail.rotation.z=Math.sin(phase+.6)*.11;
    }else if(state==='attack'){
      const phase=t%1.6;
      const lunge=Math.exp(-Math.pow((phase-.52)/.17,2));
      const windup=Math.exp(-Math.pow((phase-.22)/.14,2));
      motion.position.x=lunge*31-windup*10;
      body.rotation.z=-lunge*.045+windup*.025;
      bones.head.position.x+=lunge*18-windup*6;
      bones.head.rotation.z=-lunge*.1+windup*.04;
      bones.front.rotation.z=-lunge*.16;bones.hind.rotation.z=lunge*.09;
      bones.tail.rotation.z=-lunge*.13;
    }else if(state==='hit'){
      const phase=t%1.6, impact=Math.exp(-phase*6)*Math.sin(phase*21);
      motion.position.x=-impact*20;
      body.rotation.z=impact*.045;
      bones.head.rotation.z=impact*.11;
      bones.tail.rotation.z=-impact*.12;
      body.position.y-=Math.abs(impact)*5;
    }
    for(const [n,[x,y]]of Object.entries(offsets)){bones[n].position.x+=x*explode;bones[n].position.y+=y*explode;}
    root.updateMatrixWorld(true);
  }
  pose();
  return {root,bones,meshes,pose,dispose(){geometries.forEach(g=>g.dispose());materials.forEach(m=>m.dispose());}};
}
