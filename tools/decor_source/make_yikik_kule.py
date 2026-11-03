import random, sys
from PIL import Image
random.seed(11)
N=128
DARK=(46,33,36);MID=(72,53,52);LIGHT=(98,73,64);HI=(124,95,78);MOSS=(64,70,52);VOID=(20,12,16)
GLOW=(255,168,66);GLOW2=(255,222,140)
L0,R0=48,80  # 32 wide shaft
PROF={}
# broken crown: tall jagged left, notch, low right
pts=[(48,26),(50,16),(52,14),(54,20),(55,24),(57,18),(59,22),(61,30),(63,36),(65,33),(67,38),(69,45),(71,42),(73,50),(75,56),(77,54),(79,60)]
for i in range(len(pts)-1):
    (x0,y0),(x1,y1)=pts[i],pts[i+1]
    for x in range(x0,x1+1): PROF[x]=round(y0+(y1-y0)*((x-x0)/max(1,x1-x0)))
PROF[80]=62
def build(glow):
    im=Image.new('RGBA',(N,N),(0,0,0,0));p=im.load()
    def put(x,y,c,a=255):
        if 0<=x<N and 0<=y<N: p[x,y]=c+(a,)
    # slight flare at the base
    def edge(y): # extra width at bottom
        return max(0,(y-92)//5)
    for y in range(12,N):
        for x in range(L0-edge(y),R0+edge(y)):
            xx=min(max(x,L0),R0-1)
            t=PROF.get(xx,60)
            if y<t: continue
            u=(x-L0)/(R0-L0)
            row=y//6;off=(row%2)*4
            mortar=(y%6==0) or ((x+off)%8==0)
            # cylinder shading bands
            if u<.22: c=HI
            elif u<.5: c=LIGHT
            elif u<.78: c=MID
            else: c=DARK
            if mortar:
                c={HI:LIGHT,LIGHT:MID,MID:DARK,DARK:DARK}[c]
            if random.random()<.03: c=MOSS
            put(x,y,c)
        # lit ragged rim
    for x in range(L0,R0):
        t=PROF.get(x,60);put(x,t,HI);put(x,t+1,LIGHT)
    # interior hollow visible at the crown: dark cut
    for x in range(58,70):
        t=PROF.get(x,60)
        for y in range(t+2,t+9): put(x,y,VOID if (x+y)%7 else DARK)
    # low ruined wall on left with arch hole
    for x in range(26,48):
        t=70+(x-26)//3+(2 if x%4==0 else 0)
        for y in range(t,N):
            row=y//6;off=(row%2)*4
            mortar=(y%6==0) or ((x+off)%8==0)
            put(x,y,(LIGHT if x>38 else MID) if not mortar else MID if x>38 else DARK)
        put(x,t,HI)
    for yy in range(100,118):
        for xx in range(32,41):
            dx=xx-36;dy=yy-104
            if yy>=104 or dx*dx+dy*dy<=20: put(xx,yy,VOID)
    # arched windows
    def window(cx,y0,w,h,glow):
        r=w/2
        for yy in range(y0,y0+h):
            for xx in range(cx-int(r),cx+int(r)+1):
                dx=xx-cx;dy=yy-(y0+r)
                if yy<y0+r and dx*dx+dy*dy>r*r+1: continue
                put(xx,yy,VOID)
        for xx in range(cx-int(r)-1,cx+int(r)+2): put(xx,y0+h,LIGHT)
        if glow:
            for yy in range(y0+int(r)+1,y0+h-1):
                for xx in range(cx-int(r)+1,cx+int(r)): put(xx,yy,GLOW)
            for yy in range(y0+int(r)+2,y0+int(r)+7): put(cx,yy,GLOW2)
    window(60,58,9,20,glow)
    window(68,88,7,15,False)
    window(55,100,6,11,False)
    # crack
    x=72
    for y in range(52,95):
        x+=random.choice([-1,0,0,0,1]); put(x,y,DARK)
    # ivy hanging
    for sx in (50,57,74):
        for k in range(random.randint(10,22)):
            put(sx+random.choice([0,0,1]),PROF.get(sx,60)+2+k,MOSS)
    # rubble
    for _ in range(70):
        x=random.randint(22,108);y=random.randint(112,127)
        c=random.choice([MID,LIGHT,DARK]);put(x,y,c);put(x+1,y,c)
    # a few stones mid-air near the crown
    for sx,sy in ((82,34),(86,46),(44,40)): put(sx,sy,LIGHT);put(sx+1,sy,MID);put(sx,sy+1,DARK)
    # lean: shear the tower rows around the ground line so it tilts to the right
    GROUND=114
    src=im.copy();sp=src.load();im.paste((0,0,0,0),(0,0,N,N));p=im.load()
    for y in range(N):
        sh=round((GROUND-y)*0.16) if y<GROUND else 0
        for x in range(N):
            if sp[x,y][3]==0: continue
            nx=x+sh
            if 0<=nx<N: p[nx,y]=sp[x,y]
    def put2(x,y,c,al=255):
        if 0<=x<N and 0<=y<N: p[x,y]=c+(al,)
    # base follows the hill slope (ground drops to the right); only the last rows are soft
    for x in range(N):
        g=GROUND+round((x-64)*0.31)
        for y in range(N):
            r,gg,b,al=p[x,y]
            if not al: continue
            if y>g: p[x,y]=(0,0,0,0)
            elif y>=g-1: p[x,y]=(r,gg,b,int(al*(.45 if y==g else .75)))
    return im
a=build(False);b=build(True)
strip=Image.new('RGBA',(N*2,N),(0,0,0,0));strip.paste(a,(0,0));strip.paste(b,(N,0))
strip.save(sys.argv[1])
bg=Image.new('RGBA',(N*2*3,N*3),(150,95,60,255));bg.alpha_composite(strip.resize((N*2*3,N*3),Image.NEAREST));bg.save(sys.argv[2])
