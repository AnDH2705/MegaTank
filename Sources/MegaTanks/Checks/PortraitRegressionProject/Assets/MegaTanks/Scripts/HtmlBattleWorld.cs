using System;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MegaTanks
{
    // A dedicated native 2D scene. Each vehicle has one flipped root and a SortingGroup.
    public sealed class HtmlBattleWorld : MonoBehaviour
    {
        sealed class Vehicle { public GameObject root;public string skin;public readonly Dictionary<string,Transform> parts=new Dictionary<string,Transform>(); }
        sealed class Person { public SkeletonAnimation spine;public string pose;public float height;public Text name;public Material clipped;public bool named; }
        readonly Dictionary<HtmlBattleSimulation.Actor,Vehicle> vehicles=new Dictionary<HtmlBattleSimulation.Actor,Vehicle>();
        readonly Dictionary<object,Person> people=new Dictionary<object,Person>();
        readonly HashSet<HtmlBattleSimulation.Actor> visibleVehicles=new HashSet<HtmlBattleSimulation.Actor>();readonly HashSet<object> visiblePeople=new HashSet<object>();
        readonly List<SpriteRenderer> pool=new List<SpriteRenderer>();readonly Dictionary<string,Sprite> frames=new Dictionary<string,Sprite>();
        HtmlBattleArt art;HtmlBattleSimulation s;GameObject scene;Camera worldCamera;RenderTexture target;float delta;int cursor;Sprite white;static int nextWorld;float altitude;Func<string,Sprite> sourceSprite;
        Material blur;
        public HtmlBattleVfx Vfx { get;private set; }
        public void Initialize(HtmlBattleArt source,RawImage view,Func<string,Sprite> resolve=null)
        {
            sourceSprite=resolve;altitude=5000+100*(++nextWorld);art=source;scene=new GameObject("Native HTML battlefield");scene.layer=29;
            worldCamera=new GameObject("HTML orthographic worldCamera").AddComponent<Camera>();worldCamera.transform.position=new Vector3(0,altitude,-10);worldCamera.orthographic=true;worldCamera.orthographicSize=5.4f;
            worldCamera.cullingMask=1<<29;worldCamera.clearFlags=CameraClearFlags.SolidColor;worldCamera.backgroundColor=Color.black;
            target=new RenderTexture(1920,1080,24);target.name="Native MegaTank World";target.Create();worldCamera.targetTexture=target;worldCamera.enabled=false;view.texture=target;view.raycastTarget=false;
            blur=new Material(art.worldBlurShader);view.material=blur;
            Vfx=new HtmlBattleVfx(scene.transform,World);
            white=UnityEngine.Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,100);
        }
        Vector3 World(float x,float y)=>new Vector3((x-960)/100,altitude+(540-y)/100,0);
        Sprite Sprite(string key)=>string.IsNullOrEmpty(key)?white:sourceSprite?.Invoke(key)??art.Image(key)?.sprite;
        SpriteRenderer Quad(string key,float x,float y,float w,float h,int order,float alpha=1,bool flip=false,float rotation=0)
        {
            SpriteRenderer r;
            if(cursor<pool.Count)r=pool[cursor];else {var go=new GameObject("World sprite "+cursor);go.layer=29;go.transform.SetParent(scene.transform);r=go.AddComponent<SpriteRenderer>();pool.Add(r);}
            cursor++;r.gameObject.SetActive(true);r.sprite=Sprite(key);if(r.sprite==null){r.gameObject.SetActive(false);return r;}
            r.color=new Color(1,1,1,alpha);r.sortingOrder=order;r.transform.position=World(x,y);r.transform.localScale=new Vector3(w/r.sprite.rect.width*(flip?-1:1),h/r.sprite.rect.height,1);r.transform.rotation=Quaternion.Euler(0,0,rotation);return r;
        }
        public void Draw(HtmlBattleSimulation simulation,float dt)
        {
            s=simulation;delta=dt;cursor=0;
            blur.SetFloat("_Blur",s.phase=="choice"?3:0);
            visibleVehicles.Clear();visiblePeople.Clear();foreach(var person in people.Values)person.named=false;
            var bg=art.data.backgrounds.First(b=>b.id==s.stage.background);
            for(int i=0;i<bg.keys.Length;i++)
            {
                float[] factors=s.stage.background=="parallax_bg"?new[]{0,.15f,.35f,1,.7f,1.25f,1.15f}:new[]{0,.15f,.35f,1,.7f,1.1f,1.22f};float factor=factors[Math.Min(i,6)];
                int order=s.stage.background=="parallax_bg"&&i>=5?900+i:i;
                float offset=s.road*factor;int start=Mathf.FloorToInt(offset/1920);float shift=offset-start*1920;
                for(int tile=-1;tile<3;tile++)Quad(bg.keys[i],tile*1920-shift+960,540,1920,1080,order,1,(start+tile)%2!=0);
            }
            if((s.phase=="approach"||s.phase=="choice"||s.phase=="depart"||s.phase=="upgrade")&&!s.LandmarkConsumed)
            {
                if(s.encounter=="wreck")VehicleAt(parked,s.offeredSkin,s.landmarkX,s.config.groundY,s.config.playerWidth,false,s.offeredWeapons,100,false,0);
                else if(s.encounter=="upgrade")Quad("upgrade_station",s.landmarkX,s.config.groundY-300,960,640,100);
                else
                {
                    // Preserve the original transparent canvas and the renderer's 1000 x 562.5 rect.
                    Quad("tent",s.landmarkX,s.config.groundY-258.75f,1000,562.5f,100);
                    if(s.encounter=="medical")
                    {
                        float y=s.config.groundY-308;
                        Quad("",s.landmarkX,y,68,68,101).color=new Color(.169f,.161f,.2f);
                        Quad("",s.landmarkX,y,64,64,102).color=new Color(1,.965f,.867f);
                        var red=new Color(.914f,.388f,.322f);
                        Quad("",s.landmarkX,y,16,44,103).color=red;
                        Quad("",s.landmarkX,y,44,16,103).color=red;
                    }
                }
            }
            else if(vehicles.TryGetValue(parked,out var parkedView))parkedView.root.SetActive(false);
            foreach(var a in s.abandoned)VehicleAt(a,a.skin,a.x,s.config.groundY,a.width,false,a.weapons,110,false,0);
            foreach(var a in s.enemies.Where(e=>!e.dead).ToArray())Actor(a,300,1);
            var mounted=s.player.weapons.Select((v,i)=>v&&s.SlotUnlocked(i)).ToArray();
            VehicleAt(s.player,s.player.skin,s.player.x,s.config.groundY,s.player.width,false,mounted,400,true,dt);
            foreach(var f in s.fighters)
            {
                if(f.locked||f.dead&&f.deathAge>4){HidePerson(f);continue;}
                var profile=art.Profile(s.player.skin);var seat=profile.seats.FirstOrDefault(p=>p.id==f.role);
                if(seat==null)continue;float z=s.player.width/1351;
                if(f.aboard)
                {
                    var parent=vehicles[s.player].root.transform;
                int wi=f.role=="mg"?0:f.role=="cannon"?1:-1;bool firing=wi>=0&&s.player.cooldown[wi]>s.config.weapons[wi].cooldown-.27f;
                bool seated=f.role=="mg"||f.role=="cannon"||f.role=="driver";
                var person=PersonAt(f,f.role=="front"?art.catMelee:art.catDriver,parent,f.role=="front"||f.role=="mortar"?"idle":firing?"sit_fire":"sit_drive",new Vector3((seat.x-652)/100,(1258-seat.y-(seated?seat.scale*100:0))/100,0),seat.scale*4.7f,65,dt,true);
                }
                else
                {
                    bool moving=f.motion=="walk"||f.motion=="following"||f.motion=="walk_follow"||f.motion=="return";
                    string anim=f.dead?"death":f.motion=="attack"?"shovel_attack":moving?f.role=="front"?"walk":"run":"idle";
                    bool inside=f.jump>0&&f.x<=s.player.x+(1150-652)*z+12;
                    var parent=inside?vehicles[s.player].root.transform:scene.transform;Vector3 position=inside?parent.InverseTransformPoint(World(f.x,f.y)):World(f.x,f.y);
                    var person=PersonAt(f,f.role=="front"?art.catMelee:art.catDriver,parent,anim,position,seat.scale*4.7f*(inside?1:z),inside?65:500+(int)(f.y-s.config.groundY),dt,false,f.dead?Mathf.Clamp01(1-f.deathAge/4):1,f.target!=null&&f.target.x<f.x);
                    if(!f.dead&&f.jump<=0){var rail=Quad("",f.x,f.y-165,100,8,650);rail.color=new Color(.12f,.12f,.16f);var bar=Quad("",f.x-50+50*f.hp/Mathf.Max(1,f.maxHP),f.y-165,100*f.hp/Mathf.Max(1,f.maxHP),8,651);bar.color=new Color(.3f,.9f,.5f);Name(person,f.displayName??f.role,f.x,f.y-185);}
                }
            }
            foreach(var item in s.loot)
            {
                if(!item.chest)Quad(item.gear?"counter_gear":"counter_coin",item.x,item.y,45,45,800);
                else DrawChest(item.x,item.y,1,800);
            }
            if(s.config.useMainBattleVfx)Vfx.Draw(s,dt);else Vfx.Clear();
            foreach(var fx in s.effects)
            {
                if(s.config.useMainBattleVfx&&Vfx.Handles(fx))continue;
                if(fx.clip=="UpgradeAura")
                {
                    float t=fx.age,fade=Mathf.Min(1,t*4)*Mathf.Clamp01((s.config.upgradeAuraSeconds-t)*2);
                    Quad("aura_glow",s.player.x,s.config.groundY-180,650,650,700,fade*.65f);Quad("aura_ring",s.player.x,s.config.groundY,440,80,701,fade);
                    for(int i=0;i<9;i++)Quad(i%3==0?"aura_arrow_up":"aura_spark",s.player.x+Mathf.Sin(i*12.4f)*190,s.config.groundY-40-Mathf.Repeat(t*230+i*57,470),30,40,702,fade);
                    continue;
                }
                var clip=art.Clip(fx.clip);if(clip==null)continue;int frame=Math.Min(clip.frames-1,(int)(Mathf.Repeat(fx.age/clip.duration,1)*clip.frames));string id=clip.id+":"+frame;
                if(!frames.TryGetValue(id,out var sprite))
                {
                    var tex=art.Image(clip.key)?.texture;if(tex==null)continue;
                    int w=tex.width/clip.cols,h=tex.height/clip.rows;
                    sprite=UnityEngine.Sprite.Create(tex,new Rect(frame%clip.cols*w,tex.height-(frame/clip.cols+1)*h,w,h),Vector2.one*.5f,100);frames[id]=sprite;
                }
                var r=Quad("",fx.x,fx.y,fx.size,fx.size,fx.road?160:1100,Mathf.Clamp01(1-fx.age/fx.life));r.sprite=sprite;r.transform.localScale=Vector3.one*fx.size/sprite.rect.width;
            }
            foreach(var b in s.bullets)
            {
                if(s.config.useMainBattleVfx&&Vfx.HasProjectile(b))continue;
                string key=b.homing?"homing_missile":b.weapon=="mg"?"":"ammo_"+b.weapon;float w=b.weapon=="mg"?28:b.homing?55:78;
                var r=Quad(key,b.x,b.y,w,b.weapon=="mg"?8:28,1050,1,false,-Mathf.Atan2(b.ty-b.sy,b.tx-b.sx)*Mathf.Rad2Deg);if(b.weapon=="mg")r.color=new Color(1,.85f,.25f);
            }
            for(int i=cursor;i<pool.Count;i++)pool[i].gameObject.SetActive(false);
            foreach(var vehicle in vehicles)if(!visibleVehicles.Contains(vehicle.Key))vehicle.Value.root.SetActive(false);
            foreach(var person in people){if(!visiblePeople.Contains(person.Key))person.Value.spine.gameObject.SetActive(false);if(!person.Value.named&&person.Value.name!=null)person.Value.name.transform.parent.gameObject.SetActive(false);}
            worldCamera.transform.position=new Vector3(Mathf.Sin(s.time*99)*s.shake/100,altitude+Mathf.Cos(s.time*111)*s.shake/100,-10);SortingGroup.UpdateAllSortingGroups();worldCamera.Render();
        }
        readonly HtmlBattleSimulation.Actor parked=new HtmlBattleSimulation.Actor();
        void Name(Person person,string value,float x,float y)
        {
            if(person==null)return;
            if(person.name==null)
            {
                var go=new GameObject("Crew name",typeof(RectTransform),typeof(Canvas));go.layer=29;go.transform.SetParent(scene.transform);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=worldCamera;canvas.overrideSorting=true;canvas.sortingOrder=660;
                var child=new GameObject("Name",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));child.layer=29;child.transform.SetParent(go.transform,false);person.name=child.GetComponent<Text>();person.name.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");person.name.fontStyle=FontStyle.Bold;person.name.fontSize=20;person.name.alignment=TextAnchor.MiddleCenter;person.name.color=Color.white;person.name.horizontalOverflow=HorizontalWrapMode.Overflow;person.name.verticalOverflow=VerticalWrapMode.Overflow;person.name.raycastTarget=false;
                var rect=person.name.rectTransform;rect.sizeDelta=new Vector2(180,30);var outline=child.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(2,-2);
            }
            person.named=true;var root=person.name.transform.parent;root.gameObject.SetActive(true);root.position=World(x,y);root.localScale=Vector3.one*.01f;person.name.text=value;
        }
        void DrawChest(float x,float y,float scale,int order)
        {
            var edge=Quad("",x,y,58*scale,44*scale,order);edge.color=new Color(.15f,.15f,.2f);
            var basePart=Quad("",x,y,52*scale,38*scale,order+1);basePart.color=new Color(.67f,.35f,.17f);
            var lid=Quad("",x,y-12*scale,52*scale,16*scale,order+2);lid.color=new Color(.82f,.55f,.21f);
            for(int i=-1;i<=1;i+=2){var band=Quad("",x+i*15*scale,y,7*scale,38*scale,order+3);band.color=new Color(1,.83f,.35f);}
            var clasp=Quad("",x,y,13*scale,13*scale,order+4);clasp.color=new Color(1,.9f,.55f);
        }
        void Actor(HtmlBattleSimulation.Actor a,int order,float opacity)
        {
            if(a.kind=="dog")
            {
                var rig=a.crewRole=="cannon"?art.dogPilot:a.crewRole=="driver"||a.crewRole=="mg"?art.dogDriver:art.dogMelee;
                string moving=rig.GetSkeletonData(false).FindAnimation("walk")!=null?"walk":"run";
                PersonAt(a,rig,scene.transform,a.dead?"death":a.attack>.6f?"shovel_attack":moving,World(a.x,a.y),a.bossCrew?1.1f:1.4f,order,delta,false,opacity,true);return;
            }
            VehicleAt(a,a.skin,a.x,a.y,a.width,true,a.weapons,order,!a.dead,delta,opacity);
            if(!a.dead&&a.kind!="boss"){float y=s.Center(a).y-a.width*.3f;var rail=Quad("",a.x,y,120,9,620);rail.color=new Color(.1f,.08f,.1f);var fill=Quad("",a.x-60+60*a.hp/a.maxHP,y,120*a.hp/a.maxHP,7,621);fill.color=new Color(1,.83f,.25f);}
        }
        void VehicleAt(HtmlBattleSimulation.Actor actor,string skin,float x,float y,float width,bool enemy,bool[] weapons,int order,bool crew,float dt,float opacity=1)
        {
            visibleVehicles.Add(actor);
            var profile=art.Profile(skin);bool tank=actor.kind=="player"||actor.kind=="boss"||actor.kind=="parked"||actor==parked;
            var enemyArt=tank?null:art.data.enemies.FirstOrDefault(e=>e.id==actor.kind);if(!tank&&enemyArt==null)return;
            var parts=tank?profile.parts:enemyArt.parts;float[] origin=tank?new float[]{652,1258}:enemyArt.origin?.Length==2?enemyArt.origin:new float[]{768,actor.kind=="plane"?550:1002};
            float nativeWidth=tank?1351:actor.kind=="helicopter"?1400:actor.kind=="cannon_car"||actor.kind=="trojan"?1200:1536;
            if(!vehicles.TryGetValue(actor,out var visual))
            {visual=new Vehicle{root=new GameObject("Actor "+actor.id+" "+actor.kind)};visual.root.layer=29;visual.root.transform.SetParent(scene.transform);visual.root.AddComponent<SortingGroup>();vehicles.Add(actor,visual);}
            visual.root.SetActive(true);visual.root.GetComponent<SortingGroup>().sortingOrder=order;
            var root=visual.root.transform;root.position=World(x,y);root.localScale=new Vector3((enemy&&actor.kind!="trojan"?-1:1)*width/nativeWidth,width/nativeWidth,1);
            var visibleParts=new HashSet<string>();
            foreach(var p in parts)
            {
                if(visual.parts.ContainsKey(p.key))continue;var go=new GameObject(p.name);go.layer=29;go.transform.SetParent(root,false);visual.parts[p.key]=go.transform;
                var child=new GameObject("Art");child.layer=29;child.transform.SetParent(go.transform,false);child.AddComponent<SpriteRenderer>();
            }
            foreach(var p in parts.OrderBy(p=>p.order))
            {
                int wi=p.name.StartsWith("06_main_cannon")?1:p.name.StartsWith("08_roof")?0:p.name.StartsWith("09_mortar")||p.name.StartsWith("10_mortar")?2:-1;
                if(wi>=0&&!weapons[wi])continue;
                visibleParts.Add(p.key);
                if(!visual.parts.TryGetValue(p.key,out var pivot))
                {
                    var go=new GameObject(p.name);go.layer=29;go.transform.SetParent(root,false);pivot=go.transform;visual.parts[p.key]=pivot;
                    var child=new GameObject("Art");child.layer=29;child.transform.SetParent(pivot,false);child.AddComponent<SpriteRenderer>();
                }
                pivot.gameObject.SetActive(true);var renderer=pivot.Find("Art").GetComponent<SpriteRenderer>();renderer.sprite=Sprite(p.key);if(renderer.sprite==null)continue;
                renderer.color=new Color(1,1,1,opacity);renderer.sortingOrder=tank?p.order*10:p.order;
                float px=p.pivot?.Length==2?p.pivot[0]:p.position[0]+p.size[0]/2,py=p.pivot?.Length==2?p.pivot[1]:p.position[1]+p.size[1]/2;
                var parentPart=parts.FirstOrDefault(part=>part.name==p.parent&&part!=p);var parent=parentPart==null?root:visual.parts[parentPart.key];pivot.SetParent(parent,false);
                float parentX=parentPart?.pivot?.Length==2?parentPart.pivot[0]:origin[0],parentY=parentPart?.pivot?.Length==2?parentPart.pivot[1]:origin[1];
                pivot.localPosition=new Vector3((px-parentX)/100,(parentY-py)/100,0);
                renderer.transform.localPosition=new Vector3((p.position[0]+p.size[0]/2-px)/100,(py-p.position[1]-p.size[1]/2)/100,0);
                renderer.transform.localScale=new Vector3(p.size[0]/renderer.sprite.rect.width,p.size[1]/renderer.sprite.rect.height,1);pivot.localRotation=Quaternion.identity;
                if(wi==0||wi==1)pivot.localRotation=Quaternion.Euler(0,0,-s.AimAngle(actor,wi,enemy)*Mathf.Rad2Deg);
                if(p.name=="barrel"||p.name=="machine_gun")
                {
                    pivot.localRotation=Quaternion.Euler(0,0,-actor.gunAngle*Mathf.Rad2Deg);
                    if(p.name=="barrel")renderer.transform.localPosition-=new Vector3(s.EnemyGunRecoil(actor)/100,0,0);
                }
                if(p.name.Contains("wheel")&&actor.kind!="parked"&&!actor.dead)pivot.localRotation=Quaternion.Euler(0,0,-actor.wheel*.7f);
                if(p.name.Contains("tail_rotor")&&!actor.dead)pivot.localRotation=Quaternion.Euler(0,0,-s.time*36*Mathf.Rad2Deg);
                if(actor.kind=="trojan"&&actor.revealed&&p.parent=="shell_root")
                {
                    float t=actor.releaseAge,angle=t*.85f,dx=px-710,dy=py-570;
                    pivot.localPosition=new Vector3((710+t*400+dx*Mathf.Cos(angle)-dy*Mathf.Sin(angle)-origin[0])/100,(origin[1]-570+950*t-750*t*t-dx*Mathf.Sin(angle)-dy*Mathf.Cos(angle))/100,0);
                    pivot.localRotation=Quaternion.Euler(0,0,-angle*Mathf.Rad2Deg);renderer.color=new Color(1,1,1,opacity*Mathf.Clamp01((2-t)/.45f));
                }
                // Rotate a circular rotor within a vertically squashed parent; the hub remains fixed.
                if(p.name.Contains("main_rotor")||p.name=="rotor_blur")
                {pivot.localScale=new Vector3(1,.165f,1);renderer.transform.localScale=new Vector3(p.size[0]/renderer.sprite.rect.width,p.size[0]/renderer.sprite.rect.height,1);renderer.transform.localRotation=Quaternion.Euler(0,0,-s.time*30*Mathf.Rad2Deg);renderer.color=new Color(1,1,1,opacity*.42f);}
            }
            foreach(var part in visual.parts)if(!visibleParts.Contains(part.Key))part.Value.gameObject.SetActive(false);
            if(actor.kind=="trojan"&&!actor.revealed)
            {
                var top=root.TransformPoint(new Vector3((410-origin[0])/100,(origin[1]-775)/100));var bottom=root.TransformPoint(new Vector3((1030-origin[0])/100,(origin[1]-845)/100));
                for(int i=0;i<3;i++)
                {
                    var person=PersonAt(Tuple.Create(actor,"payload "+i),art.dogMelee,root,"walk",new Vector3((520+i*130-origin[0])/100,(origin[1]-840)/100),1.4f/(width/nativeWidth),45,dt,true,1,true);
                    Clip(person,new Vector4(Mathf.Min(top.x,bottom.x),Mathf.Min(top.y,bottom.y),Mathf.Max(top.x,bottom.x),Mathf.Max(top.y,bottom.y)));
                }
            }
            if(!crew||actor.kind=="player"||actor.kind=="parked"||actor==parked)return;
            if(tank)
            {
                foreach(var seat in profile.seats)
                {
                    int weapon=seat.id=="mg"?0:seat.id=="cannon"?1:-1;
                    if(seat.id=="mortar"||actor.deployedRoles.Contains(seat.id)||weapon>=0&&!actor.weapons[weapon])continue;
                    var key=Tuple.Create(actor,seat.id);
                    PersonAt(key,seat.id=="front"?art.dogMelee:seat.id=="cannon"?art.dogPilot:art.dogDriver,root,seat.id=="front"?"idle":"sit_drive",new Vector3((seat.x-652)/100,(1258-seat.y-(seat.id=="front"?0:seat.scale*100))/100,0),seat.scale*4.7f,65,dt,true);
                }
            }
            else if(actor.kind=="cannon_car"||actor.kind=="helicopter")
            {
                bool helicopter=actor.kind=="helicopter";float sx=helicopter?1030:780,sy=helicopter?665:636;
                PersonAt(actor,helicopter?art.dogPilot:art.dogDriver,root,"sit_drive",new Vector3((sx-origin[0])/100,(origin[1]-sy)/100,0),3.4f,40,dt,true);
            }
        }
        Person PersonAt(object key,SkeletonDataAsset rig,Transform parent,string pose,Vector3 position,float height,int order,float dt,bool local,float opacity=1,bool flip=false)
        {
            visiblePeople.Add(key);
            if(rig==null)return null;
            if(!people.TryGetValue(key,out var p))
            {
                var animation=SkeletonAnimation.NewSkeletonAnimationGameObject(rig);animation.enabled=false;animation.gameObject.layer=29;animation.gameObject.AddComponent<SortingGroup>();
                p=new Person{spine=animation};people[key]=p;
                animation.AnimationState.SetAnimation(0,rig.GetSkeletonData(false).FindAnimation(pose)!=null?pose:"idle",pose!="death");animation.Update(.01f);animation.LateUpdate();
                p.height=Mathf.Max(.01f,animation.GetComponent<MeshRenderer>().localBounds.size.y);
            }
            p.spine.gameObject.SetActive(true);if(p.spine.transform.parent!=parent)p.spine.transform.SetParent(parent,false);p.spine.transform.localPosition=position;
            p.spine.transform.localScale=new Vector3((flip?-1:1)*height/p.height,height/p.height,1);p.spine.GetComponent<SortingGroup>().sortingOrder=order;
            if(p.pose!=pose){var data=rig.GetSkeletonData(false);string selected=data.FindAnimation(pose)!=null?pose:data.FindAnimation("idle")!=null?"idle":data.Animations.Items[0].Name;p.spine.AnimationState.ClearTracks();p.spine.Skeleton.SetToSetupPose();p.spine.AnimationState.SetAnimation(0,selected,pose!="death");p.pose=pose;}
            p.spine.Skeleton.A=opacity;p.spine.Update(Mathf.Max(0,dt));p.spine.LateUpdate();return p;
        }
        void HidePerson(object key){if(people.TryGetValue(key,out var p))p.spine.gameObject.SetActive(false);}
        public bool ActorVisible(HtmlBattleSimulation.Actor actor)=>vehicles.TryGetValue(actor,out var vehicle)&&vehicle.root.activeInHierarchy||people.TryGetValue(actor,out var person)&&person.spine.gameObject.activeInHierarchy;
        public bool PartVisible(HtmlBattleSimulation.Actor actor,string key)=>vehicles.TryGetValue(actor,out var vehicle)&&vehicle.parts.TryGetValue(key,out var part)&&part.gameObject.activeInHierarchy;
        void Clip(Person person,Vector4 rect)
        {
            if(person==null)return;var renderer=person.spine.GetComponent<MeshRenderer>();
            if(person.clipped==null){var original=renderer.sharedMaterial;person.clipped=new Material(art.worldClipShader);person.clipped.mainTexture=original.mainTexture;person.spine.CustomMaterialOverride[original]=person.clipped;renderer.sharedMaterial=person.clipped;}
            var block=new MaterialPropertyBlock();block.SetVector("_ClipRect",rect);renderer.SetPropertyBlock(block);
        }
        void OnDestroy()
        {
            Vfx?.Clear();
            if(scene!=null)Destroy(scene);if(worldCamera!=null)Destroy(worldCamera.gameObject);if(target!=null){target.Release();Destroy(target);}foreach(var sprite in frames.Values)Destroy(sprite);foreach(var person in people.Values)if(person.clipped!=null)Destroy(person.clipped);if(white!=null)Destroy(white);
            if(blur!=null)Destroy(blur);
        }
    }
}




