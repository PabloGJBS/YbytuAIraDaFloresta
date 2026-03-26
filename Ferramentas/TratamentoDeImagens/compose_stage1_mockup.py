"""Mockup de composição da Stage 1 a partir dos tiles processados.

Camadas, do fundo pra frente:
  L1 sky/parallax distante  : TileSet3-1
  L2 silhuetas/midground    : TileSet1
  L3 banda de chão          : TileSet11-2 (apenas a faixa superior, raízes)
  L4 árvores fundo          : TileSet9-2 (queimada), TileSet10 (paliçada)
  L5 árvores médio          : TileSet5 (esq), TileSet8 (dir)
  L6 árvore central         : TileSet13 (sagrada com losangos)
  L7 borda foliagem (esq/dir): TileSet11-1
"""
from __future__ import annotations
import os
from PIL import Image

TILES = (
    r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta"
    r"\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\Stage1\Tilesets"
)
OUT = os.path.join(TILES, "_Composition.png")

W, H = 1920, 1080
GROUND_LINE = 920  # y onde fica a linha do chão jogável


def load(name: str) -> Image.Image:
    return Image.open(os.path.join(TILES, name + ".png")).convert("RGBA")


def fit_height(im: Image.Image, target_h: int) -> Image.Image:
    return im.resize((max(1, int(im.width * target_h / im.height)), target_h), Image.NEAREST)


def fit_width(im: Image.Image, target_w: int) -> Image.Image:
    return im.resize((target_w, max(1, int(im.height * target_w / im.width))), Image.NEAREST)


canvas = Image.new("RGBA", (W, H), (10, 8, 18, 255))


def paste(layer: Image.Image, x: int, y: int) -> None:
    canvas.alpha_composite(layer, (x, y))


# L1 - fundo distante (sky + fogo). Ocupa tela inteira em altura.
bg = fit_width(load("TileSet3-1"), W)
paste(bg, 0, H - bg.height)

# L2 - silhuetas com fogo (TileSet1). Empurra um pouco pra baixo pra deixar o céu visível.
mid = fit_width(load("TileSet1"), W)
paste(mid, 0, GROUND_LINE - mid.height + 200)

# L3 - banda de chão fina. Pega só a parte superior do TileSet11-2 (foliagem + raízes).
ground_full = fit_width(load("TileSet11-2"), W + 200)
ground_strip = ground_full.crop((0, 0, ground_full.width, int(ground_full.height * 0.42)))
paste(ground_strip, -100, GROUND_LINE - 80)

# L4 - árvore queimada atrás das árvores principais
burnt = fit_height(load("TileSet9-2"), 580)
paste(burnt, 320, GROUND_LINE - burnt.height + 60)

# Paliçada de troncos no fundo direito
palisade = fit_height(load("TileSet10"), 480)
paste(palisade, W - palisade.width - 360, GROUND_LINE - palisade.height + 80)

# L5 - árvores laterais
left_tree = fit_height(load("TileSet5"), 760)
paste(left_tree, 80, GROUND_LINE - left_tree.height + 100)
right_tree = fit_height(load("TileSet8"), 720)
paste(right_tree, W - right_tree.width - 100, GROUND_LINE - right_tree.height + 100)

# L6 - árvore sagrada central (TileSet13)
center = fit_height(load("TileSet13"), 760)
paste(center, (W - center.width) // 2, GROUND_LINE - center.height + 80)

# L7 - bordas com foliagem em colunas (TileSet11-1) cortadas em altura, só topo
edge = fit_height(load("TileSet11-1"), 540)
edge_top = edge.crop((0, 0, edge.width, int(edge.height * 0.6)))
paste(edge_top, -60, 0)
paste(edge_top.transpose(Image.FLIP_LEFT_RIGHT), W - edge_top.width + 60, 0)

canvas.save(OUT)
print("Saved:", OUT, canvas.size)
