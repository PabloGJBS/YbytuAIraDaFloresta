"""
Remove checker-pattern transparency artifacts (and pure white borders) from
exported PNGs by flood-filling from the four edges.

A pixel is treated as "background" if it is either:
  - near-white  (R,G,B all >= WHITE_THR)
  - near-gray   (each channel within GRAY_TOL of GRAY_REF)
Edge-connected runs of such pixels become alpha = 0.

Run from the project root or pass folder as argv[1].
"""
import sys, os
from collections import deque
from PIL import Image

WHITE_THR = 235        # >= this on every channel = "white"
GRAY_REF  = 204        # typical photoshop checker dark square
GRAY_TOL  = 12         # +- around GRAY_REF
ALPHA_THR = 8          # already-transparent pixels skipped

def is_bg(px):
    r, g, b, a = px
    if a <= ALPHA_THR:
        return True
    if r >= WHITE_THR and g >= WHITE_THR and b >= WHITE_THR:
        return True
    if (abs(r - GRAY_REF) <= GRAY_TOL
        and abs(g - GRAY_REF) <= GRAY_TOL
        and abs(b - GRAY_REF) <= GRAY_TOL):
        return True
    return False

def clean(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    visited = bytearray(w * h)
    q = deque()

    def push(x, y):
        idx = y * w + x
        if visited[idx]:
            return
        if not is_bg(px[x, y]):
            return
        visited[idx] = 1
        q.append((x, y))

    for x in range(w):
        push(x, 0)
        push(x, h - 1)
    for y in range(h):
        push(0, y)
        push(w - 1, y)

    cleared = 0
    while q:
        x, y = q.popleft()
        r, g, b, _ = px[x, y]
        px[x, y] = (r, g, b, 0)
        cleared += 1
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h:
                push(nx, ny)

    im.save(path)
    return cleared, w * h

def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else r"YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\Stage1\Tilesets"
    files = [f for f in os.listdir(folder) if f.lower().endswith(".png") and f.lower().startswith("tileset")]
    files.sort()
    for f in files:
        path = os.path.join(folder, f)
        cleared, total = clean(path)
        pct = 100.0 * cleared / total
        print(f"{f}: cleared {cleared:>8} / {total:>8} px ({pct:5.2f}%)")

if __name__ == "__main__":
    main()
