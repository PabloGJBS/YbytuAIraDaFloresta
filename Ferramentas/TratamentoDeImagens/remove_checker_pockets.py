"""
Remove ENCLOSED checker-background pockets left behind by the border flood in
process_checker_bg_tile.py.

The canonical flood only clears checker connected to the image border, so light
gray pockets trapped between leaves / branches / trunk strands survive as opaque
near-white blobs. For cutout props (full transparency) those must go too.

A pocket pixel is unambiguous checker: neutral gray (R~=G~=B) and light. After
killing them we re-run the canonical defringe / hard-erode / decontaminate so the
new hole rims get the same clean treatment as the outer silhouette.

Usage:  python remove_checker_pockets.py <path1> [<path2> ...]
        (edits each PNG in place)
"""
import sys
import numpy as np
from PIL import Image

import process_checker_bg_tile as pc

# checker pocket signature
VAL_THR    = 205   # near-white
CHROMA_THR = 20    # neutral gray (low saturation)


def kill_pockets(rgba):
    rgb = rgba[..., :3].astype(np.int16)
    a = rgba[..., 3]
    val = rgb.max(2)
    chroma = rgb.max(2) - rgb.min(2)
    pocket = (a > 0) & (val >= VAL_THR) & (chroma <= CHROMA_THR)
    n = int(pocket.sum())
    if n:
        rgba[pocket, 3] = 0
    return n


def main():
    paths = sys.argv[1:]
    if not paths:
        print("usage: remove_checker_pockets.py <png> [...]"); return
    for p in paths:
        rgba = np.array(Image.open(p).convert("RGBA"))
        killed = kill_pockets(rgba)
        # clean the rims of the freshly opened pockets, same as the outer edge
        d1 = pc.defringe_light(rgba)
        d2 = pc.hard_erode_bright_edge(rgba)
        d3 = pc.decontaminate(rgba)
        Image.fromarray(rgba, "RGBA").save(p)
        print(f"{p}")
        print(f"  enclosed checker killed : {killed}")
        print(f"  defringe / erode / decon: {d1} / {d2} / {d3}")


if __name__ == "__main__":
    main()
