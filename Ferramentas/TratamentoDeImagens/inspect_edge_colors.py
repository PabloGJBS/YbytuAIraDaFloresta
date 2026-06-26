"""Compare luminance of leftmost/rightmost columns vs interior of a PNG."""
import sys
import numpy as np
from PIL import Image

def luma(rgb):
    return 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]

def stats(arr):
    a = arr[..., 3]
    rgb = arr[..., :3].astype(np.float32)
    visible = a > 128
    if not visible.any():
        return None
    L = luma(rgb)[visible]
    return float(L.mean()), float(L.min()), float(L.max())

def main():
    path = sys.argv[1]
    arr = np.array(Image.open(path).convert("RGBA"))
    h, w, _ = arr.shape
    print(f"Image: {w}x{h}")
    for label, slc in [
        ("col 0",       arr[:, 0:1]),
        ("col 1",       arr[:, 1:2]),
        ("col 2",       arr[:, 2:3]),
        ("col w/2",     arr[:, w // 2:w // 2 + 1]),
        ("col w-3",     arr[:, w - 3:w - 2]),
        ("col w-2",     arr[:, w - 2:w - 1]),
        ("col w-1",     arr[:, w - 1:w]),
        ("interior",    arr[:, 50:-50] if w > 100 else arr),
    ]:
        s = stats(slc)
        if s:
            mean, mn, mx = s
            print(f"  {label:10}  luma mean={mean:6.1f}  min={mn:5.1f}  max={mx:5.1f}")
        else:
            print(f"  {label:10}  (no visible pixels)")

if __name__ == "__main__":
    main()
