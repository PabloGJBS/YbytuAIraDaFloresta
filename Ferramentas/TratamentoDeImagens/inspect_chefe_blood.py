from PIL import Image
import os

BASE = r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites_Temporarios\Sprites"
ORIG = os.path.join(BASE, "Enemy-Chefe1", "Hurt")
DARK = os.path.join(BASE, "Enemy-Chefe1-BlackHair", "Hurt")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_blood")
os.makedirs(OUT, exist_ok=True)

def colors(path):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    w, h = im.size
    cnt = {}
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 20:
                continue
            cnt[(r, g, b)] = cnt.get((r, g, b), 0) + 1
    return im.size, cnt

def reddish(c):
    r, g, b = c
    return r > 90 and r > g + 30 and r > b + 10  # vermelho/rosado

def purplish(c):
    r, g, b = c
    return r > 70 and b > 70 and g + 25 < r and g + 25 < b  # roxo (r e b altos, g baixo)

for name, folder in [("ORIG", ORIG), ("DARK", DARK)]:
    print(f"==== {name} ====")
    for i in range(1, 5):
        p = os.path.join(folder, f"hurt{i}.png")
        if not os.path.exists(p):
            continue
        size, cnt = colors(p)
        reds = [(c, n) for c, n in cnt.items() if reddish(c)]
        purps = [(c, n) for c, n in cnt.items() if purplish(c)]
        reds.sort(key=lambda x: -x[1])
        purps.sort(key=lambda x: -x[1])
        print(f"hurt{i} {size} | vermelhos={reds[:6]} | roxos={purps[:6]}")

# preview lado a lado x10
scale = 10
for i in range(1, 5):
    po = os.path.join(ORIG, f"hurt{i}.png")
    pd = os.path.join(DARK, f"hurt{i}.png")
    if not (os.path.exists(po) and os.path.exists(pd)):
        continue
    io = Image.open(po).convert("RGBA")
    idk = Image.open(pd).convert("RGBA")
    w = max(io.width, idk.width)
    h = max(io.height, idk.height)
    canvas = Image.new("RGBA", (w * 2 + 4, h), (30, 30, 40, 255))
    canvas.alpha_composite(io, (0, h - io.height))
    canvas.alpha_composite(idk, (w + 4, h - idk.height))
    big = canvas.resize(((w * 2 + 4) * scale, h * scale), Image.NEAREST)
    big.convert("RGB").save(os.path.join(OUT, f"hurt{i}_orig_vs_dark.png"))
print("previews em", OUT)
