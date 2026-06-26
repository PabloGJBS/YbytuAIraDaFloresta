"""Inspect a PNG to determine background color and edge characteristics."""
import sys
import numpy as np
from PIL import Image

def main():
    path = sys.argv[1]
    arr = np.array(Image.open(path).convert("RGBA"))
    h, w, _ = arr.shape
    print(f"Image: {w}x{h}")

    a = arr[..., 3]
    print(f"Alpha:  min={a.min()} max={a.max()} mean={a.mean():.1f}")
    print(f"  zero-alpha pixels: {(a == 0).sum()}")
    print(f"  fully-opaque (255): {(a == 255).sum()}")

    rgb = arr[..., :3]
    luma = 0.299*rgb[...,0] + 0.587*rgb[...,1] + 0.114*rgb[...,2]
    print(f"Luma:   min={luma.min():.1f} max={luma.max():.1f} mean={luma.mean():.1f}")

    print("\nCorner samples (3x3):")
    for label, slc in [
        ("TL", arr[:3, :3]),
        ("TR", arr[:3, -3:]),
        ("BL", arr[-3:, :3]),
        ("BR", arr[-3:, -3:]),
    ]:
        rgb_mean = slc[..., :3].mean(axis=(0, 1))
        a_mean = slc[..., 3].mean()
        print(f"  {label}: RGB=({rgb_mean[0]:.0f},{rgb_mean[1]:.0f},{rgb_mean[2]:.0f})  alpha={a_mean:.0f}")

    print("\nEdge column means (luma, alpha):")
    for label, col_idx in [("col 0", 0), ("col 1", 1), ("col w/2", w//2), ("col w-1", w-1)]:
        col = arr[:, col_idx]
        L = (0.299*col[...,0] + 0.587*col[...,1] + 0.114*col[...,2]).mean()
        A = col[...,3].mean()
        print(f"  {label}: luma={L:.1f}  alpha={A:.0f}")

if __name__ == "__main__":
    main()
