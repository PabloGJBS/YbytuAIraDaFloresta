"""Ajusta os .meta dos troncos interativos (PPU 256) e do background da StageScore
(PPU 100) para a convencao: Single, FullRect, Point, Compression None."""
PROJ = (r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta"
        r"\YbytuAIraDafloresta(VersaoInicial)\Assets")
TARGETS = {
    r"Sprites\Stage1\Tilesets\Props\TroncoInterativo1.png.meta": 256,
    r"Sprites\Stage1\Tilesets\Props\TroncoInterativo2.png.meta": 256,
    r"Sprites\Stage1\Tilesets\Props\TroncoInterativo3.png.meta": 256,
    r"Sprites\UI\StageScore\BackGroundFinalFase1.png.meta": 100,
}
DEFAULT_BLOCK_OLD = ("buildTarget: DefaultTexturePlatform\n"
                     "    maxTextureSize: 2048\n"
                     "    resizeAlgorithm: 0\n"
                     "    textureFormat: -1\n"
                     "    textureCompression: 1\n")

for rel, ppu in TARGETS.items():
    path = f"{PROJ}\\{rel}"
    s = open(path, encoding="utf-8").read()
    subs = [
        ("    filterMode: 1\n", "    filterMode: 0\n"),
        ("  spriteMode: 2\n", "  spriteMode: 1\n"),
        ("  spriteMeshType: 1\n", "  spriteMeshType: 0\n"),
        ("  spritePixelsToUnits: 100\n", f"  spritePixelsToUnits: {ppu}\n"),
    ]
    rep = []
    for old, new in subs:
        rep.append(f"{old.strip()} x{s.count(old)}")
        s = s.replace(old, new, 1)
    cb = s.count(DEFAULT_BLOCK_OLD)
    s = s.replace(DEFAULT_BLOCK_OLD, DEFAULT_BLOCK_OLD.replace("textureCompression: 1", "textureCompression: 0"), 1)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(s)
    print(f"{rel.split(chr(92))[-1]} (PPU {ppu}): " + " | ".join(rep) + f" | compr x{cb}")
