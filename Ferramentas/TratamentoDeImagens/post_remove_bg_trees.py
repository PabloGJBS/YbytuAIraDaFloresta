"""
Post-process for TileSet1-Noite (and similar):
  - Kill any leftover near-white pixels globally.
  - Kill bright blue-silhouette trees in a middle x-range, leaving the
    foreground stump on the left and dead trees on the right intact.

Args:
  --white-thr  : luma >= this anywhere => alpha 0   (default 220)
  --bg-x0      : start of middle BG zone (% of width)   (default 0.30)
  --bg-x1      : end   of middle BG zone (% of width)   (default 0.85)
  --bg-y1      : bottom of BG zone (% of height) - protects dirt floor (default 0.78)
  --bg-luma    : in BG zone, luma > this => alpha 0 (default 45)
"""
import sys, argparse, os
import numpy as np
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("path")
    ap.add_argument("--white-thr", type=int, default=220)
    ap.add_argument("--bg-x0", type=float, default=0.30)
    ap.add_argument("--bg-x1", type=float, default=0.85)
    ap.add_argument("--bg-y1", type=float, default=0.78)
    ap.add_argument("--bg-luma", type=int, default=45)
    args = ap.parse_args()

    arr = np.array(Image.open(args.path).convert("RGBA"))
    h, w, _ = arr.shape
    rgb = arr[..., :3]
    a = arr[..., 3]
    luma = 0.299*rgb[...,0] + 0.587*rgb[...,1] + 0.114*rgb[...,2]

    # 1) global near-white kill
    white_mask = (a > 0) & (luma >= args.white_thr)
    n_white = int(white_mask.sum())
    a[white_mask] = 0

    # 2) BG zone bright kill, protecting red-dominant pixels (flames, embers)
    x0 = int(args.bg_x0 * w); x1 = int(args.bg_x1 * w)
    y1 = int(args.bg_y1 * h)
    bg_zone = np.zeros_like(a, dtype=bool)
    bg_zone[:y1, x0:x1] = True
    R = rgb[..., 0].astype(np.int16)
    G = rgb[..., 1].astype(np.int16)
    B = rgb[..., 2].astype(np.int16)
    red_dom = (R > B + 10) & (R > G)  # warm pixels (fire/embers)
    bg_mask = bg_zone & (a > 0) & (luma > args.bg_luma) & ~red_dom
    n_bg = int(bg_mask.sum())
    a[bg_mask] = 0

    arr[..., 3] = a
    Image.fromarray(arr, "RGBA").save(args.path)
    print(f"Image: {w}x{h}")
    print(f"  white kills (luma>={args.white_thr})   : {n_white}")
    print(f"  bg-zone kills (x[{x0}:{x1}], y[0:{y1}], luma>{args.bg_luma}) : {n_bg}")
    print(f"Saved: {args.path}")


if __name__ == "__main__":
    main()
