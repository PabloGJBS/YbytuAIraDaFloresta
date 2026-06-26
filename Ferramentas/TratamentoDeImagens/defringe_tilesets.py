"""
Second-pass cleanup: kill the white anti-alias fringe left over
around already-cleaned PNGs.

Strategy:
  1. Re-flood from edges with a softer white/checker threshold (catches the
     missed faint corners).
  2. For pixels within FRINGE_RADIUS of a transparent pixel that are
     significantly LIGHTER than the average of their non-transparent
     neighbors, fade their alpha (dirty fringe = bright halo).
  3. Decontaminate: replace the RGB of remaining edge pixels with the mean
     RGB of their opaque neighborhood, killing residual white tint without
     making them transparent.

Run:  python defringe_tilesets.py "<folder>"
"""
import sys, os
import numpy as np
import cv2
from collections import deque
from PIL import Image

# pass 1 (re-flood, more aggressive)
WHITE_THR_2 = 215
GRAY_REF_2  = 204
GRAY_TOL_2  = 25

# pass 2 (fringe detection)
FRINGE_RADIUS  = 3      # pixels of dilation around transparent area
LIGHT_LUMA_THR = 160    # pixels lighter than this near transparent => suspect
LIGHT_DELTA    = 22     # how much lighter than neighborhood mean to be a fringe

# pass 3 (color decontamination on remaining 1-px ring)
DECONTAM_RADIUS = 2

# pass 4 (final hard erosion of bright edge pixels)
HARD_EROSION_RADIUS = 2
HARD_LUMA_THR       = 175


def reflood(rgba):
    h, w, _ = rgba.shape
    px = rgba
    visited = np.zeros((h, w), dtype=bool)
    q = deque()

    def is_bg(x, y):
        r, g, b, a = px[y, x]
        if a <= 8:
            return True
        if r >= WHITE_THR_2 and g >= WHITE_THR_2 and b >= WHITE_THR_2:
            return True
        if (abs(int(r) - GRAY_REF_2) <= GRAY_TOL_2
                and abs(int(g) - GRAY_REF_2) <= GRAY_TOL_2
                and abs(int(b) - GRAY_REF_2) <= GRAY_TOL_2):
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
        push(x, 0)
        push(x, h - 1)
    for y in range(h):
        push(0, y)
        push(w - 1, y)

    cleared = 0
    while q:
        x, y = q.popleft()
        rgba[y, x, 3] = 0
        cleared += 1
        if x + 1 < w: push(x + 1, y)
        if x - 1 >= 0: push(x - 1, y)
        if y + 1 < h: push(x, y + 1)
        if y - 1 >= 0: push(x, y - 1)
    return cleared


def defringe(rgba):
    h, w, _ = rgba.shape
    alpha = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)

    transparent = alpha == 0

    # Dilate transparent into opaque to find fringe candidate zone.
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel, iterations=FRINGE_RADIUS).astype(bool)
    fringe_zone = grown & ~transparent  # candidate pixels (still opaque, but next to alpha=0)

    if not fringe_zone.any():
        return 0

    luma = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]

    # Mean luminance of the *opaque* neighborhood (15x15 box, ignoring transparent).
    opaque_mask = (~transparent).astype(np.float32)
    luma_masked = luma * opaque_mask
    box = (15, 15)
    sum_luma = cv2.boxFilter(luma_masked, ddepth=cv2.CV_32F, ksize=box, normalize=False)
    sum_mask = cv2.boxFilter(opaque_mask, ddepth=cv2.CV_32F, ksize=box, normalize=False)
    nb_mean = np.where(sum_mask > 0, sum_luma / np.maximum(sum_mask, 1e-6), 0)

    too_bright = (luma >= LIGHT_LUMA_THR) & ((luma - nb_mean) >= LIGHT_DELTA)
    kill = fringe_zone & too_bright

    cleared = int(kill.sum())
    if cleared:
        rgba[kill, 3] = 0
    return cleared


def decontaminate(rgba):
    """Pull RGB of edge pixels toward neighborhood mean to kill remaining tint."""
    h, w, _ = rgba.shape
    alpha = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)
    transparent = alpha == 0

    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel, iterations=DECONTAM_RADIUS).astype(bool)
    edge = grown & ~transparent

    if not edge.any():
        return 0

    opaque_mask = (~transparent).astype(np.float32)
    box = (9, 9)
    sum_mask = cv2.boxFilter(opaque_mask, ddepth=cv2.CV_32F, ksize=box, normalize=False)
    means = np.zeros_like(rgb)
    for c in range(3):
        s = cv2.boxFilter(rgb[..., c] * opaque_mask, ddepth=cv2.CV_32F, ksize=box, normalize=False)
        means[..., c] = np.where(sum_mask > 0, s / np.maximum(sum_mask, 1e-6), rgb[..., c])

    # Blend 60% toward neighborhood mean for edge pixels.
    blended = rgb.copy()
    blended[edge] = 0.4 * rgb[edge] + 0.6 * means[edge]
    rgba[..., :3] = np.clip(blended, 0, 255).astype(np.uint8)
    return int(edge.sum())


def hard_erode_bright_edge(rgba):
    """Final pass: any opaque pixel near transparent that is still
    'bright-ish' gets killed unconditionally. Stronger than defringe()."""
    h, w, _ = rgba.shape
    alpha = rgba[..., 3]
    rgb = rgba[..., :3]
    transparent = alpha == 0

    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel,
                       iterations=HARD_EROSION_RADIUS).astype(bool)
    edge = grown & ~transparent
    if not edge.any():
        return 0

    luma = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    kill = edge & (luma >= HARD_LUMA_THR)
    cleared = int(kill.sum())
    if cleared:
        rgba[kill, 3] = 0
    return cleared


def process(path):
    im = np.array(Image.open(path).convert("RGBA"))
    flood = reflood(im)
    fringe = defringe(im)
    hard = hard_erode_bright_edge(im)
    decon = decontaminate(im)
    Image.fromarray(im, "RGBA").save(path)
    return flood, fringe, hard, decon, im.shape[0] * im.shape[1]


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else "."
    files = [f for f in os.listdir(folder)
             if f.lower().endswith(".png") and f.lower().startswith("tileset")]
    files.sort()
    for f in files:
        path = os.path.join(folder, f)
        flood, fringe, hard, decon, total = process(path)
        print(f"{f}: reflood={flood:>6}  fringe={fringe:>5}  hard-edge={hard:>5}  decontam={decon:>6}  / {total} px")


if __name__ == "__main__":
    main()
