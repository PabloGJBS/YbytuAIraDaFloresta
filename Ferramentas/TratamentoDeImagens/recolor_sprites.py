"""
Sistema generico de recolor de sprites por regioes HSV.

Cada preset define um source (pasta de origem), destination (pasta de saida)
e uma lista de regras. Cada regra detecta pixels por faixa de matiz/saturacao/valor
e aplica uma transformacao (substituir hue, dessaturar, etc).

Faixas de hue podem cruzar 0/360 (ex: 330-15). O codigo trata corretamente.

Uso:
    python recolor_sprites.py                 # roda todos os presets
    python recolor_sprites.py chefe1black     # roda so um
    python recolor_sprites.py --list          # lista presets
"""
import colorsys
import sys
from pathlib import Path
from typing import Callable
from PIL import Image

BASE = Path("YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites_Temporarios/Sprites")


def hue_in_range(h_deg: float, hue_min: float, hue_max: float) -> bool:
    """Aceita faixas que cruzam o 0/360, ex: 330-15."""
    if hue_min <= hue_max:
        return hue_min <= h_deg <= hue_max
    return h_deg >= hue_min or h_deg <= hue_max


def make_to_grayscale(value_remap_a: float, value_remap_b: float) -> Callable:
    """Transforma matched pixel em tom de cinza com V remapeado entre [a, b]."""
    def f(h, s, v):
        new_v = value_remap_a + (value_remap_b - value_remap_a) * v
        return (0.0, 0.0, new_v)
    return f


def make_shift_hue(target_hue_deg: float) -> Callable:
    """Mantem S e V, troca H pro target."""
    def f(h, s, v):
        return (target_hue_deg / 360.0, s, v)
    return f


def make_shift_hue_desat(target_hue_deg: float, sat_factor: float = 1.0, val_factor: float = 1.0) -> Callable:
    """Troca H, multiplica S por sat_factor e V por val_factor (clampa em 1)."""
    def f(h, s, v):
        return (target_hue_deg / 360.0, min(1.0, s * sat_factor), min(1.0, v * val_factor))
    return f


class Rule:
    def __init__(self, name, hue_min, hue_max, sat_min, val_min, transform: Callable):
        self.name = name
        self.hue_min = hue_min
        self.hue_max = hue_max
        self.sat_min = sat_min
        self.val_min = val_min
        self.transform = transform

    def matches(self, h_deg, s, v) -> bool:
        return hue_in_range(h_deg, self.hue_min, self.hue_max) and s >= self.sat_min and v >= self.val_min


PRESETS = {
    "chefe1black": {
        "src": BASE / "Enemy-Chefe1",
        "dst": BASE / "Enemy-Chefe1-BlackHair",
        "rules": [
            Rule("hair", 28, 65, 0.30, 0.30, make_to_grayscale(0.03, 0.22)),
            # Roupa: vermelho/magenta (cobre 330-15 cruzando o 0). Vira roxo (hue 280).
            Rule("clothes", 330, 15, 0.35, 0.15, make_shift_hue(280)),
        ],
    },
    "punkdark": {
        "src": BASE / "Enemy-Punk",
        "dst": BASE / "Enemy-Punk-Dark",
        "rules": [
            # Rosa e vermelho viram bordo escuro: hue 350 (red-magenta), sat reduzida, val reduzido.
            Rule("pink-red", 320, 15, 0.40, 0.20, make_shift_hue_desat(350, sat_factor=0.6, val_factor=0.55)),
        ],
    },
    "brawlerdark": {
        "src": BASE / "Brawler-Girl",
        "dst": BASE / "Brawler-Girl-Dark",
        "rules": [
            Rule("pink-red", 320, 15, 0.40, 0.20, make_shift_hue_desat(350, sat_factor=0.6, val_factor=0.55)),
        ],
    },
}


def recolor_image(src: Path, dst: Path, rules: list) -> int:
    img = Image.open(src).convert("RGBA")
    pixels = img.load()
    changed = 0
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            h_deg = h * 360.0
            new_hsv = None
            for rule in rules:
                if rule.matches(h_deg, s, v):
                    new_hsv = rule.transform(h, s, v)
                    break  # primeira regra que casa
            if new_hsv is None:
                continue
            nh, ns, nv = new_hsv
            nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, nv)
            new_pixel = (int(nr * 255), int(ng * 255), int(nb * 255), a)
            if new_pixel != (r, g, b, a):
                pixels[x, y] = new_pixel
                changed += 1
    dst.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst)
    return changed


def run_preset(name: str):
    cfg = PRESETS[name]
    src_root: Path = cfg["src"]
    dst_root: Path = cfg["dst"]
    rules = cfg["rules"]
    if not src_root.exists():
        print(f"  ! Fonte nao existe: {src_root}")
        return
    total = 0
    total_changed = 0
    for sub in sorted(src_root.iterdir()):
        if not sub.is_dir():
            continue
        for png in sorted(sub.glob("*.png")):
            rel = png.relative_to(src_root)
            dst = dst_root / rel
            changed = recolor_image(png, dst, rules)
            total += 1
            total_changed += changed
    print(f"[{name}] {total} sprites processados, {total_changed} pixels recoloridos no total -> {dst_root}")


if __name__ == "__main__":
    if "--list" in sys.argv:
        for name in PRESETS:
            cfg = PRESETS[name]
            print(f"  {name}: {cfg['src']} -> {cfg['dst']}")
        sys.exit(0)
    targets = [a for a in sys.argv[1:] if not a.startswith("--")]
    if not targets:
        targets = list(PRESETS.keys())
    for t in targets:
        if t not in PRESETS:
            print(f"Preset desconhecido: {t}")
            continue
        run_preset(t)
