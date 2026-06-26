"""Amostra a paleta de cores do trator (frames Idle)."""
import colorsys
from collections import Counter
from pathlib import Path
from PIL import Image

ROOT = Path(r"C:/Users/Administrador/Desktop/EnemTractor")
counter = Counter()
for f in sorted((ROOT / "TractorIdle").glob("*.png")):
    img = Image.open(f).convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a < 128:
                continue
            counter[(r, g, b)] += 1

print(f"{sum(counter.values())} pixels opacos, {len(counter)} cores unicas")
print("TOP 35 cores (RGB / HSV / count):")
for (r, g, b), c in counter.most_common(35):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    print(f"  RGB({r:3},{g:3},{b:3})  H={h*360:5.0f} S={s:.2f} V={v:.2f}  x{c}")
