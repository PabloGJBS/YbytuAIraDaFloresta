"""
Remove o chao dos props de animais mortos usando GrabCut (modelo de cor + coerencia).

Init por mascara automatica derivada da forma do blob:
  - fora do blob            -> GC_BGD (fundo certo)
  - nucleo (erode forte)    -> GC_FGD (animal certo)
  - anel externo do blob    -> GC_PR_BGD (provavel terra)
  - resto                   -> GC_PR_FGD

Depois roda GrabCut N iteracoes e mantem o que ele classificar como frente.

Uso:
  python remove_chao_grabcut.py            # todos
  python remove_chao_grabcut.py 2          # so o indice 2
"""
import sys
from pathlib import Path
import numpy as np
import cv2
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao/_originais")
DST = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao")

# R_core: erosao p/ marcar animal certo (maior = nucleo menor/mais seguro)
# R_ring: espessura do anel externo marcado como provavel-terra
# iters : iteracoes do grabcut
CFG = {
    1: dict(R_core=70, R_ring=22, iters=6),
    2: dict(R_core=90, R_ring=40, iters=8),
    3: dict(R_core=70, R_ring=28, iters=6),
    4: dict(R_core=80, R_ring=30, iters=6),
    5: dict(R_core=70, R_ring=26, iters=6),
}


def ek(r):
    return cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1))


def process(idx: int):
    cfg = CFG[idx]
    img = Image.open(SRC / f"PropsAnimaisMortos{idx}.png").convert("RGBA")
    arr = np.array(img)
    rgb = cv2.cvtColor(arr[:, :, :3], cv2.COLOR_RGB2BGR)
    alpha = arr[:, :, 3]
    blob = (alpha > 0).astype(np.uint8)

    core = cv2.erode(blob, ek(cfg["R_core"]))
    ring_inner = cv2.erode(blob, ek(cfg["R_ring"]))
    ring = blob & (~ring_inner & 1)  # anel externo do blob

    gc = np.full(blob.shape, cv2.GC_PR_FGD, np.uint8)
    gc[blob == 0] = cv2.GC_BGD
    gc[ring == 1] = cv2.GC_PR_BGD
    gc[core == 1] = cv2.GC_FGD

    bgdModel = np.zeros((1, 65), np.float64)
    fgdModel = np.zeros((1, 65), np.float64)
    cv2.grabCut(rgb, gc, None, bgdModel, fgdModel, cfg["iters"], cv2.GC_INIT_WITH_MASK)

    fg = np.where((gc == cv2.GC_FGD) | (gc == cv2.GC_PR_FGD), 1, 0).astype(np.uint8)
    # limpa: maior componente + fecha buracos
    num, labels, stats, _ = cv2.connectedComponentsWithStats(fg, 8)
    if num > 1:
        biggest = 1 + np.argmax([stats[i, cv2.CC_STAT_AREA] for i in range(1, num)])
        fg = (labels == biggest).astype(np.uint8)
    fg = cv2.morphologyEx(fg, cv2.MORPH_CLOSE, ek(5))
    fg = fg & blob

    out = arr.copy()
    out[:, :, 3] = np.where(fg > 0, alpha, 0)
    Image.fromarray(out).save(DST / f"PropsAnimaisMortos{idx}.png")
    kept = int((fg > 0).sum()); orig = int(blob.sum())
    print(f"  [{idx}] grabcut iters={cfg['iters']}: {kept}/{orig} ({100*kept/orig:.0f}% mantido)")


def main():
    args = [int(a) for a in sys.argv[1:] if a.isdigit()]
    for i in (args if args else list(CFG.keys())):
        process(i)


if __name__ == "__main__":
    main()
