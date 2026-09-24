# AI prompts for the art the Ninja Adventure pack doesn't have

Every prompt below is ready to paste. They're written to match the **Ninja Adventure** pack, so the new
art sits next to the pack's sprites without looking out of place.

## How to get matching results

1. **Site:** [PixelLab](https://www.pixellab.ai) for characters, bosses and animations; it can animate
   an image you give it and keep the style. [Retro Diffusion](https://www.retrodiffusion.ai) is cheaper
   per image for batches of icons and banners. Check that your plan allows commercial use before you ship.
2. **Size:** generate at the size in each heading (16×16, 24×24, 64×64…). **Don't upscale**: the import
   script upscales everything 8× with sharp pixels.
3. **Style reference:** if the site accepts a reference or style image, upload one from the pack:
   - characters: `~/Downloads/NinjaAdventure/Actor/Character/NinjaRed/SpriteSheet.png`
   - bosses: `~/Downloads/NinjaAdventure/Actor/Boss/GiantRedSamurai/Idle.png`
   - icons: `~/Downloads/NinjaAdventure/Ui/Skill Icon/Preview.png`
   - items: `~/Downloads/NinjaAdventure/Items/AllPreview.png`
   - colours: `~/Downloads/NinjaAdventure/Palette.png` (the pack's 53 colours; outline is `#141b1b`)
4. **Animations:** make the first frame, then animate *that image* (PixelLab: animate with text). Save
   each animation as **one horizontal strip**, all frames the same size.
5. **Save** to `~/Downloads/NinjaAdventure_AI/` using the file name given for each prompt. The import
   script will pick them up, cut the frames and upscale them.

---

## 1. Must have: gameplay art the pack is missing

### Spider Queen boss (64×64 frames) → `boss_spiderqueen_*.png`
Base image:
```
Spider Queen boss for a top-down ninja game: giant demon spider with a small golden crown, glossy black-purple body with a red hourglass marking, eight long jointed legs, six glowing red eyes, facing the camera in a front 3/4 top-down view. 64x64 pixel art, Ninja Adventure asset pack style (Pixel-boy), chunky shapes, 1px dark outline #141b1b, flat 2-3 tone shading, no anti-aliasing, no dithering, transparent background
```
Animations (use the base image as the reference):
```
idle: 6 frames, legs shift slightly and eyes blink, body bobs 1 pixel, same size and position every frame
walk: 6 frames, skittering forward toward the camera, legs alternate in pairs
attack: 4 frames, rears up on the back legs then strikes down to the right with its fangs, small green venom splash on the last frame
```

### Nine-Tailed Fox boss (64×64 frames) → `boss_ninetailedfox_*.png`
Base image:
```
Nine-Tailed Fox (kitsune) boss for a top-down ninja game: large white and gold fox demon crouching, nine tails fanned out behind it with blue spirit-flame tips, red markings around the eyes, facing the camera in a front 3/4 top-down view. 64x64 pixel art, Ninja Adventure asset pack style (Pixel-boy), 1px dark outline #141b1b, flat 2-3 tone shading, no anti-aliasing, no dithering, transparent background
```
Animations:
```
idle: 6 frames, tails sway in a wave and the blue flames flicker, same size and position every frame
walk: 6 frames, stalking forward toward the camera, head low, tails trailing
attack: 4 frames, lunges to the right with a claw swipe leaving a blue fire trail
```

### Wolf enemy (16×16 frames) → `enemy_wolf_*.png`
Base image:
```
grey wolf demon enemy, lean body, pointed ears, glowing red eyes, bushy tail, side view facing right. 16x16 pixel art, Ninja Adventure asset pack style (Pixel-boy), 1px dark outline #141b1b, flat 2-3 tone shading, no anti-aliasing, transparent background
```
Animations:
```
idle: 2 frames, breathing, chest rises 1 pixel
walk: 4 frames, trotting to the right, legs alternate, tail bounces
```

### Castle building (96×96) → `village_castle.png`
```
Japanese castle keep for a hidden ninja village: sloped white stone base, two tiers of dark red tiled roofs with gold ridge ornaments, wooden gate, small white banners, top-down 3/4 RPG view like the houses in the Ninja Adventure tileset. 96x96 pixel art, 1px dark outline #141b1b, flat 2-3 tone shading, no anti-aliasing, transparent background
```

### Chain sickle weapon icon (16×16) → `icon_weapon_chainsickle.png`
```
kusarigama weapon icon: small curved sickle with a wooden handle attached to a short iron chain ending in a round weight, drawn diagonally. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
```

---

## 2. Nice to have: closer matches than the pack's stand-ins

### Hero hurt frames (16×16, 2 frames each) → `hero_<name>_hurt_0..1.png`
Upload the hero's `SeparateAnim/Idle.png` from `~/Downloads/NinjaAdventure/Actor/Character/<Name>/` as the
reference (the right-facing one is the 4th sprite). Run once per hero: NinjaDark (assassin), SamuraiRed
(samurai), Monk2 (monk), NinjaWater (mage ninja).
```
Using the uploaded sprite as the exact reference, draw 2 hurt frames of the same character facing right: frame 1 recoils backward (leaning left, eyes squeezed shut, small white impact flash on its chest), frame 2 half-recovered. Keep the same size, colours, outline and proportions. 16x16 each, horizontal strip, transparent background
```
(Skipping this is fine: the game can flash the sprite white when hit instead.)

### Blue Mage Ninja hero (16×16) → `hero_mageninja_*.png`
```
blue ninja mage hero, round body with big head, pointed blue wizard hat over a ninja mask, blue scarf, holding a small glowing scroll, side view facing right. 16x16 pixel art, Ninja Adventure asset pack style (Pixel-boy), 1px dark outline #141b1b, flat 2-3 tone shading, no anti-aliasing, transparent background
```
Animations: `idle: 2 frames breathing` · `walk: 4 frames walking right` · `attack: 2 frames thrusting the scroll forward with a blue spark`

### Fox pet (16×16) → `pet_fox_*.png`
```
small orange fox pet with a white chest and white tail tip, big fluffy tail, side view facing right. 16x16 pixel art, Ninja Adventure animal style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background
```
Animations: `idle: 2 frames, tail wags` · `walk: 2 frames trotting right`

### Hawk pet (16×16) → `pet_hawk_*.png`
```
small brown hawk pet flying, white chest, yellow beak and feet, side view facing right, wings open. 16x16 pixel art, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background
```
Animations: `idle: 2 frames hovering` · `walk: 4 frames flapping, flying right`

---

## 3. Icons

### Equipment (16×16 item icons) → `icon_equip_<id>.png`
One prompt per line:
```
plain iron ring with a small grey stone. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
paper ofuda talisman with a blood-red painted sigil and torn edges. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
dark purple hooded ninja cloak, folded. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
single large shiny green and gold dragon scale. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
blue ninja headband with a metal plate engraved with a shuriken emblem. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
green jade magatama charm on a red cord. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
```
Order: iron_ring, blood_talisman, shadow_cloak, dragon_scale, ninja_headband, jade_charm.

### Pet gear (16×16 item icons) → `icon_petgear_<id>.png`
```
thin gold anklet with tiny beads. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
hawk feather with brown and white bands. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
small golden bell with a red ribbon. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
glowing orange ember stone with tiny flames. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
red leather pet collar with a gold buckle and round tag. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
```
Order: anklet, feather (the pack already has one in `Items/Resource/feather.png`), bell, ember, collar.

### Loot crates (16×16 item icons) → `icon_crate_<rarity>.png`
```
plain wooden crate tied with rope. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
blue painted wooden crate with silver metal bands. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
purple lacquered chest with gold corners and a soft glow. 16x16 pixel art item icon, Ninja Adventure style, 1px dark outline #141b1b, flat shading, no anti-aliasing, transparent background, centered
```
Order: common, rare, epic.

### Skill icons the pack doesn't cover (24×24) → `icon_skill_<id>.png`
The pack's `Ui/Skill Icon` set already covers Wind, Lightning Strike, Explosive Bomb, Fire Blade, Auto Heal,
Shield, Smoke Bomb, Teleport, Lucky Drop, Gold Bonus, Movement Speed and Giant Shuriken. These five are missing:
```
three kunai fanning out from one point, on an orange rounded-square tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing
horseshoe magnet pulling in small green gems, on a green rounded-square tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing
kunai dripping green poison, on a purple rounded-square tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing
ninja silhouette dodging sideways leaving a faded afterimage, on a blue rounded-square tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing
katana with fast speed lines, on an orange rounded-square tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing
```
Order: triple_throw, xp_magnet, poison_kunai, dodge_chance, attack_speed.

### Ultimates and evolutions (24×24) → `icon_ultimate_<id>.png`, `icon_evolution_<id>.png`
```
ring of dark purple ninja shadow clones, on a purple tile with a thin gold border. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
shuriken raining down from dark storm clouds, on a blue tile with a thin gold border. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
golden eastern dragon coiling along a katana slash, on a red tile with a thin gold border. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
kunai crackling with yellow lightning, on a split yellow and orange tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
spinning tornado made of fire, on a split red and green tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
ninja fading into a puff of grey smoke, half transparent, on a split grey and purple tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
three shadow clones raising katanas together, on a split purple and blue tile. 24x24 pixel art skill icon, Ninja Adventure skill icon style, white sparkles, 1px dark outline #141b1b, no anti-aliasing
```
Order: ultimate shadow_clone_army, heavenly_storm, dragon_slash; evolution thunder_kunai, firestorm,
invisible_assassin, shadow_army.

### Talents (24×24) → `icon_talent_<id>.png`
Suffix for every line: `24x24 pixel art skill icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing`
```
narrowed eye with a red target mark, on a red tile. <suffix>
ninja torso wearing iron plates, on a grey tile. <suffix>
U-shaped magnet pulling in gold coins, on a green tile. <suffix>
ninja leaping sideways with motion lines, on a blue tile. <suffix>
ninja sitting cross-legged in a glowing aura, on a purple tile. <suffix>
floating paper charm inside a blue spirit shield, on a blue tile. <suffix>
golden star with a four-leaf clover, on a green tile. <suffix>
hand holding three shuriken with speed lines, on an orange tile. <suffix>
heart with a lightning bolt inside, on a yellow tile. <suffix>
coin purse next to an abacus, on a yellow tile. <suffix>
eye with a bright sparkle, on a blue tile. <suffix>
grey stone fist, on a grey tile. <suffix>
open scroll with glowing writing, on a purple tile. <suffix>
straw sandal with small wind wings, on a green tile. <suffix>
gleaming katana edge next to a whetstone, on an orange tile. <suffix>
```
Order: lethal_focus, iron_body, magnetism, evasion, meditation, spirit_ward, lucky_star, quick_hands,
storm_heart, merchant, keen_eye, stone_skin, scholar, swift_feet, sharpened_blades.

### Shrine blessings (24×24) → `icon_blessing_<id>.png`
Suffix: `24x24 pixel art skill icon, Ninja Adventure skill icon style, soft golden glow, white sparkles, 1px dark outline #141b1b, no anti-aliasing`
```
iron shield with a small red torii gate emblem, on a grey tile. <suffix>
stack of gold coins with a green rice sprout, on a yellow tile. <suffix>
glowing red heart with two green leaves, on a red tile. <suffix>
maneki-neko lucky cat raising its paw, on a yellow tile. <suffix>
old scroll with a small owl perched on it, on a purple tile. <suffix>
arrow hitting the centre of a target, on a blue tile. <suffix>
```
Order: iron_skin, prosperity, vitality, fortune, wisdom, precision.

### Main menu icons (24×24) → `menu_<id>.png`
Suffix: `24x24 pixel art menu icon, Ninja Adventure skill icon style: coloured rounded-square background with small white sparkles, cream symbol with 1px dark outline #141b1b, no anti-aliasing`
```
crossed katana and shuriken, on a red tile. <suffix>
small Japanese house with a red torii gate, on a green tile. <suffix>
ninja head with a red headband, on an orange tile. <suffix>
paw print with a small fox face, on a green tile. <suffix>
three glowing orbs joined by lines like a skill tree, on a purple tile. <suffix>
ninja chest armour, on a grey tile. <suffix>
wrapped gift box with a red ribbon, on a red tile. <suffix>
scroll with a red seal and a check mark, on an orange tile. <suffix>
gold trophy cup, on a yellow tile. <suffix>
open book showing monster silhouettes, on a blue tile. <suffix>
golden ticket decorated with cherry blossoms, on a pink tile. <suffix>
paper lantern with small festival flags, on a red tile. <suffix>
market stall awning with coins under it, on a yellow tile. <suffix>
podium with 1-2-3 steps, on a blue tile. <suffix>
cloud with an upward arrow, on a blue tile. <suffix>
ninja portrait in a round frame, on an orange tile. <suffix>
iron gear cog, on a grey tile. <suffix>
```
Order: play, village, heroes, pets, talents, gear, daily, quests, achievements, collection, battle_pass,
events, shop, leaderboard, account, profile, settings.

### Achievement badges (24×24) → `icon_achievement_<id>.png`
Suffix: `inside a round bronze medal with a red ribbon, 24x24 pixel art badge, Ninja Adventure style, 1px dark outline #141b1b, no anti-aliasing, transparent background`
```
hourglass <suffix>
hammer on an anvil <suffix>
two crossed katanas <suffix>
open treasure chest <suffix>
black martial arts belt <suffix>
laurel wreath around a crown <suffix>
book with a gem on its cover <suffix>
cracked demon skull <suffix>
wave crashing against a shield <suffix>
oni mask split by a sword <suffix>
calendar page with check marks <suffix>
rolled diploma scroll <suffix>
pagoda blueprint <suffix>
heart with a ribbon <suffix>
paw print with a heart <suffix>
```
Order: survivor, smith, veteran, treasure, sensei, champion, collector, boss_hunter, wave_breaker,
demon_slayer, diligent, scholar, architect, loyal, beast_friend.

---

## 4. Store and marketing

The UI shows gems in light blue, so the gem art below is blue.

### Store items (32×32) → `store_<id>.png`
Suffix: `32x32 pixel art shop item, Ninja Adventure style, 1px dark outline #141b1b, flat 2-3 tone shading, no anti-aliasing, transparent background, centered`
```
small pile of three light-blue gems. <suffix>
leather pouch overflowing with light-blue gems. <suffix>
open treasure chest full of glowing light-blue gems. <suffix>
red and gold gift box with a ninja headband and gold coins on top. <suffix>
small TV screen showing the letters AD crossed out by a red slash. <suffix>
golden scroll ticket with cherry blossoms and a small crown. <suffix>
pink coin pouch decorated with cherry blossoms. <suffix>
bundle wrapped in pink furoshiki cloth with falling sakura petals. <suffix>
red oni mask with a katana and a red potion. <suffix>
wooden crate with maple leaves and a small training dummy. <suffix>
three glowing paper lanterns on a string. <suffix>
```
Order: gems_100, gems_550, gems_1200, starter_pack, remove_ads, battle_pass_premium, offer_blossom_coins,
offer_sakura_bundle, offer_oni_kit, offer_autumn_training, offer_lantern_pack.

### Event and season banners (128×48) → `banner_<id>.png`
Suffix: `wide 128x48 pixel art banner, Ninja Adventure style, 1px dark outlines, flat shading, no anti-aliasing, no text`
```
snowy ninja village at night lit by glowing paper lanterns, snow falling. <suffix>
huge red full moon over a pagoda, oni demon silhouettes on the rooftops. <suffix>
ninja village street under pink cherry blossom trees, petals falling. <suffix>
katana blade among swirling cherry blossom petals, pink and gold. <suffix>
armoured oni samurai silhouette in front of a red moon. <suffix>
```
Order: event_winter_lantern, event_oni_moon, event_cherry_blossom, season_1 (Blossom Blades), season_2 (Oni Moon).

### Main menu background (135×240, portrait) → `bg_mainmenu.png`
```
hidden ninja village in a misty mountain valley at dusk: pagoda, red torii gate, glowing lanterns, cherry blossom trees, calm and dark enough for white text on top, keep the middle area simple and empty for the title and buttons, portrait 135x240 pixel art, Ninja Adventure style, no text
```

### Title emblem (64×64) → `logo_emblem.png`
AI tools garble text, so keep "NINJA VILLAGE DEFENSE" as in-game text and only generate the emblem:
```
emblem of two crossed kunai behind a large shuriken with a small red torii gate in the centre, 64x64 pixel art, Ninja Adventure style, 1px dark outline #141b1b, no anti-aliasing, transparent background, no text
```

### App icon (64×64) → `app_icon.png`
```
close-up of a ninja face with a red mask and determined eyes, a shuriken beside it, bold shapes readable at small size, solid dark blue background, 64x64 pixel art app icon, Ninja Adventure style, no text
```

### Play Store feature graphic (256×125) → `store_feature_graphic.png`
The script scales this 4× to the required 1024×500 instead of 8×.
```
ninja hero defending a village gate against a horde of demons and a giant oni, cherry trees and lanterns, dramatic, wide 256x125 pixel art, Ninja Adventure style, 1px dark outlines, no text
```

---

## Art you don't need AI for

| Art | Where it comes from |
|---|---|
| 7 hero skins (Crimson/Sakura Assassin, Oni/Golden Samurai, Jade Monk, Frost Mage, Shadow Beastmaster) | Colour swaps of the hero sprites in the import script, using the pack's palette |
| Purple Assassin | Colour swap of NinjaDark to the pack's purples (`#543c52 #8f3e56 #a5608b #d3a2c0`) |
| Music and sound effects | The pack's `Audio/` folder (music, jingles and sounds) |
| Coins, XP gems, hearts, chests | The pack's `Items/Treasure`, `Items/Resource` and `Ui/Receptacle` |
