"""
Deixa o TERNO do chefe1dark roxo (distinto do Chefe1), preservando o SANGUE vermelho.

Opera nos sprites ja recoloridos (cabelo preto). Desloca a matiz dos tons QUENTES
(terno marrom + gola maroon) pra roxo, mas PROTEGE:
  - pele e vermelho VIVO do sangue (val alto > 0.62);
  - cabelo/preto/cinza (sat < 0.15);
  - cores nao-quentes.

Roda in-place na pasta Enemy-Chefe1-BlackHair.
"""
import colorsys, os
from pathlib import Path
from PIL import Image

ROOT = Path(r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites_Temporarios\Sprites\Enemy-Chefe1-BlackHair")

PURPLE_HUE = 282.0 / 360.0  # violeta

def is_warm(h_deg):
    return h_deg >= 320.0 or h_deg <= 50.0

def recolor_pixel(rgba):
    r, g, b, a = rgba
    if a == 0:
        return rgba
    h, s, v = colorsys.rgb_to_hsv(r/255.0, g/255.0, b/255.0)
    h_deg = h * 360.0
    # protege pele + sangue vivo (claro) e neutros/preto (cabelo)
    if v > 0.62 or s < 0.15:
        return rgba
    if not is_warm(h_deg):
        return rgba
    # quente e escuro -> vira roxo, mantendo sat/val (preserva o shading)
    nr, ng, nb = colorsys.hsv_to_rgb(PURPLE_HUE, s, v)
    return (int(nr*255), int(ng*255), int(nb*255), a)

def process(path):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    changed = 0
    for y in range(im.height):
        for x in range(im.width):
            old = px[x, y]
            new = recolor_pixel(old)
            if new != old:
                px[x, y] = new
                changed += 1
    im.save(path)
    return changed

total = 0
for sub in sorted(ROOT.iterdir()):
    if not sub.is_dir():
        continue
    for png in sorted(sub.glob("*.png")):
        c = process(png)
        total += 1
print(f"Done. {total} sprites do chefe1dark com terno roxo.")
