"""Render a staged CatTanks VFX battle with Pillow and FFmpeg."""
from argparse import ArgumentParser
from collections import deque
from dataclasses import dataclass, field
from functools import lru_cache
import json
from math import atan2, cos, degrees, hypot, pi, sin
from os import environ
from pathlib import Path
from random import Random
from subprocess import PIPE, Popen, run as run_process
from uuid import uuid4

from PIL import Image, ImageDraw, ImageFont, ImageOps
from battle_audio import write_battle_audio
from battle_polish import focus, heavy, landscape, spill, stamp
import battle_iterations as layers


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/CatTanks/Resources/Art"
KIT = ROOT / "Assets/CatTanksVfxKit/Resources/CatTanksVfxKit"
CONFIG = json.loads((KIT / "effects.json").read_text(encoding="utf-8"))
CATALOG = json.loads((ART.parent / "Data/catalog.json").read_text(encoding="utf-8"))
RECIPES = {recipe["id"]: recipe for recipe in CONFIG["recipes"]}
ROUTES = {row["kind"]: row for row in CONFIG["weapons"]}
WEAPONS = {row["id"]: row for row in CATALOG["weapons"]}
ATLAS = Image.open(KIT / "atlas.png").convert("RGBA")
WIDTH, HEIGHT, GROUND = 960, 540, 439
FPS, SECONDS = 60, 12
RNG = Random(73)


def font(size, bold=False):
    name = "segoeuib.ttf" if bold else "segoeui.ttf"
    candidates = [Path("C:/Windows/Fonts") / name, "DejaVuSans.ttf"]
    for candidate in candidates:
        try:
            return ImageFont.truetype(str(candidate), size)
        except OSError:
            pass
    return ImageFont.load_default()


TITLE, LABEL, SMALL = font(26, True), font(16, True), font(13)


@lru_cache(maxsize=200)
def tile(index):
    size, cols = CONFIG["cell"], CONFIG["columns"]
    x, y = index % cols * size, index // cols * size
    return ATLAS.crop((x, y, x + size, y + size))


@lru_cache(maxsize=120)
def art(name, width, mirror=False):
    image = Image.open(ART / (name + ".png")).convert("RGBA")
    if not name.startswith("wolf-frames/"):
        image = image.crop(image.getbbox())
    image = image.resize((width, round(image.height * width / image.width)), Image.Resampling.LANCZOS)
    return ImageOps.mirror(image) if mirror else image


def paste(canvas, image, x, y, angle=0, alpha=1):
    if angle:
        image = image.rotate(-degrees(angle), Image.Resampling.BICUBIC, expand=True)
    if alpha < .999:
        image = image.copy()
        image.putalpha(image.getchannel("A").point(lambda value: int(value * max(0, alpha))))
    canvas.alpha_composite(image, (round(x - image.width / 2), round(y - image.height / 2)))


def glyph(canvas, index, x, y, width, height=None, angle=0, alpha=1):
    image = tile(index).resize((max(1, round(width)), max(1, round(height or width))), Image.Resampling.BILINEAR)
    paste(canvas, image, x, y, angle, alpha)


@dataclass
class Enemy:
    name: str
    x: float
    offset: float
    flash: float = -1
    freeze: float = -1
    health: float = 1

    def aim(self):
        return self.x, GROUND - (37 if self.name == "wolf" else 26) + self.offset


@dataclass
class Shot:
    kind: str
    x: float
    y: float
    angle: float
    speed: float
    target: Enemy
    born: float
    serial: int = 0
    trace: deque = field(default_factory=deque)
    lane: int = 0
    last_smoke: float = -1


@dataclass
class Effect:
    key: str
    x: float
    y: float
    start: float
    angle: float = 0
    scale: float = 1
    seed: int = field(default_factory=lambda: RNG.randrange(100000))


def show_effect(canvas, effect, time, visual_round):
    recipe = RECIPES[effect.key]
    age = time-effect.start
    if effect.key in ("rocketblast","blast"):
        if age >= 1:
            return False
        strength=max(.56,effect.scale)*(1.14 if visual_round>=3 else 1)
        heavy(canvas,paste,effect.key,effect.x,effect.y,age,effect.seed,strength,core=visual_round<3)
        layers.impact(canvas,paste,effect,age,visual_round,GROUND)
        return True
    if effect.key in ("flash","cannonflash","ignition"):
        if age >= recipe["seconds"]:
            return False
        phase=age/recipe["seconds"]
        width={"flash":46,"cannonflash":103,"ignition":75}[effect.key]
        if visual_round:
            width*=1.14
        x,y=effect.x,effect.y
        if phase < .66:
            power=(1-phase/.66)**.57
            spill(canvas,paste,x,y,66 if effect.key=="cannonflash" else 37,(255,148,48),power)
            stamp(canvas,paste,"flash",x,y,width*(.7+.55*phase),effect.angle,power,.73)
        if effect.key!="flash" and .36<phase:
            p=(phase-.36)/.64
            stamp(canvas,paste,"smoke",x-9+p*9,y-6-p*11,width*(.31+p*.24),
                  .38,.68*sin(pi*p),.61)
        return True
    phase = age / recipe["seconds"]
    if not 0 <= phase < 1:
        return False
    if layers.impact(canvas,paste,effect,age,visual_round,GROUND):
        return True
    size = recipe["size"] * effect.scale
    if visual_round >= 5 and effect.key == "frost" and phase < .58:
        spill(canvas,paste,effect.x,effect.y+11,79,(92,202,255),.74*(1-phase/.58))
        ice=RECIPES["shot-ice"]["tile"]
        for sign in (-1,1):
            glyph(canvas,ice,effect.x+sign*(18+phase*53),effect.y+10-phase*39,
                  size*.25,angle=sign*(.75+phase),alpha=.74*(1-phase/.58))
    seed = Random(effect.seed)
    for _ in range(recipe["count"]):
        angle = seed.random() * 2*pi
        r = seed.uniform(.28,.68) * size * (1 - (1-phase)**2)
        grain_size = seed.uniform(.08,.18) * size * (1-.58*phase)
        glyph(canvas, recipe["grain"], effect.x + cos(angle)*r,
              effect.y + sin(angle)*r + phase**2*size*.14,
              grain_size, angle=angle, alpha=(1-phase)*.9)
    frame = min(recipe["frames"]-1, int(phase*recipe["frames"]))
    glyph(canvas, recipe["tile"]+frame, effect.x, effect.y, size,
          angle=effect.angle, alpha=min(1,(1-phase)*3.1))
    return True


def terrain_line(draw, points):
    draw.line(points, fill="#6f64b0", width=13, joint="curve")
    draw.line(points, fill="#bca3ff", width=7, joint="curve")
    draw.line(points, fill="#f4e9ff", width=3, joint="curve")


def run(visual_round):
    encoder = environ.get("CATTANKS_FFMPEG", "ffmpeg")
    output = ROOT / "VFX_Handoff"
    if visual_round:
        output=output/"Nam_Vong"
        output.mkdir(exist_ok=True)
    nonce = uuid4().hex
    video = output / f".picture-{nonce}.mp4"
    sound = output / f".sound-{nonce}.wav"
    try:
        cues=render_video(encoder,video,visual_round)
        write_battle_audio(sound,SECONDS,cues)
        final=output / (f"Vong_{visual_round:02d}.mp4" if visual_round else "Mo_Phong_Tran_Dau_V2.mp4")
        run_process([encoder,"-loglevel","error","-y","-i",str(video),"-i",str(sound),
                     "-c:v","copy","-c:a","aac","-b:a","156k","-movflags","+faststart",str(final)],check=True)
    finally:
        video.unlink(missing_ok=True)
        sound.unlink(missing_ok=True)


def render_video(encoder_path, output, visual_round):
    command = [encoder_path, "-loglevel", "error", "-y", "-f", "rawvideo",
               "-pixel_format", "rgb24", "-video_size", f"{WIDTH}x{HEIGHT}", "-framerate", str(FPS),
               "-i", "-", "-an", "-c:v", "libx264", "-crf", "17", "-pix_fmt", "yuv420p",
               "-movflags", "+faststart", str(output)]
    bg = landscape(WIDTH,HEIGHT)
    enemies = [Enemy("bug", 700, 1), Enemy("wolf", 825, -1), Enemy("bug", 913, 7)]
    mounts = {"turret-3": (215,376), "turret-0": (209,312), "turret-2": (204,249)}
    start_times = {"turret-3": .28, "turret-0": 2.58, "turret-2": 5.4}
    intervals = {"turret-3": [(0, 2.4), (8, 11.15)], "turret-0": [(2.4, 5.25), (7.9, 11.2)],
                 "turret-2": [(5.25, 7.8), (8.44, 11.2)]}
    next_fire = start_times.copy()
    last_fire = {name: -3 for name in mounts}
    rotation = {name: 0 for name in mounts}
    shots, effects, smoke, zaps = [], [], [], []
    cues = []
    shot_serial=0
    hits = {key: 0 for key in mounts}
    blast_at = -10
    rocket_blast_at = -10
    with Popen(command, stdin=PIPE) as encoder:
        for n in range(FPS * SECONDS):
            now = n / FPS
            for i, enemy in enumerate(enemies):
                if now > enemy.freeze:
                    enemy.x = max(572 + i*91, enemy.x - (19+i*7)/FPS)
                if enemy.health <= 0:
                    enemy.x += 65
                    enemy.flash, enemy.health = -1, 1

            for key, weapon_pos in mounts.items():
                weapon = WEAPONS[key]
                kind = weapon["kind"]
                i = 1 if kind == "rocket" and now < 8 else 0
                target = enemies[i]
                tx, ty = target.aim()
                x,y=weapon_pos
                angle = atan2(ty-y,tx-x)
                rotation[key] = angle
                if now < next_fire[key] or not any(start <= now < end for start, end in intervals[key]):
                    continue
                next_fire[key] = max(next_fire[key]+weapon["rate"], now+weapon["rate"]-.016)
                last_fire[key] = now
                cues.append((now,kind,.25))
                for j in range(weapon.get("count", 1)):
                    shot_serial+=1
                    spread = (j-1)*.29 if kind == "rocket" else RNG.uniform(-.017,.017)
                    shots.append(Shot(kind, x+cos(angle)*38, y+sin(angle)*38, angle+spread,
                                      weapon["speed"] * WIDTH / 1600, target, now, shot_serial,
                                      layers.make_trace(kind) if visual_round>=2 else deque(), j-1 if kind=="rocket" else 0))
                effects.append(Effect(ROUTES[kind]["muzzle"], x+cos(angle)*57, y+sin(angle)*57, now, angle, .67))

            if n in (int(FPS*8.6), int(FPS*9.9), int(FPS*10.5)):
                target = enemies[0 if n == int(FPS*8.6) else 1]
                x,y=target.aim()
                if n == int(FPS*9.9):
                    cues.append((now,"frost",.72))
                    for i, enemy in enumerate(enemies):
                        ox,oy=enemy.aim()
                        enemy.freeze=now+.69
                        effects.append(Effect("frost",ox,oy-4,now+i*.045,scale=.93))
                else:
                    cues.append((now,"arc",x/WIDTH))
                    zaps.append((now,(281,310),(x,y)))
                    target.flash=now
                    effects.append(Effect("arc",x,y,now,scale=.95))

            active = []
            for shot in shots:
                tx,ty=shot.target.aim()
                if shot.kind == "rocket":
                    desired=atan2(ty-shot.y,tx-shot.x)
                    delta=(desired-shot.angle+pi)%(2*pi)-pi
                    shot.angle += max(-4.8/FPS,min(4.8/FPS,delta))
                    if now-shot.last_smoke >= .065:
                        px,py=layers.display_position(shot,now,visual_round)
                        smoke.append((now,px,py,shot.angle))
                        shot.last_smoke=now
                dx,dy=cos(shot.angle)*shot.speed/FPS,sin(shot.angle)*shot.speed/FPS
                ratio=max(0,min(1,((tx-shot.x)*dx+(ty-shot.y)*dy)/(dx*dx+dy*dy)))
                hit_x,hit_y=shot.x+ratio*dx,shot.y+ratio*dy
                if hypot(tx-hit_x,ty-hit_y)<(26 if shot.kind=="rocket" else 22):
                    shot.target.flash=now
                    shot.target.health-=.035 if shot.kind=="rapid" else .132
                    scale=.67
                    if visual_round>=5 and shot.kind=="rocket":
                        if now-rocket_blast_at<.17:
                            scale=.53
                        else:
                            scale=.77
                            rocket_blast_at=now
                    effects.append(Effect(ROUTES[shot.kind]["impact"],hit_x,hit_y,now,shot.angle,scale))
                    cues.append((now,ROUTES[shot.kind]["impact"],hit_x/WIDTH))
                    weapon_id = {"rapid":"turret-3","shell":"turret-0","rocket":"turret-2"}[shot.kind]
                    hits[weapon_id]+=1
                    if shot.kind!="rapid":
                        blast_at=now
                    continue
                if visual_round>=2:
                    shot.trace.append(layers.display_position(shot,now,visual_round))
                shot.x+=dx
                shot.y+=dy
                if now-shot.born<3 and shot.x<WIDTH+20:
                    active.append(shot)
            shots=active

            frame=bg.copy()
            draw=ImageDraw.Draw(frame)
            draw.ellipse((58,425,344,450), fill="#18243d")
            paste(frame,art("fortress-chassis",286),197,409)
            for index in range(3):
                paste(frame,art("fortress-floor",227),190,373-index*63)
            paste(frame,art("fortress-roof",209),186,202)
            paste(frame,art("cat-0",62),155,337)
            for key, (x,y) in mounts.items():
                age=now-last_fire[key]
                recoil=max(0,1-age/.16)**2*(16 if key=="turret-0" else 7)
                paste(frame,art(key,91,True),x-recoil,y,rotation[key])

            for enemy in enemies:
                x,y=enemy.aim()
                draw.ellipse((x-43,GROUND-4,x+43,GROUND+10),fill="#101d2b")
                if enemy.name=="wolf":
                    sprite=art(f"wolf-frames/{'hit' if now-enemy.flash<.10 else 'run'}-{int(now*11)%6}",140,True)
                    offset=-8
                else:
                    sprite=art("bug-0",111,True)
                    offset=-10-abs(sin(now*9+x)) * 3
                tinted = now-enemy.flash < .075 or now < enemy.freeze
                if tinted:
                    rgb="#fff0a6" if now-enemy.flash<.075 else "#8ddfff"
                    overlay=Image.new("RGBA",sprite.size,rgb)
                    overlay.putalpha(sprite.getchannel("A"))
                    sprite=Image.blend(sprite,overlay,.44 if rgb=="#fff0a6" else .28)
                paste(frame,sprite,x,y+offset)

            smoke=[puff for puff in smoke if now-puff[0]<.37]
            for birth,x,y,angle in smoke:
                p=(now-birth)/.37
                stamp(frame,paste,"smoke",x,y-p*13,13+p*27,angle+.36,.78*(1-p),.49)
            layers.ribbons(frame,shots,now,visual_round)
            for shot in shots:
                if visual_round:
                    layers.projectile(frame,paste,shot,now,visual_round)
                    continue
                key=ROUTES[shot.kind]["flight"]
                recipe=RECIPES[key]
                rocket=shot.kind=="rocket"
                size=recipe["size"]*(.76 if rocket else .69)
                if rocket:
                    width=35+4*sin(now*169+shot.angle*4)
                    stamp(frame,paste,"jet",shot.x-cos(shot.angle)*23,shot.y-sin(shot.angle)*23,width,shot.angle,.96,.69)
                else:
                    glyph(frame,RECIPES["shot-fast"]["tile"],shot.x-cos(shot.angle)*size*.5,shot.y-sin(shot.angle)*size*.5,
                          size*1.68,size*.66,shot.angle,.94)
                glyph(frame,recipe["tile"],shot.x,shot.y,size,angle=shot.angle)
            effects=[effect for effect in effects if effect.start>now or show_effect(frame,effect,now,visual_round)]

            zaps=[zap for zap in zaps if now-zap[0]<.16]
            draw=ImageDraw.Draw(frame)
            for birth,(x,y),(tx,ty) in zaps:
                seed=Random(n//2)
                points=[(x,y)]+[(x+(tx-x)*i/9,y+(ty-y)*i/9+seed.randrange(-21,22)) for i in range(1,9)]+[(tx,ty)]
                terrain_line(draw,points)
                if visual_round>=5:
                    layers.lightning_branches(frame,paste,points,now-birth)

            frame=focus(frame,now,now-blast_at)
            draw=ImageDraw.Draw(frame)
            title = "Súng máy Gatling" if now<2.4 else "Đại bác" if now<5.25 else "Chùm tên lửa" if now<7.8 else "Bắn đồng thời · Sét và băng"
            panel_end = 56 + max(202, round(draw.textlength(title, font=TITLE)))
            draw.rounded_rectangle((24,24,panel_end,95),radius=10,fill="#101832",outline="#334669")
            draw.text((40,33),"CATTANKS",font=SMALL,fill="#9abce5")
            draw.text((40,52),title,font=TITLE,fill="#fff0cc")
            encoder.stdin.write(frame.convert("RGB").tobytes())
        encoder.stdin.close()
        if encoder.wait():
            raise RuntimeError("FFmpeg failed")
    if not all(hits.values()):
        raise RuntimeError(f"Missing weapon impacts: {hits}")
    print(f"{FPS * SECONDS} frames. Confirmed impacts: {hits}")
    return cues


if __name__ == "__main__":
    parser=ArgumentParser(description=__doc__)
    parser.add_argument("--round",type=int,choices=range(6),default=0)
    run(parser.parse_args().round)
