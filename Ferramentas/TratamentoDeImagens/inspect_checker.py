"""Find dominant gray brightness values across all sources."""
from PIL import Image
import numpy as np
from collections import Counter
import os

SRC = r"C:\Users\Administrador\Downloads\TileSetsFase1"

for f in sorted(os.listdir(SRC)):
    if not f.endswith(".png"):
        continue
    im = Image.open(os.path.join(SRC, f)).convert("RGB")
    a = np.array(im)
    r = a[..., 0].astype(int)
    g = a[..., 1].astype(int)
    b = a[..., 2].astype(int)
    chroma = np.maximum(np.maximum(np.abs(r - g), np.abs(g - b)), np.abs(r - b))
    brightness = (r + g + b) // 3
    mask = (chroma < 4) & (brightness > 180)
    vals = brightness[mask]
    if len(vals) == 0:
        print(f"{f}: nothing")
        continue
    common = Counter(vals.tolist()).most_common(5)
    print(f"{f}: {common}")
