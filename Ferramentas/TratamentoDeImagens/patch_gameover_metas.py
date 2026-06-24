"""Ajusta os .meta dos sprites do GameOver para a convencao do projeto:
PPU 32, Single, FullRect, Point, Compression None. Edita in-place."""
import sys

BASE = (r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta"
        r"\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\UI\GameOver")
FILES = ["GameOverTitle.png.meta", "ButtonContinuar.png.meta", "ButtonDesistir.png.meta"]

SUBS = [
    ("    filterMode: 1\n", "    filterMode: 0\n"),          # Point
    ("  spriteMode: 2\n", "  spriteMode: 1\n"),               # Single
    ("  spriteMeshType: 1\n", "  spriteMeshType: 0\n"),       # FullRect
    ("  spritePixelsToUnits: 100\n", "  spritePixelsToUnits: 32\n"),
]
# Compressao none so na plataforma default (as outras tem overridden:0, herdam a default)
DEFAULT_BLOCK_OLD = ("buildTarget: DefaultTexturePlatform\n"
                     "    maxTextureSize: 2048\n"
                     "    resizeAlgorithm: 0\n"
                     "    textureFormat: -1\n"
                     "    textureCompression: 1\n")
DEFAULT_BLOCK_NEW = DEFAULT_BLOCK_OLD.replace("textureCompression: 1",
                                              "textureCompression: 0")

for name in FILES:
    path = f"{BASE}\\{name}"
    s = open(path, encoding="utf-8").read()
    report = []
    for old, new in SUBS:
        c = s.count(old)
        s = s.replace(old, new, 1)
        report.append(f"{old.strip()} x{c}")
    cb = s.count(DEFAULT_BLOCK_OLD)
    s = s.replace(DEFAULT_BLOCK_OLD, DEFAULT_BLOCK_NEW, 1)
    report.append(f"default-compression block x{cb}")
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(s)
    print(f"{name}: " + " | ".join(report))
