"""
Recolore o cabelo loiro do Chefe1 pra preto.

Detecta pixels com matiz amarela (35-65 graus em HSV) e saturacao/valor suficientes
pra serem cabelo (descarta pele e fundos). Aplica preto com variacao de
luminosidade pra preservar o shading original (highlights ficam cinza escuro,
sombras quase pretas).

Uso:
    python recolor_chefe_hair.py --test         # processa so um sprite pra validar
    python recolor_chefe_hair.py                # processa tudo

Saida:
    Sprites_Temporarios/Sprites/Enemy-Chefe1-BlackHair/<sub>/<arquivo>.png

Nao toca nos sprites originais.
"""
import argparse
import colorsys
from pathlib import Path
from PIL import Image

SRC_ROOT = Path("YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites_Temporarios/Sprites/Enemy-Chefe1")
DST_ROOT = Path("YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites_Temporarios/Sprites/Enemy-Chefe1-BlackHair")

# Faixa de matiz do cabelo loiro em graus (HSV 0-360).
HUE_MIN_DEG = 28.0
HUE_MAX_DEG = 65.0
# Limiares pra distinguir cabelo de pele/fundo (HSV 0-1).
SAT_MIN = 0.30
VAL_MIN = 0.30

# Funcao de remapeamento de luminosidade: original V -> nova V (no preto).
# Mantem shading: highlights ficam cinza escuro, sombras quase pretas.
def remap_value(v: float) -> float:
    # v in [0, 1]. Comprime drasticamente pra faixa 0.03 - 0.22.
    return 0.03 + 0.19 * v

def recolor_pixel(rgba: tuple) -> tuple:
    r, g, b, a = rgba
    if a == 0:
        return rgba
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    h_deg = h * 360.0
    if HUE_MIN_DEG <= h_deg <= HUE_MAX_DEG and s >= SAT_MIN and v >= VAL_MIN:
        new_v = remap_value(v)
        nr, ng, nb = colorsys.hsv_to_rgb(0.0, 0.0, new_v)
        return (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return rgba

def recolor_image(src: Path, dst: Path) -> int:
    img = Image.open(src).convert("RGBA")
    pixels = img.load()
    changed = 0
    for y in range(img.height):
        for x in range(img.width):
            old = pixels[x, y]
            new = recolor_pixel(old)
            if new != old:
                pixels[x, y] = new
                changed += 1
    dst.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst)
    return changed

def process_all(test_only: bool):
    if not SRC_ROOT.exists():
        raise SystemExit(f"Fonte nao encontrada: {SRC_ROOT}")

    total = 0
    if test_only:
        sample = SRC_ROOT / "Idle" / "idle1.png"
        if not sample.exists():
            raise SystemExit(f"Arquivo de teste nao encontrado: {sample}")
        rel = sample.relative_to(SRC_ROOT)
        dst = DST_ROOT / rel
        changed = recolor_image(sample, dst)
        print(f"TEST: {rel} -> {dst}  ({changed} pixels recoloridos)")
        return

    for sub in sorted(SRC_ROOT.iterdir()):
        if not sub.is_dir():
            continue
        for png in sorted(sub.glob("*.png")):
            rel = png.relative_to(SRC_ROOT)
            dst = DST_ROOT / rel
            changed = recolor_image(png, dst)
            total += 1
            print(f"{rel}: {changed} pixels recoloridos")
    print(f"\nDone. {total} sprites processados em {DST_ROOT}")

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--test", action="store_true", help="Processa so Idle/idle1.png como teste")
    args = ap.parse_args()
    process_all(args.test)
