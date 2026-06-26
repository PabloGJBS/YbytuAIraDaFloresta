"""
Iterative defringe: repeatedly dilate transparent region and kill bright
pixels at the new edge, until no more progress.

Usage: python iterative_defringe.py <path> [--luma-thr N] [--max-iter N]
"""
import sys, argparse
import numpy as np
import cv2
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("path")
    ap.add_argument("--luma-thr", type=int, default=130,
                    help="kill pixels with luma >= this at the edge")
    ap.add_argument("--protect-warm", action="store_true",
                    help="never kill red-dominant pixels (fire/embers)")
    ap.add_argument("--max-iter", type=int, default=8)
    ap.add_argument("--decontam", action="store_true",
                    help="also blend remaining edge RGB toward neighborhood")
    args = ap.parse_args()

    arr = np.array(Image.open(args.path).convert("RGBA"))
    h, w, _ = arr.shape
    rgb = arr[..., :3]
    alpha = arr[..., 3]

    R = rgb[..., 0].astype(np.int16)
    G = rgb[..., 1].astype(np.int16)
    B = rgb[..., 2].astype(np.int16)
    warm = (R > B + 10) & (R > G)

    luma = (0.299*rgb[...,0] + 0.587*rgb[...,1] + 0.114*rgb[...,2])

    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    total = 0
    for it in range(args.max_iter):
        transparent = alpha == 0
        grown = cv2.dilate(transparent.astype(np.uint8), kernel, iterations=1).astype(bool)
        edge = grown & ~transparent
        kill = edge & (luma >= args.luma_thr) & (alpha > 0)
        if args.protect_warm:
            kill &= ~warm
        n = int(kill.sum())
        if n == 0:
            print(f"iter {it+1}: no more bright edge pixels; stop")
            break
        alpha[kill] = 0
        total += n
        print(f"iter {it+1}: killed {n} bright edge pixels")

    arr[..., 3] = alpha

    if args.decontam:
        # Blend edge RGB toward neighborhood to kill residual tint
        rgbf = arr[..., :3].astype(np.float32)
        transparent = alpha == 0
        grown = cv2.dilate(transparent.astype(np.uint8), kernel, iterations=2).astype(bool)
        edge = grown & ~transparent
        opaque_mask = (~transparent).astype(np.float32)
        sum_mask = cv2.boxFilter(opaque_mask, ddepth=cv2.CV_32F, ksize=(9, 9), normalize=False)
        means = np.zeros_like(rgbf)
        for c in range(3):
            s = cv2.boxFilter(rgbf[..., c] * opaque_mask, ddepth=cv2.CV_32F, ksize=(9, 9), normalize=False)
            means[..., c] = np.where(sum_mask > 0, s / np.maximum(sum_mask, 1e-6), rgbf[..., c])
        blended = rgbf.copy()
        blended[edge] = 0.4 * rgbf[edge] + 0.6 * means[edge]
        arr[..., :3] = np.clip(blended, 0, 255).astype(np.uint8)
        print(f"decontam: blended {edge.sum()} edge pixels")

    Image.fromarray(arr, "RGBA").save(args.path)
    print(f"Total killed: {total} pixels")
    print(f"Saved: {args.path}")


if __name__ == "__main__":
    main()
