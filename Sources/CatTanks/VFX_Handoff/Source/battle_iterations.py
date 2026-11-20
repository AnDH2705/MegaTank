"""Progressive visual passes applied to the same CatTanks preview timeline."""
from collections import deque
from functools import lru_cache
import json
from math import cos, hypot, pi, sin
from pathlib import Path
from random import Random

from PIL import Image, ImageDraw

from battle_polish import smooth, spill, stamp


KIT = Path(__file__).resolve().parents[2] / "Assets/CatTanksVfxKit/Resources/CatTanksVfxKit"
SIZE = {
    "rapid": (42, 45, 45, 44, 48),
    "shell": (56, 64, 67, 67, 74),
    "rocket": (57, 64, 68, 69, 72),
}
HUES = {
    "rapid": (255, 189, 80), "shell": (255, 116, 53), "rocket": (255, 126, 95),
}


@lru_cache(maxsize=1)
def sheet():
    data = json.loads((KIT / "CombatPoses.json").read_text(encoding="utf-8"))
    image = Image.open(KIT / "CombatPoses.png").convert("RGBA")
    if image.size != (data["width"],data["height"]):
        raise ValueError("CombatPoses atlas size changed")
    return {
        p["key"]: image.crop((p["x"],p["y"],p["x"]+p["w"],p["y"]+p["h"]))
        for p in data["poses"]
    }


@lru_cache(maxsize=180)
def pose(key, width, stretch):
    image = sheet()[key]
    height = round(width*image.height/image.width*stretch/100)
    return image.resize((max(2,width),max(2,height)),Image.Resampling.LANCZOS)


def image_pose(frame, put, key, x, y, width, angle=0, alpha=1, stretch=1):
    if alpha>.02 and width>2:
        put(frame,pose(key,round(width),round(stretch*100)),x,y,angle,alpha)


def projectile(frame, put, shot, now, visual_round):
    width = SIZE[shot.kind][visual_round-1]
    age = now - shot.born
    if visual_round >= 2:
        width *= .74 + .26*smooth(age/.087)
    if visual_round >= 5:
        width *= 1 + .065*sin(age*51 + shot.serial*.94)
    center_x, center_y = display_position(shot, now, visual_round)
    if visual_round >= 4 and shot.kind != "rapid":
        spill(frame, put, center_x, center_y, 49 if shot.kind=="shell" else 41, HUES[shot.kind], .53)
    if visual_round >= 5 and shot.kind!="rapid":
        spill(frame,put,center_x+.23*width*cos(shot.angle),center_y+.23*width*sin(shot.angle),
              29,(255,210,110),.35)
    offset = width*(.12 if shot.kind=="rocket" else .24)
    x = center_x-cos(shot.angle)*offset
    y = center_y-sin(shot.angle)*offset
    image_pose(frame,put,shot.kind,x,y,width,shot.angle)
    if visual_round >= 5 and shot.kind=="rocket":
        stamp(frame,put,"jet",center_x-cos(shot.angle)*41,center_y-sin(shot.angle)*41,
              29+4*sin(age*122+shot.serial),shot.angle,.78,.77)


def display_position(shot, now, visual_round):
    if visual_round < 4 or shot.kind != "rocket":
        return shot.x,shot.y
    tx,ty=shot.target.aim()
    remaining=hypot(tx-shot.x,ty-shot.y)
    envelope=smooth((now-shot.born)/.22)*smooth((remaining-28)/92)
    separation=shot.lane*(24 if visual_round==4 else 33)*envelope
    return shot.x-sin(shot.angle)*separation,shot.y+cos(shot.angle)*separation


def ribbons(frame, shots, now, visual_round):
    if visual_round < 2:
        return
    overlay = Image.new("RGBA",frame.size)
    draw = ImageDraw.Draw(overlay)
    for shot in shots:
        if not shot.trace:
            continue
        pts = (*shot.trace,display_position(shot,now,visual_round))
        color = HUES[shot.kind]
        base = {"rapid":5,"shell":10,"rocket":8}[shot.kind]
        total = len(pts)
        for i,(a,b) in enumerate(zip(pts,pts[1:]),1):
            p = i/total
            draw.line((a,b), fill=(*color,round(132*p**1.4)),width=max(1,round(base*p)))
            if p>.36:
                draw.line((a,b),fill=(255,236,163,round(215*p**1.8)),width=max(1,round(base*.30*p)))
        if visual_round >= 4:
            seed = Random(shot.serial)
            for i in range(2 if shot.kind=="rapid" else 5):
                j = seed.randrange(total)
                xx,yy=pts[j]
                pulse=.38+.62*(.5+.5*sin(now*(20+i*7)+i+shot.serial))
                slant=sin(now*18+i)*3
                draw.line((xx+slant, yy+i-2, xx-4+slant, yy+i-1),
                          fill=(*color,round(160*pulse*j/total)),width=1)
    frame.alpha_composite(overlay)


def impact(frame,put,effect,age,visual_round,ground):
    key,x,y = effect.key,effect.x,effect.y
    rapid=key=="hit"
    if visual_round < 3 or key not in ("hit","blast","rocketblast"):
        return False
    duration=.17 if rapid else .42 if key=="blast" else .34
    cap = (63+(visual_round-3)*4 if rapid else 137+(visual_round-3)*15 if key=="blast" else 112+(visual_round-3)*13)
    cap *= effect.scale/.67
    if age < duration:
        p=max(0,age/duration)
        growth=.31+.69*smooth(p*3.35)
        # Hit point sits in the visible hot part of the drawing.
        lift=0 if rapid else cap*.10*(1+.4*p)
        angle=effect.angle*(.88 if rapid else .34)
        if visual_round >= 5:
            angle+=sin(effect.seed*.95)*.14*(1-p)
            growth*=.95+.10*(.5+.5*sin(effect.seed*2.1))
        image_pose(frame,put,key,x+cap*.035*p,y-lift,cap*growth,angle,min(1,(1-p)*2.5),.8+.20*p)
    if visual_round >= 4 and not rapid and .04 < age < .29:
        p=(age-.04)/.25
        layer=Image.new("RGBA",(294,58))
        draw=ImageDraw.Draw(layer)
        stretch=round(cap*min(.94,.18+p))
        for side in (-1,1):
            x0=147+side*stretch*.40
            x1=147+side*stretch
            draw.line((x0,31,x1,31+side*3),fill=(255,156,101,round(144*(1-p))),width=2)
        put(frame,layer,x,min(ground+1,y+19))
    if visual_round >= 5 and not rapid and .12 < age < .48:
        p=(age-.12)/.36
        seed=Random(effect.seed+15)
        for i in range(4):
            side=-1 if i%2 else 1
            radius=cap*(.45+.41*seed.random())
            angle=seed.uniform(.08,.45)*side
            image_pose(frame,put,"hit",x+side*radius*smooth(p),y-p*(13+i*5)+18*p*p,
                       15+(1-p)*22,angle,(1-p)*.81,.63)
    return True


def make_trace(kind):
    count={"rapid":5,"shell":8,"rocket":11}[kind]
    return deque(maxlen=count)


def lightning_branches(frame, put, points, age):
    layer=Image.new("RGBA",frame.size)
    draw=ImageDraw.Draw(layer)
    fall=max(0,1-age/.17)
    for i in (2,4,6,8):
        x,y=points[i]
        side=-1 if i%4 else 1
        branch=((x,y),(x+18,y+side*17),(x+25,y+side*13),(x+36,y+side*30))
        draw.line(branch,fill=(108,114,255,round(200*fall)),width=4,joint="curve")
        draw.line(branch,fill=(209,222,255,round(215*fall)),width=1,joint="curve")
    frame.alpha_composite(layer)
    x,y=points[-1]
    spill(frame,put,x,y,42,(146,143,255),.85*fall)
