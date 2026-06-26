"""
For a single PNG, find the best horizontal seamless crop:
  pick a left column L and right column R such that the pixels at L and the
  pixels at R-1 match as closely as possible. This makes the cropped image
  tile horizontally without a visible seam.

Strategy:
  1. Run cleanup: flood-fill + light defringe + tight bbox trim (alpha>=200).
  2. Within the trimmed image, search:
       L in [0..MAX_LEFT]
       R in [W-MAX_RIGHT..W]
     pick (L,R) that minimize MSE between col(L) and col(R-1) over rows
     where both have alpha > 200, with a bias toward keeping width.

Run:  python seamless_horizontal_crop.py "<png_path>"
"""
import sys
import numpy as np
from PIL import Image
from collections import deque

WHITE_THR = 215
GRAY_REF, GRAY_TOL = 204, 25

MAX_LEFT  = 60   # max columns to consider stripping from left
MAX_RIGHT = 60   # max columns to consider stripping from right
KEEP_BIAS = 0.15 # weight to discourage cropping (per pixel cropped)
ALPHA_OPAQUE = 200


def reflood(rgba):
    h, w, _ = rgba.shape
    visited = np.zeros((h, w), dtype=bool)
    q = deque()

    def is_bg(x, y):
        r, g, b, a = rgba[y, x]
        if a <= 8:
            return True
        if r >= WHITE_THR and g >= WHITE_THR and b >= WHITE_THR:
            return True
        if (abs(int(r) - GRAY_REF) <= GRAY_TOL
                and abs(int(g) - GRAY_REF) <= GRAY_TOL
                and abs(int(b) - GRAY_REF) <= GRAY_TOL):
            return True
        return False

    def push(x, y):
        if visited[y, x]:
            return
        if not is_bg(x, y):
            return
        visited[y, x] = True
        q.append((x, y))

    for x in range(w):
        push(x, 0); push(x, h - 1)
    for y in range(h):
        push(0, y); push(w - 1, y)
    while q:
        x, y = q.popleft()
        rgba[y, x, 3] = 0
        if x + 1 < w: push(x + 1, y)
        if x - 1 >= 0: push(x - 1, y)
        if y + 1 < h: push(x, y + 1)
        if y - 1 >= 0: push(x, y - 1)


def trim_bbox(rgba, alpha_thr=ALPHA_OPAQUE):
    a = rgba[..., 3]
    mask = a >= alpha_thr
    if not mask.any():
        return rgba, 0, 0
    rows = np.any(mask, axis=1)
    cols = np.any(mask, axis=0)
    y0, y1 = np.where(rows)[0][[0, -1]]
    x0, x1 = np.where(cols)[0][[0, -1]]
    return rgba[y0:y1 + 1, x0:x1 + 1], x0, y0


def column_distance(a_col, b_col):
    """MSE between two RGB columns, considering only rows where both opaque."""
    mask = (a_col[:, 3] >= ALPHA_OPAQUE) & (b_col[:, 3] >= ALPHA_OPAQUE)
    if mask.sum() < 4:
        return None
    diff = a_col[mask, :3].astype(np.float32) - b_col[mask, :3].astype(np.float32)
    return float(np.mean(diff ** 2))


def find_seamless_crop(rgba):
    h, w, _ = rgba.shape

    best = None  # (score, l, r)
    for l in range(0, MAX_LEFT + 1):
        col_l = rgba[:, l]
        # pick a few candidate "compare" rows to speed up: use full column
        for r in range(w - MAX_RIGHT, w + 1):
            r_idx = r - 1
            if r_idx <= l + 50:  # need some image left
                continue
            col_r = rgba[:, r_idx]
            d = column_distance(col_l, col_r)
            if d is None:
                continue
            cropped = l + (w - r)
            score = d + KEEP_BIAS * cropped
            if best is None or score < best[0]:
                best = (score, l, r, d, cropped)
    return best


def main():
    path = sys.argv[1]
    rgba = np.array(Image.open(path).convert("RGBA"))
    print(f"Original: {rgba.shape[1]}x{rgba.shape[0]}")
    reflood(rgba)
    rgba, ox, oy = trim_bbox(rgba)
    print(f"After flood+trim: {rgba.shape[1]}x{rgba.shape[0]}  (offset {ox},{oy})")

    best = find_seamless_crop(rgba)
    if best is None:
        print("No seamless crop found.")
        return
    score, l, r, d, cropped = best
    print(f"Best crop: L={l} R-from-right={rgba.shape[1] - r}  MSE={d:.2f}  cropped_total={cropped}")
    out = rgba[:, l:r]
    print(f"Final: {out.shape[1]}x{out.shape[0]}")
    Image.fromarray(out, "RGBA").save(path)
    print(f"Saved to {path}")


if __name__ == "__main__":
    main()
