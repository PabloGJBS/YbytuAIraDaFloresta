"""Compara cor media (HSV + Lab) de regioes 'animal' vs 'chao' em cada prop,
pra medir se da pra separar por cor."""
import numpy as np
import cv2
from pathlib import Path
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Desktop/PropsAnimaisMortos_SemChao/_originais")

# Regioes (x0,y0,x1,y1) que parecem ser animal vs chao, por imagem.
REGIONS = {
    1: {"animal": [(150,230,260,310),(350,150,520,260)], "chao": [(480,380,620,440),(780,260,900,340)]},
    2: {"animal": [(280,300,460,420),(560,330,760,440)], "chao": [(120,120,260,260),(820,120,1000,260)]},
    3: {"animal": [(250,300,520,470)], "chao": [(120,520,300,600),(650,180,840,300)]},
    4: {"animal": [(430,120,640,260),(380,330,620,520)], "chao": [(120,560,320,680),(780,540,980,660)]},
    5: {"animal": [(170,160,420,360),(520,180,720,360)], "chao": [(120,460,320,560),(700,440,880,560)]},
}


def stats(rgb, lab, hsv, rects):
    px_h, px_s, px_v, px_L, px_a, px_b = [], [], [], [], [], []
    for (x0, y0, x1, y1) in rects:
        h = hsv[y0:y1, x0:x1].reshape(-1, 3)
        l = lab[y0:y1, x0:x1].reshape(-1, 3)
        px_h += list(h[:, 0]); px_s += list(h[:, 1]); px_v += list(h[:, 2])
        px_L += list(l[:, 0]); px_a += list(l[:, 1]); px_b += list(l[:, 2])
    f = lambda a: (np.mean(a), np.std(a))
    return f(px_h), f(px_s), f(px_v), f(px_L), f(px_a), f(px_b)


for idx, regs in REGIONS.items():
    img = Image.open(SRC / f"PropsAnimaisMortos{idx}.png").convert("RGBA")
    arr = np.array(img)
    bgr = cv2.cvtColor(arr[:, :, :3], cv2.COLOR_RGB2BGR)
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    lab = cv2.cvtColor(bgr, cv2.COLOR_BGR2LAB)
    print(f"\n=== Prop {idx} ===")
    for tag in ("animal", "chao"):
        H, S, V, L, A, B = stats(bgr, lab, hsv, regs[tag])
        print(f"  {tag:6}: H={H[0]:5.1f} S={S[0]:5.1f} V={V[0]:5.1f} | Lab L={L[0]:5.1f} a={A[0]:5.1f} b={B[0]:5.1f}")
