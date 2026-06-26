"""Corrige o rect do 1o sprite de cada .meta dos botoes do menu para cobrir a
imagem inteira (0,0,W,H). Necessario porque os PNGs foram sobrescritos com arte
de dimensao diferente, mas o sprite-sheet mantinha o recorte antigo (gera o
'fundo invisivel'). Preserva internalID/spriteID -> nao quebra a referencia da cena."""
import re
from PIL import Image

BASE = (r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta"
        r"\YbytuAIraDafloresta(VersaoInicial)\Assets\Sprites\UI\MainMenu")
NAMES = ["BotaoContinuar", "BotaoJogar", "BotaoTutorial", "BotaoConfig", "BotaoSair"]

# Casa o rect do PRIMEIRO sprite logo apos "sprites:"
PAT = re.compile(
    r"(    sprites:\n    - serializedVersion: 2\n      name: [^\n]+\n"
    r"      rect:\n        serializedVersion: 2\n"
    r"        x: )\d+(\n        y: )\d+(\n        width: )\d+(\n        height: )\d+"
)

for name in NAMES:
    png = f"{BASE}\\{name}.png"
    meta = f"{png}.meta"
    W, H = Image.open(png).size
    s = open(meta, encoding="utf-8").read()
    m = PAT.search(s)
    if not m:
        print(f"{name}: PADRAO NAO ENCONTRADO (verificar manualmente)")
        continue
    old = m.group(0).replace("\n", " ")
    s2 = PAT.sub(rf"\g<1>0\g<2>0\g<3>{W}\g<4>{H}", s, count=1)
    with open(meta, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(s2)
    print(f"{name}: img={W}x{H}  rect -> 0,0,{W},{H}")
