"""
Split Raider_3 (ImigoForte2) sprite sheets into individual 128x128 frames.
Output: Assets/Sprites_Temporarios/Sprites/Enemy-Raider3/<Anim>/<anim><N>.png
"""
from pathlib import Path
from PIL import Image

SRC = Path(r"C:/Users/Administrador/Downloads/craftpix-net-679950-free-raider-sprite-sheets-pixel-art/ImigoForte2")
DST = Path(r"D:/Apasta/Projetos/TCC/YbytuAIraDaFloresta/YbytuAIraDafloresta(VersaoInicial)/Assets/Sprites_Temporarios/Sprites/Enemy-Raider3")

SHEETS = [
    ("Idle.png",     "Idle",    "idle"),
    ("Idle_2.png",   "Idle2",   "idle2"),
    ("Walk.png",     "Walk",    "walk"),
    ("Run.png",      "Run",     "run"),
    ("Jump.png",     "Jump",    "jump"),
    ("Attack_1.png", "Punch",   "punch"),    # melee -> Punch slot
    ("Attack_2.png", "Attack2", "attack2"),
    ("Attack_3.png", "Attack3", "attack3"),
    ("Hurt.png",     "Hurt",    "hurt"),
    ("Dead.png",     "Dead",    "dead"),
]

FRAME_H = 128

def split_sheet(src_path: Path, out_dir: Path, base: str):
    img = Image.open(src_path).convert("RGBA")
    w, h = img.size
    assert h == FRAME_H, f"Expected 128 tall, got {h} for {src_path.name}"
    n = w // FRAME_H
    out_dir.mkdir(parents=True, exist_ok=True)
    for i in range(n):
        frame = img.crop((i * FRAME_H, 0, (i + 1) * FRAME_H, FRAME_H))
        frame.save(out_dir / f"{base}{i+1}.png")
    return n

total = 0
for sheet, folder, base in SHEETS:
    src = SRC / sheet
    if not src.exists():
        print(f"  SKIP (missing): {src}")
        continue
    n = split_sheet(src, DST / folder, base)
    print(f"  {sheet:14s} -> {folder:8s}  {n:2d} frames")
    total += n

print(f"\nTotal: {total} frames written under {DST}")
