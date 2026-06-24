"""
Remove o chao dos props de animais mortos por LUMINANCIA (Lab L).

Observacao chave: a terra/galhos/folhas iluminados sao bem mais CLAROS que o
animal escuro (ver sample_chao_vs_bicho.py). Entao:
  1. mantem pixels escuros (L < thr)  -> animal (+ sombra/terra escura colada)
  2. maior componente conectado       -> tira terra escura solta nas bordas
  3. preenche buracos internos         -> brasas/olhos claros no corpo ficam opacos
  4. limpeza morfologica leve

Cada prop tem seu limiar. mode='dark' mantem escuro; 'bright' mantem claro.

Uso:
  python remove_chao_lum.py            # todos
  python remove_chao_lum.py 3 4 5
"""
import sys
from pathlib import Path
import numpy as np
import cv2
from scipy import ndimage
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao/_originais")
DST = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao")

CFG = {
    1: dict(thr=80,  mode="dark", open=3, close=7),
    2: dict(thr=27,  mode="dark", open=3, close=7),
    3: dict(thr=60,  mode="dark", open=3, close=7),
    4: dict(thr=80,  mode="dark", open=3, close=9),
    5: dict(thr=85,  mode="dark", open=3, close=7),
}


def ek(r):
    return cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1))


def process(idx: int):
    cfg = CFG[idx]
    img = Image.open(SRC / f"PropsAnimaisMortos{idx}.png").convert("RGBA")
    arr = np.array(img)
    alpha = arr[:, :, 3]
    blob = (alpha > 0).astype(np.uint8)
    bgr = cv2.cvtColor(arr[:, :, :3], cv2.COLOR_RGB2BGR)
    L = cv2.cvtColor(bgr, cv2.COLOR_BGR2LAB)[:, :, 0]

    if cfg["mode"] == "dark":
        keep = ((L < cfg["thr"]) & (blob > 0)).astype(np.uint8)
    else:
        keep = ((L > cfg["thr"]) & (blob > 0)).astype(np.uint8)

    # tira ruido pontual
    keep = cv2.morphologyEx(keep, cv2.MORPH_OPEN, ek(cfg["open"]))
    # maior componente
    num, labels, stats, _ = cv2.connectedComponentsWithStats(keep, 8)
    if num > 1:
        biggest = 1 + int(np.argmax([stats[i, cv2.CC_STAT_AREA] for i in range(1, num)]))
        keep = (labels == biggest).astype(np.uint8)
    # fecha + preenche buracos internos (brasas/olhos)
    keep = cv2.morphologyEx(keep, cv2.MORPH_CLOSE, ek(cfg["close"]))
    keep = ndimage.binary_fill_holes(keep).astype(np.uint8)
    keep = keep & blob

    out = arr.copy()
    out[:, :, 3] = np.where(keep > 0, alpha, 0)
    Image.fromarray(out).save(DST / f"PropsAnimaisMortos{idx}.png")
    k = int((keep > 0).sum()); o = int(blob.sum())
    print(f"  [{idx}] thr={cfg['thr']} {cfg['mode']}: {k}/{o} ({100*k/o:.0f}% mantido)")


def main():
    args = [int(a) for a in sys.argv[1:] if a.isdigit()]
    for i in (args if args else list(CFG.keys())):
        process(i)


if __name__ == "__main__":
    main()
