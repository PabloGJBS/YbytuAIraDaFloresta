"""Processa os 6 frames da animacao do quadro de save com o mesmo pipeline."""
from PIL import Image
import numpy as np
from scipy import ndimage
import os

SRC_DIR = r"C:\Users\Administrador\Downloads\QuadroSave"
DST_DIR = r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\UI\SaveSelect\SlotFrameAnim"
os.makedirs(DST_DIR, exist_ok=True)

# precisamos uniformizar o crop entre frames pra alinhar a animacao.
# 1) carrega todos
# 2) processa interior translucido em cada um
# 3) calcula um bbox COMUM (uniao dos bboxes alpha)
# 4) cropa todos com o mesmo bbox

frames = []
for i in range(1, 7):
    path = os.path.join(SRC_DIR, f"QuadroSave-sprite{i}.png")
    im = Image.open(path).convert("RGBA")
    arr = np.array(im)
    H, W = arr.shape[:2]
    print(f"frame {i}: {W}x{H}")

    r, g, b = arr[..., 0].astype(int), arr[..., 1].astype(int), arr[..., 2].astype(int)

    # checkerboard out
    gray_diff = np.maximum(np.maximum(np.abs(r - g), np.abs(g - b)), np.abs(r - b))
    brightness = (r + g + b) // 3
    is_checker = (gray_diff < 18) & (brightness > 110)
    arr[is_checker, 3] = 0

    # interior translucent: cor uniforme escura + maior componente conectado
    seed_r, seed_g, seed_b = 30, 22, 13
    TOL = 12
    is_interior_color = (
        (np.abs(r - seed_r) < TOL)
        & (np.abs(g - seed_g) < TOL)
        & (np.abs(b - seed_b) < TOL)
        & (brightness < 45)
    )
    labels, n = ndimage.label(is_interior_color)
    if n > 0:
        sizes = ndimage.sum(is_interior_color, labels, range(1, n + 1))
        biggest = np.argmax(sizes) + 1
        interior_mask = labels == biggest
        arr[interior_mask, 3] = 90
    frames.append(arr)

# bbox comum
bbox_y0, bbox_y1, bbox_x0, bbox_x1 = None, None, None, None
for arr in frames:
    alpha = arr[..., 3]
    ys, xs = np.where(alpha > 0)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    bbox_y0 = y0 if bbox_y0 is None else min(bbox_y0, y0)
    bbox_y1 = y1 if bbox_y1 is None else max(bbox_y1, y1)
    bbox_x0 = x0 if bbox_x0 is None else min(bbox_x0, x0)
    bbox_x1 = x1 if bbox_x1 is None else max(bbox_x1, x1)

pad = 2
H, W = frames[0].shape[:2]
bbox_y0 = max(0, bbox_y0 - pad)
bbox_y1 = min(H - 1, bbox_y1 + pad)
bbox_x0 = max(0, bbox_x0 - pad)
bbox_x1 = min(W - 1, bbox_x1 + pad)
print(f"common bbox: x={bbox_x0}-{bbox_x1}  y={bbox_y0}-{bbox_y1}  -> {bbox_x1-bbox_x0+1}x{bbox_y1-bbox_y0+1}")

for i, arr in enumerate(frames, 1):
    cropped = arr[bbox_y0 : bbox_y1 + 1, bbox_x0 : bbox_x1 + 1]
    out_path = os.path.join(DST_DIR, f"SlotFrame_{i:02d}.png")
    Image.fromarray(cropped, "RGBA").save(out_path)
    a = cropped[..., 3]
    print(f"  saved {out_path}  alpha 0={(a==0).sum()}, 90={(a==90).sum()}, 255={(a==255).sum()}")
