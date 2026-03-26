"""
Detect and crop halo columns/rows: edge bands whose mean visible luminance
deviates strongly from the interior, caused by decontamination bleed.

For each side (top/bottom/left/right):
  scan inward column-by-column / row-by-row.
  if mean luma differs from interior baseline by > LUMA_DEV, mark as halo.
  stop at first non-halo line.

Run:  python crop_halo.py "<folder>"
"""
import sys, os
import numpy as np
from PIL import Image

LUMA_DEV       = 6       # how many luma units off-baseline counts as halo
MAX_STRIP      = 14      # never strip more than this from a side (sanity)
MIN_VISIBLE    = 4        # minimum visible (alpha>128) pixels in a line to compare


def line_luma(slc):
    a = slc[..., 3]
    rgb = slc[..., :3].astype(np.float32)
    visible = a > 128
    if visible.sum() < MIN_VISIBLE:
        return None
    L = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    return float(L[visible].mean())


def interior_baseline(arr):
    h, w, _ = arr.shape
    inner = arr[h // 4: 3 * h // 4, w // 4: 3 * w // 4]
    return line_luma(inner) or 0.0


def crop_halo(path):
    arr = np.array(Image.open(path).convert("RGBA"))
    h, w, _ = arr.shape
    base = interior_baseline(arr)

    def is_halo(slc):
        l = line_luma(slc)
        return l is not None and abs(l - base) > LUMA_DEV

    left = 0
    while left < MAX_STRIP and is_halo(arr[:, left:left + 1]):
        left += 1
    right = 0
    while right < MAX_STRIP and is_halo(arr[:, w - 1 - right:w - right]):
        right += 1
    top = 0
    while top < MAX_STRIP and is_halo(arr[top:top + 1, :]):
        top += 1
    bottom = 0
    while bottom < MAX_STRIP and is_halo(arr[h - 1 - bottom:h - bottom, :]):
        bottom += 1

    if left or right or top or bottom:
        cropped = arr[top:h - bottom, left:w - right]
        Image.fromarray(cropped, "RGBA").save(path)

    return left, right, top, bottom, base, w, h


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else "."
    files = [f for f in os.listdir(folder)
             if f.lower().endswith(".png") and f.lower().startswith("tileset")]
    files.sort()
    for f in files:
        l, r, t, b, base, w, h = crop_halo(os.path.join(folder, f))
        if (l + r + t + b) > 0:
            print(f"{f}: base={base:5.1f}  L={l} R={r} T={t} B={b}  -> {w-l-r}x{h-t-b}")
        else:
            print(f"{f}: base={base:5.1f}  no halo detected")


if __name__ == "__main__":
    main()
