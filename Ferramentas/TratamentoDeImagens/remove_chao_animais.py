"""
Remove o 'chao' (terra/cinzas/galhos esparsos) dos props de animais mortos,
mantendo so o corpo do animal.

Estrategia (morfologica, agnostica de cor):
  A terra eh uma camada FINA e esparsa na borda; o animal eh uma massa DENSA.
  - binariza alpha>0
  - opening (erode R -> mantem maior(es) componente(s) -> dilata R)
    => some o que for mais fino que ~2R (terra solta), preserva o corpo.
  - aplica a mascara resultante ao RGBA original.

Cada prop tem seu raio (R) ajustavel + quantos componentes manter.

Uso:
  python remove_chao_animais.py            # processa todos
  python remove_chao_animais.py 1 2        # so os indices listados
"""
import sys
from pathlib import Path
import numpy as np
import cv2
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao/_originais")
DST = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao")

# index -> (raio_erosao, n_componentes_manter, fechar_buracos)
CFG = {
    1: dict(R=26, keep=1, close=9),
    2: dict(R=30, keep=2, close=9),
    3: dict(R=26, keep=1, close=9),
    4: dict(R=30, keep=1, close=11),
    5: dict(R=28, keep=1, close=9),
}


def keep_main_components(mask: np.ndarray, n: int) -> np.ndarray:
    num, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if num <= 1:
        return mask
    areas = [(stats[i, cv2.CC_STAT_AREA], i) for i in range(1, num)]
    areas.sort(reverse=True)
    keep_ids = {i for _, i in areas[:n]}
    out = np.zeros_like(mask)
    for _, i in areas[:n]:
        out[labels == i] = 255
    return out


def process(idx: int):
    cfg = CFG[idx]
    src = SRC / f"PropsAnimaisMortos{idx}.png"
    img = Image.open(src).convert("RGBA")
    arr = np.array(img)
    alpha = arr[:, :, 3]
    mask = (alpha > 0).astype(np.uint8) * 255

    R = cfg["R"]
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * R + 1, 2 * R + 1))
    eroded = cv2.erode(mask, k)
    core = keep_main_components(eroded, cfg["keep"])
    dilated = cv2.dilate(core, k)
    # fecha buracos pequenos internos
    c = cfg["close"]
    kc = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (c, c))
    final = cv2.morphologyEx(dilated, cv2.MORPH_CLOSE, kc)
    final = cv2.bitwise_and(final, mask)  # nunca cria pixel fora do original

    out = arr.copy()
    out[:, :, 3] = np.where(final > 0, alpha, 0)
    Image.fromarray(out).save(DST / f"PropsAnimaisMortos{idx}.png")
    kept = int((final > 0).sum())
    orig = int((mask > 0).sum())
    print(f"  [{idx}] R={R} keep={cfg['keep']}: {kept}/{orig} px mantidos ({100*kept/orig:.0f}%)")


def main():
    args = [int(a) for a in sys.argv[1:] if a.isdigit()]
    targets = args if args else list(CFG.keys())
    print(f"Processando: {targets}")
    for i in targets:
        process(i)


if __name__ == "__main__":
    main()
