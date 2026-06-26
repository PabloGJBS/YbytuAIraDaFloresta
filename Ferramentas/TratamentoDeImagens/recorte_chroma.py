"""
Recorte por "pintura": o usuario pinta de uma cor chapada (default MAGENTA puro)
tudo que deve ser REMOVIDO da jaula (a parede de tras). O script remove essa cor
+ o fundo branco, limpa o halo da borda e recorta a bbox. Preserva as barras da
frente intactas, porque quem decidiu o que e frente/tras foi o pincel humano.

Cor-chave default: magenta (255,0,255) - nada na madeira/corda chega perto.
Alternativas seguras: verde (0,255,0) ou ciano (0,255,255).

IMPORTANTE (orientacao pro usuario):
- Salvar em PNG (NUNCA jpg: a compressao borra a cor e cria artefato no recorte).
- Pincel DURO (sem suavizar/anti-alias) deixa a borda mais limpa, mas nao precisa
  ser perfeito: o defringe/decontaminate cuida do resto.
- Pode pintar so a parede de tras; o fundo branco o script ja remove sozinho.

Usage:  python recorte_chroma.py <src> <dst> [--key magenta|green|cyan]
"""
import sys, os
from collections import deque
import numpy as np
import cv2
from PIL import Image

KEYS = {"magenta": (255, 0, 255), "green": (0, 255, 0), "cyan": (0, 255, 255)}
KEY_TOL    = 150     # distancia euclidiana ao chroma -> remove
WHITE_THR  = 228
ALPHA_OPAQUE = 200
FRINGE_RADIUS = 2
DECONTAM_RADIUS = 2


def remove_chroma(rgba, key):
    rgb = rgba[..., :3].astype(np.int32)
    a = rgba[..., 3]
    dist = np.sqrt(((rgb - np.array(key)) ** 2).sum(axis=2))
    kill = (a > 0) & (dist <= KEY_TOL)
    n = int(kill.sum())
    if n:
        rgba[kill, 3] = 0
    return n


def flood_white(rgba):
    h, w, _ = rgba.shape
    visited = np.zeros((h, w), bool)
    q = deque()

    def is_bg(x, y):
        r, g, b, al = rgba[y, x]
        if al <= 8:
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


def defringe_key(rgba, key):
    """Mata pixels na banda de borda que ainda tem componente forte da cor-chave
    (halo antialiasado magenta/verde sangrando na madeira)."""
    a = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.int32)
    transparent = a == 0
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    grown = cv2.dilate(transparent.astype(np.uint8), kernel,
                       iterations=FRINGE_RADIUS).astype(bool)
    fringe = grown & ~transparent
    if not fringe.any():
        return 0
    # "keyness": projecao na direcao da cor-chave vs cinza
    k = np.array(key, np.float32)
    kc = k - k.mean()
    proj = ((rgb - rgb.mean(axis=2, keepdims=True)) * kc).sum(axis=2)
    kill = fringe & (proj > 9000)
    n = int(kill.sum())
    if n:
        rgba[kill, 3] = 0
    return n


def decontaminate(rgba):
    a = rgba[..., 3]
    rgb = rgba[..., :3].astype(np.float32)
    transparent = a == 0
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
        s = cv2.boxFilter(rgb[..., c] * opaque, cv2.CV_32F, box, normalize=False)
        means[..., c] = np.where(m > 0, s / np.maximum(m, 1e-6), rgb[..., c])
    blended = rgb.copy()
    blended[edge] = 0.30 * rgb[edge] + 0.70 * means[edge]
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
    return rgba[max(0, y0 - pad):min(h, y1 + pad + 1),
                max(0, x0 - pad):min(w, x1 + pad + 1)]


def main():
    src, dst = sys.argv[1], sys.argv[2]
    keyname = "magenta"
    if "--key" in sys.argv:
        keyname = sys.argv[sys.argv.index("--key") + 1]
    key = KEYS[keyname]
    rgba = np.array(Image.open(src).convert("RGBA"))
    print(f"Source: {src}  {rgba.shape[1]}x{rgba.shape[0]}  key={keyname}{key}")
    print(f"  remove chroma (parede tras): {remove_chroma(rgba, key)}")
    print(f"  flood white (fundo)        : {flood_white(rgba)}")
    print(f"  defringe key halo          : {defringe_key(rgba, key)}")
    print(f"  decontaminate edge         : {decontaminate(rgba)}")
    rgba = trim_bbox(rgba)
    print(f"  final bbox                 : {rgba.shape[1]}x{rgba.shape[0]}")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(dst)
    print(f"Saved: {dst}")


if __name__ == "__main__":
    main()
