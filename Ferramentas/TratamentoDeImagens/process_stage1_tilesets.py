"""Pipeline para os tiles brutos de Stage 1.

1) Detecta + remove o fundo quadriculado de cada PNG.
2) Cropa pra alpha bbox (com 2px de padding).
3) Detecta o fator de upscale (run-length horizontal de cores iguais) - só relata,
   não baixa a resolução automaticamente. Decisão de downscale vai pro usuário.
4) Salva os processados em Assets/Sprites/Stage1/Tilesets/<nome>.png.
5) Gera um contact sheet (_ContactSheet.png) com todos os tiles dispostos numa
   grade pra revisão visual.

Sobre as imagens já em RGBA (TileSet5, TileSet7): pulamos a etapa de remoção do
checkerboard, mas ainda cropamos e analisamos upscale.
"""
from __future__ import annotations
import os
from collections import Counter
from PIL import Image, ImageDraw, ImageFont
import numpy as np

SRC_DIR = r"C:\Users\Administrador\Downloads\TileSetsFase1"
DST_DIR = (
    r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta"
    r"\YbytuAIraDafloresta(VersaoInicial)"
    r"\Assets\Sprites\Stage1\Tilesets"
)
SHEET_PATH = os.path.join(DST_DIR, "_ContactSheet.png")
os.makedirs(DST_DIR, exist_ok=True)


# --- 1. Checkerboard removal ----------------------------------------------------
def remove_checkerboard(arr: np.ndarray) -> np.ndarray:
    """Detect the gray checkerboard and zero its alpha.

    Heuristic: pixels that are near-gray (low chroma) and in the brightness band
    of the checker squares (~190 and ~255). Robust on these particular sources
    where the foreground rarely contains pure light grays.
    """
    if arr.shape[2] == 3:
        # add alpha 255
        h, w, _ = arr.shape
        alpha = np.full((h, w, 1), 255, dtype=np.uint8)
        arr = np.concatenate([arr, alpha], axis=2)

    r = arr[..., 0].astype(int)
    g = arr[..., 1].astype(int)
    b = arr[..., 2].astype(int)
    chroma = np.maximum(np.maximum(np.abs(r - g), np.abs(g - b)), np.abs(r - b))
    brightness = (r + g + b) // 3

    # The PIL checker on these sources spans brightness ~239-255 (the dark square
    # varies by image, light is ~254). Catch all near-grayscale pixels in that
    # band - the foreground in these tilesets has no near-white grays.
    is_checker = (chroma < 8) & (brightness >= 235)
    arr2 = arr.copy()
    arr2[is_checker, 3] = 0
    return arr2


# --- 2. Crop to alpha bbox ------------------------------------------------------
def crop_alpha_bbox(arr: np.ndarray, pad: int = 2) -> np.ndarray:
    alpha = arr[..., 3]
    ys, xs = np.where(alpha > 0)
    if len(ys) == 0:
        return arr
    y0, y1 = ys.min(), ys.max()
    x0, x1 = xs.min(), xs.max()
    h, w = arr.shape[:2]
    y0 = max(0, y0 - pad)
    y1 = min(h - 1, y1 + pad)
    x0 = max(0, x0 - pad)
    x1 = min(w - 1, x1 + pad)
    return arr[y0 : y1 + 1, x0 : x1 + 1]


# --- 3. Detect upscale factor ---------------------------------------------------
def detect_upscale(arr: np.ndarray) -> int:
    """Test candidate upscale factors K. For each K, count what fraction of KxK
    blocks (snapped to grid) are perfectly uniform. The largest K with a
    consistency above ~0.85 wins.

    Skip transparent blocks (alpha=0)."""
    h, w = arr.shape[:2]
    rgb = arr[..., :3]
    alpha = arr[..., 3]
    best = 1
    best_score = 0.0
    for k in (8, 6, 5, 4, 3, 2):
        if h < k * 4 or w < k * 4:
            continue
        # grid-snap
        bh = h // k
        bw = w // k
        cropped = rgb[: bh * k, : bw * k]
        a = alpha[: bh * k, : bw * k]
        blocks = cropped.reshape(bh, k, bw, k, 3)
        a_blocks = a.reshape(bh, k, bw, k)
        # block has any opaque pixel?
        any_opaque = (a_blocks > 0).any(axis=(1, 3))
        # uniform = all pixels equal to top-left of block, per channel
        ref = blocks[:, 0:1, :, 0:1, :]
        uniform = (blocks == ref).all(axis=(1, 3, 4))
        # only count opaque blocks
        eligible = any_opaque
        if eligible.sum() < 50:
            continue
        score = uniform[eligible].mean()
        if score > best_score and score > 0.85:
            best_score = score
            best = k
    return best


# --- 4. Pipeline ---------------------------------------------------------------
def process_one(src: str, name: str) -> dict:
    im = Image.open(src).convert("RGBA")
    arr = np.array(im)

    if im.mode == "RGBA" and arr[..., 3].min() < 255:
        # already had alpha - skip checkerboard removal but keep the alpha
        cleaned = arr
    else:
        cleaned = remove_checkerboard(arr)

    cropped = crop_alpha_bbox(cleaned, pad=2)
    upscale = detect_upscale(cropped)

    out_path = os.path.join(DST_DIR, f"{name}.png")
    Image.fromarray(cropped, "RGBA").save(out_path)

    return {
        "name": name,
        "src_size": im.size,
        "out_size": (cropped.shape[1], cropped.shape[0]),
        "upscale_guess": upscale,
        "out_path": out_path,
    }


# --- 5. Contact sheet ----------------------------------------------------------
def build_contact_sheet(reports: list[dict]) -> None:
    """Disposição em grade. Cada célula 480px de largura, altura proporcional.

    Cada item recebe um quadro com legenda <Nome> + dimensões + fator estimado.
    """
    cell_w = 480
    label_h = 60
    pad = 16
    cols = 4
    rows = (len(reports) + cols - 1) // cols

    # carrega todos pra calcular altura por linha (= maior altura escalada na linha)
    thumbs = []
    for r in reports:
        im = Image.open(r["out_path"]).convert("RGBA")
        ratio = cell_w / im.width
        new_h = max(1, int(im.height * ratio))
        thumb = im.resize((cell_w, new_h), Image.NEAREST)
        thumbs.append((thumb, r))

    row_heights = []
    for ri in range(rows):
        row_thumbs = thumbs[ri * cols : (ri + 1) * cols]
        max_h = max(t[0].height for t in row_thumbs)
        row_heights.append(max_h)

    sheet_w = cols * cell_w + (cols + 1) * pad
    sheet_h = sum(row_heights) + rows * (label_h + pad) + pad
    sheet = Image.new("RGBA", (sheet_w, sheet_h), (28, 28, 32, 255))
    draw = ImageDraw.Draw(sheet)

    try:
        font = ImageFont.truetype("arial.ttf", 18)
        font_small = ImageFont.truetype("arial.ttf", 14)
    except Exception:
        font = ImageFont.load_default()
        font_small = ImageFont.load_default()

    y = pad
    for ri in range(rows):
        row_thumbs = thumbs[ri * cols : (ri + 1) * cols]
        rh = row_heights[ri]
        for ci, (thumb, r) in enumerate(row_thumbs):
            x = pad + ci * (cell_w + pad)
            # frame
            draw.rectangle([x - 2, y - 2, x + cell_w + 2, y + rh + 2], outline=(60, 60, 70), width=2)
            # paste thumb top-left of cell
            sheet.alpha_composite(thumb, (x, y + (rh - thumb.height) // 2))
            # legend below
            ly = y + rh + 4
            draw.text((x, ly), r["name"], fill=(240, 240, 240), font=font)
            label2 = f'{r["src_size"][0]}x{r["src_size"][1]} -> {r["out_size"][0]}x{r["out_size"][1]}  (upscale ~{r["upscale_guess"]}x)'
            draw.text((x, ly + 22), label2, fill=(180, 180, 190), font=font_small)
        y += rh + label_h + pad
    sheet.save(SHEET_PATH)


def main() -> None:
    files = sorted(f for f in os.listdir(SRC_DIR) if f.lower().endswith(".png"))
    reports = []
    for f in files:
        name = os.path.splitext(f)[0]
        src = os.path.join(SRC_DIR, f)
        try:
            r = process_one(src, name)
            print(
                f"  OK {name:14s}  {r['src_size'][0]}x{r['src_size'][1]} -> "
                f"{r['out_size'][0]}x{r['out_size'][1]}   upscale~{r['upscale_guess']}x"
            )
            reports.append(r)
        except Exception as ex:
            print(f"  FAIL {name}: {ex}")
    build_contact_sheet(reports)
    print(f"\nContact sheet: {SHEET_PATH}")


if __name__ == "__main__":
    main()
