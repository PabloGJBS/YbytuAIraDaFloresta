"""
Remove o fundo branco "fake PNG" das jaulas (Jaula1/2/3) por flood-fill a partir
das bordas, depois limpa o halo claro antialiasado e recorta a bbox.

Fundo: branco quase puro (RGB >= ~228), ~65% da imagem; sujeito escuro (madeira
marrom / corda dourada). Flood-fill por conectividade preserva qualquer branco
"preso" pela madeira que nao toca a borda (highlights), e limpa os vaos da jaula
que se conectam ao exterior.

NAO faz seamless crop (isso e so pra tileset). Mantem o sprite inteiro, so trima
o excesso transparente ao redor.

Usage:  python process_jaula.py <src> <dst>
"""
import sys, os
from collections import deque
import numpy as np
import cv2
from PIL import Image

WHITE_THR = 228          # canal >= isto nos 3 canais = fundo branco
ALPHA_OPAQUE = 200

FRINGE_RADIUS  = 2       # banda de borda pra matar halo claro
LIGHT_LUMA_THR = 205     # so mata fringe MUITO claro (perto do branco)
LIGHT_DELTA    = 30

DECONTAM_RADIUS = 2      # sangra cor do vizinho opaco pra dentro da borda

POCKET_VAL    = 230      # bolsao de branco preso: val alto...
POCKET_CHROMA = 18       # ...e cinza neutro (chroma baixo). Madeira tan tem chroma alto.


def flood_white(rgba):
    h, w, _ = rgba.shape
    visited = np.zeros((h, w), dtype=bool)
    q = deque()

    def is_bg(x, y):
        r, g, b, a = rgba[y, x]
        if a <= 8:
            return True
        return r >= WHITE_THR and g >= WHITE_THR and b >= WHITE_THR

    def push(x, y):
        if visited[y, x] or not is_bg(x, y):
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


def kill_white_pockets(rgba):
    """Remove bolsoes de branco fechados que o flood da borda nao alcanca.
    So pega branco neutro (chroma baixo): highlights tan da madeira ficam."""
    rgb = rgba[..., :3].astype(np.int16)
    a = rgba[..., 3]
    val = rgb.max(2)
    chroma = rgb.max(2) - rgb.min(2)
    pocket = (a > 0) & (val >= POCKET_VAL) & (chroma <= POCKET_CHROMA)
    n = int(pocket.sum())
    if n:
        rgba[pocket, 3] = 0
    return n


def defringe_light(rgba):
    """Mata pixels muito claros na banda de borda (resto de branco antialiasado)."""
    alpha = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)
    transparent = alpha == 0
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel,
                       iterations=FRINGE_RADIUS).astype(bool)
    fringe = grown & ~transparent
    if not fringe.any():
        return 0
    luma = 0.299*rgb[...,0] + 0.587*rgb[...,1] + 0.114*rgb[...,2]
    opaque = (~transparent).astype(np.float32)
    box = (15, 15)
    s = cv2.boxFilter(luma*opaque, cv2.CV_32F, box, normalize=False)
    m = cv2.boxFilter(opaque, cv2.CV_32F, box, normalize=False)
    nb = np.where(m > 0, s/np.maximum(m, 1e-6), 0)
    kill = fringe & (luma >= LIGHT_LUMA_THR) & ((luma - nb) >= LIGHT_DELTA)
    n = int(kill.sum())
    if n: rgba[kill, 3] = 0
    return n


def decontaminate(rgba):
    """Substitui a cor da borda por media dos vizinhos opacos (tira sangria branca)."""
    alpha = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)
    transparent = alpha == 0
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel,
                       iterations=DECONTAM_RADIUS).astype(bool)
    edge = grown & ~transparent
    if not edge.any():
        return 0
    opaque = (~transparent).astype(np.float32)
    box = (9, 9)
    m = cv2.boxFilter(opaque, cv2.CV_32F, box, normalize=False)
    means = np.zeros_like(rgb)
    for c in range(3):
        s = cv2.boxFilter(rgb[..., c]*opaque, cv2.CV_32F, box, normalize=False)
        means[..., c] = np.where(m > 0, s/np.maximum(m, 1e-6), rgb[..., c])
    blended = rgb.copy()
    blended[edge] = 0.35*rgb[edge] + 0.65*means[edge]
    rgba[..., :3] = np.clip(blended, 0, 255).astype(np.uint8)
    return int(edge.sum())


def trim_bbox(rgba, pad=4):
    a = rgba[..., 3]
    mask = a >= ALPHA_OPAQUE
    if not mask.any():
        return rgba
    rows = np.any(mask, axis=1); cols = np.any(mask, axis=0)
    y0, y1 = np.where(rows)[0][[0, -1]]
    x0, x1 = np.where(cols)[0][[0, -1]]
    h, w, _ = rgba.shape
    y0 = max(0, y0-pad); x0 = max(0, x0-pad)
    y1 = min(h-1, y1+pad); x1 = min(w-1, x1+pad)
    return rgba[y0:y1+1, x0:x1+1]


def main():
    src, dst = sys.argv[1], sys.argv[2]
    rgba = np.array(Image.open(src).convert("RGBA"))
    print(f"Source: {src}  {rgba.shape[1]}x{rgba.shape[0]}")
    print(f"  flood (white bg)        : {flood_white(rgba)}")
    print(f"  kill white pockets      : {kill_white_pockets(rgba)}")
    print(f"  defringe (light halo)   : {defringe_light(rgba)}")
    print(f"  decontaminate edge band : {decontaminate(rgba)}")
    rgba = trim_bbox(rgba)
    print(f"  final bbox              : {rgba.shape[1]}x{rgba.shape[0]}")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(dst)
    print(f"Saved: {dst}")


if __name__ == "__main__":
    main()
