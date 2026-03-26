"""
Lista as cores principais de um sprite de inimigo.
Uso: python sample_sprite_palette.py <character>

Onde <character> e o nome da pasta em Sprites_Temporarios/Sprites/ (ex: Enemy-Punk).
Usa o primeiro frame de Idle.
"""
import colorsys
import sys
from collections import Counter
from pathlib import Path
from PIL import Image

if len(sys.argv) < 2:
    print("Uso: python sample_sprite_palette.py <character>")
    print("Ex: python sample_sprite_palette.py Enemy-Punk")
    raise SystemExit(1)

character = sys.argv[1]
root = Path("YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites_Temporarios/Sprites") / character / "Idle"
candidates = sorted(root.glob("idle*.png"))
if not candidates:
    print(f"Nenhum idle*.png em {root}")
    raise SystemExit(1)

src = candidates[0]
img = Image.open(src).convert("RGBA")
pixels = img.load()
w, h = img.width, img.height

counter = Counter()
for y in range(h):
    for x in range(w):
        r, g, b, a = pixels[x, y]
        if a == 0:
            continue
        counter[(r, g, b)] += 1

print(f"\n[{character}] {src.name}: {w}x{h}, {sum(counter.values())} pixels opacos, {len(counter)} cores unicas")
print("\nTOP 20 cores (RGB / HSV / count):")
for (r, g, b), count in counter.most_common(20):
    h_, s, v = colorsys.rgb_to_hsv(r/255, g/255, b/255)
    print(f"  RGB({r:3},{g:3},{b:3})  HSV({h_*360:6.1f}, {s:.2f}, {v:.2f})  x{count}")
