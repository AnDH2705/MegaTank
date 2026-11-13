// Rigid cutout rig: polygon UVs sample the original PNG without modifying it.
// Coordinates use a 322 x 345 design space mapped onto cat-0.png (316 x 349).
// Positive Y is up. These are rigid Bone attachments, not weighted skinning.
export function createCatRig(T, texture) {
  const root = new T.Group();
  const material = new T.MeshBasicMaterial({map:texture, transparent:true, depthWrite:false, side:T.DoubleSide});
  const bones = {};
  const geometries = [];
  function part(name, parent, pivot, outline, order) {
    const bone = new T.Bone(); bone.name = name;
    bone.position.set(pivot[0]-(parent.userData.pivot?.[0]??161), (parent.userData.pivot?.[1]??345)-pivot[1], 0);
    bone.userData.pivot = pivot; parent.add(bone); bones[name] = bone;
    const shape = new T.Shape(outline.map(([x,y])=>new T.Vector2(x-pivot[0], pivot[1]-y)));
    const geometry = new T.ShapeGeometry(shape);
    const p=geometry.attributes.position, uv=geometry.attributes.uv;
    for(let i=0;i<p.count;i++) uv.setXY(i,(p.getX(i)+pivot[0])/322,1-(pivot[1]-p.getY(i))/345);
    const mesh=new T.Mesh(geometry,material); mesh.renderOrder=order; bone.add(mesh); geometries.push(geometry);
    return bone;
  }
  const hips = part('hips',root,[173,283],[[87,216],[160,214],[256,216],[283,247],[291,306],[270,326],[238,331],[202,332],[163,329],[114,329],[80,301],[74,262]],2);
  part('tail',hips,[96,294],[[0,202],[60,201],[78,235],[68,274],[107,292],[113,328],[69,337],[20,319],[0,287]],1);
  const chest = part('chest',hips,[201,259],[[104,215],[258,211],[283,246],[282,294],[265,311],[205,316],[167,303],[145,284],[111,271]],3);
  part('head',chest,[202,214],[[53,0],[322,0],[322,205],[289,218],[255,227],[204,232],[150,230],[102,220],[60,199]],7);
  part('rearPaw',hips,[140,307],[[104,300],[151,291],[170,308],[185,336],[160,345],[101,345],[87,328]],4);
  part('farPaw',hips,[264,294],[[248,275],[287,272],[294,345],[239,345],[233,321]],4);
  part('frontPaw',chest,[211,277],[[183,254],[234,250],[241,302],[255,332],[248,345],[187,345],[178,321]],6);
  const rest = Object.fromEntries(Object.entries(bones).map(([n,b])=>[n,b.position.clone()]));
  let time=0;
  function pose(state='idle', t=time) {
    for(const [name,bone] of Object.entries(bones)){bone.position.copy(rest[name]);bone.rotation.z=0;bone.scale.set(1,1,1);}
    const breath=Math.sin(t*2.6);
    bones.chest.scale.y=1+breath*.009;
    bones.head.rotation.z=Math.sin(t*1.8)*.018;
    bones.tail.rotation.z=Math.sin(t*2.2)*.08;
    if(state==='walk') {
      const s=Math.sin(t*9);
      bones.hips.position.y+=Math.abs(Math.cos(t*9))*5;
      bones.rearPaw.rotation.z=s*.13;
      bones.frontPaw.rotation.z=-s*.14;
      bones.farPaw.rotation.z=s*.12;
      bones.chest.rotation.z=s*.014;
      bones.head.rotation.z=-s*.024;
      bones.tail.rotation.z=Math.sin(t*9+.7)*.1;
    } else if(state==='shoot') {
      const kick=Math.exp(-((t%0.7)/.085));
      bones.chest.position.x-=kick*7;
      bones.chest.rotation.z=kick*.045;
      bones.frontPaw.rotation.z=kick*.1;
      bones.head.rotation.z=-kick*.055;
    } else if(state==='hit') {
      const phase=t%1.4, kick=Math.exp(-phase*7)*Math.sin(phase*22);
      bones.hips.position.x-=kick*13;
      bones.chest.rotation.z=kick*.09;
      bones.head.rotation.z=-kick*.12;
      bones.tail.rotation.z=kick*.16;
    }
    root.updateMatrixWorld(true);
  }
  pose();
  return {root,bones,pose,update(dt,state){time+=dt;pose(state,time);},dispose(){geometries.forEach(g=>g.dispose());material.dispose();}};
}
