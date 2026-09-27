# Cowork prompt: generate the missing game art with free credits only

Give Cowork access to these folders, then paste everything inside the box below into Cowork.

- `~/Downloads/NinjaAdventure/` (the pack, used as style references)
- `~/Downloads/NinjaAdventure_AI/` (create it empty; the results go here)
- `Tools/art_import/AI_ASSET_PROMPTS.md` from the game project (the prompts). Copy it next to the other two if
  Cowork can't reach the project folder.

Log in to PixelLab, Retro Diffusion and Ludo.ai in the browser Cowork uses before you start.

````text
You are generating pixel-art game assets for my mobile game "Ninja Village Defense" on three websites
where I'm already logged in: PixelLab (pixellab.ai), Retro Diffusion (retrodiffusion.ai) and Ludo.ai.
Use ONLY the free credits already on my accounts.

FILES
- Prompts: AI_ASSET_PROMPTS.md. Each asset has a target file name, a size and a ready-made prompt.
  Paste the prompts exactly as written.
- Style references (upload when a site has a reference / style / palette field):
  - characters: ~/Downloads/NinjaAdventure/Actor/Character/NinjaRed/SpriteSheet.png
  - bosses: ~/Downloads/NinjaAdventure/Actor/Boss/GiantRedSamurai/Idle.png
  - skill icons: ~/Downloads/NinjaAdventure/Ui/Skill Icon/Preview.png
  - items: ~/Downloads/NinjaAdventure/Items/AllPreview.png
  - palette: ~/Downloads/NinjaAdventure/Palette.png
- Output: ~/Downloads/NinjaAdventure_AI/<category>/<file name from the prompts file>.png
  Categories: bosses, enemies, heroes, pets, village, icons, store.

HARD RULES
1. Never pay. Never enter card details, start a paid trial, upgrade a plan or accept any "buy credits" dialog.
   If a step needs payment, skip that asset and log it as "needs paid credits".
2. Never create new accounts or sign up for extra trials. Use my existing logins.
3. Before every generation, check the credit cost shown by the site and my remaining balance. Don't start a
   generation you can't afford; move to the next site instead.
4. Never use a site's "Pro", "Pro Flash" or premium model: they cost 20-40 credits per image. Use the basic
   or standard model (about 1 credit each).
5. Upload only the reference files listed above. Don't upload anything else from my computer.
6. If you hit a captcha, a login page or anything unexpected, stop and ask me.

WHICH SITE DOES WHAT (in this order, most important first)

A. PixelLab (about 40 free trial generations). Use the basic text-to-pixel-art image tool and the basic
   "animate image" tool, never Pro modes. Settings when available: outline = single color black outline,
   shading = basic, detail = medium, no background / transparent ON, palette or colour image = Palette.png.
   Make the base image first, then animate THAT image with each animation line from the prompts file.
   1. Spider Queen boss: base 64x64 + idle, walk, attack
   2. Nine-Tailed Fox boss: base 64x64 + idle, walk, attack
   3. Wolf enemy: base 16x16 (use 32x32 if 16 is not allowed and log it) + idle, walk
   4. Castle: 96x96, one image
   5. Only if credits are left: hero hurt frames (animate each hero's idle sprite), Mage Ninja, Fox pet, Hawk pet

B. Retro Diffusion (50 free credits; a small icon costs about 2). Use the "Skill Icon" style for the 24x24
   icons and the "Topdown Item" style (or "Low Res") for 16x16 and 32x32 items. Turn ON remove background.
   If there is a palette input, use Palette.png. Work down this list until the credits run out:
   1. icon_weapon_chainsickle
   2. equipment icons (6)
   3. skill icons the pack doesn't cover (5)
   4. ultimate + evolution icons (7)
   5. loot crates (3)
   6. pet gear (5, skip the feather)
   7. talents (15), blessings (6), menu icons (17), achievements (15)
   8. store items (11), banners (5), main menu background, logo emblem, app icon, feature graphic

C. Ludo.ai (30 free credits). First generate ONE cheap single image and download it. If the download has a
   Ludo watermark, stop using Ludo, don't spend more, and log "Ludo free downloads are watermarked".
   Otherwise use Ludo for whatever is still missing from list B, starting at the top.

FOR EACH ASSET
1. Set the canvas to the size in the prompts file (16x16, 24x24, 32x32, 64x64...).
2. Paste the prompt exactly.
3. Check the result before downloading:
   - transparent background (except banners, backgrounds and the app icon)
   - dark 1-pixel outline, flat shading, clean pixels, no blur or anti-aliasing
   - no text or letters (except the "AD" on the remove-ads icon)
   - facing matches the prompt (right-facing, or facing the camera for bosses)
   - animation frames all the same size, with the character in the same spot
   If it fails, retry ONCE with the same prompt. If it still fails, keep the better one and note the problem.
4. Download the PNG at its ORIGINAL pixel size (not an upscaled or preview version). Save animations as one
   horizontal strip; if the site only gives separate frames, save them as <name>_0.png, <name>_1.png...
5. Save with the exact file name from the prompts file into the right category folder.
6. Add a line to ~/Downloads/NinjaAdventure_AI/GENERATION_LOG.md right away (keep it updated as you go):
   | file | site | size | credits spent | status (done / retried / failed / skipped: reason) | notes |

WHEN YOU FINISH OR RUN OUT OF FREE CREDITS
Write at the top of GENERATION_LOG.md:
- credits left on each site
- what's done, grouped by category
- what's still missing, in priority order
- any asset that came out wrong or at a different size than asked
Then stop. Don't buy anything to finish the list.
````
