"""
Lista todos os pixels unicos do sprite com seus HSV pra entender que tons
o cabelo usa. Filtra pra mostrar so a area provavel do cabelo (terco superior
do sprite).
"""
import colorsys
from collections import Counter
from pathlib import Path
from PIL import Image

import sys
src = Path(sys.argv[1] if len(sys.argv) > 1 else "YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites_Temporarios/Sprites/Enemy-Chefe1/Idle/idle1.png")
img = Image.open(src).convert("RGBA")
pixels = img.load()
w, h = img.width, img.height

counter = Counter()
# Sprite inteiro, ignorando alpha=0
for y in range(h):
    for x in range(w):
        r, g, b, a = pixels[x, y]
        if a == 0:
            continue
        counter[(r, g, b)] += 1

print(f"Tamanho: {w}x{h}, pixels opacos no terco superior: {sum(counter.values())}")
print(f"\nCores unicas: {len(counter)}")
print("\nTOP 20 cores (RGB / HSV / count):")
for (r, g, b), count in counter.most_common(20):
    h_, s, v = colorsys.rgb_to_hsv(r/255, g/255, b/255)
    print(f"  RGB({r:3},{g:3},{b:3})  HSV({h_*360:6.1f}°, {s:.2f}, {v:.2f})  x{count}")
