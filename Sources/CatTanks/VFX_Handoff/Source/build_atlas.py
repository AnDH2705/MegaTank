"""Render the original CatTanks VFX vector shapes to the Unity flipbook atlas.

Run with Python 3.10+ and Pillow. All drawing coordinates use a 128 px tile.
"""
import json
from math import cos, pi, sin
from pathlib import Path
from random import Random

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets/CatTanksVfxKit/Resources/CatTanksVfxKit"
DATA = json.loads((ASSETS / "effects.json").read_text(encoding="utf-8"))
CELL = DATA["cell"]
S = 3

AMBER = ("#63344c", "#e9592c", "#ffad38", "#fff1ad")
ROSE = ("#4f2046", "#aa365e", "#fa6684", "#ffd4c1")
ICE = ("#193c75", "#288cd3", "#55ddf3", "#d5fff9")
MINT = ("#1c5f61", "#29ab81", "#73e6a6", "#e7ffcd")
LIME = ("#39462b", "#779b32", "#bfdb43", "#ffffa0")
VIOLET = ("#302555", "#7345b8", "#bb82fc", "#f1e0ff")
DUST = ("#3c364a", "#746075", "#aa8390", "#e8b9ac")
METAL = ("#172842", "#355c78", "#76bbc4", "#e7f7ef")


class Tile:
    def __init__(self):
        self.image = Image.new("RGBA", (CELL * S, CELL * S))
        self.pen = ImageDraw.Draw(self.image)

    def poly(self, pts, color):
        self.pen.polygon([(round(x * S), round(y * S)) for x, y in pts], fill=color)

    def line(self, pts, color, width):
        if width > 0:
            self.pen.line([(round(x * S), round(y * S)) for x, y in pts],
                          fill=color, width=max(1, round(width * S)), joint="curve")

    def oval(self, x, y, rx, ry, color):
        self.pen.ellipse(tuple(round(v * S) for v in (x-rx, y-ry, x+rx, y+ry)), fill=color)

    def arc(self, x, y, rx, ry, begin, end, color, width):
        self.pen.arc(tuple(round(v * S) for v in (x-rx, y-ry, x+rx, y+ry)),
                     begin, end, fill=color, width=max(1, round(width * S)))

    def finish(self, alpha=1):
        image = self.image.resize((CELL, CELL), Image.Resampling.LANCZOS)
        if alpha < 1:
            image.putalpha(image.getchannel("A").point(lambda value: round(value * alpha)))
        return image


def star(tile, x, y, radius, color, inner=.36, points=6, phase=0):
    vertices = []
    for i in range(points * 2):
        angle = phase + i * pi / points
        r = radius * (1 if i % 2 == 0 else inner)
        vertices.append((x + cos(angle) * r, y + sin(angle) * r))
    tile.poly(vertices, color)


def glyph(tile, x, y, size, color):
    w = size / 3
    tile.poly([(x-w,y-size),(x+w,y-size),(x+w,y-w),(x+size,y-w),
               (x+size,y+w),(x+w,y+w),(x+w,y+size),(x-w,y+size),
               (x-w,y+w),(x-size,y+w),(x-size,y-w),(x-w,y-w)], color)


def plume(tile, x, y, r, p, rng, hot=True):
    lobes = [(x,y,r*.89)]
    for i in range(8):
        a = i * pi / 4 + rng.uniform(-.20, .20)
        lobes.append((x + cos(a)*r*.54, y + sin(a)*r*.53, r*rng.uniform(.36,.64)))
    for cx,cy,k in lobes:
        tile.oval(cx,cy,k,k*.86,p[0])
    for cx,cy,k in lobes:
        tile.oval(cx+1,cy-k*.14,k*.88,k*.74,p[1])
    for i,(cx,cy,k) in enumerate(lobes):
        if i == 0 or cy < y + r*.2:
            tile.oval(cx-k*.14,cy-k*.20,k*.64,k*.50,p[2])
    if hot:
        tile.oval(x + r*.08, y - r*.13, r*.49, r*.44, p[3])
        tile.oval(x + r*.37, y - r*.25, r*.34, r*.29, p[3])


def shard(tile,x,y,n,angle,p):
    def xy(a,b):
        return (x+a*cos(angle)-b*sin(angle),y+a*sin(angle)+b*cos(angle))
    tile.poly([xy(-n*.45,-n*.26),xy(n,0),xy(-n*.38,n*.32),xy(-n*.63,0)],p[1])
    tile.poly([xy(-n*.36,-n*.20),xy(n,0),xy(-n*.38,n*.20)],p[2])
    tile.poly([xy(n*.66,0),xy(-n*.34,-n*.14),xy(-n*.24,.03*n)],p[3])


def facet(tile,x,y,n,angle,p):
    def xy(a,b):
        return (x+a*cos(angle)-b*sin(angle),y+a*sin(angle)+b*cos(angle))
    tile.poly([xy(-n*.6,-n*.26),xy(n,0),xy(-n*.40,n*.34),xy(-n*.68,n*.04)],p[0])
    tile.poly([xy(-n*.5,-n*.22),xy(n,0),xy(-n*.46,n*.12)],p[2])
    tile.poly([xy(-n*.46,n*.12),xy(n,0),xy(-n*.40,n*.29)],p[1])
    tile.poly([xy(-n*.14,-n*.13),xy(n*.78,0),xy(-n*.32,.045*n)],p[3])


def blade(tile, x, y, r, angle, color, thickness=.25, sweep=1.8):
    path = []
    for i in range(15):
        u = i / 14
        a = angle + u*sweep
        path.append((x + cos(a)*r, y + sin(a)*r))
    for i in range(14,-1,-1):
        u = i / 14
        a = angle + u*sweep
        inner = r * (1 - thickness * sin(pi*u)**.67)
        path.append((x + cos(a)*inner,y + sin(a)*inner))
    tile.poly(path,color)


def directional(tile, t, p, width, reach, phase=0):
    x, tail = 60, 45-t*18
    top = 64-width*(1-.67*t)
    bottom = 64+width*(1-.68*t)
    tip = min(119,x+reach*(.66+.34*t))
    tile.poly([(tail,62),(53,top+7),(64,top),(68,top+11),(tip-8,54),
               (tip,64),(tip-12,71),(68,bottom-9),(64,bottom),
               (55,bottom-6),(tail-6,71)],p[0])
    tile.poly([(tail+1,63),(60,top+2),(71,top+15),(tip,64),(70,bottom-5),(62,bottom-1)],p[2])
    tile.poly([(47,65),(64,top+8),(71,top+18),(tip-8,64),(63,bottom-6)],p[1])
    tile.poly([(52,65),(70,61),(89+phase,59),(tip-10,64),(71,70),(60,73)],p[3])
    tile.poly([(27+t*11,66),(39+t*8,61),(45+t*9,62),(38+t*13,66)],p[2])


def draw_clip(key, frame):
    tile = Tile()
    t = frame / 7
    rng = Random(100 + sum(map(ord, key)))
    fade = 1 - .78 * max(0, (t-.58)/.42)
    if key in {"flash", "cannonflash", "ignition", "fire"}:
        p = AMBER if key != "ignition" else ROSE
        width, reach = {"flash":(28,53),"cannonflash":(42,57),"ignition":(29,57),"fire":(33,58)}[key]
        if key == "cannonflash" and t > .42:
            plume(tile,48+t*12,64-t*11,21+10*t,DUST,rng,False)
        else:
            directional(tile,t,p,width,reach,frame%3*2)
            if key == "fire":
                blade(tile,66,67,42,2.6+t*2,AMBER[1],.23,2.3)
        return tile.finish(fade)

    if key in {"blast", "rocketblast", "death", "dust"}:
        p = ROSE if key == "rocketblast" else AMBER
        if key in {"blast","rocketblast"} and frame <= 1:
            star(tile,64,64,47+frame*6,p[1],.48,9,frame*.20)
            star(tile,64,64,36+frame*4,p[2],.54,8,frame*.14)
            star(tile,64,64,27+frame*4,p[3],.64,7)
        else:
            if key == "dust":
                for i in range(3):
                    plume(tile,40+i*24,83-i*4-t*13,11+t*8,DUST,rng,False)
            else:
                x = 62+(t*9 if key=="rocketblast" else 0)
                y = 70 - t*20
                r = (28 + t*13) if key == "death" else (34 + t*12)
                plume(tile,x,y,r,DUST if t > .67 or key == "death" else p,rng,
                      key!="death" and t<.77)
                if key == "death" and frame < 5:
                    for offset in [-8,8]:
                        tile.line([(x+offset-2,y-3),(x+offset+2,y+2)],DUST[0],2.3)
                        tile.line([(x+offset+2,y-3),(x+offset-2,y+2)],DUST[0],2.3)
        return tile.finish(fade)

    if key in {"hit","frost","arc","crystal","pulse","poison","vortex"}:
        p = {"hit":AMBER,"frost":ICE,"arc":VIOLET,"crystal":MINT,"pulse":ICE,"poison":LIME,"vortex":VIOLET}[key]
        if key in {"frost","crystal"}:
            for i,a in enumerate([-2.65,-2.20,-1.78,-1.36,-.78,-.32]):
                lift = max(0,t-.48)*24
                facet(tile,64+cos(a)*lift,76+sin(a)*lift,(28+(i%3)*10)*(min(1,.60+t*1.9)),a+t*.16,p)
            tile.line([(17,90),(49,84),(42,80),(78,86),(71,91),(107,94)],p[1],3)
            if frame < 3:
                star(tile,63,75,25+t*24,p[3],.32,5,.46)
        elif key == "vortex":
            r = 48-t*23
            tile.oval(64,64,r*.48,r*.48,p[0])
            for i in range(3):
                a = i*2*pi/3+t*5
                blade(tile,64,64,r,a,p[1],.42,2.36)
                blade(tile,64,64,r*.83,a+.23,p[2],.29,1.92)
            if frame >= 5:
                star(tile,64,64,8+t*6,p[3],.44,4,.10)
        elif key == "poison":
            for i in range(5):
                a = i*pi*2/5+.7
                distance = 10 + t*36
                r = 10 + rng.uniform(0,6) - 4*t
                x,y = 64+cos(a)*distance,66+sin(a)*distance*.7-t*10
                tile.oval(x,y,r,r,p[0])
                tile.oval(x-1,y-1,r*.85,r*.84,p[1])
                tile.oval(x-2,y-3,r*.70,r*.64,p[2])
                tile.oval(x-r*.29,y-r*.35,r*.24,r*.18,p[3])
            tile.arc(64,90,18+26*t,7+3*t,0,168,p[2],2.2)
        elif key == "arc":
            for i in range(4):
                a = i*pi/2 + frame*.21
                r = 30+17*t
                turn = a+(-1 if i%2 else 1)*.35
                path = [(64,64),(64+cos(a)*r*.34,64+sin(a)*r*.34),
                        (64+cos(turn)*r*.51,64+sin(turn)*r*.51),(64+cos(a)*r,64+sin(a)*r)]
                tile.line(path,p[0],10-6*t)
                tile.line(path,p[2],6-3*t)
                tile.line(path,p[3],2.5-1.4*t)
            star(tile,64,64,20*(1-t)+8,p[3],.34,4,.5)
        else:
            r = 21+28*t
            if key == "pulse" or frame > 2:
                tile.arc(64,64,r,r*.81,8+frame*12,218+frame*12,p[2],5-3*t)
                tile.arc(64,64,r*.89,r*.72,197+frame*12,347+frame*12,p[1],4-2*t)
            star(tile,64,64,max(7,31*(1-t)),p[1],.41,7,t*1.5)
            star(tile,64,64,max(5,23*(1-t)),p[3],.34,6,t*.9)
        return tile.finish(fade)

    if key == "slash":
        angle = 2.86+t*1.48
        for i in range(3):
            r = 54-i*10
            blade(tile,62,67,r,angle+i*.14,ROSE[0],.37,2.68)
            blade(tile,62,67,r*.97,angle+i*.13+.08,ROSE[2],.25,2.52)
            blade(tile,62,67,r*.96,angle+i*.12+.12,ROSE[3],.09,2.25)
        return tile.finish(fade)

    if key == "web":
        r = 23+t*27
        spokes = [(cos(i*pi/4+.2),sin(i*pi/4+.2)) for i in range(8)]
        for x,y in spokes:
            tile.line([(64,64),(64+x*r,64+y*r)],VIOLET[3],2.3)
        for k in [.38,.67,.90]:
            path = [(64+x*r*k,64+y*r*k) for x,y in spokes]
            tile.line(path+[path[0]],VIOLET[2],2)
        return tile.finish(fade*.88)

    if key in {"heal","ward","rally","cage","portal","drop","spawn","warning"}:
        p = {"heal":MINT,"ward":ICE,"rally":AMBER,"cage":AMBER,"portal":VIOLET,
             "drop":AMBER,"spawn":VIOLET,"warning":ROSE}[key]
        r = 28 + t*24
        if key != "warning":
            tile.arc(64,91-t*5,r,r*.24,0,360,p[0],6)
            tile.arc(64,91-t*5,r,r*.24,8+t*36,332+t*37,p[2],3.1)
            tile.arc(64,91-t*5,r*.82,r*.19,70,150,p[3],2.2)
        if key == "heal":
            for i in range(3):
                x = 43+i*20
                y = 79 - t*53 - (i==1)*11
                glyph(tile,x,y,8+(i==1)*5,p[1])
                glyph(tile,x,y-1,6+(i==1)*4,p[3])
        elif key == "ward":
            tile.arc(64,92,r,r+.2*r,181,358,p[0],10)
            tile.arc(64,92,r,r+.2*r,181,358,p[2],5)
            tile.arc(64,92,r-1,r+.2*r-1,209,292,p[3],2.6)
            x = r*.40
            tile.line([(64-x,92-r*.79),(64,90-r*.60),(64+x,92-r*.79)],p[2],2.2)
            tile.line([(64,90-r*.60),(64,89-r*.26)],p[1],2)
        elif key == "rally":
            for i in range(3):
                x = 43+i*21
                y = 72-t*37+(i!=1)*13
                tile.poly([(x-10,y+6),(x,y-6),(x+10,y+6),(x+10,y+12),(x,y+4),(x-10,y+12)],p[0])
                tile.poly([(x-9,y+4),(x,y-6),(x+9,y+4),(x+9,y+8),(x,y+1),(x-9,y+8)],p[2])
                tile.line([(x-7,y+3),(x,y-4),(x+7,y+3)],p[3],2)
        elif key == "cage":
            peak = 57 - t*32
            apex = (64,peak)
            for i in range(5):
                x = 29+i*17.5
                path = [(x,88+9*sin(i*pi/4)),(64+(x-64)*.81,peak+16),apex]
                tile.line(path,p[0],5.6)
                tile.line(path,p[2],2.6)
            star(tile,*apex,8+3*sin(t*pi),p[3],.34,5,-pi/2)
        elif key in {"portal","spawn"}:
            for i in range(3):
                a = i*2.1+t*4
                blade(tile,64,61,r*.76,a,p[1],.34,2)
                blade(tile,64,61,r*.63,a+.14,p[2],.23,1.85)
            for i in range(4):
                x = 43 + i*14
                y = 77 - t*40 - (i%2)*13
                tile.poly([(x,y-7),(x+4,y),(x,y+7),(x-4,y)],p[3] if i%3==0 else p[2])
        elif key == "drop":
            x,y=64,77-t*46
            rx=7+abs(cos(t*pi*2))*9
            tile.oval(x,y,rx,15,p[0])
            tile.oval(x,y-1,rx*.88,13,p[2])
            tile.oval(x,y-2,rx*.70,10,p[1])
            star(tile,x,y-1,7,p[3],.39,4,-pi/4)
        else:
            for i in range(3):
                x = 29+i*26+t*15
                tile.poly([(x-8,43),(x+7,43),(x+24,64),(x+7,85),(x-8,85),(x+10,64)],p[0])
                tile.poly([(x-5,46),(x+6,46),(x+21,64),(x+6,82),(x-5,82),(x+13,64)],p[2])
                tile.line([(x+5,47),(x+19,64),(x+5,81)],p[3],2.6)
        return tile.finish(fade if key!="warning" else max(.48,fade))

    raise ValueError(key)


def draw_static(key):
    tile = Tile()
    if key in {"spark","ember","ice","void","plasma","orb","enemyshot"}:
        p = {"spark":AMBER,"ember":AMBER,"ice":ICE,"void":VIOLET,"plasma":MINT,"orb":VIOLET,"enemyshot":ROSE}[key]
        if key in {"orb","plasma","enemyshot"}:
            tile.oval(65,64,44,35,p[0])
            blade(tile,62,64,49,1.34,p[1],.35,4.8)
            blade(tile,62,64,45,1.65,p[2],.35,4.36)
            tile.oval(69,61,26,22,p[2])
            tile.oval(76,58,17,15,p[3])
        elif key in {"ice","void"}:
            facet(tile,59,68,50,-.33,p)
        elif key == "ember":
            directional(tile,.2,p,36,55)
        else:
            star(tile,64,64,54,p[1],.19,4,.15)
            star(tile,64,64,42,p[2],.17,4,.15)
            star(tile,64,64,25,p[3],.22,4,.15)
    elif key == "smoke":
        plume(tile,64,66,41,DUST,Random(17),False)
    elif key == "bubble":
        tile.oval(64,64,46,40,LIME[0])
        tile.oval(64,62,42,36,LIME[1])
        tile.oval(63,59,38,30,LIME[2])
        tile.oval(51,51,12,8,LIME[3])
    elif key in {"plus","cross"}:
        glyph(tile,64,64,47,MINT[0])
        glyph(tile,64,63,42,MINT[2])
        glyph(tile,64,62,28,MINT[3])
    elif key in {"tracer","shell","pellet"}:
        w = {"tracer":7,"shell":17,"pellet":14}[key]
        tile.poly([(9,64),(59,64-w),(98,64-w),(119,64),(100,64+w),(59,64+w)],AMBER[0])
        tile.poly([(12,64),(63,65-w),(98,67-w),(115,64),(95,60+w),(53,60+w)],AMBER[2])
        tile.poly([(22,64),(84,61-w*.38),(106,64),(84,65+w*.34)],AMBER[3])
    elif key == "rocket":
        tile.poly([(38,38),(27,24),(41,25),(60,44),(90,43),(113,55),(119,65),
                   (112,76),(90,85),(57,85),(42,103),(26,102),(38,86),(32,74),(32,53)],METAL[0])
        tile.poly([(39,40),(29,27),(41,29),(58,44),(59,51),(48,53)],ROSE[1])
        tile.poly([(59,78),(56,86),(40,99),(30,99),(43,82)],ROSE[2])
        tile.oval(73,64,38,22,METAL[1])
        tile.oval(74,59,34,15,METAL[2])
        tile.poly([(91,43),(106,50),(116,64),(106,75),(91,82),(86,63)],ROSE[2])
        tile.poly([(87,63),(116,64),(106,75),(91,82)],ROSE[1])
        tile.line([(56,56),(70,51),(83,51)],METAL[3],3)
        tile.poly([(28,56),(39,54),(41,72),(28,73)],METAL[0])
    elif key == "arrow":
        tile.line([(17,64),(99,64)],METAL[0],9)
        tile.line([(17,62),(96,62)],AMBER[3],3)
        tile.poly([(24,63),(14,50),(35,55),(45,63),(35,73),(14,77)],MINT[2])
        facet(tile,92,64,25,0,METAL)
    elif key == "blade":
        star(tile,64,64,49,METAL[0],.79,12,.2)
        star(tile,64,64,45,METAL[2],.82,12,.2)
        tile.oval(64,64,27,27,METAL[1])
        tile.oval(64,64,10,10,AMBER[3])
    elif key == "drone":
        tile.poly([(22,56),(39,47),(89,47),(108,58),(108,70),(85,77),(41,77),(22,70)],METAL[0])
        tile.oval(66,64,33,25,METAL[1])
        tile.oval(65,59,30,18,METAL[2])
        tile.oval(65,62,15,11,ICE[0])
        tile.oval(65,62,9,6,ICE[3])
    elif key == "webshot":
        tile.oval(64,64,43,32,VIOLET[0])
        for i in range(3):
            tile.arc(65,62,17+8*i,14+6*i,0,360,VIOLET[2],3)
        tile.line([(24,64),(103,64)],VIOLET[3],3)
    elif key == "ring":
        for i in range(4):
            tile.arc(64,64,50,50,13+90*i,78+90*i,"#ffffff",4)
            a = (i*pi/2) + pi/4
            star(tile,64+cos(a)*50,64+sin(a)*50,6,"#ffffff",.42,4,0)
    elif key == "dome":
        tile.arc(64,100,54,70,181,359,(255,255,255,150),6)
        tile.arc(64,100,50,65,196,338,(255,255,255,83),3)
        tile.arc(64,100,54,70,218,292,(255,255,255,255),3)
        for path in [
            [(23,63),(39,58),(57,73),(57,99)],
            [(39,58),(46,39),(70,37),(86,55),(82,81),(57,92)],
            [(86,55),(103,63),(113,89)],
            [(70,37),(75,30)],
            [(82,81),(97,96)],
        ]:
            tile.line(path,(255,255,255,91),1.65)
    elif key == "chevron":
        tile.poly([(23,27),(52,27),(99,64),(52,101),(23,101),(67,64)],"#ffffff")
    elif key == "coin":
        tile.oval(64,64,38,45,AMBER[0])
        tile.oval(64,62,34,41,AMBER[2])
        tile.oval(64,62,25,31,AMBER[1])
        star(tile,64,60,22,AMBER[3],.38,5,-pi/2)
    elif key == "dot":
        tile.oval(64,64,49,49,"#ffffff")
    elif key == "stripe":
        tile.poly([(2,40),(126,40),(126,88),(2,88)],(255,255,255,72))
        tile.poly([(2,54),(126,54),(126,74),(2,74)],(255,255,255,255))
    else:
        raise ValueError(key)
    return tile.finish()


def main():
    cols, rows = DATA["columns"], DATA["rows"]
    atlas = Image.new("RGBA", (cols * CELL, rows * CELL))
    for index, clip in enumerate(DATA["tiles"]["clips"]):
        for frame in range(8):
            tile = index*8+frame
            atlas.paste(draw_clip(clip,frame), ((tile%cols)*CELL,(tile//cols)*CELL))
    for index, key in enumerate(DATA["tiles"]["statics"], DATA["tiles"]["staticStart"]):
        atlas.paste(draw_static(key), ((index%cols)*CELL,(index//cols)*CELL))
    atlas.save(ASSETS / "atlas.png", optimize=True)
    print(f"Built {atlas.width}x{atlas.height} transparent atlas")


if __name__ == "__main__":
    main()
