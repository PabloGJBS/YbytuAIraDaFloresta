"""
Recolore os sprites do trator (boss) do esquema cartoon roxo/vermelho para
o amarelo de maquina de construcao da vida real (tipo CAT / escavadeira).

- Le os PNGs originais em EnemTractor/ (ignora _backup) SEM altera-los.
- Escreve a versao recolorida em EnemTractor_Yellow/ preservando a estrutura.

Parametros no topo pra calibrar a cada iteracao.

Uso:
    python recolor_tractor_yellow.py            # processa tudo
    python recolor_tractor_yellow.py --idle     # so o TractorIdle (preview rapido)
"""
import colorsys
import sys
from pathlib import Path
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Desktop/EnemTractor")
DST = Path(r"C:/Users/Administrador/Desktop/EnemTractor_Yellow")
ANIM_DIRS = [
    "TractorAttack1", "TractorAttack2", "TractorAttack3", "TractorAttack4",
    "TractorDeath", "TractorHurt", "TractorIdle", "TractorSpecial", "TractorWalk",
]
SHEETS = ["EnemyTractor.png", "EnemyTractor2.png"]

# ---------------------------------------------------------------------------
# PARAMETROS DE CALIBRACAO
# ---------------------------------------------------------------------------
# Faixa de matiz do "corpo quente" (cabine vermelha + corpo magenta/roxo).
# Cruza o 0: de WARM_HUE_MIN ... 360 ... WARM_HUE_MAX.
WARM_HUE_MIN = 250.0   # roxo
WARM_HUE_MAX = 35.0    # laranja/vermelho
WARM_SAT_MIN = 0.18    # ignora cinzas/pretos puros (outline)

# Alvo amarelo (CAT yellow ~ H45 S0.95 V1.0 = RGB 255,200,0)
YELLOW_HUE = 46.0
YELLOW_SAT = 0.95

# Remapeamento de valor: o corpo original e bem escuro (V~0.21). Clareia
# preservando shading relativo. new_v = V_LO + (v/V_REF clamp 1) * (V_HI-V_LO)
V_REF = 0.24   # valor original tido como "tom base do corpo"
V_LO = 0.48    # quao escuro fica a sombra mais funda do corpo
V_HI = 1.00    # quao claro fica o highlight

# Nas zonas bem claras, real-life amarelo desatura levemente (rumo ao branco).
DESAT_HIGHLIGHT = 0.35  # quanto reduzir a sat quando new_v ~ 1

# Partes azuis (escapamento, esteiras/rodas, vidros) viram aco cinza-escuro
# pra parecer metal real em vez de azul cartoon.
COOL_HUE_MIN = 180.0
COOL_HUE_MAX = 260.0
COOL_SAT_MIN = 0.18
STEEL_SAT = 0.06        # quase neutro
STEEL_VAL_FACTOR = 0.85  # escurece levemente pra ler como metal/sombra


def hue_in_range(h, lo, hi):
    return (lo <= h <= hi) if lo <= hi else (h >= lo or h <= hi)


def remap_body_value(v):
    t = min(1.0, v / V_REF)
    return V_LO + t * (V_HI - V_LO)


def recolor_pixel(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    h_deg = h * 360.0
    if s < WARM_SAT_MIN:
        return None  # cinza/preto: outline e sombras neutras, mantem
    # Azul -> aco cinza-escuro (metal real)
    if hue_in_range(h_deg, COOL_HUE_MIN, COOL_HUE_MAX) and s >= COOL_SAT_MIN:
        nr, ng, nb = colorsys.hsv_to_rgb(h, STEEL_SAT, v * STEEL_VAL_FACTOR)
        return (int(nr * 255), int(ng * 255), int(nb * 255))
    if not hue_in_range(h_deg, WARM_HUE_MIN, WARM_HUE_MAX):
        return None  # outros matizes: mantem
    new_v = remap_body_value(v)
    new_s = YELLOW_SAT
    if new_v > 0.85:
        new_s = YELLOW_SAT * (1.0 - DESAT_HIGHLIGHT * (new_v - 0.85) / 0.15)
    nr, ng, nb = colorsys.hsv_to_rgb(YELLOW_HUE / 360.0, new_s, new_v)
    return (int(nr * 255), int(ng * 255), int(nb * 255))


def process_image(src: Path, dst: Path):
    img = Image.open(src).convert("RGBA")
    px = img.load()
    changed = 0
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            out = recolor_pixel(r, g, b)
            if out is not None and out != (r, g, b):
                px[x, y] = (out[0], out[1], out[2], a)
                changed += 1
    dst.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst)
    return changed


def main():
    only_idle = "--idle" in sys.argv
    total_files = total_changed = 0
    dirs = ["TractorIdle"] if only_idle else ANIM_DIRS
    for d in dirs:
        for png in sorted((SRC / d).glob("*.png")):
            c = process_image(png, DST / d / png.name)
            total_files += 1
            total_changed += c
    if not only_idle:
        for sheet in SHEETS:
            sp = SRC / sheet
            if sp.exists():
                c = process_image(sp, DST / sheet)
                total_files += 1
                total_changed += c
    print(f"OK: {total_files} arquivos, {total_changed} pixels recoloridos -> {DST}")


if __name__ == "__main__":
    main()
