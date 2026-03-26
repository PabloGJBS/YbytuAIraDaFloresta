"""
Harden the alpha channel at the *outer borders* of each PNG.

Within EDGE_BAND pixels of the bounding rectangle, any pixel with alpha
between ALPHA_KEEP and 254 is bumped to 255 (full opaque). This kills
the slight see-through at tile seams without affecting the interior
silhouette of the sprite (foliage tops etc. keep their soft alpha).

Run:  python harden_tile_edges.py "<folder>"
"""
import sys, os
import numpy as np
from PIL import Image

EDGE_BAND  = 3    # how many pixels in from each border to harden
ALPHA_KEEP = 32   # if alpha > this in the band, force to 255

def harden(path):
    arr = np.array(Image.open(path).convert("RGBA"))
    h, w, _ = arr.shape
    a = arr[..., 3]
    band = np.zeros_like(a, dtype=bool)
    band[:EDGE_BAND, :]      = True
    band[-EDGE_BAND:, :]     = True
    band[:, :EDGE_BAND]      = True
    band[:, -EDGE_BAND:]     = True
    bumped = band & (a > ALPHA_KEEP) & (a < 255)
    n = int(bumped.sum())
    if n:
        a[bumped] = 255
        arr[..., 3] = a
        Image.fromarray(arr, "RGBA").save(path)
    return n

def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else "."
    files = [f for f in os.listdir(folder)
             if f.lower().endswith(".png") and f.lower().startswith("tileset")]
    files.sort()
    total = 0
    for f in files:
        n = harden(os.path.join(folder, f))
        total += n
        print(f"{f}: hardened {n} edge pixels")
    print(f"---\nTotal hardened: {total}")

if __name__ == "__main__":
    main()
