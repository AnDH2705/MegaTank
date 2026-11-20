"""Paint the close-up VFX preview with independent impact and smoke layers."""
from functools import lru_cache
from math import cos, pi, sin
from pathlib import Path
from random import Random

from PIL import Image, ImageDraw


ASSETS = Path(__file__).resolve().parents[1] / "Polish"
POSES = {
    "flash": (0, 0), "ball": (1, 0), "smoke": (2, 0),
    "rocket": (0, 1), "dust": (1, 1), "jet": (2, 1),
}


def landscape(width, height):
    source = Image.open(ASSETS / "Mat_Trang.png").convert("RGBA")
    return source.resize((width, height), Image.Resampling.LANCZOS)


@lru_cache(maxsize=1)
def plate():
    return Image.open(ASSETS / "Vo_No_Poses.png").convert("RGBA")


@lru_cache(maxsize=6)
def pose(key):
    sheet = plate()
    col, row = POSES[key]
    x = (0, 418, 836, sheet.width) if row == 0 else (0, 415, 836, sheet.width)
    y = (115, 627, 1080)
    art = sheet.crop((x[col], y[row], x[col+1], y[row+1]))
    solid = art.getchannel("A").point(lambda value: 255 if value > 40 else 0)
    left, top, right, bottom = solid.getbbox()
    return art.crop((max(0,left-3),max(0,top-3),min(art.width,right+3),min(art.height,bottom+3)))


@lru_cache(maxsize=160)
def resized(key, width, stretch):
    asset = pose(key)
    height = round(asset.height / asset.width * width * stretch / 100)
    return asset.resize((max(2,width),max(2,height)), Image.Resampling.BILINEAR)


def stamp(canvas, paste, key, x, y, width, angle=0, alpha=1, stretch=1):
    if alpha > .015 and width > 2:
        paste(canvas,resized(key,round(width),round(100*stretch)),x,y,angle,min(1,alpha))


@lru_cache(maxsize=16)
def radial(radius, color):
    layer = Image.new("RGBA", (radius*2+1,radius*2+1))
    draw = ImageDraw.Draw(layer)
    for size in range(radius, 0, -max(1,radius//16)):
        weight=(1-size/radius)**2
        draw.ellipse((radius-size,radius-size,radius+size,radius+size),
                     fill=(*color,round(165*weight)))
    return layer


def spill(canvas, paste, x, y, radius, color, opacity):
    if opacity > .025:
        paste(canvas,radial(radius,color),x,y,alpha=opacity)


def mix(a,b,t):
    return a+(b-a)*t


def smooth(p):
    p=max(0,min(1,p))
    return p*p*(3-2*p)


def heavy(canvas, paste, key, x, y, age, seed, strength, core=True):
    """A short hot core, growing fire silhouette, dust sheet, and delayed smoke."""
    rocket=key=="rocketblast"
    width=(126 if rocket else 172)*strength
    if age < .20:
        spill(canvas,paste,x,y,94 if rocket else 129,
              (255,101,68) if rocket else (255,153,49),.98*(1-age/.20)**2)

    # Expanding elliptical wave reads as force along the ground plane.
    if age < .27:
        p=age/.27
        radius=round(width*mix(.12,.77,smooth(p)))
        layer=Image.new("RGBA",(radius*2+9,round(radius*.75)+12))
        line=ImageDraw.Draw(layer)
        line.ellipse((4,4,layer.width-5,layer.height-5),
                     outline=(255,164 if rocket else 195,114,round(178*(1-p))),width=max(1,round(4*(1-p))))
        paste(canvas,layer,x,y+13,angle=.10 if rocket else -.06)

    if .05 < age < .60:
        p=smooth((age-.05)/.55)
        stamp(canvas,paste,"dust",x-9,y+19,width*mix(.52,1.49,p),
              .06 if rocket else -.015,.72*(1-p),.63)

    for index in range(2):
        start=.16+index*.085
        life=.65+index*.14
        if start < age < start+life:
            p=(age-start)/life
            stamp(canvas,paste,"smoke",x+(-31 if index==0 else 34)*strength*p,
                  y-11-width*(.12+.44*p),width*(.38+.43*smooth(p)),
                  p*(.34 if index==0 else -.29),.87*sin(pi*p)**.75,.82)

    if core and age < (.29 if rocket else .41):
        duration=.29 if rocket else .41
        p=age/duration
        kick=mix(.28,1.0,smooth(p*3.4))
        stamp(canvas,paste,"rocket" if rocket else "ball",x+width*.10*p,y-width*.045*p,
              width*kick,.16*p if rocket else -.11*p,min(1,(1-p)*2.65),mix(.77,1,p))
    if age < .115:
        p=age/.115
        stamp(canvas,paste,"flash",x,y,width*(.34+p*.48),-.32 if rocket else .18,(1-p)**1.15,.43)

    if age < .38:
        p=age/.38
        rng=Random(seed)
        sparks=Image.new("RGBA",canvas.size)
        draw=ImageDraw.Draw(sparks)
        for i in range(11 if rocket else 15):
            angle=rng.uniform(-pi,pi)
            reach=rng.uniform(.29,1.10)*width
            dx,dy=cos(angle),sin(angle)
            r=reach*(1-(1-p)**2)
            xx,yy=x+dx*r,y+dy*r+width*.28*p*p
            length=rng.uniform(6,23)*(1-p)**.75
            draw.line((xx,yy,xx-dx*length,yy-dy*length),
                      fill=(255,122+int(108*(i%3)/2),70,round(245*(1-p)**.75)),
                      width=1+i%3)
        canvas.alpha_composite(sparks)


def focus(canvas, time, shot_age):
    """Ease between a wide view and a small push toward the active side."""
    wide=1.04+.025*sin(time*.65)
    shell=smooth((time-2.12)/.45)*(1-smooth((time-5.05)/.42))
    rockets=smooth((time-5.18)/.46)*(1-smooth((time-7.72)/.45))
    zoom=wide+.075*shell+.035*rockets
    width,height=canvas.size
    shake=max(0,1-shot_age/.17) if shot_age>=0 else 0
    cx=width*(.48+.025*shell+.025*rockets)
    cy=height*(.51+.022*shell+.015*rockets)
    w,h=width/zoom,height/zoom
    x=min(width-w,width*.043,max(0,cx-w/2+shake*6*sin(shot_age*91)))
    y=min(height-h,max(0,cy-h/2+shake*3*sin(shot_age*126)))
    return canvas.transform((width,height),Image.Transform.EXTENT,(x,y,x+w,y+h),Image.Resampling.BICUBIC)
