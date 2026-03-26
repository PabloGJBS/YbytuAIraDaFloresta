"""
Crop each PNG to the tight bounding box of its non-transparent pixels.

After flood-fill + defringe, sprites carry a margin of fully transparent pixels
that ruins Tiled draw mode (visible gaps between repetitions). Trimming kills
the gap without touching visible content.

Run:  python trim_tilesets.py "<folder>"
"""
import sys, os
import numpy as np
from PIL import Image

ALPHA_THR = 200  # column/row counts as content if any pixel has alpha > this

def trim(path):
    im = Image.open(path).convert("RGBA")
    arr = np.array(im)
    alpha = arr[..., 3]
    mask = alpha > ALPHA_THR
    if not mask.any():
        return None
    rows = np.any(mask, axis=1)
    cols = np.any(mask, axis=0)
    y0, y1 = np.where(rows)[0][[0, -1]]
    x0, x1 = np.where(cols)[0][[0, -1]]
    cropped = arr[y0:y1 + 1, x0:x1 + 1]
    Image.fromarray(cropped, "RGBA").save(path)
    return arr.shape, cropped.shape

def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else "."
    files = [f for f in os.listdir(folder)
             if f.lower().endswith(".png") and f.lower().startswith("tileset")]
    files.sort()
    for f in files:
        path = os.path.join(folder, f)
        result = trim(path)
        if result is None:
            print(f"{f}: empty image, skipped")
            continue
        before, after = result
        bh, bw = before[0], before[1]
        ah, aw = after[0], after[1]
        print(f"{f}: {bw}x{bh} -> {aw}x{ah}  (trimmed L/R: {bw - aw}, T/B: {bh - ah})")

if __name__ == "__main__":
    main()
