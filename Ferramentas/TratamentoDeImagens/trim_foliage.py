"""Apara a folhagem que se projeta pra fora da placa: detecta vegetacao (verde),
acha o bounding box da placa de madeira (maior componente nao-verde opaco) e
recorta nele. Remove folhas/cipos que saem do retangulo. Saida _trim_<n>.png."""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = "_processed"

def trim(name):
    a = np.array(Image.open(f"{SRC}/{name}.png").convert("RGBA"))
    rgb = a[..., :3].astype(int)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    al = a[..., 3]
    opaque = al >= 128
    # vegetacao: verde dominante (folha/cipo). Lava(laranja) e madeira(marrom) ficam fora.
    green = opaque & (g > r + 6) & (g > b - 8) & (g > 40)
    plaque = opaque & ~green
    # erode leve pra soltar pontes finas (cipos que tocam a placa)
    plaque_e = ndimage.binary_erosion(plaque, iterations=2)
    lab, k = ndimage.label(plaque_e)
    if k == 0:
        print(f"{name}: sem placa detectada"); return
    sizes = np.bincount(lab.ravel()); sizes[0] = 0
    main = lab == sizes.argmax()
    # dilata de volta pra recuperar a borda da placa erodida
    main = ndimage.binary_dilation(main, iterations=2)
    ys, xs = np.where(main)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    crop = a[y0:y1 + 1, x0:x1 + 1]
    Image.fromarray(crop).save(f"{SRC}/_trim_{name}.png")
    print(f"{name}: {a.shape[1]}x{a.shape[0]} -> {crop.shape[1]}x{crop.shape[0]} "
          f"(cortou L{x0} R{a.shape[1]-1-x1} T{y0} B{a.shape[0]-1-y1})")

for n in ["BotaoJogar", "BotaoTutorial", "BotaoConfig", "BotaoSair", "ButtonContinuar"]:
    trim(n)
