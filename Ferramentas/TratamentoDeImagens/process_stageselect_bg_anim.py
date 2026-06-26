"""Importa os 8 frames do background animado da tela de seleção de fase.

Diferente do SlotFrame: estes são backgrounds RGB rectangulares 1672x941, sem alpha
e sem crop necessário. Pipeline mínimo:
1) abre cada PNG, garante mode RGBA pra ficar consistente com a paleta de sprites
   do projeto (alphaIsTransparency on no .meta).
2) salva em Assets/Sprites/UI/StageSelect/BackgroundAnim/StageSelectBackground_NN.png
3) gera .meta espelhado em BackgroundSaveSelect.png.meta (PPU 32, Point,
   FullRect, Compression None, Single, max 2048).

Usa GUIDs determinísticos a partir do nome do arquivo (md5) pra que rerunning o
script não troque GUIDs e quebre referências em cenas.
"""
from __future__ import annotations
import hashlib
import os
import shutil
from PIL import Image

SRC_DIR = r"C:\Users\Administrador\Downloads\SeletorDeFaseBackGroundImages"
DST_DIR = (
    r"D:\Apasta\Projetos\TCC\YbytuAIraDaFloresta"
    r"\YbytuAIraDafloresta(VersaoInicial)"
    r"\Assets\Sprites\UI\StageSelect\BackgroundAnim"
)
os.makedirs(DST_DIR, exist_ok=True)


def make_guid(seed: str) -> str:
    return hashlib.md5(seed.encode("utf-8")).hexdigest()


META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: iOS
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    sizes = []
    for i in range(1, 9):
        src = os.path.join(SRC_DIR, f"FaseSelectBackground{i}.png")
        if not os.path.isfile(src):
            raise FileNotFoundError(src)
        im = Image.open(src)
        sizes.append(im.size)
        if im.mode != "RGBA":
            im = im.convert("RGBA")
        out_name = f"StageSelectBackground_{i:02d}.png"
        out_path = os.path.join(DST_DIR, out_name)
        im.save(out_path, "PNG")
        guid = make_guid(f"StageSelect/BackgroundAnim/{out_name}")
        meta_path = out_path + ".meta"
        with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(META_TEMPLATE.format(guid=guid))
        print(f"  -> {out_name}  size={im.size}  guid={guid}")

    if len(set(sizes)) != 1:
        print(f"WARN: dimensões variam entre frames: {sizes}")
    else:
        print(f"OK - todos {len(sizes)} frames em {sizes[0]}")


if __name__ == "__main__":
    main()
