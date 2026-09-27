#!/usr/bin/env python3
"""Turn Ludo.ai downloads (768x768 "pixel style" images) into real pixel-art icons.

Ludo's images aren't on a clean pixel grid: thousands of colours, soft edges. For each image this script:
  1. crops to the art and fits it into the target size x4,
  2. snaps every pixel to the pack's own colours (Palette.png plus the colours the pack's icons and items use),
  3. picks the dominant colour of each 4x4 block, preferring the dark outline colour so outlines survive.

Images are matched by content (MD5), not file name, so Ludo's random gen-....png names and repeated
downloads don't matter. Re-runnable: output is overwritten from the raw files each time.

    python3 Tools/art_import/ludo_convert.py                 # scans ~/Downloads and _ludo_raw/
    python3 Tools/art_import/ludo_convert.py --unknown       # also list images it has no name for

Output: ~/Downloads/NinjaAdventure_AI/icons/<name>.png (native size; the import script upscales 8x later)
and one raw copy per image in ~/Downloads/NinjaAdventure_AI/_ludo_raw/<name>_ludo_raw.png.
"""
import argparse
import glob
import hashlib
import os
import shutil
from collections import Counter

from PIL import Image

PACK = os.path.expanduser("~/Downloads/NinjaAdventure")
AI = os.path.expanduser("~/Downloads/NinjaAdventure_AI")
RAW = os.path.join(AI, "_ludo_raw")
ICONS = os.path.join(AI, "icons")

TILE = dict(size=32)                              # icons on a coloured tile: talents, blessings, evolutions, menu
ITEM = dict(size=24)                              # loose objects: crates, pet gear
THIN = dict(size=24, opaque_min=4)                # thin shapes (the anklet ring) need a lower coverage threshold

# MD5 of the downloaded PNG -> (name, settings). Identified by eye from a contact sheet.
KNOWN = {
    "d0101ceab6c24c4a86387dbaaaca2fb9": ("menu_play", TILE),
    "0a28bc52283fdbc6d1e0b5efdc85d219": ("icon_blessing_precision", TILE),
    "27c03b7f54843d2b91cf73b53031f2ee": ("icon_blessing_wisdom", TILE),
    "8651b726ceeaf843007fc0dbc5cff765": ("icon_blessing_fortune", TILE),
    "e501cc54a378146eddf42a74a13d96f3": ("icon_blessing_vitality", TILE),
    "9447b69f1ee82271ad631b6d4f4f7480": ("icon_blessing_prosperity", TILE),
    "4cbb584ffd809bfd8d27f49ae29c423d": ("icon_blessing_iron_skin", TILE),
    "096365aba6ed52c8d3e8624095ec9473": ("icon_talent_sharpened_blades", TILE),
    "44eacd8a171bae4ba9b88235cb030fa6": ("icon_talent_swift_feet", TILE),
    "b86d93616f6da8cb5ef401a799229b5c": ("icon_talent_stone_skin", TILE),
    "b900f031ea437afdccdceafff454d276": ("icon_talent_keen_eye", TILE),
    "d8f94c363e62a218c1b0bd9ec307588e": ("icon_talent_merchant", TILE),
    "da082621294e530209311a9d35e637b3": ("icon_talent_storm_heart", TILE),
    "c2c35e1087f4e1c094f3d40912187337": ("icon_talent_quick_hands", TILE),
    "67d1c4316d60003107b7713902ca1405": ("icon_talent_lucky_star", TILE),
    "ca64a84227b2c5d925603f43e4b04eab": ("icon_talent_spirit_ward", TILE),
    "1e3fe396c1a514f8f918d950694c5346": ("icon_talent_meditation", TILE),
    "d298f2e81bfd2fbcba096d3d757a5f52": ("icon_talent_evasion", TILE),
    "6daf29a55836ac8b4b63d459a998938e": ("icon_talent_magnetism", TILE),
    "9a420f854315cb6ac6058c4ffe9d897a": ("icon_talent_iron_body", TILE),
    "c9003a109c9065fa48a9954d3b66b1ae": ("icon_talent_lethal_focus", TILE),
    "662fbbd7579f53b1c7d17e7096c33bcd": ("icon_petgear_ember", ITEM),
    "85c93a80a19427e667f2ac4f8e21b2f5": ("icon_petgear_bell", ITEM),
    "8e832c3152dd1df8296d80cf8c119cd1": ("icon_petgear_anklet", THIN),
    "307939bd52df8f50be1cb9e95a627aa9": ("icon_crate_epic", ITEM),
    "b2130935301c1208fc782de21bfb59bf": ("icon_crate_rare", ITEM),
    "9222ff609b3249152e69e2a729f29bcd": ("icon_crate_common", ITEM),
    "a5060d3855f090783c0e54b7de4b88a4": ("icon_evolution_shadow_army", dict(TILE, dark_min=8)),
    "f5fe517e4594b75856bc1589d3e6695c": ("icon_evolution_invisible_assassin", TILE),
    "7908faad536752d558f414743f806028": ("icon_petgear_collar", ITEM),
}


def pack_palette():
    """Every opaque colour in the pack's Palette.png, Ui/Skill Icon and Items (85 colours)."""
    files = [os.path.join(PACK, "Palette.png")]
    files += glob.glob(os.path.join(PACK, "Ui", "Skill Icon", "**", "*.png"), recursive=True)
    files += glob.glob(os.path.join(PACK, "Items", "**", "*.png"), recursive=True)
    colours = set()
    for f in files:
        if "Preview" in os.path.basename(f):
            continue
        img = Image.open(f).convert("RGBA")
        colours.update(c[:3] for c in img.getdata() if c[3] == 255)
    return sorted(colours)


class Snapper:
    def __init__(self, palette):
        self.palette = palette
        self.cache = {}

    def __call__(self, rgb):
        hit = self.cache.get(rgb)
        if hit is None:
            r, g, b = rgb

            def dist(p):  # "redmean" colour distance: cheap and close to how the eye judges it
                rm = (r + p[0]) / 2
                return (2 + rm / 256) * (r - p[0]) ** 2 + 4 * (g - p[1]) ** 2 + (2 + (255 - rm) / 256) * (b - p[2]) ** 2

            hit = self.cache[rgb] = min(self.palette, key=dist)
        return hit


def fit(img, size):
    """Crop to the art and centre it in a size x size canvas, keeping its proportions."""
    img = img.crop(img.getchannel("A").point(lambda a: 255 if a > 100 else 0).getbbox())
    scale = size / max(img.size)
    w, h = max(1, round(img.width * scale)), max(1, round(img.height * scale))
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(img.resize((w, h), Image.BOX), ((size - w) // 2, (size - h) // 2))
    return canvas


def convert(img, snap, size, oversample=4, opaque_min=5, dark_min=5):
    """Downsample to size x size: dominant snapped colour per block, keeping dark outlines."""
    big = fit(img, size * oversample).load()
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = out.load()
    for y in range(size):
        for x in range(size):
            block = (big[x * oversample + i, y * oversample + j] for i in range(oversample) for j in range(oversample))
            opaque = [snap(c[:3]) for c in block if c[3] >= 128]
            if len(opaque) < opaque_min:
                continue
            counts = Counter(opaque)
            colour = counts.most_common(1)[0][0]
            dark = [(c, n) for c, n in counts.items() if sum(c) < 130]
            if dark:
                dark_colour, dark_count = max(dark, key=lambda t: t[1])
                if dark_count >= dark_min:
                    colour = dark_colour
            px[x, y] = colour + (255,)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--unknown", action="store_true", help="list downloads that aren't in KNOWN")
    args = ap.parse_args()

    os.makedirs(RAW, exist_ok=True)
    os.makedirs(ICONS, exist_ok=True)
    sources = glob.glob(os.path.expanduser("~/Downloads/gen-*.png")) + glob.glob(os.path.join(RAW, "*.png"))
    sources += glob.glob(os.path.join(AI, "_downloads_raw", "ludo", "gen-*.png"))

    by_hash = {}
    for path in sorted(sources):
        with open(path, "rb") as f:
            by_hash.setdefault(hashlib.md5(f.read()).hexdigest(), path)

    snap = Snapper(pack_palette())
    done, unknown = [], []
    for digest, path in by_hash.items():
        if digest not in KNOWN:
            unknown.append(path)
            continue
        name, settings = KNOWN[digest]
        raw_copy = os.path.join(RAW, f"{name}_ludo_raw.png")
        if os.path.abspath(path) != os.path.abspath(raw_copy):
            shutil.copyfile(path, raw_copy)
        icon = convert(Image.open(raw_copy).convert("RGBA"), snap, **settings)
        icon.save(os.path.join(ICONS, f"{name}.png"))
        done.append(name)
        colours = len({c for c in icon.getdata() if c[3]})
        print(f"ok  {name}: {icon.width}x{icon.height}, {colours} colours")

    missing = sorted({n for n, _ in KNOWN.values()} - set(done))
    print(f"\n{len(done)} icons converted; {len(missing)} known names without a download: {missing}")
    if unknown:
        print(f"{len(unknown)} downloads aren't identified yet" + (":" if args.unknown else " (run with --unknown)"))
        if args.unknown:
            for path in unknown:
                print("   ", path)


if __name__ == "__main__":
    main()
