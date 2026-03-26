"""
Sanity check: for each PNG, count fully-transparent edge rows/columns.
After trim_tilesets.py, all four edges should have at least one opaque pixel.
"""
import sys, os
import numpy as np
from PIL import Image

ALPHA_THR = 1

def check(path):
    arr = np.array(Image.open(path).convert("RGBA"))
    a = arr[..., 3]
    h, w = a.shape

    def empty_count_top():
        n = 0
        for y in range(h):
            if (a[y] > ALPHA_THR).any():
                break
            n += 1
        return n
    def empty_count_bottom():
        n = 0
        for y in range(h - 1, -1, -1):
            if (a[y] > ALPHA_THR).any():
                break
            n += 1
        return n
    def empty_count_left():
        n = 0
        for x in range(w):
            if (a[:, x] > ALPHA_THR).any():
                break
            n += 1
        return n
    def empty_count_right():
        n = 0
        for x in range(w - 1, -1, -1):
            if (a[:, x] > ALPHA_THR).any():
                break
            n += 1
        return n

    return empty_count_top(), empty_count_bottom(), empty_count_left(), empty_count_right(), w, h

def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else "."
    files = [f for f in os.listdir(folder)
             if f.lower().endswith(".png") and f.lower().startswith("tileset")]
    files.sort()
    bad = 0
    for f in files:
        path = os.path.join(folder, f)
        t, b, l, r, w, h = check(path)
        flag = " <-- still has transparent edge!" if (t or b or l or r) else ""
        if flag:
            bad += 1
        print(f"{f:18} {w:>5}x{h:<5}  empty edges  T={t} B={b} L={l} R={r}{flag}")
    print()
    print(f"Result: {bad} of {len(files)} files still have empty rows/cols at edges.")

if __name__ == "__main__":
    main()
