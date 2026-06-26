"""Inspeciona os props de animais mortos: tamanho, alpha, e amostra de cores
do 'chao' vs do 'bicho'."""
import colorsys
from collections import Counter
from pathlib import Path
from PIL import Image

ROOT = Path(r"D:/Apasta/Projetos/TCC/YbytuAIraDaFloresta/YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites/Stage1/Tilesets/Props")
for i in range(1, 6):
    p = ROOT / f"PropsAnimaisMortos{i}.png"
    img = Image.open(p).convert("RGBA")
    px = img.load()
    w, h = img.size
    n_op = n_tr = n_semi = 0
    for y in range(h):
        for x in range(w):
            a = px[x, y][3]
            if a == 0:
                n_tr += 1
            elif a == 255:
                n_op += 1
            else:
                n_semi += 1
    print(f"\n=== {p.name}  {w}x{h} ===")
    print(f"  opaco={n_op}  transp={n_tr}  semi={n_semi}")
    # cor dos 4 cantos (provavel chao/fundo)
    corners = [px[2, 2], px[w-3, 2], px[2, h-3], px[w-3, h-3]]
    print(f"  cantos: {corners}")
