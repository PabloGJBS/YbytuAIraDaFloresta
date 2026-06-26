"""
Process a tile whose background is white/gray checker pattern (Photoshop-style
fake transparency). Mirrors process_black_bg_tile.py but for white+gray.

Usage:  python process_checker_bg_tile.py <src_path> <dst_path>
"""
import sys, os
from collections import deque
import numpy as np
import cv2
from PIL import Image

WHITE_THR = 215
GRAY_REF, GRAY_TOL = 204, 25
ALPHA_OPAQUE = 200

FRINGE_RADIUS  = 3
LIGHT_LUMA_THR = 160
LIGHT_DELTA    = 22

DECONTAM_RADIUS = 2

HARD_EROSION_RADIUS = 2
HARD_LUMA_THR       = 175

MAX_LEFT  = 100
MAX_RIGHT = 100
KEEP_BIAS = 0.10


def flood_checker(rgba):
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


def defringe_light(rgba):
    h, w, _ = rgba.shape
    alpha = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)
    transparent = alpha == 0
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel,
                       iterations=FRINGE_RADIUS).astype(bool)
    fringe_zone = grown & ~transparent
    if not fringe_zone.any():
        return 0
    luma = 0.299*rgb[...,0] + 0.587*rgb[...,1] + 0.114*rgb[...,2]
    opaque_mask = (~transparent).astype(np.float32)
    luma_masked = luma * opaque_mask
    box = (15, 15)
    sum_luma = cv2.boxFilter(luma_masked, ddepth=cv2.CV_32F, ksize=box, normalize=False)
    sum_mask = cv2.boxFilter(opaque_mask, ddepth=cv2.CV_32F, ksize=box, normalize=False)
    nb_mean = np.where(sum_mask > 0, sum_luma / np.maximum(sum_mask, 1e-6), 0)
    too_bright = (luma >= LIGHT_LUMA_THR) & ((luma - nb_mean) >= LIGHT_DELTA)
    kill = fringe_zone & too_bright
    n = int(kill.sum())
    if n: rgba[kill, 3] = 0
    return n


def hard_erode_bright_edge(rgba):
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
    luma = 0.299*rgb[...,0] + 0.587*rgb[...,1] + 0.114*rgb[...,2]
    kill = edge & (luma >= HARD_LUMA_THR)
    n = int(kill.sum())
    if n: rgba[kill, 3] = 0
    return n


def decontaminate(rgba):
    h, w, _ = rgba.shape
    alpha = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)
    transparent = alpha == 0
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel,
                       iterations=DECONTAM_RADIUS).astype(bool)
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
    blended = rgb.copy()
    blended[edge] = 0.4 * rgb[edge] + 0.6 * means[edge]
    rgba[..., :3] = np.clip(blended, 0, 255).astype(np.uint8)
    return int(edge.sum())


def trim_bbox(rgba):
    a = rgba[..., 3]
    mask = a >= ALPHA_OPAQUE
    if not mask.any():
        return rgba
    rows = np.any(mask, axis=1)
    cols = np.any(mask, axis=0)
    y0, y1 = np.where(rows)[0][[0, -1]]
    x0, x1 = np.where(cols)[0][[0, -1]]
    return rgba[y0:y1 + 1, x0:x1 + 1]


def column_distance(a_col, b_col):
    mask = (a_col[:, 3] >= ALPHA_OPAQUE) & (b_col[:, 3] >= ALPHA_OPAQUE)
    if mask.sum() < 4:
        return None
    diff = a_col[mask, :3].astype(np.float32) - b_col[mask, :3].astype(np.float32)
    return float(np.mean(diff ** 2))


def find_seamless_crop(rgba):
    h, w, _ = rgba.shape
    best = None
    for l in range(0, MAX_LEFT + 1):
        col_l = rgba[:, l]
        for r in range(w - MAX_RIGHT, w + 1):
            r_idx = r - 1
            if r_idx <= l + 50:
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
    src = sys.argv[1]
    dst = sys.argv[2]
    seamless = ('--no-seamless' not in sys.argv)
    rgba = np.array(Image.open(src).convert("RGBA"))
    print(f"Source: {src}  {rgba.shape[1]}x{rgba.shape[0]}")
    print(f"  flood (checker bg)         : {flood_checker(rgba)}")
    print(f"  defringe (light halo)      : {defringe_light(rgba)}")
    print(f"  hard erode bright edge     : {hard_erode_bright_edge(rgba)}")
    print(f"  decontaminate edge band    : {decontaminate(rgba)}")
    rgba = trim_bbox(rgba)
    print(f"After bbox trim: {rgba.shape[1]}x{rgba.shape[0]}")
    if seamless:
        best = find_seamless_crop(rgba)
        if best:
            score, l, r, d, cropped = best
            print(f"Seamless crop: L={l} R-from-right={rgba.shape[1] - r}  MSE={d:.2f}")
            rgba = rgba[:, l:r]
        else:
            print("No seamless crop found.")
    print(f"Final: {rgba.shape[1]}x{rgba.shape[0]}")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(dst)
    print(f"Saved to: {dst}")


if __name__ == "__main__":
    main()
