from PIL import Image
import colorsys, os

BASE = r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites_Temporarios\Sprites\Enemy-Chefe1-BlackHair"

def dump(path):
    im = Image.open(path).convert("RGBA")
    px = im.load(); w, h = im.size
    cnt = {}
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 20:
                continue
            cnt[(r, g, b)] = cnt.get((r, g, b), 0) + 1
    items = sorted(cnt.items(), key=lambda kv: -kv[1])
    print(f"--- {os.path.basename(path)} ({w}x{h}) top cores:")
    for (r, g, b), n in items[:18]:
        hh, s, v = colorsys.rgb_to_hsv(r/255, g/255, b/255)
        print(f"  ({r:3},{g:3},{b:3}) x{n:4}  hue={hh*360:5.1f} sat={s:.2f} val={v:.2f}")

for f in ["Idle/idle1.png", "Walk/walk1.png"]:
    dump(os.path.join(BASE, f))
