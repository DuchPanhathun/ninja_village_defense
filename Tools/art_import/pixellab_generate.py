#!/usr/bin/env python3
"""Generate the art the Ninja Adventure pack is missing with the PixelLab API.

Re-runnable: assets that already exist in the output folder are skipped, so nothing is paid for twice.
The API key is never stored here: pass it with PIXELLAB_SECRET=... or --key-file PATH.

    python3 Tools/art_import/pixellab_generate.py --key-file ~/.pixellab_key --group must --max-gens 16
    python3 Tools/art_import/pixellab_generate.py --list          # show jobs, spend nothing

Every call checks the balance first and stops before the --max-gens cap or the trial running out.
Results go to ~/Downloads/NinjaAdventure_AI/<category>/ at native size (import_ninja_adventure.py
upscales them later). Animations are saved as one horizontal strip plus one PNG per frame.
"""
import argparse
import base64
import io
import json
import os
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime

from PIL import Image

API = "https://api.pixellab.ai/v2"
PACK = os.path.expanduser("~/Downloads/NinjaAdventure")
OUT = os.path.expanduser("~/Downloads/NinjaAdventure_AI")
PALETTE = os.path.join(PACK, "Palette.png")
LOG = os.path.join(OUT, "GENERATION_LOG.md")

STYLE = ("cute chunky RPG sprite in the Ninja Adventure asset pack style by Pixel-boy: big rounded simple shapes, "
         "thick 1px black outline, flat 2-3 tone shading, few colours, no anti-aliasing")
BOSS_STYLE = {"outline": "single color black outline", "shading": "basic shading", "detail": "medium detail",
              "view": "high top-down", "direction": "south"}
SIDE_STYLE = {"outline": "single color black outline", "detail": "medium detail", "view": "low top-down",
              "direction": "east"}

# kind: pixflux (32x32 area and up, forced palette), pixen (true 16x16/24x24), animate (from a base image).
JOBS = [
    # --- must have -------------------------------------------------------------------------------
    dict(group="must", cat="bosses", name="boss_spiderqueen_base", kind="pixflux", size=(64, 64), style=BOSS_STYLE,
         prompt="Spider Queen boss: big round chubby demon spider seen from the front, a golden crown on its head, "
                "dark purple body with a red hourglass marking, eight short thick legs, big glowing red eyes, "
                "facing the camera. " + STYLE),
    dict(group="must", cat="bosses", name="boss_spiderqueen_idle", kind="animate", base="boss_spiderqueen_base", frames=6,
         prompt="idle: legs shift slightly and the eyes blink, body bobs one pixel, stays in place"),
    dict(group="must", cat="bosses", name="boss_spiderqueen_walk", kind="animate", base="boss_spiderqueen_base", frames=6,
         prompt="walking: skittering forward toward the camera, legs alternate in pairs"),
    dict(group="must", cat="bosses", name="boss_spiderqueen_attack", kind="animate", base="boss_spiderqueen_base", frames=4,
         prompt="attack: rears up on the back legs then strikes down to the right with its fangs, small green venom splash"),

    dict(group="must", cat="bosses", name="boss_ninetailedfox_base", kind="pixflux", size=(64, 64), style=BOSS_STYLE,
         prompt="Nine-Tailed Fox boss seen from the front: menacing white and gold fox demon sitting upright, NINE big "
                "fluffy tails spread in a wide fan behind its body like a peacock, each tail tip burning with blue "
                "spirit fire, red markings around narrow eyes, facing the camera. " + STYLE),
    dict(group="must", cat="bosses", name="boss_ninetailedfox_idle", kind="animate", base="boss_ninetailedfox_base", frames=6,
         prompt="idle: the nine tails sway in a wave and the blue flames flicker, body stays in place"),
    dict(group="must", cat="bosses", name="boss_ninetailedfox_walk", kind="animate", base="boss_ninetailedfox_base", frames=6,
         prompt="walking: stalking forward toward the camera, head low, tails trailing"),
    dict(group="must", cat="bosses", name="boss_ninetailedfox_attack", kind="animate", base="boss_ninetailedfox_base", frames=4,
         prompt="attack: lunges to the right with a claw swipe leaving a blue fire trail"),

    dict(group="must", cat="enemies", name="enemy_wolf_base", kind="pixen", size=(16, 16), style=SIDE_STYLE,
         prompt="grey wolf demon enemy, lean body, pointed ears, glowing red eyes, bushy tail, side view facing right, "
                "top-down RPG sprite. " + STYLE),
    dict(group="must", cat="enemies", name="enemy_wolf_idle", kind="animate", base="enemy_wolf_base", frames=4,
         prompt="idle: breathing, chest rises one pixel, tail sways, stays in place facing right"),
    dict(group="must", cat="enemies", name="enemy_wolf_walk", kind="animate", base="enemy_wolf_base", frames=4,
         prompt="walking: trotting to the right, legs alternate, tail bounces"),

    dict(group="must", cat="village", name="village_castle", kind="pixflux", size=(96, 96),
         style={"outline": "single color black outline", "shading": "basic shading", "detail": "medium detail",
                "view": "low top-down", "direction": "south"},
         prompt="Japanese castle keep for a hidden ninja village, straight front view like a house in a top-down "
                "Zelda-style RPG (not isometric, not diagonal): flat front wall of white stone at the bottom, two tiers "
                "of dark red tiled roofs with gold ridge ornaments, big wooden gate in the middle, small white banners, "
                "only the building with no ground, grass or shadow under it. " + STYLE),

    dict(group="must", cat="icons", name="icon_weapon_chainsickle", kind="pixen", size=(16, 16),
         style={"outline": "single color black outline", "detail": "medium detail"},
         prompt="kusarigama weapon item icon: small curved sickle with a wooden handle attached to a short iron chain "
                "ending in a round weight, drawn diagonally, centered. " + STYLE),

    # --- extras: hurt frames animated straight from the pack's right-facing hero sprites -------------
    *[dict(group="extras", cat="heroes", name=f"hero_{hero}_hurt", kind="animate", frames=4,
           first_frame=(f"Actor/Character/{src}", (48, 0, 64, 16)),
           prompt="hurt: flinches and recoils backward to the left as if hit, eyes squeezed shut, then recovers, "
                  "stays facing right, same character and colours")
      for hero, src in [("assassin", "NinjaDark/SeparateAnim/Idle.png"), ("samurai", "SamuraiRed/SeparateAnim/Idle.png"),
                        ("monk", "Monk2/SeparateAnim/Idle.png")]],

    dict(group="extras", cat="heroes", name="hero_mageninja_base", kind="pixen", size=(16, 16), style=SIDE_STYLE,
         prompt="blue ninja mage hero, round body with a big head, pointed blue wizard hat over a ninja mask, blue "
                "scarf, holding a small glowing scroll, side view facing right, top-down RPG sprite. " + STYLE),
    dict(group="extras", cat="heroes", name="hero_mageninja_idle", kind="animate", base="hero_mageninja_base", frames=4,
         prompt="idle: breathing, body bobs one pixel, scroll glows, stays in place facing right"),
    dict(group="extras", cat="heroes", name="hero_mageninja_walk", kind="animate", base="hero_mageninja_base", frames=4,
         prompt="walking to the right, legs alternate, hat bounces"),
    dict(group="extras", cat="heroes", name="hero_mageninja_attack", kind="animate", base="hero_mageninja_base", frames=4,
         prompt="attack: thrusts the scroll forward to the right releasing a small blue magic spark"),
    dict(group="extras", cat="heroes", name="hero_mageninja_hurt", kind="animate", base="hero_mageninja_base", frames=4,
         prompt="hurt: flinches and recoils backward to the left as if hit, eyes squeezed shut, then recovers, "
                "stays facing right, same character and colours"),

    dict(group="extras", cat="pets", name="pet_fox_base", kind="pixen", size=(16, 16), style=SIDE_STYLE,
         prompt="small orange fox pet with a white chest and white tail tip, big fluffy tail, side view facing right, "
                "top-down RPG animal sprite. " + STYLE),
    dict(group="extras", cat="pets", name="pet_fox_idle", kind="animate", base="pet_fox_base", frames=4,
         prompt="idle: sitting, tail wags, ears twitch, stays in place facing right"),
    dict(group="extras", cat="pets", name="pet_fox_walk", kind="animate", base="pet_fox_base", frames=4,
         prompt="trotting to the right, legs alternate, tail bounces"),

    dict(group="extras", cat="pets", name="pet_hawk_base", kind="pixen", size=(16, 16), style=SIDE_STYLE,
         prompt="small brown hawk pet flying with wings open, white chest, yellow beak and feet, side view facing "
                "right, top-down RPG sprite. " + STYLE),
    dict(group="extras", cat="pets", name="pet_hawk_idle", kind="animate", base="pet_hawk_base", frames=4,
         prompt="hovering in place, wings flap gently, facing right"),
    dict(group="extras", cat="pets", name="pet_hawk_walk", kind="animate", base="pet_hawk_base", frames=4,
         prompt="flying to the right, strong wing flaps up and down"),

    # --- final: redo the weak Retro Diffusion items, Mage Ninja death, launch visuals ----------------
    dict(group="final", cat="icons", name="icon_equip_jade_charm", kind="pixen", size=(16, 16),
         style={"outline": "single color black outline", "detail": "medium detail"},
         prompt="green jade magatama charm item icon: one comma-shaped curved jade bead with a small hole, hanging on "
                "a thin red cord, centered. " + STYLE),
    dict(group="final", cat="icons", name="icon_equip_iron_ring", kind="pixen", size=(16, 16),
         style={"outline": "single color black outline", "detail": "medium detail"},
         prompt="plain iron ring item icon seen from the front, thick grey metal band with one small dark grey stone "
                "on top, empty middle, centered. " + STYLE),
    dict(group="final", cat="icons", name="icon_equip_dragon_scale", kind="pixen", size=(16, 16),
         style={"outline": "single color black outline", "detail": "medium detail"},
         prompt="single large dragon scale item icon shaped like a rounded shield, shiny green with a gold rim, "
                "simple clean shading, centered. " + STYLE),
    dict(group="final", cat="heroes", name="hero_mageninja_death", kind="animate", base="hero_mageninja_base", frames=4,
         prompt="death: staggers, falls backward to the ground and lies still, hat falls off"),
    dict(group="final", cat="backgrounds", name="bg_mainmenu", kind="pixflux", size=(132, 240), keep_background=True,
         style={"outline": "single color black outline", "shading": "basic shading", "detail": "medium detail"},
         prompt="hidden ninja village in a misty mountain valley at dusk: pagoda, red torii gate, glowing lanterns, "
                "cherry blossom trees, calm and dark enough for white text on top, the middle area simple and empty "
                "for a title and buttons, portrait game menu background, no text. " + STYLE),
    dict(group="final", cat="store", name="app_icon", kind="pixflux", size=(64, 64), keep_background=True,
         style={"outline": "single color black outline", "shading": "basic shading", "detail": "medium detail"},
         prompt="mobile game app icon: close-up of a cute ninja face with a red mask and determined eyes, a shuriken "
                "beside it, bold shapes readable at small size, solid dark blue background, no text. " + STYLE),
    dict(group="final", cat="store", name="logo_emblem", kind="pixflux", size=(64, 64),
         style={"outline": "single color black outline", "shading": "basic shading", "detail": "medium detail"},
         prompt="game logo emblem: two crossed kunai behind a large shuriken with a small red torii gate in the "
                "centre, no text. " + STYLE),
    dict(group="final", cat="store", name="store_feature_graphic", kind="pixflux", size=(256, 124), keep_background=True,
         style={"outline": "single color black outline", "shading": "basic shading", "detail": "medium detail"},
         prompt="wide game banner: ninja hero defending a village gate against a horde of cute demons and a giant "
                "oni, cherry trees and lanterns, dramatic dusk sky, no text. " + STYLE),
]


# ---------------------------------------------------------------------------------------------------
def read_key(key_file):
    key = os.environ.get("PIXELLAB_SECRET")
    if not key and key_file:
        with open(os.path.expanduser(key_file)) as f:
            key = f.read().strip()
    if not key:
        sys.exit("No API key: set PIXELLAB_SECRET or pass --key-file.")
    return key


def call(key, method, path, body=None, timeout=300):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(API + path, data=data, method=method, headers={
        "Authorization": "Bearer " + key, "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            return json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"{method} {path} -> HTTP {e.code}: {e.read().decode()[:500]}") from None


def generations_left(key):
    sub = call(key, "GET", "/balance").get("subscription") or {}
    return float(sub.get("generations") or 0)


def b64_png(path_or_img):
    img = Image.open(path_or_img) if isinstance(path_or_img, str) else path_or_img
    buf = io.BytesIO()
    img.save(buf, "PNG")
    return {"type": "base64", "base64": base64.b64encode(buf.getvalue()).decode(), "format": "png"}


def to_img(b64image):
    return Image.open(io.BytesIO(base64.b64decode(b64image["base64"]))).convert("RGBA")


def wait_job(key, job_id, timeout=900):
    start = time.time()
    while time.time() - start < timeout:
        job = call(key, "GET", f"/background-jobs/{job_id}")
        status = job.get("status")
        if status == "completed":
            return job
        if status == "failed":
            raise RuntimeError(f"job {job_id} failed: {json.dumps(job)[:500]}")
        time.sleep(4)
    raise RuntimeError(f"job {job_id} timed out")


def used(resp):
    usage = resp.get("usage") or {}
    return usage.get("generations") if usage.get("type") == "generations" else usage.get("usd")


def out_path(job):
    return os.path.join(OUT, job["cat"], job["name"] + ".png")


def first_frame(job):
    """The image an animation starts from: a pack sprite cell (first_frame) or a generated base image (base)."""
    if "first_frame" in job:
        rel, box = job["first_frame"]
        return Image.open(os.path.join(PACK, rel)).convert("RGBA").crop(box)
    return Image.open(out_path([j for j in JOBS if j["name"] == job["base"]][0])).convert("RGBA")


def first_frame_ready(job):
    return "first_frame" in job or os.path.exists(out_path([j for j in JOBS if j["name"] == job["base"]][0]))


def run_job(key, job):
    kind = job["kind"]
    if kind in ("pixflux", "pixen"):
        w, h = job["size"]
        transparent = not job.get("keep_background", False)
        body = {"description": job["prompt"], "image_size": {"width": w, "height": h}, "no_background": transparent}
        body.update(job.get("style", {}))
        if kind == "pixflux":
            body["color_image"] = b64_png(PALETTE)
            if transparent:
                body["background_removal_task"] = "remove_complex_background"
        resp = call(key, "POST", f"/create-image-{kind}", body)
        if "background_job_id" in resp and "image" not in resp:
            resp = wait_job(key, resp["background_job_id"])
            img = to_img(resp["last_response"]["image"])
        else:
            img = to_img(resp["image"])
        img.save(out_path(job))
        return used(resp), f"{img.width}x{img.height}"

    if kind == "animate":
        body = {"first_frame": b64_png(first_frame(job)), "action": job["prompt"],
                "frame_count": job["frames"], "no_background": True}
        resp = call(key, "POST", "/animate-with-text-v3", body)
        job_resp = wait_job(key, resp["background_job_id"])
        frames = [to_img(im) for im in job_resp["last_response"]["images"]]
        fw, fh = frames[0].size
        strip = Image.new("RGBA", (fw * len(frames), fh), (0, 0, 0, 0))
        for i, fr in enumerate(frames):
            strip.alpha_composite(fr, (i * fw, 0))
            fr.save(out_path(job)[:-4] + f"_{i}.png")
        strip.save(out_path(job))
        return used(job_resp) or used(resp), f"{len(frames)} frames of {fw}x{fh}"

    raise ValueError(kind)


def log(line):
    new = not os.path.exists(LOG)
    with open(LOG, "a") as f:
        if new:
            f.write("# AI generation log\n\n| time | file | site | result | cost | status |\n|---|---|---|---|---|---|\n")
        f.write(line + "\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--key-file")
    ap.add_argument("--group", default="must")
    ap.add_argument("--only", nargs="*", help="job names to run (default: the whole group)")
    ap.add_argument("--max-gens", type=float, default=16, help="stop before spending more than this in this run")
    ap.add_argument("--reserve", type=float, default=2, help="never go below this many trial generations")
    ap.add_argument("--list", action="store_true")
    args = ap.parse_args()

    jobs = [j for j in JOBS if j["group"] == args.group and (not args.only or j["name"] in args.only)]
    if args.list:
        for j in jobs:
            print(f"{'done ' if os.path.exists(out_path(j)) else 'todo '} {j['cat']}/{j['name']} ({j['kind']})")
        return

    key = read_key(args.key_file)
    start_left = generations_left(key)
    print(f"Trial generations left: {start_left}")
    # The balance updates a few seconds late, so budget by what the responses report (at least 1 per call).
    spent = 0.0
    for job in jobs:
        os.makedirs(os.path.join(OUT, job["cat"]), exist_ok=True)
        if os.path.exists(out_path(job)):
            print(f"skip (exists) {job['name']}")
            continue
        if job["kind"] == "animate" and not first_frame_ready(job):
            print(f"skip (no base image) {job['name']}")
            continue
        left = min(generations_left(key), start_left - spent)
        if left - 1 < args.reserve or spent + 1 > args.max_gens:
            print(f"stop: ~{left:g} generations left, {spent:g} spent this run (cap {args.max_gens:g}, reserve {args.reserve:g})")
            break
        t = datetime.now().strftime("%H:%M:%S")
        try:
            cost, result = run_job(key, job)
            cost = float(cost) if isinstance(cost, (int, float)) and cost > 0 else 1.0
            spent += cost
            print(f"ok   {job['name']}: {result}, cost {cost:g} gen (~{start_left - spent:g} left)")
            log(f"| {t} | {job['cat']}/{job['name']}.png | PixelLab {job['kind']} | {result} | {cost:g} gen | done |")
        except Exception as e:  # keep going with the next asset, but record why this one failed
            spent += 1  # assume the worst so the cap still holds
            print(f"FAIL {job['name']}: {e}")
            log(f"| {t} | {job['cat']}/{job['name']}.png | PixelLab {job['kind']} | - | ? | failed: {str(e)[:120]} |")
    time.sleep(5)
    print(f"Spent this run: ~{spent:g} generations; balance now: {generations_left(key):g}")


if __name__ == "__main__":
    main()
