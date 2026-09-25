#!/usr/bin/env python3
"""Import the Ninja Adventure asset pack (Pixel-boy, CC0) plus our AI-generated extras into Unity.

    python3 Tools/art_import/import_ninja_adventure.py

Re-runnable: every run rebuilds the same files from the pack, and files a previous run wrote that are no
longer produced are removed (only those; nothing else in the project is touched). Unity creates the
.meta files itself on import.

Rules (from the art brief):
- Everything is upscaled 8x with NEAREST so 16 px pixel art becomes 128 px and stays sharp. The few
  images that would pass 2048 px at 8x (the fog overlay and the two wide dialog boxes) use 6x instead.
- One PNG per animation frame (the importer uses Single sprite mode); fully transparent frames are skipped.
- Characters: only the right-facing frames (the game flips for left). Bosses face the camera.
- Tilesets stay whole but are cut into pages of at most 16x16 tiles (2048 px) on the tile grid.

Inputs:  ~/Downloads/NinjaAdventure/ (unzipped from ~/Downloads/Ninja Adventure - Asset Pack.zip if missing)
         ~/Downloads/NinjaAdventure_AI/ (PixelLab / Retro Diffusion / Ludo extras, see GENERATION_LOG.md)
Outputs: Assets/_Project/Art/Sprites/**, Assets/_Project/Art/Sprites/ART_MAPPING.md,
         Assets/_Project/Art/CREDITS.md, Tools/art_import/preview.png, Tools/art_import/store/
"""
import colorsys
import os
import random
import re
import sys
import zipfile
from collections import Counter, OrderedDict, defaultdict

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from ludo_convert import Snapper, pack_palette  # noqa: E402  (same folder)

REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
ZIP = os.path.expanduser("~/Downloads/Ninja Adventure - Asset Pack.zip")
PACK = os.path.expanduser("~/Downloads/NinjaAdventure")
AI = os.path.expanduser("~/Downloads/NinjaAdventure_AI")
ART = os.path.join(REPO, "Assets", "_Project", "Art")
OUT = os.path.join(ART, "Sprites")
STORE = os.path.join(HERE, "store")
MANIFEST = os.path.join(HERE, ".last_outputs.txt")
SCALE = 8
MAX_SIZE = 2048

FOLDERS = ["Characters/Heroes", "Characters/Enemies", "Characters/Pets", "Characters/Mounts", "Projectiles", "Pickups", "UI/Icons",
           "UI/Buttons", "UI/Bars", "UI/Panels", "UI/MenuIcons", "Environment/Backgrounds",
           "Environment/Village", "Environment/Tiles", "VFX"]

written = []                         # every PNG this run produced (relative to OUT)
mapping = OrderedDict()              # section -> list of (item, source, outputs)
missing = []                         # (section, item, note)
snap = None                          # palette snapper for AI art, set in main()


# --------------------------------------------------------------------------------------------------
# Basic image helpers
# --------------------------------------------------------------------------------------------------
def ensure_pack():
    if os.path.isdir(PACK):
        return
    print(f"Unzipping {ZIP} -> {PACK}")
    prefix = "Ninja Adventure - Asset Pack/"
    with zipfile.ZipFile(ZIP) as z:
        for info in z.infolist():
            if not info.filename.startswith(prefix) or info.filename == prefix:
                continue
            dest = os.path.join(PACK, info.filename[len(prefix):])
            if info.filename.endswith("/"):
                os.makedirs(dest, exist_ok=True)
                continue
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with z.open(info) as src, open(dest, "wb") as dst:
                dst.write(src.read())


def load(rel):
    return Image.open(os.path.join(PACK, rel)).convert("RGBA")


def load_ai(rel, palette=True):
    """AI art is snapped to the pack's colours so it sits next to the pack sprites."""
    img = Image.open(os.path.join(AI, rel)).convert("RGBA")
    return snap_image(img) if palette else img


def ai_frames(prefix, first=0):
    """Numbered frames saved by pixellab_generate.py: <prefix>_0.png, _1.png ... (from `first`)."""
    frames, i = [], first
    while os.path.exists(os.path.join(AI, f"{prefix}_{i}.png")):
        frames.append(load_ai(f"{prefix}_{i}.png"))
        i += 1
    return frames


def crop(img, box):
    return img.crop(box)


def strip(img, fw=None, fh=None, n=None):
    """Split a horizontal strip into frames of fw x fh (fw defaults to a square frame, or width / n)."""
    fh = fh or img.height
    fw = fw or (img.width // n if n else fh)
    return [img.crop((i * fw, 0, i * fw + fw, fh)) for i in range(img.width // fw)]


def column(img, col, fw, fh, rows=None):
    rows = rows if rows is not None else range(img.height // fh)
    return [img.crop((col * fw, r * fh, col * fw + fw, r * fh + fh)) for r in rows]


def pad(img, w, h):
    """Centre img on a transparent w x h canvas."""
    if img.size == (w, h):
        return img
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.alpha_composite(img, ((w - img.width) // 2, (h - img.height) // 2))
    return canvas


def square(img, size=None):
    size = size or max(img.size)
    return pad(img, size, size)


def trim(img):
    box = img.getbbox()
    return img.crop(box) if box else img


def upscale(img, factor=SCALE):
    return img.resize((img.width * factor, img.height * factor), Image.NEAREST)


def snap_image(img):
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a < 128:
                px[x, y] = (0, 0, 0, 0)
            else:
                px[x, y] = snap((r, g, b)) + (255,)
    return out


PURPLE_HUE = 285 / 360


def purple(img):
    """NinjaDark -> purple assassin: shift body colours to purple, keep outline, red eyes/scarf and skin."""
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if v < 0.16 or (s > 0.35 and (h < 0.08 or h > 0.9)) or (0.03 < h < 0.14 and s > 0.25 and v > 0.55):
                continue
            nr, ng, nb = colorsys.hsv_to_rgb(PURPLE_HUE, min(1, max(s, 0.28) + 0.12), min(1, v * 1.05))
            px[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return out


def tintable(img):
    """Greyscale with the most common (fill) brightness mapped to white; outlines stay dark."""
    px = img.load()
    lum = lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]
    fill = Counter(round(lum(px[x, y])) for y in range(img.height) for x in range(img.width) if px[x, y][3]).most_common(1)[0][0]
    out = img.copy()
    o = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a:
                v = min(255, round(lum((r, g, b)) * 255 / max(1, fill)))
                o[x, y] = (v, v, v, a)
    return out


def remove_ground_shadow(img, colour=(80, 80, 79), tolerance=4):
    """PixelLab sometimes adds a flat grey shadow stripe under animals; drop it (bottom 2 rows only)."""
    box = img.getbbox()
    if not box:
        return img
    out = img.copy()
    px = out.load()
    for y in range(max(0, box[3] - 2), box[3]):
        for x in range(out.width):
            c = px[x, y]
            if c[3] and all(abs(c[i] - colour[i]) <= tolerance for i in range(3)):
                px[x, y] = (0, 0, 0, 0)
    return out


# --------------------------------------------------------------------------------------------------
# Writing
# --------------------------------------------------------------------------------------------------
def save(img, folder, name, factor=SCALE):
    rel = f"{folder}/{name}.png"
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    upscale(img, factor).save(path, optimize=True)
    written.append(rel)
    return name


def save_frames(frames, folder, prefix, factor=SCALE):
    """Save non-empty frames as prefix_0..N (renumbered so skipped frames leave no gaps)."""
    names = []
    for f in frames:
        if f.getbbox() is None:
            continue
        names.append(save(f, folder, f"{prefix}_{len(names)}", factor))
    return names


def record(section, item, source, outputs):
    mapping.setdefault(section, []).append((item, source, summarize(outputs)))


def summarize(names):
    """hero_x_run_0..3 style summary of a list of output names."""
    groups = OrderedDict()
    for n in names:
        m = re.match(r"(.*)_(\d+)$", n)
        key, idx = (m.group(1), int(m.group(2))) if m else (n, None)
        groups.setdefault(key, []).append(idx)
    parts = []
    for key, idxs in groups.items():
        numbered = [i for i in idxs if i is not None]
        if None in idxs:
            parts.append(f"`{key}`")
        if len(numbered) == 1:
            parts.append(f"`{key}_{numbered[0]}`")
        elif numbered:
            parts.append(f"`{key}_{min(numbered)}..{max(numbered)}`")
    return ", ".join(parts)


# --------------------------------------------------------------------------------------------------
# Characters
# --------------------------------------------------------------------------------------------------
def std_right(char, sheet_file="SpriteSheet.png"):
    """Right-facing frames of a standard 16x16 character (columns = down, up, left, RIGHT), plus its front (down) idle."""
    base = f"Actor/Character/{char}"
    sep = os.path.join(PACK, base, "SeparateAnim")
    sheet = load(f"{base}/{sheet_file}")
    walk = column(load(f"{base}/SeparateAnim/Walk.png"), 3, 16, 16) if os.path.exists(os.path.join(sep, "Walk.png")) \
        else column(sheet, 3, 16, 16, range(4))
    idle = [load(f"{base}/SeparateAnim/Idle.png").crop((48, 0, 64, 16))] if os.path.exists(os.path.join(sep, "Idle.png")) \
        else walk[:1]
    attack = [load(f"{base}/SeparateAnim/Attack.png").crop((48, 0, 64, 16))]
    dead = [load(f"{base}/SeparateAnim/Dead.png")] if os.path.exists(os.path.join(sep, "Dead.png")) else [sheet.crop((0, 96, 16, 112))]
    front = [load(f"{base}/SeparateAnim/Idle.png").crop((0, 0, 16, 16))] if os.path.exists(os.path.join(sep, "Idle.png")) \
        else [sheet.crop((0, 0, 16, 16))]
    return dict(idle=idle, walk=walk, attack=attack, dead=dead, front=front)


def monster_right(rel):
    """64x64 monster sheets: columns are directions, column 3 faces right, rows are the 4 walk frames."""
    walk = column(load(rel), 3, 16, 16, range(4))
    return dict(idle=walk[:1], walk=walk)


def hero_frames():
    """hero id -> (state -> frames, source note). Shared by the heroes and their skins."""
    heroes = OrderedDict()
    for hero, char, sheet, tint in [("assassin", "NinjaDark", "SpriteSheet.png", purple),
                                    ("samurai", "SamuraiRed", "redsamurai.png", None),
                                    ("monk", "Monk2", "SpriteSheet.png", None)]:
        s = std_right(char, sheet)
        fix = tint or (lambda im: im)
        states = OrderedDict(idle=s["idle"], run=s["walk"], attack=s["attack"],
                             hurt=ai_frames(f"heroes/hero_{hero}_hurt", first=1),   # frame 0 is the pack's idle pose
                             death=s["dead"], front=s["front"])   # front: facing the camera (Equipment showcase)
        states = OrderedDict((k, [fix(f) for f in v]) for k, v in states.items())
        note = " (recoloured purple)" if tint else ""
        heroes[hero] = (states, f"`Actor/Character/{char}`{note}; hurt: AI (PixelLab, animated from the pack sprite)")

    # Beast Ninja: the pack's only fully animated character, 32x32 cells, column 3 faces right.
    base = "Actor/CharacterAnimated/NinjaGreen/Separate"
    heroes["beastninja"] = (OrderedDict(
        idle=column(load(f"{base}/Idle.png"), 3, 32, 32), run=column(load(f"{base}/Walk.png"), 3, 32, 32),
        attack=column(load(f"{base}/Attack.png"), 3, 32, 32), hurt=column(load(f"{base}/Hit.png"), 3, 32, 32),
        death=column(load(f"{base}/Dead.png"), 0, 32, 32), front=column(load(f"{base}/Idle.png"), 0, 32, 32)),
        f"`{base}` (32x32 cells, so frames are 256 px)")

    # Mage Ninja: AI (PixelLab); frame 0 of each animation is the base pose.
    heroes["mageninja"] = (OrderedDict(
        idle=ai_frames("heroes/hero_mageninja_idle"), run=ai_frames("heroes/hero_mageninja_walk"),
        attack=ai_frames("heroes/hero_mageninja_attack"), hurt=ai_frames("heroes/hero_mageninja_hurt", first=1),
        death=ai_frames("heroes/hero_mageninja_death")), "AI (PixelLab): `hero_mageninja_*`")
    return heroes


def import_heroes():
    F = "Characters/Heroes"
    heroes = hero_frames()
    for hero, (states, source) in heroes.items():
        out = []
        for state, frames in states.items():
            out += save_frames(frames, F, f"hero_{hero}_{state}")
        record("Heroes", {"beastninja": "Beast Ninja", "mageninja": "Mage Ninja"}.get(hero, hero.title()), source, out)
    import_skins(heroes)


# Skins recolour a hero's costume (one hue band) toward the tint on the skin's asset in Data/Store/Skins.
# band = source hues in degrees (wraps past 360), hue = target hue, sat/val = multipliers.
SKINS = [("skin_assassin_crimson", "assassin", (240, 320), 355, 1.15, 1.0),
         ("skin_assassin_sakura", "assassin", (240, 320), 330, 0.55, 1.4),
         ("skin_samurai_oni", "samurai", (330, 15), 355, 1.0, 0.6),
         ("skin_samurai_gold", "samurai", (330, 15), 45, 1.0, 1.2),
         ("skin_monk_jade", "monk", (330, 15), 145, 0.9, 0.95),
         ("skin_beast_shadow", "beastninja", (50, 160), 265, 0.6, 0.65),
         ("skin_mage_frost", "mageninja", (180, 260), 195, 0.55, 1.3)]


def recolour(img, band, hue, sat, val):
    lo, hi = band
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            deg = h * 360
            inside = lo <= deg <= hi if lo < hi else (deg >= lo or deg <= hi)
            if v < 0.16 or s < 0.2 or not inside:
                continue                                     # outline, greys, skin and eyes keep their colour
            nr, ng, nb = colorsys.hsv_to_rgb(hue / 360, min(1, s * sat), min(1, v * val))
            px[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return out


EXTRA_PASSES = {"skin_samurai_oni": ((30, 60), 350, 0.5, 0.45)}   # oni: the straw hat turns charcoal red too


def import_skins(heroes):
    F = "Characters/Heroes"
    for skin, hero, band, hue, sat, val in SKINS:
        states, _ = heroes[hero]
        out = []
        for state, frames in states.items():
            frames = [recolour(f, band, hue, sat, val) for f in frames]
            if skin in EXTRA_PASSES:
                frames = [recolour(f, *EXTRA_PASSES[skin]) for f in frames]
            out += save_frames(frames, F, f"{skin}_{state}")
        record("Skins (recolours, no AI)", skin, f"hero `{hero}` recoloured toward the tint in `Data/Store/Skins/Skin_{skin}.asset`", out)


def import_enemies():
    F = "Characters/Enemies"
    for enemy, char in [("bandit", "MaskRacoon"), ("oni", "DemonRed"), ("skeletonninja", "Skeleton"),
                        ("cursedsamurai", "RedGladiator")]:
        s = std_right(char)
        out = save_frames(s["idle"], F, f"enemy_{enemy}_idle") + save_frames(s["walk"], F, f"enemy_{enemy}_walk")
        record("Enemies", enemy, f"`Actor/Character/{char}`", out)
    for enemy, rel in [("spider", "Actor/Monster/SpiderRed/SpriteSheet.png"), ("ghost", "Actor/Monster/Spirit/SpriteSheet.png"),
                       ("giant", "Actor/Monster/Cyclope2/SpriteSheet.png"), ("dragon", "Actor/Monster/Dragon/SpriteSheet.png")]:
        s = monster_right(rel)
        out = save_frames(s["idle"], F, f"enemy_{enemy}_idle") + save_frames(s["walk"], F, f"enemy_{enemy}_walk")
        record("Enemies", enemy + (" (extra: the game has a Dragon enemy)" if enemy == "dragon" else ""), f"`{rel}`", out)
    out = save_frames(ai_frames("enemies/enemy_wolf_idle"), F, "enemy_wolf_idle")
    out += save_frames(ai_frames("enemies/enemy_wolf_walk"), F, "enemy_wolf_walk")
    record("Enemies", "wolf", "AI (PixelLab): `enemy_wolf_*`", out)


def import_bosses():
    F = "Characters/Enemies"

    def boss(name, anims, source):
        """anims: state -> frames. All of a boss's frames share one canvas so its feet don't jump."""
        w = max(f.width for fr in anims.values() for f in fr)
        h = max(f.height for fr in anims.values() for f in fr)
        out = []
        for state, frames in anims.items():
            out += save_frames([pad(f, w, h) for f in frames], F, f"boss_{name}_{state}")
        record("Bosses", name, source + f" (all frames padded to {w}x{h})", out)

    for name, folder in [("giantoni", "GiantRedSamurai"), ("shadowninja", "GiantBlueSamurai")]:
        b = f"Actor/Boss/{folder}"
        boss(name, OrderedDict(idle=strip(load(f"{b}/Idle.png"), 96, 48), walk=strip(load(f"{b}/Walk.png"), 96, 48),
                               attack=strip(load(f"{b}/AttackRight.png"), 96, 96), hurt=strip(load(f"{b}/Hit.png"), 96, 48),
                               charge=strip(load(f"{b}/ChargeRight.png"), 96, 96)), f"`{b}` (attack/charge: the *Right files)")
    b = "Actor/Boss/TenguRed"
    boss("demonking", OrderedDict(idle=strip(load(f"{b}/Idle.png"), 82), walk=strip(load(f"{b}/Walk.png"), 82),
                                  attack=strip(load(f"{b}/Attack.png"), 82), hurt=strip(load(f"{b}/Hit.png"), 82),
                                  transform=strip(load(f"{b}/Trans.png"), 82)), f"`{b}`")
    for name in ["spiderqueen", "ninetailedfox"]:
        boss(name, OrderedDict(idle=ai_frames(f"bosses/boss_{name}_idle"), walk=ai_frames(f"bosses/boss_{name}_walk"),
                               attack=ai_frames(f"bosses/boss_{name}_attack")), f"AI (PixelLab): `boss_{name}_*`")


def import_pets():
    F = "Characters/Pets"
    fox = lambda frames: [remove_ground_shadow(f) for f in frames]  # noqa: E731
    out = save_frames(fox(ai_frames("pets/pet_fox_idle")), F, "pet_fox_idle") + save_frames(fox(ai_frames("pets/pet_fox_walk")), F, "pet_fox_walk")
    record("Pets", "fox", "AI (PixelLab): `pet_fox_*` (grey ground shadow removed)", out)
    out = save_frames(ai_frames("pets/pet_hawk_idle"), F, "pet_hawk_idle") + save_frames(ai_frames("pets/pet_hawk_walk"), F, "pet_hawk_walk")
    record("Pets", "hawk", "AI (PixelLab): `pet_hawk_*`", out)
    for pet, rel, fw, fh in [("wolf", "Actor/Animal/DogBlack/SpriteSheet.png", 18, 17),
                             ("monkey", "Actor/Animal/Monkey/SpriteSheetBrown.png", 16, 16)]:
        frames = strip(load(rel), fw, fh)                                 # 2-frame side strips, already facing right
        out = save_frames(frames[:1], F, f"pet_{pet}_idle") + save_frames(frames, F, f"pet_{pet}_walk")
        record("Pets", pet, f"`{rel}` (2-frame strip; idle = frame 0)", out)
    s = monster_right("Actor/Monster/DragonYellow/SpriteSheet.png")
    out = save_frames(s["idle"], F, "pet_babydragon_idle") + save_frames(s["walk"], F, "pet_babydragon_walk")
    record("Pets", "baby dragon", "`Actor/Monster/DragonYellow/SpriteSheet.png`", out)


def import_portraits():
    F = "UI/Icons"
    faces = [("hero", "assassin", "Actor/Character/NinjaDark/Faceset.png", purple), ("hero", "samurai", "Actor/Character/SamuraiRed/Faceset.png", None),
             ("hero", "monk", "Actor/Character/Monk2/Faceset.png", None), ("hero", "beastninja", "Actor/Character/NinjaGreen/Faceset.png", None),
             ("enemy", "bandit", "Actor/Character/MaskRacoon/Faceset.png", None), ("enemy", "oni", "Actor/Character/DemonRed/Faceset.png", None),
             ("enemy", "skeletonninja", "Actor/Character/Skeleton/Faceset.png", None), ("enemy", "cursedsamurai", "Actor/Character/RedGladiator/Faceset.png", None),
             ("enemy", "spider", "Actor/Monster/SpiderRed/Faceset.png", None), ("enemy", "ghost", "Actor/Monster/Spirit/Faceset.png", None),
             ("enemy", "giant", "Actor/Monster/Cyclope2/Faceset.png", None), ("enemy", "dragon", "Actor/Monster/Dragon/Faceset.png", None),
             ("boss", "giantoni", "Actor/Boss/GiantRedSamurai/Faceset.png", None), ("boss", "shadowninja", "Actor/Boss/GiantBlueSamurai/Faceset.png", None),
             ("boss", "demonking", "Actor/Boss/TenguRed/Faceset.png", None), ("pet", "wolf", "Actor/Animal/DogBlack/Faceset.png", None),
             ("pet", "monkey", "Actor/Animal/Monkey/FacesetBrown.png", None), ("pet", "babydragon", "Actor/Monster/DragonYellow/Faceset.png", None)]
    for kind, name, rel, tint in faces:
        img = load(rel)
        out = [save(tint(img) if tint else img, F, f"portrait_{kind}_{name}")]
        record("Portraits (extra)", f"{kind} {name}", f"`{rel}`", out)
    for who in ["hero Mage Ninja", "enemy wolf", "boss Spider Queen", "boss Nine-Tailed Fox", "pet fox", "pet hawk"]:
        missing.append(("Portraits (extra)", who, "AI character, the pack has no face for it"))


# --------------------------------------------------------------------------------------------------
# Icons, projectiles, pickups
# --------------------------------------------------------------------------------------------------
def snake(name):
    return re.sub(r"[^a-z0-9]+", "_", re.sub(r"(?<=[a-z0-9])([A-Z])", r"_\1", name).lower()).strip("_")


def import_icons():
    F = "UI/Icons"
    for weapon, rel in [("kunai", "Items/Projectile/Kunai.png"), ("shuriken", "Items/Projectile/Shuriken.png"),
                        ("katana", "Items/Weapons/Katana/Sprite.png"), ("bow", "Items/Weapons/Bow/Sprite.png")]:
        out = [save(square(load(rel), 16), F, f"icon_weapon_{weapon}")]
        record("Weapon and item icons", f"weapon {weapon}", f"`{rel}` (centred on 16x16)", out)
    out = [save(load_ai("icons/icon_weapon_chainsickle.png"), F, "icon_weapon_chainsickle")]
    record("Weapon and item icons", "weapon chain sickle", "AI (PixelLab)", out)

    for group, prefix in [("Items & Weapon", "icon_item"), ("Spell", "icon_spell"), ("Job & Action", "icon_action"), ("Meteo", "icon_weather")]:
        folder = os.path.join(PACK, "Ui", "Skill Icon", group)
        out = []
        for f in sorted(os.listdir(folder)):
            if f.endswith(".png") and "Disabled" not in f:
                out.append(save(load(f"Ui/Skill Icon/{group}/{f}"), F, f"{prefix}_{snake(f[:-4])}"))
        record("Weapon and item icons", f"pack skill icons: {group}", f"`Ui/Skill Icon/{group}` (24x24; greyed Disabled copies skipped)", out)

    # AI icons: saved as-is (Retro Diffusion/PixelLab ones snapped to the pack palette; Ludo ones already converted).
    groups = defaultdict(list)
    for f in sorted(os.listdir(os.path.join(AI, "icons"))):
        name = f[:-4]
        if not f.endswith(".png") or name.endswith("_rd") or name == "icon_weapon_chainsickle" or name.startswith("menu_"):
            continue
        ludo = os.path.exists(os.path.join(AI, "_ludo_raw", f"{name}_ludo_raw.png"))
        img = load_ai(f"icons/{f}", palette=not ludo)
        kind = name.split("_")[1]
        groups[kind].append(save(img, F, name))
    labels = dict(equip="equipment (6)", skill="skills the pack lacks (5)", ultimate="ultimates (3)", evolution="evolutions (4)",
                  talent="talents", blessing="shrine blessings (6)", petgear="pet gear", crate="loot crates (3)")
    for kind, out in groups.items():
        record("AI icons", labels.get(kind, kind), "AI: Retro Diffusion / Ludo / PixelLab (see GENERATION_LOG.md)", out)
    if "icon_talent_scholar" not in groups.get("talent", []):
        missing.append(("AI icons", "talent scholar", "not generated yet"))

    # Logo emblem for the title screen.
    out = [save(load_ai("store/logo_emblem.png"), F, "logo_emblem")]
    record("Store and title", "logo emblem", "AI (PixelLab)", out)
    out = [save(load_ai("store/app_icon.png", palette=False), F, "app_icon")]
    record("Store and title", "app icon (512 px, for Player Settings)", "AI (PixelLab)", out)


def import_menu_icons():
    F = "UI/MenuIcons"
    reuse = [("play", None, "AI (Ludo)"), ("shop", "Items/Object/MoneyBag.png", None), ("quests", "Items/Scroll/Scroll.png", None),
             ("achievements", "Items/Treasure/GoldCup.png", None), ("collection", "Items/Object/Book.png", None),
             ("events", "Items/Object/Hourglass.png", None), ("battle_pass", "Items/Treasure/GoldKey.png", None),
             ("leaderboard", "Items/Treasure/SilverCup.png", None), ("account", "Items/Other/Letter.png", None),
             ("gear", "Ui/Skill Icon/Items & Weapon/Armor.png", None), ("talents", "Ui/Skill Icon/Spell/AttackUpgrade.png", None),
             ("settings", "Ui/Skill Icon/Job & Action/Repair.png", None), ("heroes", "Actor/Character/NinjaRed/Faceset.png", None),
             ("pets", "Actor/Animal/CatOrange/Faceset.png", None), ("profile", "Actor/Character/NinjaBlue/Faceset.png", None)]
    for name, rel, ai_src in reuse:
        img = load_ai(f"icons/menu_{name}.png", palette=False) if ai_src else square(load(rel))
        out = [save(img, F, f"menu_{name}")]
        record("Menu icons", name, ai_src or f"`{rel}` (reused)", out)
    daily = strip(load("Items/Treasure/LittleTreasureChest.png"), 16, 16)[0]
    record("Menu icons", "daily", "`Items/Treasure/LittleTreasureChest.png` frame 0 (reused)", [save(daily, F, "menu_daily")])
    if os.path.exists(os.path.join(AI, "icons", "menu_village.png")):
        record("Menu icons", "village", "AI (PixelLab)", [save(load_ai("icons/menu_village.png"), F, "menu_village")])
    else:
        missing.append(("Menu icons", "village", "no house icon in the pack; generate `menu_village` (prompt in AI_ASSET_PROMPTS.md)"))


# --------------------------------------------------------------------------------------------------
# Composites built from pack art (no AI): achievement badges and store items, 32x32
# --------------------------------------------------------------------------------------------------
OUTLINE = (20, 27, 27, 255)            # #141b1b, the pack's outline colour


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) + (255,)


def place(canvas, img, cx, cy):
    """Paste img (trimmed) centred on (cx, cy)."""
    img = trim(img)
    canvas.alpha_composite(img, (cx - img.width // 2, cy - img.height // 2))


def hue_shift(img, hue, sat=1.0, val=1.0):
    return recolour(img, (0, 360), hue, sat, val)


def medal(inner=hexc("#3b3643")):
    """32x32 gold medal on a red ribbon, drawn pixel by pixel in the pack's palette."""
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    px = img.load()
    red, red_dark = hexc("#e0394c"), hexc("#8f3e56")
    for y in range(20, 32):                            # two ribbon tails with a notched end
        for side, x0 in ((-1, 9), (1, 17)):
            for x in range(x0, x0 + 6):
                notch = y >= 29 and abs(x - (x0 + 2.5)) < (y - 28)
                if not notch:
                    px[x, y] = red if (x - x0) in (1, 2, 3, 4) else red_dark
    for y in range(20, 32):                            # ribbon outline
        for x in range(32):
            if px[x, y][3] and any(not (0 <= x + dx < 32 and 0 <= y + dy < 32) or not px[x + dx, y + dy][3]
                                   for dx, dy in ((1, 0), (-1, 0), (0, 1))):
                px[x, y] = OUTLINE
    cx, cy = 15.5, 13.5
    rim_light, rim, rim_dark = hexc("#ffe18d"), hexc("#f1c471"), hexc("#d78b4a")
    for y in range(28):
        for x in range(32):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if d <= 13.4:
                if d > 12.3:
                    px[x, y] = OUTLINE
                elif d > 10.2:
                    px[x, y] = rim_light if (x + y) < 26 else rim_dark if (x + y) > 34 else rim
                elif d > 9.3:
                    px[x, y] = OUTLINE
                else:
                    px[x, y] = inner
    return img


ACHIEVEMENTS = [("survivor", "Items/Object/Hourglass.png"), ("smith", "Items/Tool/Anvil.png"),
                ("veteran", "Items/Weapons/Katana/Sprite.png"), ("treasure", ("Items/Treasure/LittleTreasureChest.png", 1)),
                ("sensei", "Items/Object/Book.png"), ("champion", "Items/Treasure/GoldCup.png"),
                ("collector", "Items/Resource/GemRed.png"), ("boss_hunter", "Items/Weapons/Bone/Sprite.png"),
                ("wave_breaker", "Items/Resource/Water.png"), ("demon_slayer", "Items/Weapons/BigSword/Sprite.png"),
                ("diligent", "Items/Other/Stamp.png"), ("scholar", "Items/Scroll/Scroll.png"),
                ("architect", "Items/Tool/Hammer.png"), ("loyal", "Items/Potion/Heart.png"), ("beast_friend", "Items/Food/Meat.png")]


def item(src):
    if isinstance(src, tuple):                       # (strip, frame index) for 2-frame items like the chest
        rel, i = src
        return strip(load(rel), 16, 16)[i]
    return load(src)


def import_achievements():
    out = []
    for name, src in ACHIEVEMENTS:
        badge = medal()
        place(badge, item(src), 16, 14)
        out.append(save(badge, "UI/Icons", f"icon_achievement_{name}"))
    record("Achievement badges (composites, no AI)", "15 achievements",
           "gold medal drawn in the pack palette + a pack item: " + ", ".join(
               f"{n}={s if isinstance(s, str) else s[0]}".replace("Items/", "") for n, s in ACHIEVEMENTS), out)


def text_ad():
    """'AD' in a 3x5 pixel font, drawn 2x: for the remove-ads icon."""
    glyphs = {"A": [".#.", "#.#", "###", "#.#", "#.#"], "D": ["##.", "#.#", "#.#", "#.#", "##."]}
    img = Image.new("RGBA", (14, 10), (0, 0, 0, 0))
    for gi, ch in enumerate("AD"):
        for y, row in enumerate(glyphs[ch]):
            for x, c in enumerate(row):
                if c == "#":
                    for dx in range(2):
                        for dy in range(2):
                            img.putpixel((gi * 8 + x * 2 + dx, y * 2 + dy), (255, 255, 255, 255))
    return img


def import_store_items():
    F = "UI/Icons"
    gem = hue_shift(load("Items/Resource/GemRed.png"), 200, 1.0, 1.05)         # faceted gem in light blue, like "Gems" in the UI
    coin = load("Items/Treasure/GoldCoin.png")
    chest_open = strip(load("Items/Treasure/LittleTreasureChest.png"), 16, 16)[1]
    sparkle = strip(load("FX/Particle/Spark.png"), 10, 8)[0]
    petals = strip(load("FX/Particle/LeafPink.png"), 12, 7)
    leaves = [hue_shift(f, 25, 1.1, 1.0) for f in strip(load("FX/Particle/Leaf.png"), 12, 7)]

    def canvas():
        return Image.new("RGBA", (32, 32), (0, 0, 0, 0))

    items = OrderedDict()
    c = canvas(); place(c, gem, 11, 18); place(c, gem, 20, 15); items["gems_100"] = c
    c = canvas()
    for x, y in [(8, 22), (16, 22), (24, 22), (12, 15), (20, 15), (16, 8)]:
        place(c, gem, x, y)
    items["gems_550"] = c
    c = canvas(); place(c, chest_open, 16, 21)
    for x, y in [(10, 11), (16, 8), (22, 11)]:
        place(c, gem, x, y)
    place(c, sparkle, 27, 5); items["gems_1200"] = c
    c = canvas(); place(c, load("Items/Object/Bag.png"), 14, 17); place(c, coin, 25, 24); place(c, coin, 21, 27)
    place(c, gem, 25, 11); items["starter_pack"] = c
    c = canvas()
    screen = Image.new("RGBA", (24, 18), OUTLINE)
    screen.paste(hexc("#4a5270"), (1, 1, 23, 17))
    place(c, screen, 16, 16)
    px = c.load()
    for y in range(32):                                  # red "no" circle with a slash, drawn under the letters
        for x in range(32):
            d = ((x - 15.5) ** 2 + (y - 15.5) ** 2) ** 0.5
            if 12.2 < d <= 14.8 or (d <= 13 and abs((x - 15.5) - (y - 15.5)) <= 1.2):
                px[x, y] = hexc("#e0394c")
    c.alpha_composite(text_ad(), (9, 11))
    items["remove_ads"] = c
    c = canvas(); place(c, hue_shift(load("Items/Scroll/ScrollEmpty.png"), 45, 1.2, 1.1), 16, 17)
    place(c, sparkle, 6, 6); place(c, sparkle, 26, 9); place(c, load("Items/Treasure/GoldKey.png"), 22, 25)
    items["battle_pass_premium"] = c
    c = canvas(); place(c, hue_shift(load("Items/Object/MoneyBag.png"), 330, 0.6, 1.25), 14, 16); place(c, coin, 25, 25)
    place(c, coin, 21, 28); place(c, petals[0], 25, 7); place(c, petals[2], 6, 26); items["offer_blossom_coins"] = c
    c = canvas(); place(c, hue_shift(load("Items/Object/Bag.png"), 330, 0.6, 1.25), 16, 18)
    for (x, y), p in zip([(6, 6), (25, 8), (27, 25), (5, 24)], petals):
        place(c, p, x, y)
    items["offer_sakura_bundle"] = c
    c = canvas(); oni = load("Actor/Character/DemonRed/SpriteSheet.png").crop((0, 0, 16, 16))
    place(c, oni, 12, 12); place(c, load("Items/Weapons/Katana/Sprite.png"), 25, 16); place(c, load("Items/Potion/LifePot.png"), 10, 25)
    items["offer_oni_kit"] = c
    c = canvas(); place(c, load("Items/Object/CrateEmpty.png"), 16, 19)
    for (x, y), lf in zip([(7, 7), (25, 6), (27, 27), (5, 26)], leaves):
        place(c, lf, x, y)
    items["offer_autumn_training"] = c
    c = canvas(); lantern = load("Actor/Monster/LanternRed/SpriteSheet.png").crop((0, 0, 16, 16))
    draw = ImageDraw.Draw(c); draw.line([(2, 6), (16, 9), (30, 6)], fill=hexc("#816855"))
    place(c, lantern, 8, 17); place(c, lantern, 24, 17); place(c, sparkle, 16, 25)
    items["offer_lantern_pack"] = c

    out = [save(img, F, f"store_{name}") for name, img in items.items()]
    record("Store items (composites, no AI)", "gem packs, starter pack, remove ads, battle pass, 5 offers",
           "pack items combined (gems recoloured light blue; pink/gold/orange recolours for the themed offers)", out)


def import_projectiles():
    F = "Projectiles"
    singles = [("kunai", "FX/Projectile/Kunai.png"), ("arrow", "FX/Projectile/Arrow.png"), ("bigkunai", "FX/Projectile/BigKunai.png"),
               ("bomb", "Items/Projectile/Bomb.png")]
    for name, rel in singles:
        record("Projectiles", name, f"`{rel}` (points right)" if "Bomb" not in rel else f"`{rel}`", [save(load(rel), F, f"projectile_{name}")])
    for name, rel in [("shuriken", "FX/Projectile/Shuriken.png"), ("giantshuriken", "FX/Projectile/BigShuriken.png"),
                      ("shurikenmagic", "FX/Projectile/ShurikenMagic.png"), ("fireball", "FX/Projectile/Fireball.png"),
                      ("energyball", "FX/Projectile/EnergyBall.png"), ("bigenergyball", "FX/Projectile/BigEnergyBall.png")]:
        record("Projectiles", name, f"`{rel}` (spin/flicker frames)", save_frames(strip(load(rel)), F, f"projectile_{name}"))


def import_pickups():
    F = "Pickups"
    record("Pickups", "coin (spinning)", "`Items/Treasure/Coin2.png`", save_frames(strip(load("Items/Treasure/Coin2.png"), 10, 10), F, "pickup_coin"))
    record("Pickups", "coin (static)", "`Items/Treasure/GoldCoin.png`", [save(load("Items/Treasure/GoldCoin.png"), F, "pickup_coin")])
    record("Pickups", "silver coin", "`Items/Treasure/SilverCoin.png`", [save(load("Items/Treasure/SilverCoin.png"), F, "pickup_coin_silver")])
    for tier, rel in [("small", "Items/Resource/GemGreen.png"), ("medium", "Items/Resource/GemPurple.png"), ("large", "Items/Resource/GemRed.png")]:
        record("Pickups", f"XP gem {tier}", f"`{rel}`", [save(load(rel), F, f"pickup_xp_{tier}")])
    record("Pickups", "heart (heal)", "`Items/Potion/Heart.png`", [save(load("Items/Potion/Heart.png"), F, "pickup_heart")])
    record("Pickups", "chest closed/open", "`Items/Treasure/LittleTreasureChest.png`",
           save_frames(strip(load("Items/Treasure/LittleTreasureChest.png"), 16, 16), F, "pickup_chest"))
    record("Pickups", "boss chest closed/open", "`Items/Treasure/BigTreasureChest.png`",
           save_frames(strip(load("Items/Treasure/BigTreasureChest.png"), 16, 14), F, "pickup_bigchest"))


# --------------------------------------------------------------------------------------------------
# UI
# --------------------------------------------------------------------------------------------------
def import_ui():
    wood = os.path.join(PACK, "Ui", "Theme", "Theme Wood")
    buttons, panels, bars = [], [], []
    for f in sorted(os.listdir(wood)):
        if not f.endswith(".png"):
            continue
        n = f[:-4].replace("slidder", "slider")
        img = load(f"Ui/Theme/Theme Wood/{f}")
        if n.startswith(("nine_path", "inventory_cell")):
            panels.append(save(img, "UI/Panels", "panel_wood_" + n.replace("nine_path_", "")))
        elif "slider" in n:
            bars.append(save(img, "UI/Bars", n))
        else:
            buttons.append(save(img, "UI/Buttons", n if n.startswith(("button", "arrow", "tab")) else "toggle_" + n))
    # Greyscale copies whose main fill is pure white: tinting them with any UI colour keeps the pixel-art
    # frame, bevel and dark outline (the code-built UI tints its buttons and cards per state).
    buttons.append(save(tintable(load("Ui/Theme/Theme Wood/button_normal.png")), "UI/Buttons", "button_tint"))
    panels.append(save(tintable(load("Ui/Theme/Theme Wood/nine_path_panel.png")), "UI/Panels", "panel_tint"))
    record("UI", "buttons, arrows, toggles, tabs", "`Ui/Theme/Theme Wood` (nine-slice friendly; `button_tint`/`panel_tint` are greyscale tintable copies)", buttons)
    record("UI", "panels", "`Ui/Theme/Theme Wood` nine_path_* and inventory_cell", panels)
    record("UI", "sliders", "`Ui/Theme/Theme Wood` slider/grabber", bars)

    out = []
    for theme in sorted(os.listdir(os.path.join(PACK, "Ui", "Theme", "Wip"))):
        out.append(save(load(f"Ui/Theme/Wip/{theme}/nine_path_panel.png"), "UI/Panels", "panel_" + snake(theme.replace("Theme", ""))))
    record("UI", "panel colour themes", "`Ui/Theme/Wip/*/nine_path_panel.png`", out)

    out = []
    for f, name in [("ChoiceBox", "dialog_choice"), ("DialogBox", "dialog_box"), ("DialogBoxFaceset", "dialog_box_faceset"),
                    ("DialogInfo", "dialog_info"), ("DialogueBoxSimple", "dialog_box_simple"), ("FacesetBox", "dialog_faceset_frame")]:
        img = load(f"Ui/Dialog/{f}.png")
        factor = SCALE if max(img.size) * SCALE <= MAX_SIZE else 6
        out.append(save(img, "UI/Panels", name, factor))
    record("UI", "dialog boxes", "`Ui/Dialog` (the 300+ px wide boxes at 6x to stay under 2048)", out)
    out = [save(load("Ui/Dialog/YesButton.png"), "UI/Buttons", "button_yes"), save(load("Ui/Dialog/NoButton.png"), "UI/Buttons", "button_no")]
    record("UI", "yes / no buttons", "`Ui/Dialog`", out)

    rec = "Ui/Receptacle"
    out = [save(load(f"{rec}/LifeBarMiniUnder.png"), "UI/Bars", "bar_hp_bg"), save(load(f"{rec}/LifeBarMiniProgress.png"), "UI/Bars", "bar_hp_fill"),
           save(recolour(load(f"{rec}/LifeBarMiniProgress.png"), (0, 360), 200, 0.9, 1.1), "UI/Bars", "bar_xp_fill"),
           save(load(f"{rec}/IconHeart.png"), "UI/Bars", "bar_heart_icon")]
    out += save_frames(strip(load(f"{rec}/Heart.png"), 16, 16), "UI/Bars", "bar_heart")
    out += save_frames(strip(load(f"{rec}/Heart2.png"), 16, 16), "UI/Bars", "bar_heart2")
    out += save_frames(strip(load(f"{rec}/Heart3.png"), 16, 16), "UI/Bars", "bar_heart3")
    record("UI", "HP bar, hearts", f"`{rec}` (Heart strips split into fill states)", out)
    for sub, prefix in [("Receptacle Sphere", "bar_orb"), ("Receptacle Rectangle", "bar_rect")]:
        out = []
        for f in sorted(os.listdir(os.path.join(PACK, rec, sub))):
            if f.endswith(".png") and "Preview" not in f:
                out.append(save(load(f"{rec}/{sub}/{f}"), "UI/Bars", f"{prefix}_{snake(f[:-4])}"))
        record("UI", f"gauges ({sub})", f"`{rec}/{sub}`", out)


# --------------------------------------------------------------------------------------------------
# VFX
# --------------------------------------------------------------------------------------------------
VFX = [  # (output name, pack file, frame count or None for square frames)
    ("hit", "FX/Magic/Circle/SpriteSheetSpark.png", None), ("hit_ring", "FX/Magic/Circle/SpriteSheetWhite.png", None),
    ("hit_ring_orange", "FX/Magic/Circle/SpriteSheetOrange.png", None), ("hit_spark", "FX/Magic/Circle/SpriteSheetSpark2.png", None),
    ("slash_circular_small", "FX/Attack/CircularSlash/SpriteSheet.png", None), ("claw", "FX/Attack/Claw/SpriteSheet.png", None),
    ("claw_double", "FX/Attack/ClawDouble/SpriteSheet.png", None), ("cut", "FX/Attack/Cut/SpriteSheet.png", None),
    ("cut_double", "FX/Attack/CutDouble/SpriteSheet.png", None), ("cut_x", "FX/Attack/CutX/SpriteSheet.png", None),
    ("slash_curved", "FX/Attack/SlashCurved/SpriteSheet.png", None), ("slash_double_curved", "FX/Attack/SlashDoubleCurved/SpriteSheet.png", None),
    ("explosion", "FX/Elemental/Explosion/SpriteSheet.png", None), ("fire", "FX/Elemental/Flam/SpriteSheet.png", 8),
    ("ice", "FX/Elemental/Ice/SpriteSheet.png", None), ("ice_b", "FX/Elemental/Ice/SpriteSheetB.png", None),
    ("ice_flake", "FX/Elemental/Ice/SpriteSheetFlake.png", None), ("plant", "FX/Elemental/Plant/SpriteSheet.png", 8),
    ("plant_b", "FX/Elemental/Plant/SpriteSheetB.png", 7), ("rock", "FX/Elemental/Rock/SpriteSheet.png", 14),
    ("rock_b", "FX/Elemental/Rock/SpriteSheetB.png", 14), ("rock_spike", "FX/Elemental/RockSpike/SpriteSheet.png", 10),
    ("thunder", "FX/Elemental/Thunder/SpriteSheet.png", 8), ("water", "FX/Elemental/Water/SpriteSheet.png", 10),
    ("water_pillar", "FX/Elemental/WaterPillar/SpriteSheet.png", 9), ("aura", "FX/Magic/Aura/SpriteSheet.png", 5),
    ("levelup", "FX/Magic/Boost/SpriteSheet.png", 8), ("shield_blue", "FX/Magic/Shield/SpriteSheetBlue.png", 6),
    ("shield_yellow", "FX/Magic/Shield/SpriteSheetYellow.png", 6), ("sparkle", "FX/Magic/Spark/SpriteSheet.png", 10),
    ("spirit", "FX/Magic/Spirit/SpriteSheet.png", None), ("spirit_blue", "FX/Magic/Spirit/SpriteSheetBlue.png", None),
    ("spirit_double", "FX/Magic/Spirit/SpriteSheetDouble.png", None), ("slash_arc", "FX/Slash/SpriteSheetArc.png", 6),
    ("slash_circular", "FX/Slash/SpriteSheetCircular.png", 3), ("slash_multi", "FX/Slash/SpriteSheetMulti.png", 6),
    ("slash_01", "FX/Slash/SpriteSheetSlash01.png", 5), ("slash_02", "FX/Slash/SpriteSheetSlash02.png", 6),
    ("slash_03", "FX/Slash/SpriteSheetSlash03.png", 4), ("smoke", "FX/Smoke/Smoke/SpriteSheet.png", None),
    ("smoke_ring", "FX/Smoke/SmokeCircular/SpriteSheet.png", 8),
    ("particle_bamboo", "FX/Particle/Bamboo.png", 3), ("particle_clouds", "FX/Particle/Clouds.png", 1),
    ("particle_fire", "FX/Particle/Fire.png", 12), ("particle_grass", "FX/Particle/Grass.png", 6),
    ("particle_leaf", "FX/Particle/Leaf.png", 6), ("particle_leaf_pink", "FX/Particle/LeafPink.png", 6),
    ("particle_rain", "FX/Particle/Rain.png", 3), ("particle_rain_splash", "FX/Particle/RainOnFloor.png", 4),
    ("particle_rock", "FX/Particle/Rock.png", 5), ("particle_rock_gray", "FX/Particle/RockGray.png", 5),
    ("particle_snow", "FX/Particle/Snow.png", 7), ("particle_spark", "FX/Particle/Spark.png", 7),
    ("particle_vase", "FX/Particle/Vase.png", 6), ("particle_wood", "FX/Particle/Wood.png", 6),
]


def import_vfx():
    for name, rel, n in VFX:
        img = load(rel)
        frames = strip(img, n=n) if n else strip(img)
        kind = "variants (not an animation)" if name.startswith("particle_") else f"{len(frames)} frames"
        record("VFX", name, f"`{rel}` ({kind})", save_frames(frames, "VFX", f"vfx_{name}"))


# --------------------------------------------------------------------------------------------------
# Environment
# --------------------------------------------------------------------------------------------------
TILESETS = [("floor", "TilesetFloor.png"), ("floor_b", "TilesetFloorB.png"), ("floor_detail", "TilesetFloorDetail.png"),
            ("field", "TilesetField.png"), ("water", "TilesetWater.png"), ("relief", "TilesetRelief.png"),
            ("relief_detail", "TilesetReliefDetail.png"), ("nature", "TilesetNature.png"), ("house", "TilesetHouse.png"),
            ("village_abandoned", "TilesetVillageAbandoned.png"), ("towers", "TilesetTowers.png"), ("camp", "tileset_camp.png"),
            ("element", "TilesetElement.png")]
PAGE = MAX_SIZE // SCALE  # 256 source px = 16 tiles of 16 px


def import_tiles():
    for short, f in TILESETS:
        img = load(f"Backgrounds/Tilesets/{f}")
        img = img.crop((0, 0, img.width - img.width % 16, img.height - img.height % 16))  # TilesetFloor is 417 px tall
        out, origins = [], []
        for py in range(0, img.height, PAGE):
            for px in range(0, img.width, PAGE):
                page = img.crop((px, py, min(px + PAGE, img.width), min(py + PAGE, img.height)))
                if page.getbbox() is None:
                    continue
                out.append(save(page, "Environment/Tiles", f"tiles_{short}_{len(out)}"))
                origins.append(f"{len(out) - 1}: tiles {px // 16},{py // 16}")
        paged = f"; {len(out)} pages ({', '.join(origins)})" if len(out) > 1 else ""
        record("Tiles", short, f"`Backgrounds/Tilesets/{f}` (slice at 128x128{paged})", out)
    missing.append(("Tiles", "desert, dungeon, interior, pipes, logic, bed, hole tilesets", "skipped: not used by this game (add to TILESETS if needed)"))


# Hand-picked rectangles (source px, on the 16 px grid); each is trimmed to its art.
BUILDINGS = [("dojo", (400, 112, 464, 224)), ("dojo_sign", (64, 64, 96, 80)), ("forge", (464, 64, 512, 128)),
             ("shrine", (0, 80, 48, 112)), ("market", (256, 0, 304, 48)), ("pethouse", (48, 128, 96, 160)),
             ("house_orange", (0, 0, 64, 48)), ("house_tan", (64, 0, 128, 48)),
             ("house_red", (192, 0, 256, 48)), ("shop_green", (304, 0, 368, 48)), ("house_adobe", (368, 0, 416, 48)),
             ("house_tall", (416, 0, 464, 48)), ("house_wood", (464, 0, 528, 64)), ("hut_wood", (0, 112, 48, 160)),
             # extra house styles for the buildable houses (EPIC 24 Phase 4)
             ("house_orange_b", (128, 0, 192, 48)), ("hut_straw", (48, 128, 96, 160)), ("house_timber", (304, 304, 352, 352)),
             ("igloo", (0, 176, 48, 224))]
HOUSE_PROPS = [("statue_guardian", (16, 240, 48, 288)), ("statue_frog", (48, 272, 80, 304)), ("stone_arch", (464, 320, 528, 368))] + \
              [(f"banner_{c}", (352 + 16 * i, 320, 368 + 16 * i, 352)) for i, c in enumerate(["white", "red", "orange", "green", "red_b", "purple", "yellow"])]
NATURE = [("tree_green", (0, 0, 32, 32)), ("tree_pine", (32, 0, 64, 32)), ("tree_dead", (64, 0, 96, 32)), ("tree_bonsai", (96, 0, 128, 32)),
          ("tree_pine_snow", (128, 0, 160, 32)), ("tree_snow", (192, 0, 224, 32)), ("tree_cherry", (224, 0, 256, 32)),
          ("tree_light", (256, 0, 288, 32)), ("tree_autumn", (288, 0, 320, 32)),
          ("bigtree_pine", (0, 32, 64, 80)), ("bigtree_green", (64, 32, 128, 80)), ("bigtree_snow", (128, 32, 192, 80)),
          ("bigtree_cherry", (192, 32, 256, 80)), ("bigtree_light", (256, 32, 320, 80)), ("bigtree_autumn", (320, 32, 384, 80)),
          ("roundtree_cherry", (0, 288, 48, 336)), ("roundtree_green", (48, 288, 96, 336)), ("roundtree_white", (96, 288, 144, 336)),
          ("roundtree_orange", (144, 288, 192, 336)), ("rocks_brown", (192, 80, 256, 128)), ("rocks_grey", (256, 80, 320, 128)),
          ("boulder_brown", (272, 160, 336, 208)), ("boulder_grey", (272, 224, 336, 272)), ("stump", (0, 128, 32, 160)),
          ("stump_orange", (32, 128, 64, 160))] + [(f"bush_{c}", (16 * i, 160, 16 * i + 16, 176)) for i, c in enumerate("abcd")]


def import_village():
    F = "Environment/Village"
    house, nature = load("Backgrounds/Tilesets/TilesetHouse.png"), load("Backgrounds/Tilesets/TilesetNature.png")
    out = [save(trim(house.crop(box)), F, f"building_{name}") for name, box in BUILDINGS]
    out.append(save(load_ai("village/village_castle.png"), F, "building_castle"))
    record("Village", "buildings: dojo, forge, shrine, market, pet house + houses", "`TilesetHouse.png` cut-outs; castle: AI (PixelLab)", out)
    out = [save(trim(house.crop(box)), F, f"prop_{name}") for name, box in HOUSE_PROPS]
    out += [save(trim(nature.crop(box)), F, f"prop_{name}") for name, box in NATURE]
    record("Village", "trees, rocks, bushes, statues, banners", "`TilesetNature.png` and `TilesetHouse.png` cut-outs", out)
    out = []
    for colour in ["Black", "Blue", "Brown", "Gray", "Green", "Red", "White", "Yellow"]:
        out += save_frames(strip(load(f"Backgrounds/Animated/Flag/Flag{colour}16x16.png"), 16, 16), F, f"prop_flag_{colour.lower()}")
    out += save_frames(strip(load("Backgrounds/Animated/WaterMill/Watermill_A_34x36.png"), 34, 36), F, "prop_watermill")
    out += save_frames(strip(load("Backgrounds/Animated/MillPropeller/MillPropeller_A_64x64.png"), 64, 64), F, "prop_mill_propeller")
    out += save_frames(strip(load("Backgrounds/Animated/Water Ripples/SpriteSheet16x16.png"), 16, 16), F, "prop_water_ripple")
    record("Village", "animated props", "`Backgrounds/Animated` (flags, watermill, mill, water ripples)", out)


# Village life: townsfolk and farm animals that wander the village map.
VILLAGERS = [("villager", "Villager"), ("villager2", "Villager2"), ("villager3", "Villager3"), ("villager4", "Villager4"),
             ("woman", "Woman"), ("oldman", "OldMan"), ("oldwoman", "OldWoman"), ("boy", "Boy"), ("master", "Master")]
ANIMALS = [("cat", "Actor/Animal/Cat/SpriteSheet.png"), ("chicken", "Actor/Animal/Chicken/SpriteSheetWhite.png"),
           ("dog", "Actor/Animal/Dog/SpriteSheet.png"), ("pig", "Actor/Animal/Pig/SpriteSheetPink.png"),
           ("frog", "Actor/Animal/Frog/SpriteSheet.png"), ("cow", "Actor/Animal/Cow/SpriteSheetWhite.png")]


def npc_right(char):
    """Right-facing idle + walk of a 16x16 townsperson (SeparateAnim when present, else the sheet's column 3)."""
    base = f"Actor/Character/{char}"
    if os.path.exists(os.path.join(PACK, base, "SeparateAnim", "Walk.png")):
        walk = column(load(f"{base}/SeparateAnim/Walk.png"), 3, 16, 16)
        idle = [load(f"{base}/SeparateAnim/Idle.png").crop((48, 0, 64, 16))]
    else:
        walk = column(load(f"{base}/SpriteSheet.png"), 3, 16, 16, range(4))
        idle = walk[:1]
    return idle, walk


def import_villagers():
    F = "Characters/Villagers"
    for key, char in VILLAGERS:
        idle, walk = npc_right(char)
        out = save_frames(idle, F, f"npc_{key}_idle") + save_frames(walk, F, f"npc_{key}_walk")
        record("Villagers", key, f"`Actor/Character/{char}`", out)
    for key, rel in ANIMALS:
        frames = strip(load(rel), 16, 16)                                  # 2-frame side strips, facing right
        out = save_frames(frames[:1], F, f"animal_{key}_idle") + save_frames(frames, F, f"animal_{key}_walk")
        record("Villagers", f"{key} (animal)", f"`{rel}` (2-frame strip)", out)


def _cells(c0, r0, c1, r1):
    return c0 * 16, r0 * 16, (c1 + 1) * 16, (r1 + 1) * 16


# Village decorations the player can buy: (name, tileset, box). Cells are 16 px; boxes are trimmed.
DECOR = [("barrel", "TilesetElement.png", _cells(0, 0, 0, 0)), ("pot", "TilesetElement.png", _cells(1, 0, 1, 0)),
         ("crate", "TilesetElement.png", _cells(6, 0, 6, 0)), ("well", "TilesetElement.png", _cells(4, 1, 4, 1)),
         ("chest", "TilesetElement.png", _cells(3, 1, 3, 1)), ("cart", "TilesetElement.png", _cells(0, 3, 1, 4)),
         ("flower_cart", "TilesetElement.png", _cells(2, 3, 3, 4)), ("hay", "TilesetElement.png", _cells(12, 4, 12, 5)),
         ("bench", "TilesetElement.png", _cells(11, 1, 13, 1)), ("clothesline", "TilesetElement.png", _cells(11, 2, 15, 3)),
         ("signpost", "TilesetElement.png", _cells(6, 3, 6, 3)), ("pot_plant", "TilesetElement.png", _cells(0, 8, 0, 8)),
         ("vase", "TilesetElement.png", _cells(0, 5, 0, 6)), ("scarecrow", "TilesetElement.png", _cells(15, 0, 15, 0)),
         ("tent", "tileset_camp.png", _cells(4, 0, 6, 2)), ("fire_pit", "tileset_camp.png", _cells(10, 3, 11, 4)),
         ("lantern_post", "tileset_camp.png", _cells(6, 5, 6, 6)), ("banner_post", "tileset_camp.png", _cells(7, 5, 7, 6)),
         ("stump_table", "tileset_camp.png", _cells(1, 5, 2, 6)), ("big_barrel", "tileset_camp.png", _cells(4, 6, 5, 7)),
         ("log_bench", "tileset_camp.png", _cells(0, 7, 2, 7)),
         ("sunflower", "TilesetNature.png", _cells(0, 11, 0, 11)), ("yellow_flower", "TilesetNature.png", _cells(1, 11, 1, 11)),
         ("red_flower", "TilesetNature.png", _cells(3, 11, 3, 11)), ("white_flower", "TilesetNature.png", _cells(6, 11, 6, 11)),
         ("bamboo", "TilesetNature.png", _cells(11, 8, 11, 10)),
         ("crystal_red", "TilesetNature.png", _cells(0, 14, 0, 14)), ("crystal_blue", "TilesetNature.png", _cells(1, 14, 1, 14)),
         ("crystal_pink", "TilesetNature.png", _cells(2, 14, 2, 14)), ("crystal_green", "TilesetNature.png", _cells(3, 14, 3, 14)),
         ("weapon_rack", "TilesetHouse.png", _cells(14, 13, 15, 13)),
         ("statue_orb_monk", "TilesetHouse.png", (48, 240, 80, 272)), ("statue_monk", "TilesetHouse.png", (80, 240, 112, 272)),
         ("statue_fox", "TilesetHouse.png", (112, 240, 128, 272)), ("stone_pillar", "TilesetHouse.png", (0, 304, 16, 352)),
         ("statue_monk_moss", "TilesetHouse.png", (80, 304, 112, 336)), ("statue_frog_moss", "TilesetHouse.png", (48, 336, 80, 368))]


def import_decor():
    F = "Environment/Decor"
    sheets = {}
    out = []
    for name, sheet, box in DECOR:
        if sheet not in sheets:
            sheets[sheet] = load(f"Backgrounds/Tilesets/{sheet}")
        out.append(save(trim(sheets[sheet].crop(box)), F, f"deco_{name}"))
    record("Village", "decorations (shop items)", "`TilesetElement.png`, `tileset_camp.png`, `TilesetNature.png`, `TilesetHouse.png` cut-outs", out)


# Village goods: crops (farm), then kitchen/fishing items for later phases. Saved as UI icons (item_*)
# and, for crops, as world sprites standing in a tilled bed (Environment/Farm).
CROPS = [("rice", "Items/Food/SeedLargeWhite.png"), ("radish", "Items/Food/SeedBig2.png"), ("carrot", "Items/Food/SeedBig1.png"),
         ("beet", "Items/Food/SeedBig3.png"), ("herbs", "Items/Resource/Grass.png"), ("tea", "Items/Food/TeaLeaf.png")]
FOODS = [("onigiri", "Onigiri"), ("sushi", "Sushi"), ("sushi_roll", "Sushi2"), ("noodle", "Noodle"), ("yakitori", "Yakitori"),
         ("fish", "Fish"), ("shrimp", "Shrimp"), ("calamari", "Calamari"), ("octopus", "Octopus"), ("honey", "Honey"),
         ("meat", "Meat"), ("fortune_cookie", "FortuneCookie"), ("nut", "Nut")]
TOOLS = ["Hoe", "WateringCan", "Sickle", "Pickaxe", "Axe", "Shovel"]


def earth(img):
    """Recolours the field tileset's orange bed to tilled brown soil (outline kept)."""
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if v < 0.2:
                continue
            nr, ng, nb = colorsys.hsv_to_rgb(0.07, min(1, s * 1.05), v * 0.62)
            px[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return out


def import_farm():
    U, W = "UI/Icons", "Environment/Farm"
    field = load("Backgrounds/Tilesets/TilesetField.png")
    soil = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    for (sx, sy), (dx, dy) in [((0, 0), (0, 0)), ((32, 0), (16, 0)), ((0, 32), (0, 16)), ((32, 32), (16, 16))]:
        soil.paste(field.crop((sx, sy, sx + 16, sy + 16)), (dx, dy))           # the orange bed's four corners
    out = [save(earth(soil), W, "farm_soil")]
    out += [save(trim(load(f"Items/Food/Seed{i}.png")), W, f"farm_seed_{i - 1}") for i in (1, 2, 3)]
    out.append(save(trim(load("Items/Resource/Grass.png")), W, "farm_growing"))
    for name, rel in CROPS:
        art = trim(load(rel))
        out += [save(art, W, f"farm_crop_{name}"), save(art, U, f"item_{name}")]
    record("Village", "farm: soil, seeds, crops (+ item icons)", "`TilesetField.png` bed corners (recoloured), `Items/Food`, `Items/Resource`", out)
    out = [save(trim(load(f"Items/Food/{src}.png")), U, f"item_{name}") for name, src in FOODS]
    out += [save(trim(load(f"Items/Tool/{tool}.png")), U, "tool_" + re.sub(r"(?<!^)([A-Z])", r"_\1", tool).lower()) for tool in TOOLS]
    record("Village", "kitchen & fishing goods, tools", "`Items/Food`, `Items/Tool`", out)


def gilded(img):
    """Recolours a stone statue in gold (dark outline kept): the weekly Best Village trophy."""
    out = img.copy()
    px = out.load()
    ramp = [(122, 72, 22), (184, 120, 30), (232, 176, 48), (252, 222, 110), (255, 246, 196)]
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            v = (0.3 * r + 0.59 * g + 0.11 * b) / 255
            if v < 0.2:
                continue                                   # outline
            px[x, y] = ramp[min(len(ramp) - 1, int((v - 0.2) / 0.8 * len(ramp)))] + (a,)
    return out


def import_pond_and_mine():
    """EPIC 24 Phase 5: the fishing pond (9-slice pond, dock, lily pad, boat, ripples, fish), the mine's crane,
    the Golden Koi pond decoration and the metal-bar / fishing-rod icons."""
    U, V = "UI/Icons", "Environment/Village"
    water = load("Backgrounds/Tilesets/TilesetWater.png")
    pond = water.crop((0, 96, 48, 144))                                  # grass-edged pond; 16 px 9-slice borders
    out = [save(pond, V, "pond_water"), save(trim(water.crop((0, 192, 32, 240))), V, "pond_dock"),
           save(trim(water.crop((176, 48, 192, 64))), V, "pond_lily"), save(trim(water.crop((416, 0, 448, 16))), V, "pond_boat")]
    ripples = load("Backgrounds/Animated/Water Ripples/SpriteSheet16x16.png")
    base = ripples.getpixel((0, 0))
    frames = []
    for i in range(ripples.width // 16):
        f = ripples.crop((i * 16, 0, i * 16 + 16, 16))
        f.putdata([(0, 0, 0, 0) if px == base else px for px in f.getdata()])   # keep only the ripple marks
        frames.append(f)
    out += save_frames(frames, V, "pond_ripple")
    for colour in ("Red", "White", "Yellow"):
        out.append(save(trim(load(f"Actor/Animal/Fish/SpriteSheet{colour}.png").crop((0, 0, 16, 16))), V, f"fish_{colour.lower()}"))
    out += [save(trim(load("Backgrounds/Vehicles/FishNetFull.png")), V, "pond_net_full"),
            save(trim(load("Backgrounds/Vehicles/Crane.png")), V, "mine_crane")]
    record("Village", "fishing pond + mine pieces", "`TilesetWater.png` cut-outs, `Water Ripples` (background keyed out), `Actor/Animal/Fish`, `Vehicles`", out)

    koi = pond.copy()
    fish = trim(load("Actor/Animal/Fish/SpriteSheetYellow.png").crop((0, 0, 16, 16)))
    koi.alpha_composite(fish, ((48 - fish.width) // 2, (48 - fish.height) // 2))
    out = [save(koi, "Environment/Decor", "deco_koi_pond")]
    out += [save(trim(load(f"Items/Resource/Bar{metal}.png")), U, f"item_{metal.lower()}_bar") for metal in ("Iron", "Gold", "Mithril")]
    out += [save(trim(load("Items/Weapons/Fishing Rod/Sprite.png")), U, "tool_fishing_rod"),
            save(fish, U, "item_golden_koi")]
    out.append(save(gilded(trim(load("Backgrounds/Tilesets/TilesetHouse.png").crop((80, 240, 112, 272)))), "Environment/Decor", "deco_trophy_gold"))
    record("Village", "koi pond decoration, Best Village trophy (gilded monk statue), metal bars, fishing rod, golden koi icons", "pond + `Fish/SpriteSheetYellow`, `Items/Resource/Bar*`, `Items/Weapons/Fishing Rod`", out)


# S-class equipment (special items from Surprise Boxes): a themed recolour, a gold glow outline and sparkles.
S_THEMES = {
    "storm": [(40, 30, 110), (70, 70, 200), (90, 160, 255), (160, 230, 255), (240, 255, 255)],
    "fire": [(110, 20, 20), (200, 50, 30), (250, 120, 40), (255, 200, 70), (255, 245, 190)],
    "crimson": [(80, 10, 30), (160, 20, 40), (230, 50, 50), (255, 130, 110), (255, 220, 200)],
    "emerald": [(10, 60, 40), (20, 120, 70), (40, 190, 110), (140, 240, 170), (230, 255, 230)],
    "void": [(30, 10, 50), (80, 30, 130), (150, 60, 210), (210, 140, 255), (250, 230, 255)],
    "royal": [(50, 15, 70), (110, 30, 140), (180, 70, 200), (255, 190, 60), (255, 240, 170)],
    "gold": [(120, 70, 20), (200, 130, 30), (245, 190, 60), (255, 230, 120), (255, 250, 220)],
    "wood": [(60, 30, 15), (110, 60, 30), (160, 100, 50), (210, 150, 80), (240, 200, 130)],
}


def themed(img, theme):
    """Recolours by brightness into one of S_THEMES (dark outline pixels are kept)."""
    ramp = S_THEMES[theme]
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            v = (0.3 * r + 0.59 * g + 0.11 * b) / 255
            if v < 0.16:
                continue
            px[x, y] = ramp[min(len(ramp) - 1, int((v - 0.16) / 0.84 * len(ramp)))] + (a,)
    return out


def s_grade(img, theme, seed):
    """An S item: themed colours, a 1 px gold glow around it and three sparkles, on a canvas 2 px bigger."""
    base = themed(img, theme)
    out = Image.new("RGBA", (base.width + 2, base.height + 2), (0, 0, 0, 0))
    out.alpha_composite(base, (1, 1))
    px = out.load()
    solid = [[px[x, y][3] > 0 for y in range(out.height)] for x in range(out.width)]
    for y in range(out.height):
        for x in range(out.width):
            if solid[x][y]:
                continue
            if any(0 <= x + dx < out.width and 0 <= y + dy < out.height and solid[x + dx][y + dy]
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = (255, 214, 90, 255)
    rng = random.Random(seed)
    empty = [(x, y) for y in range(out.height) for x in range(out.width) if px[x, y][3] == 0]
    for x, y in rng.sample(empty, min(3, len(empty))):
        px[x, y] = (255, 255, 230, 255)
    return out


S_ITEMS = [  # (icon name, source, theme)
    ("icon_weapon_stormninjaku", ("pack", "Items/Weapons/Ninjaku/Sprite.png"), "storm"),
    ("icon_weapon_phoenixbow", ("pack", "Items/Weapons/Bow2/Sprite.png"), "fire"),
    ("icon_equip_phoenix_ring", ("ai", "icons/icon_equip_iron_ring.png"), "fire"),
    ("icon_equip_oni_warband", ("ai", "icons/icon_equip_ninja_headband.png"), "crimson"),
    ("icon_equip_dragon_mail", ("ai", "icons/icon_equip_dragon_scale.png"), "emerald"),
    ("icon_equip_void_amulet", ("ai", "icons/icon_equip_jade_charm.png"), "void"),
]


def import_s_equipment():
    """S-class items and the crates they come from (closed + open chests for the opening animation)."""
    U = "UI/Icons"
    out = []
    for i, (name, (kind, rel), theme) in enumerate(S_ITEMS):
        src = trim(load(rel) if kind == "pack" else load_ai(rel))
        out.append(save(square(s_grade(src, theme, i), 18), U, name))
    record("Icons", "S-class equipment (Surprise Box)", "pack weapons / AI equipment icons, recoloured + gold glow outline + sparkles", out)

    big = load("Items/Treasure/BigTreasureChest.png")          # red chest: closed | open
    little = load("Items/Treasure/LittleTreasureChest.png")    # teal chest: closed | open
    out = []
    for crate, sheet, theme in (("wood", big, "wood"), ("silver", little, None), ("surprise", big, "royal")):
        half = sheet.width // 2
        for state, box in (("closed", (0, 0, half, sheet.height)), ("open", (half, 0, sheet.width, sheet.height))):
            frame = trim(sheet.crop(box))
            if theme == "royal":
                frame = s_grade(frame, theme, 99)
            elif theme:
                frame = themed(frame, theme)
            out.append(save(square(frame, 20), U, f"chest_{crate}_{state}"))
    record("Icons", "supply crates (closed / open)", "`Items/Treasure` chests: wood recolour, silver as is, Surprise Box royal purple + gold glow", out)


# Mounts: the pack's side-view animals (2-frame gallop). S-class mounts are recoloured with a gold glow.
MOUNTS = [  # (id, sheet, S theme or None)
    ("horse_brown", "Actor/Animal/Horse/SpriteSheetBrownSide.png", None),
    ("horse_black", "Actor/Animal/Horse/SpriteSheetBlackSide.png", None),
    ("donkey", "Actor/Animal/Donkey/SpriteSheeGreySide.png", None),
    ("lion_red", "Actor/Animal/Lion/SpriteSheetRedSide.png", None),
    ("lion_frost", "Actor/Animal/Lion/SpriteSheetWhiteSide.png", None),
    ("lioness", "Actor/Animal/Lioness/SpriteSheetLionessSide.png", None),
    ("golden_qilin", "Actor/Animal/Lion/SpriteSheetWhiteSide.png", "gold"),
    ("nightmare", "Actor/Animal/Horse/SpriteSheetBlackSide.png", "void"),
]


def import_mounts():
    M, U = "Characters/Mounts", "UI/Icons"
    out = []
    for mount_id, rel, theme in MOUNTS:
        sheet = load(rel)
        half = sheet.width // 2
        frames = [sheet.crop((i * half, 0, (i + 1) * half, sheet.height)) for i in range(2)]
        if theme:
            frames = [s_grade(f, theme, 31 + i) for i, f in enumerate(frames)]   # sparkles twinkle between frames
        out += save_frames(frames, M, f"mount_{mount_id}")
        out.append(save(square(trim(frames[0]), max(frames[0].size) + 2), U, f"icon_mount_{mount_id}"))
    record("Characters", "mounts (side-view gallop, 2 frames) + icons; S-class Golden Qilin and Nightmare Steed",
           "`Actor/Animal/*Side.png` sheets; S mounts recoloured + gold glow outline", out)


GROUNDS = [("grass", (0, 12)), ("grass_dark", (11, 12)), ("dirt", (11, 19)), ("sand", (0, 5)), ("snow", (0, 19))]


def import_backgrounds():
    F = "Environment/Backgrounds"
    floor = load("Backgrounds/Tilesets/TilesetFloor.png")
    cells = {}
    for cy in range(floor.height // 16):
        for cx in range(floor.width // 16):
            c = floor.crop((cx * 16, cy * 16, cx * 16 + 16, cy * 16 + 16))
            px = list(c.getdata())
            if all(p[3] == 255 for p in px):
                cells[(cx, cy)] = c
    out = []
    for name, fill_cell in GROUNDS:
        fill = cells[fill_cell]
        colour = fill.getpixel((0, 0))
        details = []
        for pos, c in cells.items():
            data = list(c.getdata())
            cnt = Counter(data)
            left = [c.getpixel((0, y)) for y in range(16)]
            right = [c.getpixel((15, y)) for y in range(16)]
            if pos != fill_cell and cnt[colour] >= 0.7 * 256 and len(cnt) <= 5 and left == right:
                details.append(c)
        rng = random.Random(name)
        patch = Image.new("RGBA", (128, 128))
        for ty in range(8):
            for tx in range(8):
                patch.paste(rng.choice(details) if details and rng.random() < 0.2 else fill, (tx * 16, ty * 16))
        out.append(save(patch, F, f"bg_ground_{name}"))
    record("Backgrounds", "tiling ground (grass, dark grass, dirt, sand, snow)",
           "`TilesetFloor.png` fill tile plus ~20% of its tufted variants, 8x8 tiles; seamless", out)
    fog = load("FX/Environment/Fog.png")
    out = [save(fog, F, "bg_fog", 6), save(load("FX/Environment/Raylight.png"), F, "bg_raylight")]
    record("Backgrounds", "fog and light-ray overlays", "`FX/Environment` (fog at 6x = 1920x1080 to stay under 2048)", out)
    out = [save(load_ai("backgrounds/bg_mainmenu.png"), F, "bg_mainmenu")]
    record("Backgrounds", "main menu background", "AI (PixelLab), 132x240 -> 1056x1920", out)


def export_store_graphics():
    """Play Store art lives outside Unity: 512x512 icon and the 1024x500 feature graphic."""
    os.makedirs(STORE, exist_ok=True)
    upscale(load_ai("store/app_icon.png", palette=False)).save(os.path.join(STORE, "app_icon_512.png"))
    feature = upscale(load_ai("store/store_feature_graphic.png", palette=False), 4)      # 1024 x 496
    canvas = Image.new("RGBA", (1024, 500), feature.getpixel((0, 0)))
    canvas.alpha_composite(feature, (0, 2))
    canvas.convert("RGB").save(os.path.join(STORE, "store_feature_graphic_1024x500.png"))


# --------------------------------------------------------------------------------------------------
# Verification, docs, preview
# --------------------------------------------------------------------------------------------------
def verify():
    problems, opaque = [], []
    anims = defaultdict(set)
    for rel in written:
        name = os.path.basename(rel)
        if not re.fullmatch(r"[a-z0-9_]+\.png", name):
            problems.append(f"bad file name: {rel}")
        img = Image.open(os.path.join(OUT, rel))
        if img.mode != "RGBA":
            problems.append(f"not RGBA: {rel}")
        if max(img.size) > MAX_SIZE:
            problems.append(f"bigger than {MAX_SIZE}: {rel} {img.size}")
        if img.getchannel("A").getextrema()[0] == 255:
            opaque.append(rel)                      # square art by design: portraits, tile icons, panels, tiles
        m = re.match(r"(.*)_(\d+)\.png$", name)
        if m and not name.startswith(("tiles_", "vfx_particle_")):
            anims[os.path.join(os.path.dirname(rel), m.group(1))].add(img.size)
    for anim, sizes in anims.items():
        if len(sizes) > 1:
            problems.append(f"frame sizes differ in {anim}: {sorted(sizes)}")
    return problems, opaque


def write_docs():
    lines = ["# Art mapping", "",
             "Generated by `Tools/art_import/import_ninja_adventure.py`; re-run it instead of editing files by hand.",
             "Every sprite is the source upscaled 8x (NEAREST): 16 px -> 128 px. Pack sources are relative to",
             "`~/Downloads/NinjaAdventure/`; AI sources are in `~/Downloads/NinjaAdventure_AI/` (see its GENERATION_LOG.md).",
             "Character sprites face right; the game flips them for left. `a_0..3` means files a_0 to a_3.", ""]
    for section, rows in mapping.items():
        lines += [f"## {section}", "", "| Item | Source | Output files |", "|---|---|---|"]
        lines += [f"| {item} | {src} | {outs} |" for item, src, outs in rows]
        lines.append("")
    lines += ["## MISSING", "", "| Area | Item | Note |", "|---|---|---|"]
    lines += [f"| {a} | {i} | {n} |" for a, i, n in missing]
    lines += ["", "Also still to make with AI (prompts in `Tools/art_import/AI_ASSET_PROMPTS.md`): 5 event/season banners.",
              "The achievement badges and store items are composites of pack art; AI versions are optional.", ""]
    with open(os.path.join(OUT, "ART_MAPPING.md"), "w") as f:
        f.write("\n".join(lines))

    credits = """# Credits

## Ninja Adventure Asset Pack

Ninja Adventure Asset Pack by Pixel-boy (CC0)
https://pixel-boy.itch.io/ninja-adventure-asset-pack

Created by [Pixel-boy](https://pixel-boy.itch.io/) and [AAA](https://www.instagram.com/challenger.aaa/).
Released under Creative Commons Zero (CC0 1.0): free for any use, including commercial; attribution is
not required but appreciated. Support the author: https://www.patreon.com/pixelarchipel

Used for: characters, monsters, bosses, animals, items, UI, effects, tilesets and village props. The hero
skins, achievement badges and store items are recolours and composites of pack art.

## AI-generated art

Some sprites were generated for this game, in the pack's style, with:
- [PixelLab](https://www.pixellab.ai): Spider Queen and Nine-Tailed Fox bosses, wolf enemy, Mage Ninja,
  fox and hawk pets, hero hurt frames, castle, chain sickle and three equipment icons, app icon, logo,
  main menu background, store feature graphic, village menu icon, Scholar talent icon.
- [Retro Diffusion](https://www.retrodiffusion.ai): equipment, skill, ultimate and evolution icons.
- [Ludo.ai](https://ludo.ai): talent, blessing, pet gear, crate, evolution and menu icons.

The full list, with the tool used for each file, is in `Sprites/ART_MAPPING.md` (entries marked "AI").
Check each tool's current terms for commercial use before release.
"""
    with open(os.path.join(ART, "CREDITS.md"), "w") as f:
        f.write(credits)


def write_preview():
    """One frame per character (the first idle frame), labelled, for a quick visual check."""
    rows = [("Heroes", "Characters/Heroes", "hero_"), ("Enemies", "Characters/Enemies", "enemy_"),
            ("Bosses", "Characters/Enemies", "boss_"), ("Pets", "Characters/Pets", "pet_")]
    blocks = []
    for title, folder, prefix in rows:
        names = sorted({re.match(r"(.*?)_idle_0\.png", os.path.basename(r)).group(1)
                        for r in written if r.startswith(folder) and os.path.basename(r).startswith(prefix)
                        and re.match(r".*_idle_0\.png$", os.path.basename(r))})
        imgs = []
        for n in names:
            img = Image.open(os.path.join(OUT, folder, f"{n}_idle_0.png")).convert("RGBA")
            img = img.resize((img.width // 2, img.height // 2), Image.NEAREST)
            imgs.append((n, img))
        blocks.append((title, imgs))
    width = max(sum(max(i.width, 120) + 12 for _, i in imgs) for _, imgs in blocks) + 24
    height = sum(max(i.height for _, i in imgs) + 44 for _, imgs in blocks) + 12
    sheet = Image.new("RGBA", (width, height), (52, 60, 78, 255))
    draw = ImageDraw.Draw(sheet)
    y = 12
    for title, imgs in blocks:
        draw.text((12, y), title, fill=(255, 210, 80, 255))
        x, row_h = 12, max(i.height for _, i in imgs)
        for name, img in imgs:
            draw.text((x, y + 14), name, fill=(230, 230, 230, 255))
            sheet.paste(Image.new("RGBA", img.size, (80, 90, 110, 255)), (x, y + 28))
            sheet.alpha_composite(img, (x, y + 28))
            x += max(img.width, 120) + 12
        y += row_h + 44
    sheet.save(os.path.join(HERE, "preview.png"))


def clean_stale():
    """Delete files an earlier run wrote that this run didn't (so renamed assets don't linger)."""
    if os.path.exists(MANIFEST):
        old = set(open(MANIFEST).read().split())
        for rel in sorted(old - set(written)):
            path = os.path.join(OUT, rel)
            if os.path.exists(path):
                os.remove(path)
                meta = path + ".meta"
                if os.path.exists(meta):
                    os.remove(meta)
                print(f"removed stale {rel}")
    with open(MANIFEST, "w") as f:
        f.write("\n".join(sorted(written)) + "\n")


def main():
    global snap
    ensure_pack()
    snap = Snapper(pack_palette())
    for folder in FOLDERS:
        os.makedirs(os.path.join(OUT, folder), exist_ok=True)

    import_heroes()
    import_enemies()
    import_bosses()
    import_pets()
    import_portraits()
    import_icons()
    import_menu_icons()
    import_achievements()
    import_store_items()
    import_projectiles()
    import_pickups()
    import_ui()
    import_vfx()
    import_tiles()
    import_village()
    import_villagers()
    import_decor()
    import_farm()
    import_pond_and_mine()
    import_s_equipment()
    import_mounts()
    import_backgrounds()
    export_store_graphics()

    clean_stale()
    write_docs()
    write_preview()

    problems, opaque = verify()
    print(f"\n{len(written)} PNG files written to {os.path.relpath(OUT, REPO)}/")
    counts = Counter(os.path.dirname(r) for r in written)
    for folder in FOLDERS:
        print(f"  {folder:26s} {counts.get(folder, 0):4d}")
    print(f"Store graphics: {os.path.relpath(STORE, REPO)}/  Preview: Tools/art_import/preview.png")
    if problems:
        print(f"\n{len(problems)} problem(s):")
        for p in problems:
            print("  - " + p)
        sys.exit(1)
    print("Checks passed: PNG with alpha (RGBA), lowercase names without spaces, <= 2048 px, consistent frame sizes.")
    print(f"{len(opaque)} images fill their whole canvas by design (portraits, tile icons, panels, tiles, backgrounds).")


if __name__ == "__main__":
    main()
