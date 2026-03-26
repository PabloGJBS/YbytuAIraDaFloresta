"""Processa moldura: checkerboard out, interior translucido, crop.
Versao 3: detecta interior por cor + maior componente conectado (skipping
small dark-wood shadows que tem cor similar mas estao desconectadas)."""
from PIL import Image
import numpy as np
from scipy import ndimage

SRC = r"C:\Users\Administrador\Downloads\SlotFrame\4.png"
DST = r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\UI\SaveSelect\SlotFrame.png"

im = Image.open(SRC).convert("RGBA")
arr = np.array(im)
H, W = arr.shape[:2]
print(f"input: {W}x{H}")

r, g, b = arr[..., 0].astype(int), arr[..., 1].astype(int), arr[..., 2].astype(int)

# 1) checkerboard background: gray (R~=G~=B) e claro
gray_diff = np.maximum(np.maximum(np.abs(r - g), np.abs(g - b)), np.abs(r - b))
brightness = (r + g + b) // 3
is_checker = (gray_diff < 18) & (brightness > 110)
arr[is_checker, 3] = 0
print(f"checkerboard: {is_checker.sum()} pixels removed")

# 2) interior color match (apertado)
seed_r, seed_g, seed_b = 30, 22, 13
TOL = 12
is_interior_color = (
    (np.abs(r - seed_r) < TOL)
    & (np.abs(g - seed_g) < TOL)
    & (np.abs(b - seed_b) < TOL)
    & (brightness < 45)
)
print(f"color-matched dark pixels: {is_interior_color.sum()}")

# 3) connected components - pega so o maior (que eh o interior real)
labels, n_components = ndimage.label(is_interior_color)
print(f"components: {n_components}")
if n_components > 0:
    sizes = ndimage.sum(is_interior_color, labels, range(1, n_components + 1))
    biggest = np.argmax(sizes) + 1
    interior_mask = labels == biggest
    print(f"biggest component: {sizes[biggest-1]:.0f} pixels (vs 2nd: {sorted(sizes, reverse=True)[1] if len(sizes)>1 else 0:.0f})")
else:
    interior_mask = np.zeros_like(is_interior_color)

arr[interior_mask, 3] = 90  # ~35% opaco
print(f"interior translucentes: {interior_mask.sum()}")

# 4) crop bbox alpha + pad
alpha = arr[..., 3]
ys, xs = np.where(alpha > 0)
y0, y1 = ys.min(), ys.max()
x0, x1 = xs.min(), xs.max()
pad = 2
y0 = max(0, y0 - pad); y1 = min(H - 1, y1 + pad)
x0 = max(0, x0 - pad); x1 = min(W - 1, x1 + pad)
cropped = arr[y0 : y1 + 1, x0 : x1 + 1]
print(f"cropped: {cropped.shape[1]}x{cropped.shape[0]}")

Image.fromarray(cropped, "RGBA").save(DST)
print(f"saved to {DST}")

out_a = cropped[..., 3]
print(f"  alpha 0  : {(out_a == 0).sum()}")
print(f"  alpha 90 : {(out_a == 90).sum()}")
print(f"  alpha 255: {(out_a == 255).sum()}")
