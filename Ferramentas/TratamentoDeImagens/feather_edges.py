"""
Feather the alpha channel near the sprite border so it blends with the
underlying surface instead of looking pasted on.

Within FEATHER_RADIUS pixels of any transparent area, alpha is scaled
linearly by distance: at the boundary alpha=0, at radius distance alpha
keeps its original value.

Usage:
  python feather_edges.py <path> [--radius N]
"""
import sys, argparse
import numpy as np
import cv2
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("path")
    ap.add_argument("--radius", type=int, default=8,
                    help="feather distance in pixels")
    args = ap.parse_args()

    arr = np.array(Image.open(args.path).convert("RGBA"))
    a = arr[..., 3].copy()
    h, w = a.shape

    opaque = (a > 0).astype(np.uint8)
    # distance from each opaque pixel to nearest transparent pixel (including outside)
    dist = cv2.distanceTransform(opaque, cv2.DIST_L2, 5)

    # scale: at dist 0 -> 0, at dist >= radius -> original alpha
    factor = np.clip(dist / float(args.radius), 0.0, 1.0)
    new_a = (a.astype(np.float32) * factor).astype(np.uint8)
    arr[..., 3] = new_a

    Image.fromarray(arr, "RGBA").save(args.path)
    affected = int(((factor < 1.0) & (a > 0)).sum())
    print(f"Image: {w}x{h}")
    print(f"Feathered radius: {args.radius} px, {affected} pixels modified")
    print(f"Saved: {args.path}")


if __name__ == "__main__":
    main()
