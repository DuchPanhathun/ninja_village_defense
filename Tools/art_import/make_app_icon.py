#!/usr/bin/env python3
"""Builds the app icon set from the AI-generated ninja face (re-runnable, Pillow only).

Source: ~/Downloads/NinjaAdventure_AI/store/app_icon.png (64x64 pixel art, ninja on a teal background)
plus the pack's giant shuriken. Everything is composed at native pixel size and upscaled NEAREST so
the icon stays crisp pixel art like the game.

Outputs
  Assets/_Project/Art/AppIcon/app_icon_background.png  432x432  Android adaptive background layer
  Assets/_Project/Art/AppIcon/app_icon_foreground.png  432x432  Android adaptive foreground layer
  Assets/_Project/Art/AppIcon/app_icon_legacy.png      512x512  square icon (legacy launchers, other platforms)
  Assets/_Project/Art/AppIcon/app_icon_round.png       512x512  round icon
  Tools/art_import/store/play_icon_512.png             512x512  Google Play listing icon
  Tools/art_import/app_icon_preview.png                         how launchers will mask it

AndroidBuilder.ApplyAppIcons assigns the Assets/ files to Player Settings on every build.
"""
import math
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SOURCE = os.path.expanduser("~/Downloads/NinjaAdventure_AI/store/app_icon.png")
SHURIKEN = os.path.join(ROOT, "Assets/_Project/Art/Sprites/Projectiles/projectile_giantshuriken_0.png")
OUT = os.path.join(ROOT, "Assets/_Project/Art/AppIcon")
STORE = os.path.join(ROOT, "Tools/art_import/store")

# Night-teal sunburst with a pale moon glow: the complement of the red hood, so the ninja pops on any
# home screen (the source art's own background was teal too).
RAY_LIGHT = (52, 128, 146)
RAY_DARK = (36, 98, 118)
EDGE = (16, 38, 56)
CENTER = (255, 232, 176)


def cut_out(face):
    """Flood-fills the teal background from the border (with tolerance) and drops stray teal specks."""
    face = face.convert("RGBA")
    w, h = face.size
    px = face.load()
    bg = px[0, 0][:3]

    def near_bg(c):
        return c[3] > 0 and sum((a - b) ** 2 for a, b in zip(c[:3], bg)) < 60 ** 2

    seen = set()
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h):
            continue
        seen.add((x, y))
        if not near_bg(px[x, y]):
            continue
        px[x, y] = (0, 0, 0, 0)
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    # Specks: teal-ish pixels with no opaque non-teal neighbour are background noise.
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if c[3] and near_bg(c):
                neighbours = [px[i, j] for i, j in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)) if 0 <= i < w and 0 <= j < h]
                if not any(n[3] and not near_bg(n) for n in neighbours):
                    px[x, y] = (0, 0, 0, 0)
    return face.crop(face.getbbox())


def lerp(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def sunburst(size, rays=14):
    """Native-resolution background: alternating rays, a warm centre glow and a crimson rim."""
    img = Image.new("RGBA", (size, size))
    px = img.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            dx, dy = x - c, y - c
            r = math.hypot(dx, dy) / (size / 2)
            angle = (math.atan2(dy, dx) + math.pi) / (2 * math.pi)
            base = RAY_LIGHT if int(angle * rays * 2) % 2 == 0 else RAY_DARK
            # Banded (not smooth) falloff keeps the pixel-art look.
            if r < 0.34:
                col = lerp(CENTER, base, round(r / 0.34 * 3) / 3)
            else:
                col = lerp(base, EDGE, min(1.0, round((r - 0.34) / 0.75 * 4) / 4))
            px[x, y] = col + (255,)
    return img


def silhouette(img, color, alpha):
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    src, dst = img.load(), out.load()
    for y in range(img.height):
        for x in range(img.width):
            if src[x, y][3] > 0:
                dst[x, y] = color + (alpha,)
    return out


def foreground(face, shuriken, canvas, shuriken_center):
    """Native-resolution foreground: shuriken behind the upper right, soft shadow, ninja face centred."""
    layer = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    layer.alpha_composite(shuriken, (shuriken_center[0] - shuriken.width // 2, shuriken_center[1] - shuriken.height // 2))
    fx, fy = (canvas - face.width) // 2, (canvas - face.height) // 2 + 1
    layer.alpha_composite(silhouette(face, (60, 12, 20), 110), (fx + 1, fy + 2))
    layer.alpha_composite(face, (fx, fy))
    return layer


def rounded_mask(size, radius):
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def circle_mask(size):
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, size - 1, size - 1), fill=255)
    return mask


def up(img, size):
    return img.resize((size, size), Image.NEAREST)


def main():
    if not os.path.exists(SOURCE):
        sys.exit(f"missing source art: {SOURCE}")
    face = cut_out(Image.open(SOURCE))
    shuriken = Image.open(SHURIKEN).convert("RGBA")
    shuriken = shuriken.resize((shuriken.width // 8, shuriken.height // 8), Image.NEAREST)  # back to native pixels
    print("face", face.size, "shuriken", shuriken.size)

    os.makedirs(OUT, exist_ok=True)
    os.makedirs(STORE, exist_ok=True)

    # Adaptive layers: 108dp canvas (432 px) of which launchers show ~72dp; the face fits the 66dp safe zone.
    up(sunburst(72), 432).save(os.path.join(OUT, "app_icon_background.png"))
    # 108 native px at 4x; launchers show the middle 72 (18..90), so the ~53 px face and the shuriken
    # (centred inside the visible circle) are never clipped.
    fg = up(foreground(face, shuriken, 108, (73, 35)), 432)
    fg.save(os.path.join(OUT, "app_icon_foreground.png"))

    # Full-bleed square / round / store versions: same art, the face a bit larger.
    full = up(sunburst(64), 512)
    full.alpha_composite(up(foreground(face, shuriken, 64, (51, 14)), 512))
    full.save(os.path.join(OUT, "app_icon_legacy.png"))
    full.save(os.path.join(STORE, "play_icon_512.png"))
    round_icon = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    round_icon.paste(full, (0, 0), circle_mask(512))
    round_icon.save(os.path.join(OUT, "app_icon_round.png"))

    # Preview: adaptive icon under circle / squircle masks, plus legacy and round, on a dark launcher.
    adaptive = Image.open(os.path.join(OUT, "app_icon_background.png")).convert("RGBA")
    adaptive.alpha_composite(fg)
    visible = adaptive.crop((72, 72, 360, 360)).resize((256, 256), Image.NEAREST)  # the 72dp launchers show
    sheet = Image.new("RGBA", (256 * 4 + 50, 306), (32, 48, 40, 255))
    tiles = [
        (visible, circle_mask(256)),
        (visible, rounded_mask(256, 80)),
        (full.resize((256, 256), Image.NEAREST), rounded_mask(256, 40)),
        (round_icon.resize((256, 256), Image.NEAREST), None),
    ]
    for i, (img, mask) in enumerate(tiles):
        sheet.paste(img, (10 + i * 266, 25), mask if mask is not None else img)
    sheet.save(os.path.join(ROOT, "Tools/art_import/app_icon_preview.png"))
    print("wrote", OUT)


if __name__ == "__main__":
    main()
