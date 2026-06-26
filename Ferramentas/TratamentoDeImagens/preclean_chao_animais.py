"""
PRE-LIMPEZA (opcao 2): remove so a parte CLARA do chao (cinzas, folhas, galhos,
terra iluminada) na PERIFERIA, deixando o animal + o chao escuro fundido pro
usuario finalizar no editor.

Protecoes:
  - nucleo central do animal (erode + maior componente) NUNCA e tocado
    (protege pelo claro do rosto/orelhas dos saguis).
  - so remove pixel CLARO (L > thr) que esteja na periferia.
  - preenche buracos internos (brasas/olhos/realces dentro do corpo ficam).
  - remove especulas isoladas pequenas.

Uso:
  python preclean_chao_animais.py            # todos
  python preclean_chao_animais.py 4 5
"""
import sys
from pathlib import Path
import numpy as np
import cv2
from scipy import ndimage
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao/_originais")
DST = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao")

# thr  : acima disso = claro (litter); R_core: raio do nucleo protegido
# spk  : remove componentes menores que isso (px); R_edge: apara borda (open)
CFG = {
    1: dict(thr=95,  R_core=40, spk=120, R_edge=20),
    2: dict(thr=70,  R_core=45, spk=120, R_edge=16),
    3: dict(thr=78,  R_core=45, spk=120, R_edge=18),
    4: dict(thr=115, R_core=50, spk=120, R_edge=16),
    5: dict(thr=115, R_core=45, spk=120, R_edge=16),
}


def ek(r):
    return cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1))


def process(idx: int):
    cfg = CFG[idx]
    img = Image.open(SRC / f"PropsAnimaisMortos{idx}.png").convert("RGBA")
    arr = np.array(img)
    alpha = arr[:, :, 3]
    blob = (alpha > 0).astype(np.uint8)
    L = cv2.cvtColor(cv2.cvtColor(arr[:, :, :3], cv2.COLOR_RGB2BGR), cv2.COLOR_BGR2LAB)[:, :, 0]

    # nucleo protegido = maior componente do blob erodido
    core = cv2.erode(blob, ek(cfg["R_core"]))
    num, labels, stats, _ = cv2.connectedComponentsWithStats(core, 8)
    if num > 1:
        big = 1 + int(np.argmax([stats[i, cv2.CC_STAT_AREA] for i in range(1, num)]))
        core = (labels == big).astype(np.uint8)
    core = cv2.dilate(core, ek(8))  # margem de seguranca

    bright = ((L > cfg["thr"]) & (blob > 0)).astype(np.uint8)
    remove = (bright & (core == 0)).astype(np.uint8)  # so claro na periferia

    # nao remove especulas? na verdade remove especulas isoladas claras pequenas
    # tira tambem manchas claras pequenas mesmo dentro? nao: so periferia ja basta.
    keep = (blob & (remove == 0)).astype(np.uint8)
    keep = ndimage.binary_fill_holes(keep).astype(np.uint8)

    # apara borda: abre (erode->maior CC->dilate) p/ tirar respingos/tentaculos
    # finos soltos da borda, sem adicionar pixel fora do keep original.
    Re = cfg["R_edge"]
    eroded = cv2.erode(keep, ek(Re))
    n, lab, st, _ = cv2.connectedComponentsWithStats(eroded, 8)
    if n > 1:
        big = 1 + int(np.argmax([st[i, cv2.CC_STAT_AREA] for i in range(1, n)]))
        eroded = (lab == big).astype(np.uint8)
    opened = cv2.dilate(eroded, ek(Re))
    keep = (keep & opened).astype(np.uint8)

    # despeckle final + tapa buracos
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats(keep, 8)
    for i in range(1, n2):
        if st2[i, cv2.CC_STAT_AREA] < cfg["spk"]:
            keep[lab2 == i] = 0
    keep = ndimage.binary_fill_holes(keep).astype(np.uint8) & blob

    out = arr.copy()
    out[:, :, 3] = np.where(keep > 0, alpha, 0)
    Image.fromarray(out).save(DST / f"PropsAnimaisMortos{idx}.png")
    k = int((keep > 0).sum()); o = int(blob.sum())
    print(f"  [{idx}] thr={cfg['thr']}: removeu {o-k} px claros ({100*(o-k)/o:.0f}%), manteve {100*k/o:.0f}%")


def main():
    args = [int(a) for a in sys.argv[1:] if a.isdigit()]
    for i in (args if args else list(CFG.keys())):
        process(i)


if __name__ == "__main__":
    main()
