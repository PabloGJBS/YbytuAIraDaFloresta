from PIL import Image
import numpy as np

im = Image.open(r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\UI\SaveSelect\SlotFrame.png")
print("size:", im.size, "mode:", im.mode)
a = np.array(im)
H, W = a.shape[:2]
alpha = a[..., 3]
print("alpha stats: 0px =", (alpha == 0).sum(), "  50-150 =", ((alpha > 50) & (alpha < 150)).sum(), "  255 =", (alpha == 255).sum())

samples = [
    (50, 50, "top-left corner"),
    (50, W // 2, "top-center"),
    (H // 2, 50, "mid-left rail"),
    (H // 2, W // 2, "center fill"),
    (H - 50, W // 2, "bottom-center"),
    (H - 50, W - 50, "bottom-right corner"),
]
for y, x, label in samples:
    print(f"  {label} ({x},{y}): RGBA={tuple(int(v) for v in a[y, x])}")
